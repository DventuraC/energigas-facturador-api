using Facturador.Api.Models;
using Facturador.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Facturador.Api.Controllers;

[ApiController]
[Route("api/ventas")]
public sealed class VentasController(ConsultaVentasService consultas) : ControllerBase
{
    [HttpGet("tipos-documento")]
    public async Task<ActionResult<IReadOnlyList<DocumentoTipoDisponible>>> Tipos([FromQuery] DateTime? fechaInicio, [FromQuery] DateTime? fechaFin, CancellationToken ct)
        => Ok(await consultas.TiposDisponiblesAsync(fechaInicio, fechaFin, ct));

    [HttpGet("documentos")]
    public async Task<ActionResult<PaginaDocumentosVenta>> Documentos([FromQuery] FiltroDocumentosVenta filtro, CancellationToken ct)
        => Ok(await consultas.BuscarAsync(filtro, ct));
}
