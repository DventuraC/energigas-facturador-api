using System.Collections.Concurrent;
using ClosedXML.Excel;
using Facturador.Api.Models;

namespace Facturador.Api.Services;

public sealed class CargaService
{
    private readonly ConcurrentDictionary<Guid, IReadOnlyList<DocumentoCarga>> _cargas = new();
    public const string NombreHojaRequerida = "cargaMasiva";
    private static readonly string[] Headers = ["fecha", "fechav", "tipodocumento", "serie", "numero", "motivonota", "ruc", "moneda", "observacion", "subtotal", "impuesto", "total", "serie", "numero", "iditem", "cantidad", "precio", "subtotal", "ccosto"];

    public ValidacionCargaResponse Validar(Stream archivo)
    {
        using var libro = new XLWorkbook(archivo);
        var hoja = libro.Worksheets.FirstOrDefault(x => x.Name.Equals(NombreHojaRequerida, StringComparison.OrdinalIgnoreCase));
        if (hoja is null) return new(Guid.Empty, 0, [$"No se encontró la hoja {NombreHojaRequerida}."], []);

        var errores = new List<string>();
        for (var column = 1; column <= Headers.Length; column++)
        {
            var actual = Normalizar(hoja.Cell(2, column).GetString());
            if (actual != Headers[column - 1]) errores.Add($"Columna {column}: se esperaba {Headers[column - 1]}.");
        }
        if (errores.Count > 0) return new(Guid.Empty, 0, errores, []);

        var documentos = new List<DocumentoCarga>();
        foreach (var row in hoja.RowsUsed().Where(x => x.RowNumber() >= 3))
        {
            if (row.Cell(1).IsEmpty()) continue;
            try
            {
                documentos.Add(new DocumentoCarga(
                    row.Cell(1).GetDateTime(), FechaOpcional(row.Cell(2)), row.Cell(3).GetValue<int>(), row.Cell(4).GetString(),
                    row.Cell(5).GetValue<int>(), EnteroOpcional(row.Cell(6)), TextoOpcional(row.Cell(7)), EnteroOpcional(row.Cell(8)), TextoOpcional(row.Cell(9)),
                    DecimalOpcional(row.Cell(10)), DecimalOpcional(row.Cell(11)), DecimalOpcional(row.Cell(12)), TextoOpcional(row.Cell(13)),
                    EnteroOpcional(row.Cell(14)), row.Cell(15).GetValue<long>(), DecimalOpcional(row.Cell(16)), DecimalOpcional(row.Cell(17)),
                    DecimalOpcional(row.Cell(18)), EnteroOpcional(row.Cell(19))));
            }
            catch (Exception)
            {
                errores.Add($"Fila {row.RowNumber()}: contiene un valor obligatorio inválido.");
            }
        }
        if (documentos.Count == 0 && errores.Count == 0) errores.Add("El archivo no contiene documentos.");
        var id = Guid.NewGuid();
        if (errores.Count == 0) _cargas[id] = documentos;
        var vistaPrevia = documentos.Select((documento, index) => new DocumentoCargaEstado(index + 1, documento.Fecha, documento.TipoDocumento, documento.Serie, documento.Numero, "Pendiente", "Pendiente de cargar auxiliares.")).ToList();
        return new(errores.Count == 0 ? id : Guid.Empty, documentos.Count, errores, vistaPrevia);
    }

    public bool TryGet(Guid id, out IReadOnlyList<DocumentoCarga>? documentos) => _cargas.TryGetValue(id, out documentos);
    public bool Eliminar(Guid id) => _cargas.TryRemove(id, out _);
    private static string Normalizar(string value) => new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
    private static string? TextoOpcional(IXLCell cell) => cell.IsEmpty() ? null : cell.GetString();
    private static int? EnteroOpcional(IXLCell cell) => cell.IsEmpty() ? null : cell.GetValue<int>();
    private static decimal? DecimalOpcional(IXLCell cell) => cell.IsEmpty() ? null : cell.GetValue<decimal>();
    private static DateTime? FechaOpcional(IXLCell cell) => cell.IsEmpty() ? null : cell.GetDateTime();
}
