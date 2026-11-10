using Facturador.Api.Models;
using Facturador.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Facturador.Api.Controllers;

[ApiController]
[Route("api/cargas")]
public sealed class CargasController(CargaService cargaService, SqlCargaService sqlCargaService) : ControllerBase
{
    [HttpPost("validar")]
    [RequestSizeLimit(10_000_000)]
    public ActionResult<ValidacionCargaResponse> Validar([FromForm] IFormFile archivo)
    {
        if (archivo is null || archivo.Length == 0) return BadRequest("Selecciona un archivo Excel.");
        if (!Path.GetExtension(archivo.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase)) return BadRequest("Solo se acepta .xlsx.");
        using var stream = archivo.OpenReadStream();
        return Ok(cargaService.Validar(stream));
    }

    [HttpPost("{id:guid}/procesar")]
    public async Task<ActionResult<ResultadoProceso>> Procesar(Guid id, CancellationToken cancellationToken)
    {
        if (!cargaService.TryGet(id, out var documentos) || documentos is null) return NotFound("La sesión de carga expiró. Valida el archivo nuevamente.");
        return Ok(await sqlCargaService.ProcesarAsync(documentos, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Eliminar(Guid id)
    {
        cargaService.Eliminar(id);
        return NoContent();
    }

    [HttpPost("ejecutar-procedimiento")]
    public async Task<IActionResult> EjecutarProcedimiento(CancellationToken cancellationToken)
    {
        await sqlCargaService.EjecutarProcedimientoAsync(cancellationToken);
        return Ok(new { mensaje = "Procedimiento ejecutado correctamente." });
    }
}
