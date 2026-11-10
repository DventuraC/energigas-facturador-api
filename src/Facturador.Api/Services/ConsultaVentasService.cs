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
             select new { Origen = "Venta", v.Op, v.Fecha, v.IdDocumento, Documento = d.Nombre, v.Serie, v.Numero, v.SubTotal, Igv = v.Impuestos, Percepcion = v.Percepcion ?? 0m, v.Total, v.Estado, v.IdAgenda, AgendaNombre = a.AgendaNombre, FechaReferencia = (DateTime?)r.Fecha, DocumentoReferencia = rd.Nombre, SerieReferencia = r.Serie, NumeroReferencia = r.Numero, v.Observaciones })
            .Concat(
             from nc in creditos
             join d in db.Documentos.AsNoTracking() on nc.IdDocumento equals d.IdDocumento
             join a0 in db.Agendas.AsNoTracking() on nc.IdAgenda equals a0.IdAgenda into agendas
             from a in agendas.DefaultIfEmpty()
             join r0 in db.DocVentas.AsNoTracking() on nc.OpDocVta equals r0.Op into referencias
             from r in referencias.DefaultIfEmpty()
             join rd0 in db.Documentos.AsNoTracking() on r.IdDocumento equals rd0.IdDocumento into documentosReferencia
             from rd in documentosReferencia.DefaultIfEmpty()
             select new { Origen = "Nota de crédito", nc.Op, nc.Fecha, nc.IdDocumento, Documento = d.Nombre, nc.Serie, nc.Numero, nc.SubTotal, Igv = nc.Impuestos, Percepcion = 0m, nc.Total, nc.Estado, nc.IdAgenda, AgendaNombre = a.AgendaNombre, FechaReferencia = (DateTime?)r.Fecha, DocumentoReferencia = rd.Nombre, SerieReferencia = r.Serie, NumeroReferencia = r.Numero, nc.Observaciones });
        var total = await documentos.CountAsync(ct);
        var pagina = Math.Max(1, filtro.Pagina);
        var tamano = Math.Clamp(filtro.TamanoPagina, 1, 100);
        var items = await documentos.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Op).Skip((pagina - 1) * tamano).Take(tamano)
            .Select(x => new DocumentoVentaItem(x.Origen, x.Op, x.Fecha, x.IdDocumento, x.Documento, x.Serie, x.Numero, x.SubTotal, x.Igv, x.Percepcion, x.Total, x.Estado == 376m ? "Válido" : "Anulado", x.IdAgenda, x.AgendaNombre, x.FechaReferencia, x.DocumentoReferencia, x.SerieReferencia, x.NumeroReferencia, x.Observaciones)).ToListAsync(ct);
        return new(items, total, pagina, tamano);
    }

    public async Task<IReadOnlyList<DetalleDocumentoVentaItem>> DetalleAsync(decimal op, bool notaCredito, CancellationToken ct)
    {
        await using var db = CreateContext();
        if (notaCredito)
        {
            return await (from linea in db.DocDevVentaLineas.AsNoTracking()
                          where linea.Op == op
                          join item0 in db.ItemCodigos.AsNoTracking() on linea.IdItem equals item0.IdItem into items
                          from item in items.DefaultIfEmpty()
                          join centro0 in db.CentrosCosto.AsNoTracking() on linea.IdCentroCosto!.Value.ToString() equals centro0.IdCentroCosto into centros
                          from centro in centros.DefaultIfEmpty()
                          select new DetalleDocumentoVentaItem(linea.IdItem, linea.Cantidad, linea.Precio, linea.Importe, linea.IdCentroCosto, item.Descripcion, centro.Nombre)).ToListAsync(ct);
        }
        return await (from linea in db.DocVentaLineas.AsNoTracking()
                      where linea.Op == op
                      join item0 in db.ItemCodigos.AsNoTracking() on linea.IdItem equals item0.IdItem into items
                      from item in items.DefaultIfEmpty()
                      join centro0 in db.CentrosCosto.AsNoTracking() on linea.IdCentroCosto!.Value.ToString() equals centro0.IdCentroCosto into centros
                      from centro in centros.DefaultIfEmpty()
                      select new DetalleDocumentoVentaItem(linea.IdItem, linea.Cantidad, linea.Precio, linea.Importe, linea.IdCentroCosto, item.Descripcion, centro.Nombre)).ToListAsync(ct);
    }
}
