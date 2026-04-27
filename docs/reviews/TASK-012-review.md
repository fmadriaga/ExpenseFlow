# Review TASK-012 — Categorización de gastos

## Acceptance criteria

| Criterio | Estado |
|----------|--------|
| Campo `Category` en `Document` y SQLite | OK — migración `AddDocumentCategory`, default `otros` |
| Categorizador acierta comercios conocidos | OK — reglas en `CategoryRules` + pruebas unitarias |
| Sin coincidencia → `otros` (no null) | OK |
| Reglas sin recompilar (appsettings) | OK — `CategoryOptions` + `AddCategorization` |
| Categorización no rompe el pipeline | OK — `KeywordExpenseCategorizer` no propaga excepciones |
| DTOs API con categoría | OK — listado y detalle mapean `d.Category` |

## Arquitectura

- `IExpenseCategorizer` y `KeywordExpenseCategorizer` en Application; reglas vía `IOptions<CategoryOptions>`.
- Worker: tras `Normalize`, asigna `Category` antes de `SaveChanges`; documentos de error también reciben categoría (`otros` sin comercio).

## Tests

- `KeywordExpenseCategorizerTests`: coincidencia, subcadena, `otros`, `MerchantName` nulo/vacío, reglas vacías, mayúsculas.

**Veredicto:** aprobado.
