using System.Text.Json;
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
        if (!response.IsSuccessStatusCode) return (false, string.IsNullOrWhiteSpace(texto) ? response.ReasonPhrase ?? "Error HTTP" : texto);
        try
        {
            using var json = JsonDocument.Parse(texto);
            var root = json.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in root.EnumerateObject())
                {
                    if (!prop.Name.Equals("Ok", StringComparison.OrdinalIgnoreCase)) continue;
                    var ok = prop.Value.ValueKind == JsonValueKind.Number && prop.Value.TryGetInt32(out var n) && n == 1
                        || prop.Value.ValueKind == JsonValueKind.String && prop.Value.GetString() == "1";
                    return (ok, texto);
                }
            }
        }
        catch (JsonException) { }
        return (false, texto);
    }
}
