using Microsoft.Extensions.Options;

namespace Facturador.Api.Services;

public sealed class FacturacionOptions
{
    public string Domain { get; set; } = "https://api.efacturas.pe";
    public string Token { get; set; } = "";
}

public sealed class FacturacionClient(HttpClient http, IOptions<FacturacionOptions> options)
{
    public async Task<(bool Ok, string Respuesta)> AnularAsync(string docSunat, string serie, decimal numero, CancellationToken ct)
    {
        var token = options.Value.Token;
        if (string.IsNullOrWhiteSpace(token)) return (false, "Token de facturación no configurado");
        var numeroTexto = numero.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
        var ruta = $"pe/void/{Uri.EscapeDataString(token)}/{Uri.EscapeDataString(docSunat)}/{Uri.EscapeDataString(serie)}/{Uri.EscapeDataString(numeroTexto)}";
        using var response = await http.GetAsync(ruta, ct);
        var texto = await response.Content.ReadAsStringAsync(ct);
        // Only HTTP 200 with the exact plain-text response OK:1 confirms voiding.
        return (response.StatusCode == System.Net.HttpStatusCode.OK
            && texto.Trim() == "OK:1", texto);

    }
}
