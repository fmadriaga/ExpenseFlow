# TASK-018 Observabilidad avanzada

status: done
owner: backend
priority: low

## Goal
Agregar logging persistente a archivo y métricas básicas de ciclo
para facilitar el diagnóstico sin necesidad de consola abierta.

## Context
Los logs actuales van solo a consola. Si el Worker corre en segundo
plano o como servicio del sistema, los logs de ciclos anteriores se
pierden. Se necesita al menos un sink de archivo para retención mínima.

## Scope
- Integrar Serilog con sink a archivo (rolling diario, retención configurable)
- Mantener el sink de consola existente
- Exponer métricas mínimas del ciclo como contadores (archivos encontrados,
  procesados, fallidos) usando System.Diagnostics.Metrics
- Configurar niveles y retención de archivo en appsettings

## Out of scope
- OpenTelemetry / trazas distribuidas
- Dashboards (Grafana, Datadog, etc.)
- Alertas automáticas
- Sink a servicios cloud

## Acceptance Criteria
- Los logs se escriben en un archivo rotativo (por ejemplo logs/expenseflow-.log)
- El archivo rota diariamente y se retienen N días (configurable)
- Los logs de consola siguen funcionando como antes
- Las métricas de ciclo son observables con dotnet-counters o equivalente
- El nombre del archivo de log y la retención son configurables sin
  recompilar
- No se loguean secretos en el archivo (misma política que consola)

## Technical Notes
- Usar Serilog.Sinks.File en el Worker; no introducir Serilog en
  capas Application o Domain
- System.Diagnostics.Metrics para contadores: usar un Meter nombrado
  (por ejemplo "ExpenseFlow.Worker") con Counter<long> por tipo de evento
- La configuración de Serilog puede ir en appsettings bajo "Serilog"

## Suggested Files
- src/ExpenseFlow.Worker/Program.cs
- src/ExpenseFlow.Worker/appsettings.json
- src/ExpenseFlow.Worker/Workers/ExpenseFlowWorker.cs

## Definition of Done
- build exitoso
- archivo de log generado y rotativo verificado localmente
- métricas visibles con dotnet-counters monitor
