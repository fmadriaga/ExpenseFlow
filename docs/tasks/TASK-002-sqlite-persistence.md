# TASK-002 SQLite persistence

status: ready
owner: backend
priority: high

## Goal
Agregar persistencia SQLite con EF Core para almacenar documentos,
líneas extraídas y jobs de procesamiento.

## Context
El Worker necesitará un almacenamiento confiable para registrar qué se procesó,
evitar duplicados por hash y preservar resultados OCR para análisis posterior.

## Scope
- Crear `ExpenseFlowDbContext`
- Crear entidades mínimas `Document`, `DocumentLine`, `ProcessingJob`
- Configurar SQLite
- Crear migration inicial
- Permitir inserción y lectura de prueba

## Acceptance Criteria
- DbContext creado y registrado
- Entidades mínimas persistidas en SQLite
- Migration inicial creada
- La base se crea correctamente en entorno local
- Inserción de prueba exitosa

## Out of Scope
- API de consulta
- Categorización de gastos
- Auditoría avanzada

## Technical Notes
- Guardar `RawJson` del OCR en el documento
- Incluir `FileHash` para deduplicación
- Dejar `OcrStatus` y `ErrorMessage` para trazabilidad
