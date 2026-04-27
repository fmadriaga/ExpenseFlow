# TASK-006 File move and dedup

status: done
owner: backend
priority: high

## Cierre documental

- **Entregado:** contrato `IFileMover`, implementación `FileMover`, registro `AddFileStorage`, tests de integración de movimiento/colisiones/árbol de directorios.
- **Pendiente (TASK-007):** invocar `IFileMover` desde la orquestación del Worker tras éxito o fallo de procesamiento (criterios de aceptación end-to-end del flujo batch).

## Goal
Mover archivos a carpetas finales y evitar reprocesos.

## Context
El flujo debe ser idempotente. Un archivo ya procesado no debe volver a insertarse por error ni quedar repetido si reaparece en `inbox`.

## Scope
- Mover archivos procesados a `processed/yyyy/MM/`
- Mover archivos fallidos a `error/yyyy/MM/`
- Detectar duplicados por `FileHash`
- Registrar claramente el resultado del procesamiento
- Manejar colisiones de nombres al mover archivos

## Out of scope
- reintento manual
- API
- UI

## Acceptance Criteria
- al procesarse correctamente, el archivo se mueve a `processed/yyyy/MM/`
- al fallar, el archivo se mueve a `error/yyyy/MM/`
- si el hash ya existe, el documento no se reprocesa
- el movimiento no pierde el archivo original
- si el destino ya existe, se resuelve el conflicto de nombre de forma segura
- logs claros por éxito, error y duplicado

## Suggested Files
- `src/ExpenseFlow.Application/Abstractions/IFileMover.cs`
- `src/ExpenseFlow.Infrastructure/Storage/...`
- `src/ExpenseFlow.Worker/...`

## Definition of Done
- build exitoso
- tests o casos cubiertos para duplicados y movimientos
- comportamiento manualmente verificable con archivos de prueba
