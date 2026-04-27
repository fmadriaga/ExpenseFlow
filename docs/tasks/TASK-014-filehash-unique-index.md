# TASK-014 Índice único en FileHash

status: done
owner: backend
priority: medium

## Goal
Agregar una restricción única a nivel de base de datos sobre
Document.FileHash para garantizar deduplicación estricta incluso
ante condiciones de concurrencia.

## Context
La deduplicación actual depende de la consulta AnyAsync en FileScanner
antes de procesar. En escenarios de concurrencia (dos ciclos superpuestos
por error, o futuras instancias múltiples del Worker) es posible insertar
dos documentos con el mismo hash. El índice único elimina esa posibilidad
a nivel de datos.

## Scope
- Agregar índice único sobre Document.FileHash en ExpenseFlowDbContext
- Crear migración correspondiente
- Manejar DbUpdateException por violación de unicidad en el Worker:
  loguear como duplicado y mover el archivo a processed/ (ya existía
  en BD con ese hash) en lugar de a error/

## Out of scope
- Cambios en la lógica del scanner
- Deduplicación intra-batch por hash

## Acceptance Criteria
- La BD rechaza inserción de dos documentos con el mismo FileHash
- El Worker captura la excepción de unicidad y la trata como duplicado,
  no como fallo
- El archivo duplicado se mueve a processed/ con log de advertencia
- Los tests de integración existentes siguen en verde
- La migración se aplica correctamente sobre una BD existente

## Technical Notes
- Usar HasIndex(d => d.FileHash).IsUnique() en OnModelCreating
- Capturar DbUpdateException y verificar si es violación de unicidad
  antes de tratar como error genérico
- El mensaje de log debe incluir FileHash y FullPath para diagnóstico

## Suggested Files
- src/ExpenseFlow.Infrastructure/Data/ExpenseFlowDbContext.cs
- src/ExpenseFlow.Infrastructure/Migrations/...
- src/ExpenseFlow.Worker/Workers/ExpenseFlowWorker.cs

## Definition of Done
- build exitoso
- test que intenta insertar dos documentos con el mismo hash y verifica
  que el segundo lanza excepción capturada correctamente
- migración aplicada sin pérdida de datos en BD existente
