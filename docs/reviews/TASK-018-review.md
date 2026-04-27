# Review TASK-018 — Observabilidad avanzada (Worker)

## Acceptance criteria

| Criterio | Estado |
|----------|--------|
| Log a archivo rotativo | OK — Serilog.Sinks.File, `rollingInterval: Day` |
| Retención N días | OK — `retainedFileCountLimit: 14` (ajustable en `Serilog:WriteTo` args) |
| Consola se mantiene | OK — `Serilog` + sink `Console` + `ClearProviders` + `AddSerilog` |
| Métricas con dotnet-counters | OK — `Meter` `ExpenseFlow.Worker` + contadores `files.found` / `processed_ok` / `processed_failed` |
| Nombre/retención sin recompilar | OK — ruta y límite en `appsettings.json` |
| Sin secretos en log adicional | OK — misma plantilla, sin claves |

**Veredicto:** aprobado.
