# Review TASK-017 — Tests de validación de configuración

## Acceptance criteria

| Criterio | Estado |
|----------|--------|
| Test por campo crítico Storage / Azure / Worker | OK — valores vacíos donde `[Required]`/`Range` |
| ConnectionStrings ausente | OK — `InvalidOperationException` |
| Sin Azure/SQLite real | OK — host mínimo + `Host.StartAsync` |
| Mensaje identifica campo/sección | OK — fragmentos `Storage`, `AzureDocumentIntelligence`, `Worker`, `ConnectionStrings` |

**Veredicto:** aprobado.
