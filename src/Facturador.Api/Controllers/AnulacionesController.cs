using Facturador.Api.Data;
using Facturador.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Facturador.Api.Controllers;

public sealed record SolicitudAnulacion(string Origen, decimal Op);
public sealed record ResultadoAnulacion(bool Ok, string Mensaje);

[ApiController]
[Route("api/ventas")]
public sealed class AnulacionesController(IConfiguration config, FacturacionClient facturacion) : ControllerBase
{
    [HttpPost("anular")]
    public async Task<ActionResult<ResultadoAnulacion>> Anular([FromBody] SolicitudAnulacion solicitud, CancellationToken ct)
    {
        if (solicitud is null || solicitud.Op <= 0 || (solicitud.Origen != "Venta" && solicitud.Origen != "Nota de crédito"))
            return BadRequest(new ResultadoAnulacion(false, "Documento inválido"));
        var cadena = config.GetConnectionString("SqlServer");
        if (string.IsNullOrWhiteSpace(cadena)) return StatusCode(503, new ResultadoAnulacion(false, "Error al anular el documento - Base de datos no configurada"));
        var opciones = new DbContextOptionsBuilder<FacturadorDbContext>().UseSqlServer(cadena).Options;
        await using var db = new FacturadorDbContext(opciones);
        var documento = solicitud.Origen == "Venta"
            ? await (from v in db.DocVentas.AsNoTracking() join t in db.Documentos.AsNoTracking() on v.IdDocumento equals t.IdDocumento where v.Op == solicitud.Op select new { v.Estado, v.Serie, v.Numero, t.DocSunat }).FirstOrDefaultAsync(ct)
            : await (from v in db.DocDevVentas.AsNoTracking() join t in db.Documentos.AsNoTracking() on v.IdDocumento equals t.IdDocumento where v.Op == solicitud.Op select new { v.Estado, v.Serie, v.Numero, t.DocSunat }).FirstOrDefaultAsync(ct);
        if (documento is null) return NotFound(new ResultadoAnulacion(false, "Documento no encontrado"));
        if (documento.Estado != 376m) return Conflict(new ResultadoAnulacion(false, "Error al anular el documento - El documento ya está anulado"));
        if (string.IsNullOrWhiteSpace(documento.DocSunat) || string.IsNullOrWhiteSpace(documento.Serie) || !documento.Numero.HasValue)
            return BadRequest(new ResultadoAnulacion(false, "Error al anular el documento - Faltan DocSunat, serie o número"));
        try
        {
            var (ok, respuesta) = await facturacion.AnularAsync(documento.DocSunat.Trim(), documento.Serie.Trim(), documento.Numero.Value, ct);
            if (ok) return Ok(new ResultadoAnulacion(true, $"Documento {documento.Serie}-{documento.Numero:0} anulado con exito"));
            return StatusCode(502, new ResultadoAnulacion(false, $"Error al anular el documento - {respuesta}"));
        }
        catch (HttpRequestException ex) { return StatusCode(502, new ResultadoAnulacion(false, $"Error al anular el documento - {ex.Message}")); }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested) { return StatusCode(504, new ResultadoAnulacion(false, "Error al anular el documento - Tiempo de espera agotado")); }
    }
}
