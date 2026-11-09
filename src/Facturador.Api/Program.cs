using Facturador.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);
var authenticationEnabled = builder.Configuration.GetValue<bool>("Authentication:Enabled");

builder.Services.AddControllers();
builder.Services.Configure<JwtAuthApiConfiguration>(builder.Configuration.GetSection("JwtAuthApi"));
builder.Services.AddHttpClient("JwtAuthApi", client =>
{
    var domain = builder.Configuration["JwtAuthApi:Domain"]
        ?? throw new InvalidOperationException("JwtAuthApi:Domain no configurado");
    if (!Uri.TryCreate(domain, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        throw new InvalidOperationException("JwtAuthApi:Domain debe ser HTTPS");
    client.BaseAddress = new Uri(domain.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddScoped<JwtAuthClient>();
builder.Services.AddSingleton<CargaService>();
builder.Services.AddScoped<SqlCargaService>();
builder.Services.AddScoped<ConsultaVentasService>();
builder.Services.AddCors(options => options.AddPolicy("web", policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:4200"])
        .AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddAuthorization(options =>
{
    if (authenticationEnabled)
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser().Build();
    }
});

if (authenticationEnabled)
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = builder.Configuration["Authentication:Authority"];
            options.Audience = builder.Configuration["Authentication:Audience"];
        });
}

var app = builder.Build();
app.UseCors("web");
if (authenticationEnabled) app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
