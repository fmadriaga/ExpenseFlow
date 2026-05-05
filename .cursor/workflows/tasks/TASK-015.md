# TASK-015 — Reintentos con backoff para OCR

Archivo de task: `docs/tasks/TASK-015-retry-backoff.md`

---

## PLANNER

Eres el Planner del proyecto ExpenseFlow. Produce un plan técnico para TASK-015.

Archivos a leer:
- docs/architecture.md
- docs/tasks/TASK-015-retry-backoff.md

Contexto: IReceiptOcrProvider está en Application. La implementación
AzureDocumentIntelligenceReceiptProvider está en Infrastructure y usa
el SDK de Azure. El Worker llama al provider y cualquier excepción va
directo a error/. No hay reintentos hoy.

Produce:
1. Plan de implementación paso a paso.
2. Lista de archivos a crear y modificar por capa.
3. Dependencias NuGet (evaluar Polly vs Microsoft.Extensions.Http.Resilience).
4. Riesgos o puntos a tener en cuenta.

Reglas: no escribas código, salida concreta y accionable.

---

## IMPLEMENTER

Eres el Backend Implementer de ExpenseFlow. Implementa TASK-015.

Lee primero:
- docs/architecture.md
- docs/tasks/TASK-015-retry-backoff.md
- .cursor/agents/backend-implementer.md

Implementa exactamente:
1. Agregar a AzureDocumentIntelligenceOptions:
   MaxRetries (default 3), BaseDelaySeconds (default 2).
2. Crear una política de reintentos en Infrastructure (no en Application):
   - Reintentar solo errores transitorios: HttpRequestException,
     TaskCanceledException (timeout), RequestFailedException con
     status 429 o 503.
   - Errores no transitorios (RequestFailedException con 400, 401, 415,
     etc.): fallar inmediatamente sin reintento.
   - Backoff exponencial: delay = BaseDelaySeconds * 2^(intento-1).
   - Log Warning en cada reintento: "Retrying OCR attempt N/M for {FileName},
     reason: {ExceptionType}".
3. Aplicar la política dentro de AzureDocumentIntelligenceReceiptProvider,
   no en el Worker ni en Application.
4. No exponer Polly ni tipos de resiliencia fuera de Infrastructure.
5. Tests:
   - Error transitorio → se reintenta hasta MaxRetries, luego falla.
   - Error no transitorio → falla inmediatamente (0 reintentos).
   - Log de reintento emitido correctamente.

Reglas: build y tests en verde. Preferir Polly si ya está disponible
en el stack; si no, usar Microsoft.Extensions.Http.Resilience.

---

## REVIEWER

Eres el Reviewer de ExpenseFlow. Revisa TASK-015.

Lee primero:
- docs/architecture.md
- docs/tasks/TASK-015-retry-backoff.md
- .cursor/agents/reviewer.md

Evalúa contra:
1. Criterios de aceptación de TASK-015.
2. La política de reintentos está en Infrastructure, no en Application ni Worker.
3. La distinción transitorio/no-transitorio es correcta y robusta.
4. El backoff es exponencial y configurable.
5. Polly o equivalente no se expone fuera de Infrastructure.
6. MaxRetries y BaseDelaySeconds son configurables.
7. Cobertura de tests.

Responde con hallazgos críticos, mejoras y decisión: approve / needs changes.
Guarda en: `docs/reviews/TASK-015-review.md`

---

## DOCS-KEEPER

Eres el Docs Keeper de ExpenseFlow. Cierra TASK-015 si el Reviewer aprobó.

1. Marca TASK-015 como done.
2. Actualiza docs/architecture.md: política de reintentos, dónde vive,
   qué errores reintenta, configuración.
3. Actualiza README.md: MaxRetries y BaseDelaySeconds en appsettings.
4. Resumen breve del cierre.

---

## COMMIT

```
feat(resilience): TASK-015 exponential backoff retry for OCR

Application
- Add MaxRetries (default 3) and BaseDelaySeconds (default 2)
  to AzureDocumentIntelligenceOptions

Infrastructure
- Add retry policy in AzureDocumentIntelligenceReceiptProvider:
  retries on HttpRequestException, TaskCanceledException,
  RequestFailedException 429/503; fails fast on 4xx errors
  Exponential backoff: delay = BaseDelaySeconds * 2^(attempt-1)
  Logs Warning per retry: attempt N/M, filename, exception type
- Policy stays inside Infrastructure (not exposed to Application)

Tests
- Add OcrRetryTests: transient error retried up to MaxRetries,
  non-transient error fails immediately, retry log emitted

Docs
- Update architecture.md: retry policy, transient vs non-transient
- Update README.md: MaxRetries/BaseDelaySeconds configuration
- Close TASK-015 (status: done)
```
