namespace Facturador.Api.Models;

public sealed class FiltroDocumentosVenta
{
    public DateTime? FechaInicio { get; init; }
    public DateTime? FechaFin { get; init; }
    public decimal[]? TiposDocumento { get; init; }
    public string? Serie { get; init; }
    public decimal? Numero { get; init; }
    public string? ClienteRuc { get; init; }
    public int Pagina { get; init; } = 1;
    public int TamanoPagina { get; init; } = 25;
}

public record DocumentoVentaItem(
    string Origen, decimal Op, DateTime Fecha, decimal IdDocumento, string Documento,
    string? Serie, decimal? Numero, decimal Subtotal, decimal Igv, decimal Percepcion,
    decimal Total, string Estado, string? IdAgenda, string? AgendaNombre,
    DateTime? FechaReferencia, string? DocumentoReferencia, string? SerieReferencia, decimal? NumeroReferencia,
    string? Observaciones)
{
    public IReadOnlyList<DetalleDocumentoVentaItem> Detalles { get; init; } = [];
}
public record DocumentoTipoDisponible(decimal IdDocumento, string Nombre);
public record PaginaDocumentosVenta(IReadOnlyList<DocumentoVentaItem> Items, int Total, int Pagina, int TamanoPagina);
public record DetalleDocumentoVentaItem(string? IdItem, decimal? Cantidad, decimal? Precio, decimal? Importe, decimal? IdCentroCosto, string? Producto, string? CentroCosto);
