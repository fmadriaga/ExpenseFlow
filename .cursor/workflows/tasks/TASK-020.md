# TASK-020 — Soporte multi-familia

Archivo de task: `docs/tasks/TASK-020-multi-family.md`

---

## PLANNER

Eres el Planner del proyecto ExpenseFlow. Produce un plan técnico para TASK-020.

Archivos a leer:
- docs/architecture.md
- docs/tasks/TASK-020-multi-family.md

Contexto: StorageOptions tiene una sola tripleta Inbox/Processed/Error.
Document no tiene FamilyId. El Worker escanea una sola carpeta inbox.
Todos los endpoints de Api devuelven todos los documentos sin filtro de familia.

Produce:
1. Plan de implementación paso a paso.
2. Estrategia de migración de datos existentes (familia default).
3. Lista de archivos a crear y modificar por capa.
4. Riesgos o puntos a tener en cuenta.

Reglas: no escribas código, salida concreta y accionable.

---

## IMPLEMENTER

Eres el Backend Implementer de ExpenseFlow. Implementa TASK-020.

Lee primero:
- docs/architecture.md
- docs/tasks/TASK-020-multi-family.md
- .cursor/agents/backend-implementer.md

Implementa exactamente:
1. Entidad Family en Domain: Id (Guid), Name (string), InboxPath,
   ProcessedPath, ErrorPath.
2. Agregar FamilyId (Guid, nullable durante migración) a Document.
3. Crear migración AddFamilySupport:
   - Crear tabla Families.
   - Agregar FamilyId a Documents.
   - Seed de una fila Family default (Id fijo, Name "familia").
   - Actualizar todos los Document existentes con el Id de la familia default.
   - Luego hacer FamilyId NOT NULL (en una segunda migración si SQLite no
     soporta ALTER en un solo paso).
4. Actualizar StorageOptions para soportar lista de familias:
   Families: [{ Name, InboxPath, ProcessedPath, ErrorPath }]
   Con compatibilidad hacia atrás: si solo existe la config antigua
   (Inbox/Processed/Error plano), crear familia "familia" con esas rutas.
5. ExpenseFlowWorker: iterar sobre todas las familias configuradas en
   cada ciclo, crear scope por familia.
6. FamilyId propagado a Document al normalizar (el Worker lo conoce
   porque itera familia por familia).
7. Endpoints de Api: agregar query param ?familyId= opcional.
   Sin familyId: devolver todos (comportamiento actual).
8. Tests:
   - Worker procesa dos familias con carpetas distintas.
   - Documentos de familias distintas se pueden filtrar por familyId.

Reglas: build y tests en verde. Migración no destructiva sobre BD existente.

---

## REVIEWER

Eres el Reviewer de ExpenseFlow. Revisa TASK-020.

Lee primero:
- docs/architecture.md
- docs/tasks/TASK-020-multi-family.md
- .cursor/agents/reviewer.md

Evalúa contra:
1. Criterios de aceptación de TASK-020.
2. La migración es no destructiva: datos existentes migran a familia default.
3. FamilyId propagado correctamente en el pipeline.
4. Compatibilidad hacia atrás en StorageOptions.
5. Endpoints filtran por familyId sin romper comportamiento existente.
6. Domain sin dependencias externas.
7. Cobertura de tests.

Responde con hallazgos críticos, mejoras y decisión: approve / needs changes.
Guarda en: `docs/reviews/TASK-020-review.md`

---

## DOCS-KEEPER

Eres el Docs Keeper de ExpenseFlow. Cierra TASK-020 si el Reviewer aprobó.

1. Marca TASK-020 como done.
2. Actualiza docs/architecture.md: entidad Family, FamilyId en Document,
   configuración multi-familia, iteración por familia en el Worker.
3. Actualiza README.md: cómo configurar múltiples familias en appsettings.
4. Resumen breve del cierre.

---

## COMMIT

```
feat(multi-family): TASK-020 multi-family support with data isolation

Domain
- Add Family entity (Id, Name, InboxPath, ProcessedPath, ErrorPath)
- Add FamilyId to Document entity

Infrastructure
- Add AddFamilySupport migration: Families table, FamilyId column,
  seed default family, backfill existing Documents, NOT NULL constraint
- Update ExpenseFlowDbContext: Family DbSet, Document-Family relation

Application
- Update StorageOptions: support Families list + backward-compatible
  single Inbox/Processed/Error config

Worker
- Iterate over configured families in each cycle
- Propagate FamilyId to Document during normalization

Api
- Add optional familyId query param to GET /documents and GET /documents/export

Tests
- Worker processes two families with separate folders
- Api filters documents by familyId correctly

Docs
- Update architecture.md: Family entity, multi-family pipeline
- Update README.md: multi-family appsettings configuration
- Close TASK-020 (status: done)
```
