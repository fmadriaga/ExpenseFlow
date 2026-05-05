# TASK-014 — Índice único en FileHash

Archivo de task: `docs/tasks/TASK-014-filehash-unique-index.md`

---

## PLANNER

Eres el Planner del proyecto ExpenseFlow. Produce un plan técnico para TASK-014.

Archivos a leer:
- docs/architecture.md
- docs/tasks/TASK-014-filehash-unique-index.md

Contexto: Document.FileHash existe desde TASK-002. La deduplicación actual
es por consulta en FileScanner (AnyAsync). El Worker captura excepciones en
el pipeline. DbUpdateException se propaga como error genérico hoy.

Produce:
1. Plan de implementación paso a paso.
2. Lista de archivos a crear y modificar por capa.
3. Riesgos o puntos a tener en cuenta.

Reglas: no escribas código, salida concreta y accionable.

---

## IMPLEMENTER

Eres el Backend Implementer de ExpenseFlow. Implementa TASK-014.

Lee primero:
- docs/architecture.md
- docs/tasks/TASK-014-filehash-unique-index.md
- .cursor/agents/backend-implementer.md

Implementa exactamente:
1. Agregar índice único en ExpenseFlowDbContext:
   HasIndex(d => d.FileHash).IsUnique()
2. Crear migración AddFileHashUniqueIndex.
3. En ExpenseFlowWorker, en el bloque try/catch de ProcessOneFileAsync:
   - Detectar DbUpdateException cuya excepción interna contenga texto
     indicativo de violación de unicidad (SQLite: "UNIQUE constraint failed").
   - Si es violación de unicidad: loguear como Warning (duplicado detectado
     a nivel BD, FileHash, FileName), mover a processed/ (no a error/),
     no crear ProcessingJob de fallo.
   - Si es otro DbUpdateException: mantener flujo de error existente.
4. Tests:
   - Insertar dos Document con el mismo FileHash → DbUpdateException.
   - El Worker trata la excepción de unicidad como duplicado (no fallo).

Reglas: build y tests en verde. Verificar que migración se aplica sobre
BD existente sin pérdida de datos.

---

## REVIEWER

Eres el Reviewer de ExpenseFlow. Revisa TASK-014.

Lee primero:
- docs/architecture.md
- docs/tasks/TASK-014-filehash-unique-index.md
- .cursor/agents/reviewer.md

Evalúa contra:
1. Criterios de aceptación de TASK-014.
2. El índice único está en OnModelCreating (no solo en migración).
3. La detección de violación de unicidad es robusta (no solo string match
   frágil — verificar el enfoque elegido).
4. Duplicado a nivel BD → processed/ (no error/).
5. Los tests de integración existentes siguen en verde.
6. La migración es incremental y no destructiva.

Responde con hallazgos críticos, mejoras y decisión: approve / needs changes.
Guarda en: `docs/reviews/TASK-014-review.md`

---

## DOCS-KEEPER

Eres el Docs Keeper de ExpenseFlow. Cierra TASK-014 si el Reviewer aprobó.

1. Marca TASK-014 como done.
2. Actualiza docs/architecture.md: índice único en FileHash, comportamiento
   ante violación de unicidad en el Worker.
3. Resumen breve del cierre.

---

## COMMIT

```
feat(db): TASK-014 unique index on Document.FileHash

Infrastructure
- Add HasIndex(FileHash).IsUnique() to ExpenseFlowDbContext
- Add AddFileHashUniqueIndex migration (non-destructive)

Worker
- Detect DbUpdateException for UNIQUE constraint violation in
  ProcessOneFileAsync; treat as duplicate: log Warning, move
  to processed/, skip ProcessingJob creation
- Other DbUpdateException still follows error path

Tests
- Add unique index violation test: two docs with same hash → exception
- Add Worker duplicate-at-db-level test: treated as duplicate, not failure

Docs
- Update architecture.md: unique constraint and dedup behavior
- Close TASK-014 (status: done)
```
