# Estado de desarrollo de la API

Actualizado: 9 de noviembre de 2026.

## Implementado

- API ASP.NET Core 10 y EF Core SQL Server.
- Consulta de ventas, NC y ND con filtros y paginación.
- Cabecera con importes, estado, cliente, referencias y observaciones.
- Validación y carga masiva de Excel por etapas, con hoja requerida `cargaMasiva` y descarte de sesiones temporales.
- Ejecución configurable del procedimiento de generación documental.
- Base preparada para JWT/OIDC, desactivada por configuración.

## Pendiente

- Proveedor real JWT/OIDC y roles.
- Pruebas automatizadas.
- Auditoría y CI/CD.
