# TASK-019 UI web mínima de revisión

status: done
owner: frontend
priority: medium

## Goal
Implementar una interfaz web simple para visualizar documentos procesados
y corregir manualmente campos extraídos incorrectamente por el OCR.

## Context
El pipeline procesa tickets automáticamente pero el OCR no siempre extrae
datos correctos. Una UI mínima permite revisar resultados y corregir sin
tocar la base de datos directamente.

## Scope
- Listado de documentos con filtros por fecha, OcrStatus y categoría
- Vista de detalle de un documento: campos extraídos, líneas y RawJson
- Edición inline de MerchantName, TransactionDate, TotalAmount, Category
- Indicador visual de Confidence y OcrStatus (badge de color)
- Paginación en el listado

## Out of scope
- Autenticación / login
- Carga de tickets desde la UI (eso sigue siendo inbox/Drive)
- Reproceso desde la UI (usar TASK-011 como backend)
- Múltiples usuarios

## Acceptance Criteria
- El listado muestra documentos con paginación
- Los filtros funcionan correctamente
- La vista de detalle muestra todos los campos y líneas
- La edición guarda los cambios vía API y los refleja inmediatamente
- El badge de Confidence es verde (≥70%), amarillo (40-69%), rojo (<40%)
- La UI funciona correctamente en navegador moderno en local

## Technical Notes
- Tecnología sugerida: Blazor Server (se integra fácilmente con la
  solución .NET existente) o React con Vite como cliente separado
- Consumir los endpoints de TASK-010 y TASK-011
- Agregar endpoint PATCH /documents/{id} en Api para edición parcial
- Mantener la UI en un proyecto separado (ExpenseFlow.Web o similar)
  para no contaminar la capa Api existente

## Suggested Files
- src/ExpenseFlow.Web/... (proyecto nuevo)
- src/ExpenseFlow.Api/Endpoints/DocumentsEndpoints.cs (PATCH)

## Definition of Done
- build exitoso
- listado, detalle y edición verificados manualmente en local
- edición de un campo persiste en SQLite y se refleja al recargar

---

## Resultado
- Proyecto **Blazor Web App** (`ExpenseFlow.Web`, interactive server) con listado, detalle, filtros, paginación y formulario PATCH.
- API: `GET /documents` acepta filtro `category`; `PATCH /documents/{id}` con cuerpo parcial (`PatchDocumentRequestDto`).
- Filtros de fechas en UI con `value` + `onchange` (evita `input type=date` con `string` en `@bind`).

## Archivos principales
- `src/ExpenseFlow.Web/` (Program, Pages, `wwwroot/app.css`, `appsettings.json` con `ExpenseFlowApi:BaseUrl`)
- `src/ExpenseFlow.Api/Endpoints/DocumentsEndpoints.cs`
- `src/ExpenseFlow.Application/DTOs/PatchDocumentRequestDto.cs`
- `ExpenseFlow.sln` (proyecto Web añadido)

## Pendientes
- Ninguno respecto a criterios de aceptación.

**Cierre:** 2026-04-27
