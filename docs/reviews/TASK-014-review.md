# Review TASK-014 — Índice único en FileHash

## Acceptance criteria

| Criterio | Estado |
|----------|--------|
| BD rechaza dos documentos con el mismo `FileHash` | OK — `HasIndex(...).IsUnique()` + migración |
| Worker captura unicidad y no lo trata como fallo genérico | OK — `catch (DbUpdateException) when (...)` antes del catch de persistencia |
| Archivo duplicado → `processed/` con advertencia | OK — `TryMoveDuplicateInboxToProcessedOrFailAsync` y rama en `PersistFailure...` |
| Tests existentes en verde | OK |
| Migración aplicable | OK — `CreateIndex` único; BD previa sin duplicados en `FileHash` |

## Notas

- Detección de violación basada en mensaje SQLite (`UNIQUE constraint failed` + `FileHash`), sin acoplar tipos de proveedor en Application.
- Si existieran filas duplicadas por `FileHash` antes de migrar, habría que limpiar datos manualmente antes de `database update`.

**Veredicto:** aprobado.
