namespace Facturador.Api.Models;

public record DocumentoCarga(
    DateTime Fecha, DateTime? FechaVencimiento, int TipoDocumento, string Serie,
    int Numero, int? MotivoNota, string? Ruc, int? Moneda, string? Observacion,
    decimal? SubTotal, decimal? Impuesto, decimal? Total, string? SerieReferencia,
    int? NumeroReferencia, long IdItem, decimal? Cantidad, decimal? Precio,
    decimal? SubTotalItem, int? Ccosto);

public record ValidacionCargaResponse(Guid CargaId, int RegistrosValidos, IReadOnlyList<string> Errores);
public record ResultadoProceso(int Insertados, int Duplicados, bool ProcedimientoEjecutado, string Mensaje);
