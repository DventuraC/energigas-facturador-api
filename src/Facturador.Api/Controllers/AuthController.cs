using Facturador.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Facturador.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(JwtAuthClient auth) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Email) || string.IsNullOrWhiteSpace(input.Password))
            return BadRequest(new { message = "Correo y contraseña son obligatorios" });
        var result = await auth.LoginAsync(input.Email, input.Password, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.RawError });
        return Ok(result.Body);
    }
}
