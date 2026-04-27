# TASK-021 División de gastos familiares

status: done
owner: backend
priority: low

## Goal
Permitir asignar un porcentaje de cada gasto a cada miembro de la
familia y calcular balances periódicos.

## Context
Dentro de una familia puede haber gastos compartidos en proporciones
distintas. Esta funcionalidad permite registrar quién pagó y en qué
porcentaje corresponde el gasto a cada miembro, para luego calcular
quién debe a quién.

## Scope
- Entidad FamilyMember (Id, FamilyId, Name)
- Entidad ExpenseSplit (Id, DocumentId, FamilyMemberId, Percentage)
- Endpoint POST /documents/{id}/split para registrar la división
- Endpoint GET /members/{id}/balance?from=&to= para calcular balance
  del período: cuánto pagó, cuánto le corresponde, diferencia
- Validar que los porcentajes de un documento sumen 100%

## Out of scope
- Pagos o transferencias entre miembros
- Notificaciones
- División automática por reglas
- UI (usar endpoints desde TASK-019 en iteración futura)

## Acceptance Criteria
- Se puede registrar la división de un gasto en porcentajes
- Los porcentajes de un documento suman exactamente 100%
- El balance de un miembro para un período es correcto
- Un gasto sin división asignada no aparece en balances
- Migración aplicada correctamente

## Technical Notes
- Percentage como decimal(5,2) en BD
- Validación de suma 100% en Application antes de persistir
- El balance se calcula en Application, no en SQL, para mantener
  la lógica testeable
- Depende de TASK-020 (FamilyId) si se requiere multi-familia;
  puede implementarse antes si Family tiene un solo perfil default

## Suggested Files
- src/ExpenseFlow.Domain/Entities/FamilyMember.cs
- src/ExpenseFlow.Domain/Entities/ExpenseSplit.cs
- src/ExpenseFlow.Application/UseCases/SplitExpenseUseCase.cs
- src/ExpenseFlow.Application/UseCases/CalculateBalanceUseCase.cs
- src/ExpenseFlow.Infrastructure/Migrations/...
- src/ExpenseFlow.Api/Endpoints/...

## Definition of Done
- build exitoso
- tests unitarios de validación de suma 100% y cálculo de balance
- migración aplicada
- endpoints verificados manualmente

---

## Resultado
- **Domain:** `FamilyMember`, `ExpenseSplit`, `Document.PaidByFamilyMemberId` (FK opcional a `FamilyMembers`), colección `ExpenseSplits`.
- **Application:** `SplitExpensePercentageValidator`, `MemberPeriodBalanceCalculator` + DTOs `SetDocumentSplitRequestDto`; porcentajes sumados redondeados a 2 decimales deben ser 100.
- **API:** `POST /documents/{id}/split?familyId=1` cuerpo JSON `paidByFamilyMemberId` + `splits[]` (miembro y `percentage`); `GET /members/{memberId}/balance?familyId=1&from=YYYY-MM-DD&to=YYYY-MM-DD` (solo documentos `Success` con reparto y fecha en rango).
- **Balance:** `totalPaid` si `PaidByFamilyMemberId` coincide; `totalShare` = Σ (`TotalAmount` × %/100) por documento; `net` = `totalPaid` − `totalShare`. Documentos sin `TotalAmount` no aportan importes.
- **Migración:** `AddFamilyMembersExpenseSplitsPaidBy`.
- **Tests unitarios** en `ExpenseFlow.Application.Tests/Splitting/`.

## Archivos principales
- `src/ExpenseFlow.Domain/Entities/FamilyMember.cs`, `ExpenseSplit.cs`, `Document.cs`, `Family.cs`
- `src/ExpenseFlow.Application/Splitting/*.cs`, `DTOs/SetDocumentSplitRequestDto.cs`
- `src/ExpenseFlow.Api/Endpoints/DocumentsEndpoints.cs` (split), `MembersEndpoints.cs` (balance)
- `src/ExpenseFlow.Infrastructure/Migrations/20260427211715_AddFamilyMembersExpenseSplitsPaidBy.cs`

## Pendientes
- CRUD de miembros por API (hoy insert en BD o migración manual).

**Cierre:** 2026-04-27
