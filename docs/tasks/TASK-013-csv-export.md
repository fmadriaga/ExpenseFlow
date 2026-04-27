# TASK-013 Exportación a CSV

status: done
owner: backend
priority: medium

## Goal
Generar un archivo CSV con el histórico de documentos para importar
en Excel, Google Sheets u otras herramientas de análisis.

## Context
La base de datos acumula documentos procesados. Exportar a CSV es la
forma más simple de habilitar análisis externo sin necesidad de UI.

## Scope
- Endpoint GET /documents/export?from=&to=&status= que devuelve un CSV
- Columnas: Id, FilePath, MerchantName, TransactionDate, TotalAmount,
  TaxAmount, Currency, Category, OcrStatus, CreatedAt
- Filtros opcionales: rango de fechas, OcrStatus
- Content-Type: text/csv con header Content-Disposition para descarga
- Delimitador configurable (coma por defecto, punto y coma como opción)

## Out of scope
- Exportación de líneas de detalle (DocumentLines)
- Exportación a Excel (.xlsx)
- Exportación programada / automática
- UI de descarga

## Acceptance Criteria
- GET /documents/export devuelve un CSV válido con encabezados
- Los filtros por fecha y OcrStatus funcionan correctamente
- El archivo se descarga correctamente desde el browser (Content-Disposition)
- Con resultado vacío devuelve CSV solo con encabezados (no error)
- Los valores con comas o comillas en MerchantName están correctamente
  escapados según RFC 4180

## Technical Notes
- Usar CsvHelper o generación manual simple (sin librerías pesadas)
- No cargar todos los documentos en memoria si el volumen es grande;
  considerar streaming con yield o paginación interna
- El endpoint puede reutilizar la misma consulta base de TASK-010

## Suggested Files
- src/ExpenseFlow.Api/Endpoints/DocumentsEndpoints.cs
- src/ExpenseFlow.Application/Export/ICsvExporter.cs
- src/ExpenseFlow.Application/Export/DocumentCsvExporter.cs

## Definition of Done
- build exitoso
- CSV verificado manualmente con importación en Excel o Google Sheets
- test que verifica encabezados y escape de caracteres especiales
