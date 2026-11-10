using Facturador.Api.Data;
using Facturador.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Facturador.Api.Services;

public sealed class ConsultaVentasService(IConfiguration configuration)
{
    private FacturadorDbContext CreateContext()
    {
        var connectionString = configuration.GetConnectionString("SqlServer");
        if (string.IsNullOrWhiteSpace(connectionString)) throw new InvalidOperationException("Configura ConnectionStrings:SqlServer para consultar ventas.");
        var options = new DbContextOptionsBuilder<FacturadorDbContext>().UseSqlServer(connectionString).Options;
        return new FacturadorDbContext(options);
    }

    public async Task<IReadOnlyList<DocumentoTipoDisponible>> TiposDisponiblesAsync(DateTime? fechaInicio, DateTime? fechaFin, CancellationToken ct)
    {
        await using var db = CreateContext();
        var ventas = db.DocVentas.AsNoTracking().AsQueryable();
        var creditos = db.DocDevVentas.AsNoTracking().AsQueryable();
        if (fechaInicio.HasValue) { ventas = ventas.Where(x => x.Fecha >= fechaInicio); creditos = creditos.Where(x => x.Fecha >= fechaInicio); }
        if (fechaFin.HasValue) { var finExclusivo = fechaFin.Value.Date.AddDays(1); ventas = ventas.Where(x => x.Fecha < finExclusivo); creditos = creditos.Where(x => x.Fecha < finExclusivo); }

        var tipos = (from v in ventas join d in db.Documentos.AsNoTracking() on v.IdDocumento equals d.IdDocumento select new { d.IdDocumento, d.Nombre })
            .Concat(from nc in creditos join d in db.Documentos.AsNoTracking() on nc.IdDocumento equals d.IdDocumento select new { d.IdDocumento, d.Nombre });
        return await tipos.Distinct().OrderBy(x => x.Nombre)
            .Select(x => new DocumentoTipoDisponible(x.IdDocumento, x.Nombre)).ToListAsync(ct);
    }

    public async Task<PaginaDocumentosVenta> BuscarAsync(FiltroDocumentosVenta filtro, CancellationToken ct)
    {
        await using var db = CreateContext();
        var ventas = db.DocVentas.AsNoTracking().AsQueryable();
        var creditos = db.DocDevVentas.AsNoTracking().AsQueryable();
        if (filtro.FechaInicio.HasValue) { ventas = ventas.Where(x => x.Fecha >= filtro.FechaInicio); creditos = creditos.Where(x => x.Fecha >= filtro.FechaInicio); }
        if (filtro.FechaFin.HasValue) { var finExclusivo = filtro.FechaFin.Value.Date.AddDays(1); ventas = ventas.Where(x => x.Fecha < finExclusivo); creditos = creditos.Where(x => x.Fecha < finExclusivo); }
        if (!string.IsNullOrWhiteSpace(filtro.Serie)) { ventas = ventas.Where(x => x.Serie != null && x.Serie.Contains(filtro.Serie)); creditos = creditos.Where(x => x.Serie != null && x.Serie.Contains(filtro.Serie)); }
        if (filtro.Numero.HasValue) { ventas = ventas.Where(x => x.Numero == filtro.Numero); creditos = creditos.Where(x => x.Numero == filtro.Numero); }
        if (filtro.TiposDocumento?.Length > 0) { ventas = ventas.Where(x => filtro.TiposDocumento.Contains(x.IdDocumento)); creditos = creditos.Where(x => filtro.TiposDocumento.Contains(x.IdDocumento)); }
        if (!string.IsNullOrWhiteSpace(filtro.ClienteRuc))
        {
            var termino = filtro.ClienteRuc.Trim();
            ventas = ventas.Where(x => x.IdAgenda != null && (x.IdAgenda.Contains(termino) || db.Agendas.Any(a => a.IdAgenda == x.IdAgenda && a.AgendaNombre != null && a.AgendaNombre.Contains(termino))));
            creditos = creditos.Where(x => x.IdAgenda != null && (x.IdAgenda.Contains(termino) || db.Agendas.Any(a => a.IdAgenda == x.IdAgenda && a.AgendaNombre != null && a.AgendaNombre.Contains(termino))));
        }

        var documentos =
            (from v in ventas
             join d in db.Documentos.AsNoTracking() on v.IdDocumento equals d.IdDocumento
             join a0 in db.Agendas.AsNoTracking() on v.IdAgenda equals a0.IdAgenda into agendas
             from a in agendas.DefaultIfEmpty()
             join r0 in db.DocVentas.AsNoTracking() on v.NdOp equals r0.Op into referencias
             from r in referencias.DefaultIfEmpty()
             join rd0 in db.Documentos.AsNoTracking() on r.IdDocumento equals rd0.IdDocumento into documentosReferencia
             from rd in documentosReferencia.DefaultIfEmpty()
             select new { Origen = "Venta", v.Op, v.Fecha, v.IdDocumento, Documento = d.Nombre, v.Serie, v.Numero, v.SubTotal, Igv = v.Impuestos, Percepcion = v.Percepcion ?? 0m, v.Total, v.Estado, v.IdAgenda, AgendaNombre = a.AgendaNombre, FechaReferencia = (DateTime?)r.Fecha, DocumentoReferencia = rd.Nombre, SerieReferencia = r.Serie, NumeroReferencia = r.Numero, v.Observaciones, d.DocSunat })
            .Concat(
             from nc in creditos
             join d in db.Documentos.AsNoTracking() on nc.IdDocumento equals d.IdDocumento
             join a0 in db.Agendas.AsNoTracking() on nc.IdAgenda equals a0.IdAgenda into agendas
             from a in agendas.DefaultIfEmpty()
             join r0 in db.DocVentas.AsNoTracking() on nc.OpDocVta equals r0.Op into referencias
             from r in referencias.DefaultIfEmpty()
             join rd0 in db.Documentos.AsNoTracking() on r.IdDocumento equals rd0.IdDocumento into documentosReferencia
             from rd in documentosReferencia.DefaultIfEmpty()
             select new { Origen = "Nota de crédito", nc.Op, nc.Fecha, nc.IdDocumento, Documento = d.Nombre, nc.Serie, nc.Numero, nc.SubTotal, Igv = nc.Impuestos, Percepcion = 0m, nc.Total, nc.Estado, nc.IdAgenda, AgendaNombre = a.AgendaNombre, FechaReferencia = (DateTime?)r.Fecha, DocumentoReferencia = rd.Nombre, SerieReferencia = r.Serie, NumeroReferencia = r.Numero, nc.Observaciones, d.DocSunat });
        var total = await documentos.CountAsync(ct);
        var pagina = Math.Max(1, filtro.Pagina);
        var tamano = Math.Clamp(filtro.TamanoPagina, 1, 100);
        var items = await documentos.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Op).Skip((pagina - 1) * tamano).Take(tamano)
            .Select(x => new DocumentoVentaItem(x.Origen, x.Op, x.Fecha, x.IdDocumento, x.Documento, x.Serie, x.Numero, x.SubTotal, x.Igv, x.Percepcion, x.Total, x.Estado == 376m ? "Válido" : "Anulado", x.IdAgenda, x.AgendaNombre, x.FechaReferencia, x.DocumentoReferencia, x.SerieReferencia, x.NumeroReferencia, x.Observaciones) { DocSunat = x.DocSunat }).ToListAsync(ct);
        // Fetch all details for the current page in batches, never one request per document.
        var ventaOps = items.Where(x => x.Origen == "Venta").Select(x => x.Op).Distinct().ToArray();
        var creditoOps = items.Where(x => x.Origen == "Nota de crédito").Select(x => x.Op).Distinct().ToArray();
        var lineasVenta = await db.DocVentaLineas.AsNoTracking().Where(x => ventaOps.Contains(x.Op))
            .Select(x => new { x.Op, x.IdAmarre, x.IdItem, x.Cantidad, x.Precio, x.Importe, x.IdCentroCosto }).ToListAsync(ct);
        var lineasCredito = await db.DocDevVentaLineas.AsNoTracking().Where(x => creditoOps.Contains(x.Op))
            .Select(x => new { x.Op, x.IdAmarre, x.IdItem, x.Cantidad, x.Precio, x.Importe, x.IdCentroCosto }).ToListAsync(ct);

        var idsItem = lineasVenta.Select(x => x.IdItem).Concat(lineasCredito.Select(x => x.IdItem))
            .Where(x => x != null).Select(x => x!).Distinct().ToArray();
        var idsCentro = lineasVenta.Select(x => x.IdCentroCosto).Concat(lineasCredito.Select(x => x.IdCentroCosto))
            .Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToArray();
        var clavesCentro = idsCentro.Select(x => x.ToString(System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        var productos = await db.ItemCodigos.AsNoTracking().Where(x => idsItem.Contains(x.IdItem))
            .ToDictionaryAsync(x => x.IdItem, x => x.Descripcion, ct);
        var centros = await db.CentrosCosto.AsNoTracking().Where(x => clavesCentro.Contains(x.IdCentroCosto))
            .ToDictionaryAsync(x => x.IdCentroCosto, x => x.Nombre, ct);

        DetalleDocumentoVentaItem Mapear(string? idItem, decimal? cantidad, decimal? precio, decimal? importe, decimal? centro)
        {
            productos.TryGetValue(idItem ?? "", out var producto);
            centros.TryGetValue(centro?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "", out var nombreCentro);
            return new(idItem, cantidad, precio, importe, centro, producto, nombreCentro);
        }

        var detallesVenta = lineasVenta.GroupBy(x => x.Op).ToDictionary(g => g.Key,
            g => (IReadOnlyList<DetalleDocumentoVentaItem>)g.OrderBy(x => x.IdAmarre)
                .Select(x => Mapear(x.IdItem, x.Cantidad, x.Precio, x.Importe, x.IdCentroCosto)).ToList());
        var detallesCredito = lineasCredito.GroupBy(x => x.Op).ToDictionary(g => g.Key,
            g => (IReadOnlyList<DetalleDocumentoVentaItem>)g.OrderBy(x => x.IdAmarre)
                .Select(x => Mapear(x.IdItem, x.Cantidad, x.Precio, x.Importe, x.IdCentroCosto)).ToList());

        var conDetalle = items.Select(x => x with
        {
            Detalles = (x.Origen == "Nota de crédito" ? detallesCredito : detallesVenta)
                .GetValueOrDefault(x.Op, Array.Empty<DetalleDocumentoVentaItem>())
        }).ToList();
        return new(conDetalle, total, pagina, tamano);
    }

}
