# TASK-019 — UI web mínima de revisión

Archivo de task: `docs/tasks/TASK-019-web-ui.md`

---

## PLANNER

Eres el Planner del proyecto ExpenseFlow. Produce un plan técnico para TASK-019.

Archivos a leer:
- docs/architecture.md
- docs/tasks/TASK-019-web-ui.md

Contexto: GET /documents y GET /documents/{id} existen (TASK-010).
El proyecto Api está en src/ExpenseFlow.Api. No existe proyecto de UI todavía.
La edición de campos requiere un nuevo endpoint PATCH /documents/{id}.

Produce:
1. Plan de implementación paso a paso.
2. Tecnología recomendada (Blazor Server vs cliente separado) con justificación.
3. Lista de archivos a crear y modificar por capa.
4. Dependencias NuGet si aplica.
5. Riesgos o puntos a tener en cuenta.

Reglas: no escribas código, salida concreta y accionable.

---

## IMPLEMENTER

Eres el Backend Implementer de ExpenseFlow. Implementa TASK-019.

Lee primero:
- docs/architecture.md
- docs/tasks/TASK-019-web-ui.md
- .cursor/agents/backend-implementer.md

Implementa exactamente:

**Backend (Api):**
1. DTO UpdateDocumentDto en Application: MerchantName?, TransactionDate?,
   TotalAmount?, Category? (todos nullable, solo se actualiza lo que viene).
2. Endpoint PATCH /documents/{id} en DocumentsEndpoints:
   - 404 si no existe.
   - Aplica solo los campos no null del DTO.
   - Devuelve DocumentDetailDto actualizado.

**Frontend (Blazor Server):**
3. Crear proyecto src/ExpenseFlow.Web (Blazor Server, .NET 9).
4. Referenciar ExpenseFlow.Application para DTOs.
5. Servicio HttpClient tipado DocumentApiClient que consuma la Api.
6. Páginas:
   - /documents: listado paginado con filtros (from, to, status).
     Tabla con columnas: MerchantName, TransactionDate, TotalAmount,
     Currency, Category, OcrStatus, badge de Confidence.
     Paginación con botones Anterior/Siguiente.
   - /documents/{id}: detalle con campos editables inline.
     Edición de MerchantName, TransactionDate, TotalAmount, Category.
     Botón Guardar que llama PATCH /documents/{id}.
     Muestra DocumentLines como tabla.
7. Badge de Confidence: verde (≥70%), amarillo (40-69%), rojo (<40%).
8. Agregar ExpenseFlow.Web al .sln.

Reglas: build en verde. No autenticación. La UI consume la Api como cliente
HTTP (no referencia directa al DbContext). Verificar manualmente en local.

---

## REVIEWER

Eres el Reviewer de ExpenseFlow. Revisa TASK-019.

Lee primero:
- docs/architecture.md
- docs/tasks/TASK-019-web-ui.md
- .cursor/agents/reviewer.md

Evalúa contra:
1. Criterios de aceptación de TASK-019.
2. La UI consume la Api por HTTP, no accede al DbContext directamente.
3. PATCH /documents/{id}: solo actualiza campos presentes en el DTO.
4. Badge de Confidence con colores correctos.
5. Edición persiste en SQLite y se refleja al recargar.
6. El proyecto Blazor está en la solución y compila.
7. Sin autenticación (out of scope).

Responde con hallazgos críticos, mejoras y decisión: approve / needs changes.
Guarda en: `docs/reviews/TASK-019-review.md`

---

## DOCS-KEEPER

Eres el Docs Keeper de ExpenseFlow. Cierra TASK-019 si el Reviewer aprobó.

1. Marca TASK-019 como done.
2. Actualiza docs/architecture.md: proyecto ExpenseFlow.Web, Blazor Server,
   DocumentApiClient, endpoint PATCH /documents/{id}.
3. Actualiza README.md: cómo levantar la UI web localmente.
4. Resumen breve del cierre.

---

## COMMIT

```
feat(ui): TASK-019 Blazor Server web UI for document review

Api
- Add UpdateDocumentDto (nullable fields: MerchantName, TransactionDate,
  TotalAmount, Category)
- Add PATCH /documents/{id}: applies non-null fields, returns updated detail

Web (new project: src/ExpenseFlow.Web)
- Blazor Server targeting .NET 9
- DocumentApiClient typed HttpClient for Api consumption
- /documents page: paginated list with from/to/status filters,
  MerchantName/Date/Amount/Currency/Category/OcrStatus columns,
  Confidence badge (green ≥70%, yellow 40-69%, red <40%)
- /documents/{id} page: full detail, inline editing of
  MerchantName/TransactionDate/TotalAmount/Category,
  Save button calls PATCH, DocumentLines table
- Added ExpenseFlow.Web to ExpenseFlow.sln

Docs
- Update architecture.md: Web project, Blazor, DocumentApiClient, PATCH
- Update README.md: how to run Web UI locally
- Close TASK-019 (status: done)
```
