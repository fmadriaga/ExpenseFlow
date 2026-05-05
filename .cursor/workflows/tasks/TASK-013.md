# TASK-013 — Exportación a CSV

Archivo de task: `docs/tasks/TASK-013-csv-export.md`

---

## PLANNER

Eres el Planner del proyecto ExpenseFlow. Produce un plan técnico para TASK-013.

Archivos a leer:
- docs/architecture.md
- docs/tasks/TASK-013-csv-export.md

Contexto: GET /documents ya existe (TASK-010). Document tiene Category
(TASK-012). Los DTOs DocumentSummaryDto y DocumentDetailDto están en Application.

Produce:
1. Plan de implementación paso a paso.
2. Lista de archivos a crear y modificar por capa.
3. Dependencias NuGet si aplica (evaluar CsvHelper vs generación manual).
4. Riesgos o puntos a tener en cuenta.

Reglas: no escribas código, salida concreta y accionable.

---

## IMPLEMENTER

Eres el Backend Implementer de ExpenseFlow. Implementa TASK-013.

Lee primero:
- docs/architecture.md
- docs/tasks/TASK-013-csv-export.md
- .cursor/agents/backend-implementer.md

Implementa exactamente:
1. Definir ICsvExporter en Application/Export/ con:
   ExportDocumentsAsync(IEnumerable<DocumentSummaryDto>, Stream, char delimiter)
2. Implementar DocumentCsvExporter en Application/Export/:
   - Columnas: Id, MerchantName, TransactionDate, TotalAmount, TaxAmount,
     Currency, Category, OcrStatus, Confidence, CreatedAt.
   - Escape RFC 4180: valores con coma, comillas o salto de línea
     se encierran entre comillas dobles; las comillas internas se duplican.
   - Delimitador configurable (coma por defecto).
   - Escritura en stream (no cargar todo en memoria).
3. Endpoint GET /documents/export en DocumentsEndpoints:
   - Query params: from, to, status, delimiter (default ",").
   - Content-Type: text/csv; charset=utf-8.
   - Header Content-Disposition: attachment; filename="documents.csv".
   - Reutiliza la misma consulta base que GET /documents (sin paginación).
   - Con resultado vacío: devuelve CSV solo con encabezados (no error).
4. Registrar ICsvExporter en DI.
5. Tests:
   - Export con documentos → CSV con encabezados y filas correctas.
   - Valores con coma en MerchantName → correctamente escapados.
   - Export sin documentos → solo encabezados, status 200.

Reglas: preferir generación manual sobre CsvHelper para no agregar
dependencias pesadas. Build y tests en verde.

---

## REVIEWER

Eres el Reviewer de ExpenseFlow. Revisa TASK-013.

Lee primero:
- docs/architecture.md
- docs/tasks/TASK-013-csv-export.md
- .cursor/agents/reviewer.md

Evalúa contra:
1. Criterios de aceptación de TASK-013.
2. Escape RFC 4180 correcto (especialmente comas, comillas, saltos de línea).
3. Escritura en stream (sin cargar todo en memoria).
4. Content-Type y Content-Disposition correctos.
5. Result vacío retorna 200 con solo encabezados.
6. ICsvExporter en Application sin dependencias de EF o Infrastructure.
7. Cobertura de tests.

Responde con hallazgos críticos, mejoras y decisión: approve / needs changes.
Guarda en: `docs/reviews/TASK-013-review.md`

---

## DOCS-KEEPER

Eres el Docs Keeper de ExpenseFlow. Cierra TASK-013 si el Reviewer aprobó.

1. Marca TASK-013 como done.
2. Actualiza docs/architecture.md: endpoint GET /documents/export,
   ICsvExporter, streaming.
3. Actualiza README.md: cómo descargar el CSV (curl o browser).
4. Resumen breve del cierre.

---

## COMMIT

```
feat(export): TASK-013 CSV export endpoint

Application
- Add ICsvExporter interface
- Add DocumentCsvExporter: RFC 4180 compliant, stream-based,
  configurable delimiter, columns: Id/MerchantName/TransactionDate/
  TotalAmount/TaxAmount/Currency/Category/OcrStatus/Confidence/CreatedAt

Api
- Add GET /documents/export endpoint: from/to/status/delimiter filters,
  Content-Type text/csv, Content-Disposition attachment,
  returns headers-only CSV when result is empty

Tests
- Add CsvExportTests: rows correct, comma in name escaped,
  empty result returns headers only with 200

Docs
- Update architecture.md: export endpoint and ICsvExporter
- Update README.md: how to download CSV
- Close TASK-013 (status: done)
```
