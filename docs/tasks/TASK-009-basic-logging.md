# TASK-009 Basic logging

status: ready
owner: backend
priority: medium

## Goal
Agregar observabilidad mínima útil para operar y depurar el pipeline.

## Context
Como el proceso será automático, se necesita poder entender qué ocurrió en cada corrida y por cada archivo.

## Scope
- Logging por job
- Logging por archivo
- Niveles adecuados: Information, Warning, Error
- Correlation simple por `JobId`
- Mensajes claros para:
  - inicio del job
  - fin del job
  - duplicado
  - OCR fallido
  - persistencia fallida
  - archivo movido

## Out of scope
- stack de observabilidad completo
- tracing distribuido
- dashboards

## Acceptance Criteria
- cada corrida tiene un `JobId` o correlación equivalente
- los logs permiten seguir el flujo de un archivo de punta a punta
- errores incluyen contexto útil
- no se loguean secretos
- logging usable tanto en consola como en ejecución local

## Suggested Files
- `src/ExpenseFlow.Worker/...`
- `src/ExpenseFlow.Application/...`
- `src/ExpenseFlow.Infrastructure/...`

## Definition of Done
- build exitoso
- logs revisados manualmente con un caso de éxito y uno de error
- reviewer puede entender el flujo solo leyendo logs
