# TASK-018 — Observabilidad avanzada

Archivo de task: `docs/tasks/TASK-018-advanced-observability.md`

---

## PLANNER

Eres el Planner del proyecto ExpenseFlow. Produce un plan técnico para TASK-018.

Archivos a leer:
- docs/architecture.md
- docs/tasks/TASK-018-advanced-observability.md

Contexto: Logging actual usa Microsoft.Extensions.Logging con sink de consola
y JobId de correlación (TASK-009). Serilog no está en el stack. No hay métricas
ni logging a archivo.

Produce:
1. Plan de implementación paso a paso.
2. Lista de archivos a crear y modificar por capa.
3. Dependencias NuGet (Serilog paquetes específicos).
4. Riesgos o puntos a tener en cuenta.

Reglas: no escribas código, salida concreta y accionable.

---

## IMPLEMENTER

Eres el Backend Implementer de ExpenseFlow. Implementa TASK-018.

Lee primero:
- docs/architecture.md
- docs/tasks/TASK-018-advanced-observability.md
- .cursor/agents/backend-implementer.md

Implementa exactamente:
1. Agregar paquetes al Worker:
   - Serilog.AspNetCore (o Serilog.Extensions.Hosting)
   - Serilog.Sinks.File
   - Serilog.Sinks.Console (para mantener consola enriquecida)
2. Configurar Serilog en Program.cs del Worker:
   - Leer configuración desde sección "Serilog" en appsettings.
   - Sink de consola: mantener comportamiento actual.
   - Sink de archivo: rolling diario, ruta logs/expenseflow-.log,
     retención configurable (default 7 días).
   - No introducir Serilog en Application, Domain ni Infrastructure.
3. Configurar la sección "Serilog" en appsettings.json:
   - MinimumLevel: Information por defecto.
   - Override Microsoft/System a Warning.
   - WriteTo: Console + File con rollingInterval: Day y retainedFileCount.
   - FilePath configurable (default "logs/expenseflow-.log").
4. Agregar métricas con System.Diagnostics.Metrics en ExpenseFlowWorker:
   - Meter: "ExpenseFlow.Worker".
   - Counter<long> "files.found".
   - Counter<long> "files.processed".
   - Counter<long> "files.failed".
   - Incrementar contadores al fin de cada ciclo.
5. Verificar que no se loguean secretos en el archivo (misma política).
6. No agregar Serilog a proyectos que no sean Worker.

Reglas: build en verde. Verificar que el archivo de log se genera localmente.
Verificar que los tests existentes siguen en verde (pueden necesitar ajuste
si capturan logs de consola).

---

## REVIEWER

Eres el Reviewer de ExpenseFlow. Revisa TASK-018.

Lee primero:
- docs/architecture.md
- docs/tasks/TASK-018-advanced-observability.md
- .cursor/agents/reviewer.md

Evalúa contra:
1. Criterios de aceptación de TASK-018.
2. Serilog solo en Worker (no en otras capas).
3. El archivo de log existe y rota diariamente.
4. Retención configurable (no hardcodeada).
5. Las métricas usan System.Diagnostics.Metrics (no Serilog ni librerías extra).
6. No se exponen secretos en el archivo de log.
7. Los tests existentes siguen en verde.

Responde con hallazgos críticos, mejoras y decisión: approve / needs changes.
Guarda en: `docs/reviews/TASK-018-review.md`

---

## DOCS-KEEPER

Eres el Docs Keeper de ExpenseFlow. Cierra TASK-018 si el Reviewer aprobó.

1. Marca TASK-018 como done.
2. Actualiza docs/architecture.md: Serilog, sinks, métricas con
   System.Diagnostics.Metrics, Meter name.
3. Actualiza README.md: dónde encontrar los logs de archivo, cómo
   ver métricas con dotnet-counters monitor --name ExpenseFlow.Worker.
4. Resumen breve del cierre.

---

## COMMIT

```
feat(observability): TASK-018 file logging with Serilog and cycle metrics

Worker
- Add Serilog with Console + rolling File sinks (daily rotation)
- Log file path: logs/expenseflow-.log, retention configurable
  via Serilog:WriteTo:File:retainedFileCount (default 7)
- Configure Serilog from appsettings "Serilog" section
- Serilog scoped to Worker only (not in Application/Infrastructure)
- Add System.Diagnostics.Metrics: Meter "ExpenseFlow.Worker" with
  counters files.found, files.processed, files.failed
  incremented at end of each cycle

Packages added to Worker:
  Serilog.Extensions.Hosting
  Serilog.Sinks.File
  Serilog.Sinks.Console

Docs
- Update architecture.md: Serilog setup, metrics Meter
- Update README.md: log file location, dotnet-counters usage
- Close TASK-018 (status: done)
```
