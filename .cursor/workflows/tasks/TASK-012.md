# TASK-012 — Categorización de gastos

Archivo de task: `docs/tasks/TASK-012-expense-categorization.md`

---

## PLANNER

Eres el Planner del proyecto ExpenseFlow. Produce un plan técnico para TASK-012.

Archivos a leer:
- docs/architecture.md
- docs/tasks/TASK-012-expense-categorization.md

Contexto: Document ya tiene MerchantName, TotalAmount, DocumentLines.
El pipeline en ExpenseFlowWorker ejecuta: scan → OCR → normalize → persist → move.
La categorización debe insertarse entre normalize y persist.

Produce:
1. Plan de implementación paso a paso.
2. Lista de archivos a crear y modificar por capa.
3. Dependencias NuGet si aplica.
4. Riesgos o puntos a tener en cuenta.

Reglas: no escribas código, salida concreta y accionable.

---

## IMPLEMENTER

Eres el Backend Implementer de ExpenseFlow. Implementa TASK-012.

Lee primero:
- docs/architecture.md
- docs/tasks/TASK-012-expense-categorization.md
- .cursor/agents/backend-implementer.md

Implementa exactamente:
1. Agregar campo Category (string, nullable) a Document en Domain.
2. Crear migración AddDocumentCategory en Infrastructure.
3. Definir CategoryOptions en Application/Options/:
   - Diccionario<string, string[]> donde la clave es la categoría
     y el valor es la lista de palabras clave.
   - Ejemplo: { "supermercado": ["walmart","carrefour","disco"],
     "combustible": ["ypf","shell","axion"] }
   - Sección de appsettings: "CategoryRules".
4. Definir IExpenseCategorizer en Application/Abstractions/ con:
   Categorize(Document) → string (nunca null; "otros" por defecto).
5. Implementar KeywordExpenseCategorizer en Application/Categorization/:
   - Comparación case-insensitive y por substring sobre MerchantName.
   - Si no hay coincidencia, retorna "otros".
   - Si MerchantName es null, retorna "otros".
   - No lanza excepción bajo ninguna condición.
6. Registrar IExpenseCategorizer como singleton en DI (AddReceiptNormalization
   o nuevo método AddCategorization en Infrastructure/Worker).
7. Invocar IExpenseCategorizer en ExpenseFlowWorker después de Normalize()
   y antes de SaveChangesAsync().
8. Agregar Category al DocumentSummaryDto de TASK-010.
9. Tests unitarios:
   - Coincidencia exacta → categoría correcta.
   - Coincidencia por substring → categoría correcta.
   - Sin coincidencia → "otros".
   - MerchantName null → "otros".
   - Case insensitive: "Walmart" y "walmart" → mismo resultado.

Reglas: build y tests en verde. Actualizar architecture.md y README.md.

---

## REVIEWER

Eres el Reviewer de ExpenseFlow. Revisa TASK-012.

Lee primero:
- docs/architecture.md
- docs/tasks/TASK-012-expense-categorization.md
- .cursor/agents/reviewer.md

Evalúa contra:
1. Criterios de aceptación de TASK-012.
2. IExpenseCategorizer en Application; sin dependencias externas.
3. La categorización nunca rompe el pipeline (no lanza excepciones).
4. Category nunca queda null (siempre "otros" por defecto).
5. Migración creada para el campo Category.
6. Las reglas son configurables desde appsettings sin recompilar.
7. Cobertura de tests.

Responde con hallazgos críticos, mejoras y decisión: approve / needs changes.
Guarda en: `docs/reviews/TASK-012-review.md`

---

## DOCS-KEEPER

Eres el Docs Keeper de ExpenseFlow. Cierra TASK-012 si el Reviewer aprobó.

1. Marca TASK-012 como done.
2. Actualiza docs/architecture.md: IExpenseCategorizer, CategoryOptions,
   posición en el pipeline (después de normalize, antes de persist).
3. Actualiza README.md: cómo configurar CategoryRules en appsettings.
4. Resumen breve del cierre.

---

## COMMIT

```
feat(categorization): TASK-012 keyword-based expense categorization

Domain
- Add Category field (string, nullable) to Document entity

Infrastructure
- Add AddDocumentCategory migration

Application
- Add CategoryOptions: dictionary CategoryName → keywords[]
- Add IExpenseCategorizer interface
- Add KeywordExpenseCategorizer: case-insensitive substring match
  on MerchantName, returns "otros" when no match or null name

Worker
- Invoke IExpenseCategorizer after Normalize(), before SaveChanges()
- Register categorizer in DI

Api
- Add Category to DocumentSummaryDto

Tests
- Add KeywordExpenseCategorizerTests: exact match, substring,
  no match → "otros", null name, case insensitive

Docs
- Update architecture.md: categorization step in pipeline
- Update README.md: CategoryRules configuration
- Close TASK-012 (status: done)
```
