# Review TASK-015 — Reintentos OCR con backoff

## Acceptance criteria

| Criterio | Estado |
|----------|--------|
| Transitorios reintentados hasta el máximo | OK — `ExecuteWithRetryAsync` + 429/503/408/5xx, red, etc. |
| No transitorios fallan al primer error | OK — p. ej. 400 |
| Cada reintento en `LogWarning` con intento, máximo e intervalo | OK |
| Tras agotar reintentos, flujo de error existente | OK — excepción a `ExpenseFlowWorker` |
| Backoff exponencial y opciones configurables | OK — `MaxRetries`, `BaseDelaySeconds` + `appsettings` |
| Lógica en Infrastructure, no en Worker | OK — `OcrAnalysisRetryHelper` + `AzureDocumentIntelligenceReceiptProvider` |

## Tests

- Reintentos hasta éxito; 400 sin reintentos; clasificador 429/400.

**Veredicto:** aprobado.
