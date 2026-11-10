# Funcionamiento de la API

## Endpoints de ventas

- `GET /api/ventas/tipos-documento`: devuelve los tipos existentes según el rango de fechas.
- `GET /api/ventas/documentos`: devuelve documentos filtrados y paginados.

Filtros: fecha inicial/final, serie, número, centro de costo, tipos de documento, página y tamaño de página.

La consulta une `Doc_Venta`, `Doc_DevVenta`, sus líneas, `Documentos` y `Agenda`. Las NC referencian la venta mediante `OpDocVta`; las ND mediante `ND_Op`. El estado `376` es válido y los demás estados se reportan como anulados.

## Carga masiva

La API valida un Excel con la hoja `cargaMasiva`, carga registros auxiliares y ejecuta el procedimiento configurado `dbo.SP_GENERARDOCUMENTOMANUALESV2`. La estructura exige 19 columnas en orden; una sesión validada puede eliminarse mediante `DELETE /api/cargas/{id}` al quitar o reemplazar el archivo.

La validación y la carga devuelven el detalle de cada documento (orden, fecha, tipo, serie, número y estado). En la carga auxiliar, el estado es `Insertado` u `Omitido` si ya existe la combinación tipo, serie y número.

## Configuración

La plantilla `src/Facturador.Api/appsettings.Example.json` detalla los valores requeridos. La configuración real debe mantenerse fuera del repositorio.
