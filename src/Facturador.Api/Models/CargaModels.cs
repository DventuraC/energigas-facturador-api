namespace Facturador.Api.Models;

public record DocumentoCarga(
    DateTime Fecha, DateTime? FechaVencimiento, int TipoDocumento, string Serie,
    int Numero, int? MotivoNota, string? Ruc, int? Moneda, string? Observacion,
    decimal? SubTotal, decimal? Impuesto, decimal? Total, string? SerieReferencia,
    int? NumeroReferencia, long IdItem, decimal? Cantidad, decimal? Precio,
    decimal? SubTotalItem, int? Ccosto);

public record DocumentoCargaEstado(
    int Orden, DateTime Fecha, DateTime? FechaVencimiento, int TipoDocumento, string Serie, int Numero,
    int? MotivoNota, string? Ruc, int? Moneda, string? Observacion, decimal? Subtotal, decimal? Impuesto,
    decimal? Total, string Estado, string? Detalle);
public record ValidacionCargaResponse(Guid CargaId, int RegistrosValidos, IReadOnlyList<string> Errores, IReadOnlyList<DocumentoCargaEstado> Documentos);
public record ResultadoProceso(int Insertados, int Duplicados, bool ProcedimientoEjecutado, string Mensaje, IReadOnlyList<DocumentoCargaEstado> Documentos);
