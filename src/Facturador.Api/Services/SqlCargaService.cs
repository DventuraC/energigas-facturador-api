using System.Data;
using Facturador.Api.Models;
using Microsoft.Data.SqlClient;

namespace Facturador.Api.Services;

public sealed class SqlCargaService(IConfiguration configuration)
{
    public async Task<ResultadoProceso> ProcesarAsync(IReadOnlyList<DocumentoCarga> documentos, CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("SqlServer");
        if (string.IsNullOrWhiteSpace(connectionString))
            return new(0, 0, 0, false, "Configura ConnectionStrings:SqlServer mediante secretos o variable de entorno.", []);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        var insertados = 0;
        var reemplazados = 0;
        var duplicados = 0;
        var resultados = new List<DocumentoCargaEstado>();
        var clavesArchivo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var (documento, index) in documentos.Select((documento, index) => (documento, index)))
            {
                var clave = $"{documento.TipoDocumento}|{documento.Serie}|{documento.Numero}";
                if (!clavesArchivo.Add(clave))
                {
                    duplicados++;
                    resultados.Add(CargaService.CrearEstado(index + 1, documento, "Omitido", "Duplicado"));
                    continue;
                }
                var existente = await ObtenerExistenteAsync(connection, transaction, documento, cancellationToken);
                if (existente is not null && existente.Procesado)
                {
                    duplicados++;
                    resultados.Add(CargaService.CrearEstado(index + 1, documento, "Omitido", "Duplicado"));
                    continue;
                }
                if (existente is not null) await EliminarDocumentoPendienteAsync(connection, transaction, existente.IdCabecera, cancellationToken);
                var cabeceraId = await InsertarCabeceraAsync(connection, transaction, documento, cancellationToken);
                await InsertarDetalleAsync(connection, transaction, cabeceraId, documento, cancellationToken);
                if (existente is null) insertados++; else reemplazados++;
                resultados.Add(CargaService.CrearEstado(index + 1, documento, existente is null ? "Insertado" : "Reemplazado", null));
            }
            await transaction.CommitAsync(cancellationToken);
            return new(insertados, reemplazados, duplicados, false, "Carga auxiliar completada. El procedimiento se ejecuta en una acción independiente.", resultados);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task EjecutarProcedimientoAsync(CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("SqlServer") ?? throw new InvalidOperationException("Falta la cadena de conexión.");
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(configuration["StoredProcedure:GenerarDocumentos"], connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 120
        };
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<CabeceraTemporal?> ObtenerExistenteAsync(SqlConnection connection, SqlTransaction transaction, DocumentoCarga d, CancellationToken ct)
    {
        const string sql = "SELECT TOP 1 Id_Cabecera, ISNULL(Procesado, 0) FROM DocCabecera_Temp WHERE TipoDocumento=@tipo AND Serie=@serie AND Numero=@numero";
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@tipo", d.TipoDocumento);
        command.Parameters.AddWithValue("@serie", d.Serie);
        command.Parameters.AddWithValue("@numero", d.Numero);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? new(reader.GetInt32(0), reader.GetInt32(1) != 0) : null;
    }

    private static async Task EliminarDocumentoPendienteAsync(SqlConnection connection, SqlTransaction transaction, int idCabecera, CancellationToken ct)
    {
        const string sql = "DELETE FROM DocDetalle_Temp WHERE Id_Cabecera=@id; DELETE FROM DocCabecera_Temp WHERE Id_Cabecera=@id;";
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@id", idCabecera);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task<int> InsertarCabeceraAsync(SqlConnection connection, SqlTransaction transaction, DocumentoCarga d, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO DocCabecera_Temp (Fecha, FechaVencimiento, TipoDocumento, Serie, Numero, MotivoNota, Ruc, Moneda, Observacion, SubTotal, Impuesto, Total, Serie_Ref, Numero_Ref)
            OUTPUT INSERTED.Id_Cabecera VALUES (@fecha,@vencimiento,@tipo,@serie,@numero,@motivo,@ruc,@moneda,@observacion,@subtotal,@impuesto,@total,@serieRef,@numeroRef)
            """;
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@fecha", d.Fecha.Date); command.Parameters.AddWithValue("@vencimiento", (object?)d.FechaVencimiento?.Date ?? DBNull.Value);
        command.Parameters.AddWithValue("@tipo", d.TipoDocumento); command.Parameters.AddWithValue("@serie", d.Serie); command.Parameters.AddWithValue("@numero", d.Numero);
        command.Parameters.AddWithValue("@motivo", (object?)d.MotivoNota ?? DBNull.Value); command.Parameters.AddWithValue("@ruc", (object?)d.Ruc ?? DBNull.Value);
        command.Parameters.AddWithValue("@moneda", (object?)d.Moneda ?? DBNull.Value); command.Parameters.AddWithValue("@observacion", (object?)d.Observacion ?? DBNull.Value);
        AgregarDecimal(command, "@subtotal", d.SubTotal, 2); AgregarDecimal(command, "@impuesto", d.Impuesto, 2);
        AgregarDecimal(command, "@total", d.Total, 2); command.Parameters.AddWithValue("@serieRef", (object?)d.SerieReferencia ?? DBNull.Value);
        command.Parameters.AddWithValue("@numeroRef", (object?)d.NumeroReferencia ?? DBNull.Value);
        return Convert.ToInt32(await command.ExecuteScalarAsync(ct));
    }

    private static async Task InsertarDetalleAsync(SqlConnection connection, SqlTransaction transaction, int cabeceraId, DocumentoCarga d, CancellationToken ct)
    {
        const string sql = "INSERT INTO DocDetalle_Temp (Id_Cabecera,Id_Item,Cantidad,Precio,SubTotal_Item,Ccosto) VALUES (@cabecera,@item,@cantidad,@precio,@subtotal,@ccosto)";
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@cabecera", cabeceraId); command.Parameters.AddWithValue("@item", d.IdItem);
        AgregarDecimal(command, "@cantidad", d.Cantidad, 2); AgregarDecimal(command, "@precio", d.Precio, 4);
        AgregarDecimal(command, "@subtotal", d.SubTotalItem, 2); command.Parameters.AddWithValue("@ccosto", (object?)d.Ccosto ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static void AgregarDecimal(SqlCommand command, string nombre, decimal? valor, byte escala)
    {
        var parameter = command.Parameters.Add(nombre, SqlDbType.Decimal);
        parameter.Precision = 18;
        parameter.Scale = escala;
        parameter.Value = valor is null ? DBNull.Value : Math.Round(valor.Value, escala, MidpointRounding.AwayFromZero);
    }

    private sealed record CabeceraTemporal(int IdCabecera, bool Procesado);
}
