# TASK-021 — División de gastos familiares

Archivo de task: `docs/tasks/TASK-021-expense-splitting.md`

---

## PLANNER

Eres el Planner del proyecto ExpenseFlow. Produce un plan técnico para TASK-021.

Archivos a leer:
- docs/architecture.md
- docs/tasks/TASK-021-expense-splitting.md

Contexto: Family existe (TASK-020). Document tiene FamilyId y TotalAmount.
No existe FamilyMember ni ExpenseSplit todavía.

Produce:
1. Plan de implementación paso a paso.
2. Lista de archivos a crear y modificar por capa.
3. Riesgos o puntos a tener en cuenta (suma 100%, decimal precision).

Reglas: no escribas código, salida concreta y accionable.

---

## IMPLEMENTER

Eres el Backend Implementer de ExpenseFlow. Implementa TASK-021.

Lee primero:
- docs/architecture.md
- docs/tasks/TASK-021-expense-splitting.md
- .cursor/agents/backend-implementer.md

Implementa exactamente:
1. Entidades en Domain:
   - FamilyMember: Id (Guid), FamilyId (Guid), Name (string).
   - ExpenseSplit: Id (Guid), DocumentId (Guid), FamilyMemberId (Guid),
     Percentage (decimal 5,2).
2. Migración AddExpenseSplitting.
3. Casos de uso en Application/UseCases/:
   - SplitExpenseUseCase(documentId, splits: [{memberId, percentage}]):
     valida que suma de percentages == 100 (error si no),
     reemplaza splits existentes del documento,
     persiste en BD.
   - CalculateBalanceUseCase(memberId, from, to):
     suma TotalAmount de documentos del período donde el miembro tiene split,
     pondera por Percentage,
     retorna { MemberId, TotalPaid, TotalOwed, Balance }.
     Lógica en Application (no en SQL puro).
4. Endpoints en Api:
   - POST /documents/{id}/split: body [{memberId, percentage}].
     400 si suma ≠ 100. 404 si documento no existe.
   - GET /members/{id}/balance?from=&to=: retorna balance del período.
     404 si miembro no existe.
   - GET /families/{familyId}/members: lista miembros de una familia.
   - POST /families/{familyId}/members: crea un miembro.
5. Tests unitarios:
   - SplitExpenseUseCase: suma 100 → éxito, suma ≠ 100 → error.
   - CalculateBalanceUseCase: balance correcto para un período dado.

Reglas: build y tests en verde. Migración no destructiva.

---

## REVIEWER

Eres el Reviewer de ExpenseFlow. Revisa TASK-021.

Lee primero:
- docs/architecture.md
- docs/tasks/TASK-021-expense-splitting.md
- .cursor/agents/reviewer.md

Evalúa contra:
1. Criterios de aceptación de TASK-021.
2. Validación de suma 100% antes de persistir (en Application).
3. El cálculo de balance está en Application (testeable sin BD).
4. Precisión decimal correcta para Percentage y montos.
5. 400 claro si suma ≠ 100; 404 si documento/miembro no existe.
6. Migración no destructiva.
7. Cobertura de tests.

Responde con hallazgos críticos, mejoras y decisión: approve / needs changes.
Guarda en: `docs/reviews/TASK-021-review.md`

---

## DOCS-KEEPER

Eres el Docs Keeper de ExpenseFlow. Cierra TASK-021 si el Reviewer aprobó.

1. Marca TASK-021 como done.
2. Actualiza docs/architecture.md: FamilyMember, ExpenseSplit, casos de uso,
   endpoints de split y balance.
3. Actualiza README.md si aplica.
4. Resumen breve del cierre.

---

## COMMIT

```
feat(splitting): TASK-021 family expense splitting and balance

Domain
- Add FamilyMember entity (Id, FamilyId, Name)
- Add ExpenseSplit entity (Id, DocumentId, FamilyMemberId, Percentage decimal 5,2)

Infrastructure
- Add AddExpenseSplitting migration (non-destructive)

Application
- Add SplitExpenseUseCase: validates percentages sum to 100,
  replaces existing splits, persists
- Add CalculateBalanceUseCase: sums weighted amounts for member
  in period (logic in Application, not raw SQL)

Api
- POST /documents/{id}/split: 400 if sum≠100, 404 if not found
- GET /members/{id}/balance?from=&to=: balance for period
- GET /families/{familyId}/members: list members
- POST /families/{familyId}/members: create member

Tests
- SplitExpenseUseCase: sum=100 success, sum≠100 error
- CalculateBalanceUseCase: correct balance for given period

Docs
- Update architecture.md: splitting model and use cases
- Close TASK-021 (status: done)
```
