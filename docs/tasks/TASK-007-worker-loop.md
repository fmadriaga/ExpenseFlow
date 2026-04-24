# TASK-007 Worker loop

status: ready
owner: backend
priority: high

## Goal
Ejecutar el pipeline automáticamente en intervalos configurables.

## Context
El MVP necesita correr de manera periódica sin intervención explícita del usuario cada vez.

## Scope
- Implementar un `BackgroundService`
- Ejecutar el scanner y pipeline en un intervalo configurable
- Evitar que se superpongan dos ciclos si el anterior sigue corriendo
- Registrar inicio y fin de cada job

## Out of scope
- múltiples workers distribuidos
- colas
- escalado horizontal
- triggers cloud

## Acceptance Criteria
- existe un `BackgroundService` funcional
- el intervalo es configurable
- si un job sigue ejecutándose, no se lanza uno nuevo en paralelo
- se registra inicio y fin del job
- se registran métricas mínimas del ciclo:
  - archivos encontrados
  - archivos procesados
  - archivos fallidos

## Suggested Files
- `src/ExpenseFlow.Worker/Workers/...`
- `src/ExpenseFlow.Application/...`
- `src/ExpenseFlow.Infrastructure/...`

## Definition of Done
- build exitoso
- ejecución local verificable
- logs suficientes para seguir el ciclo completo
