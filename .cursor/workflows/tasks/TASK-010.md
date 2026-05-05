# TASK-010 — API mínima de consulta de documentos

Archivo de task: `docs/tasks/TASK-010-api-query.md`

---

## PLANNER

Eres el Planner del proyecto ExpenseFlow. Lee los archivos indicados y produce
un plan técnico para ejecutar TASK-010.

Archivos a leer antes de responder:
- docs/product/vision.md
- docs/architecture.md
- docs/tasks/TASK-010-api-query.md

Contexto acumulado (TASK-001 a TASK-009 completadas):
- Solución .NET con proyectos Domain, Application, Infrastructure, Worker, Api.
- ExpenseFlowDbContext con Document (Id, FilePath, FileHash, RawJson, OcrStatus,
  ErrorMessage, MerchantName, TransactionDate, Currency, TotalAmount, TaxAmount,
  Confidence, CreatedAt), DocumentLine, ProcessingJob. SQLite con migraciones.
- IFileScanner, IReceiptOcrProvider (Azure), IReceiptNormalizer, IFileMover.
- ExpenseFlowWorker: pipeline completo corriendo con BackgroundService.
- Configuración validada al arranque. Logging con JobId por ciclo.
- El proyecto Api existe pero no tiene lógica ni endpoints.

Produce:
1. Plan de implementación paso a paso (secuencial y técnico).
2. Lista de archivos a crear y modificar por capa.
3. Dependencias NuGet si aplica.
4. Riesgos o puntos a tener en cuenta.

Reglas: no escribas código, selecciona exactamente esta task, salida concreta
y accionable.

---

## IMPLEMENTER

Eres el Backend Implementer de ExpenseFlow. Implementa TASK-010.

Lee primero:
- docs/architecture.md
- docs/tasks/TASK-010-api-query.md
- .cursor/agents/backend-implementer.md

Contexto: mismo que el PLANNER. El proyecto Api compila pero no tiene endpoints.
AddPersistence ya existe en Infrastructure.

Implementa exactamente:
1. Registrar AddPersistence y AddFileScanning en el host del proyecto Api
   (Program.cs de Api) para que el DbContext esté disponible.
2. DTOs de respuesta en Application:
   - DocumentSummaryDto: Id, MerchantName, TransactionDate, TotalAmount,
     Currency, Category, OcrStatus, Confidence, CreatedAt.
   - DocumentDetailDto: todos los campos anteriores más FilePath, RawJson,
     ErrorMessage, y colección de DocumentLineDto
     (Description, Quantity, UnitPrice, Amount, Currency).
3. Endpoint GET /documents:
   - Retorna lista paginada de DocumentSummaryDto.
   - Query params: page (default 1), pageSize (default 20, max 100).
   - Filtros opcionales: from (fecha), to (fecha), status (OcrStatus).
   - Ordenado por CreatedAt descendente.
4. Endpoint GET /documents/{id}:
   - Retorna DocumentDetailDto.
   - 404 con mensaje claro si no existe.
5. Usar minimal API en Api/Endpoints/DocumentsEndpoints.cs.
6. No exponer entidades de Domain directamente — solo DTOs.
7. Tests mínimos con WebApplicationFactory que verifiquen:
   - GET /documents retorna 200 con lista.
   - GET /documents/{id} existente retorna 200 con detalle.
   - GET /documents/{id} inexistente retorna 404.

Reglas obligatorias: verificar build y tests antes de cerrar.
Actualizar architecture.md y README.md si cambia algo en Api.

---

## REVIEWER

Eres el Reviewer de ExpenseFlow. Revisa la implementación de TASK-010.

Lee primero:
- docs/architecture.md
- docs/tasks/TASK-010-api-query.md
- .cursor/agents/reviewer.md

Evalúa contra:
1. Criterios de aceptación de TASK-010.
2. Separación por capas: DTOs en Application, endpoints en Api, sin lógica
   de negocio en controllers/endpoints.
3. RawJson no expuesto en el listado, solo en el detalle.
4. Paginación correcta y con límite máximo.
5. Filtros funcionales.
6. 404 claro para documento inexistente.
7. Cobertura de tests: listado, detalle, 404.

Responde con:
1. Hallazgos críticos.
2. Mejoras recomendadas (no bloqueantes).
3. Decisión: approve / needs changes.

Guarda tu respuesta completa en: `docs/reviews/TASK-010-review.md`

---

## DOCS-KEEPER

Eres el Docs Keeper de ExpenseFlow. Cierra documentalmente TASK-010
solo si el Reviewer aprobó.

Lee primero:
- docs/architecture.md
- docs/tasks/TASK-010-api-query.md
- README.md

Ejecuta:
1. Marca TASK-010 como done (status: ready → status: done).
2. Actualiza docs/architecture.md: endpoints GET /documents y
   GET /documents/{id}, DTOs, cómo se registra el DbContext en Api.
3. Actualiza README.md: cómo levantar la Api localmente y verificar
   los endpoints (curl o Swagger).
4. Escribe resumen breve del cierre.

Reglas: solo documentación, sin cambios de código.

---

## COMMIT

```
feat(api): TASK-010 minimal query API for processed documents

Api
- Register AddPersistence in Api/Program.cs
- Add DocumentsEndpoints with minimal API:
  GET /documents — paginated list (page, pageSize, from, to, status filters)
  GET /documents/{id} — full detail with DocumentLines
  Returns 404 with clear message if document not found

Application
- Add DocumentSummaryDto (Id, MerchantName, TransactionDate, TotalAmount,
  Currency, Category, OcrStatus, Confidence, CreatedAt)
- Add DocumentDetailDto (all fields + FilePath, RawJson, ErrorMessage, Lines)
- Add DocumentLineDto (Description, Quantity, UnitPrice, Amount, Currency)

Tests
- Add DocumentsEndpointsTests with WebApplicationFactory:
  list returns 200, detail returns 200, missing id returns 404

Docs
- Update docs/architecture.md: Api layer, endpoints, DTOs
- Update README.md: how to run Api and verify endpoints
- Close TASK-010 (status: done)
```
