using Facturador.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var authenticationEnabled = builder.Configuration.GetValue<bool>("Authentication:Enabled");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Energigas Facturador API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Introduce el token JWT sin el prefijo Bearer.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});
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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Energigas Facturador API v1"));
}
app.UseCors("web");
if (authenticationEnabled) app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
