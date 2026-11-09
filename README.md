# Energigas Facturador API

API ASP.NET Core 10 para consultar documentos de ventas, procesar cargas Excel y ejecutar el procedimiento de generación documental.

## Estructura

```text
src/Facturador.Api/   Código de la API
docs/                 Operación y estado del proyecto
```

## Inicio local

1. Copia `src/Facturador.Api/appsettings.Example.json` como `appsettings.json`.
2. Completa la conexión SQL Server local y el proveedor JWT/OIDC si corresponde.
3. Ejecuta:

   ```powershell
   dotnet run --project src/Facturador.Api --urls http://127.0.0.1:5000
   ```

No subas el archivo `appsettings.json`: está excluido por Git.
