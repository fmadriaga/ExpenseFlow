# TASK-010 API mínima de consulta de documentos

status: done
owner: backend
priority: high

## Goal
Exponer endpoints HTTP en el proyecto Api para consultar documentos
procesados por el pipeline.

## Context
El DbContext, las entidades y el pipeline ya están operativos. La capa Api
existe pero no tiene lógica. Este es el primer punto de acceso externo
a los datos acumulados por el Worker.

## Scope
- Registrar ExpenseFlowDbContext en el host de Api
- Endpoint GET /documents — lista paginada de documentos (Id, FilePath,
  MerchantName, TransactionDate, TotalAmount, OcrStatus, CreatedAt)
- Endpoint GET /documents/{id} — detalle de un documento incluyendo
  DocumentLines y RawJson
- Filtros opcionales por fecha (desde/hasta) y por OcrStatus
- Paginación simple (page, pageSize)

## Out of scope
- Autenticación / autorización
- Edición de documentos
- Reproceso (TASK-011)
- Exportación (TASK-013)

## Acceptance Criteria
- GET /documents devuelve lista paginada con campos mínimos
- GET /documents/{id} devuelve detalle completo con líneas
- Los filtros por fecha y OcrStatus funcionan correctamente
- Si el documento no existe, devuelve 404 con mensaje claro
- No se expone RawJson en el listado, solo en el detalle
- El proyecto Api compila y los endpoints responden en local

## Technical Notes
- Usar minimal API o controllers según convención del repo
- No exponer entidades de Domain directamente — usar DTOs de respuesta
- El DbContext se registra en Api con AddPersistence existente
- No duplicar lógica de consulta en el Worker

## Suggested Files
- src/ExpenseFlow.Api/Endpoints/DocumentsEndpoints.cs
- src/ExpenseFlow.Application/DTOs/DocumentSummaryDto.cs
- src/ExpenseFlow.Application/DTOs/DocumentDetailDto.cs
- src/ExpenseFlow.Api/Program.cs

## Definition of Done
- build exitoso
- endpoints verificados manualmente con curl o Swagger
- tests mínimos de los endpoints (puede ser integración con WebApplicationFactory)
