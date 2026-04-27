# Review TASK-016 — Validación de Endpoint Azure (URI)

## Acceptance criteria

| Criterio | Estado |
|----------|--------|
| HTTP/HTTPS absoluto pasa | OK — `IValidateOptions` + `Uri.TryCreate` |
| Vacío / espacios / texto no URI falla con mensaje claro | OK — sin ApiKey en mensaje |
| ValidateOnStart (cadena de opciones existente) | OK — `AddOcrProviders` |
| Development: URI de ejemplo válida | OK — `https://placeholder.invalid` |
| Tests | OK — `AzureDocumentIntelligenceOptionsValidatorTests` |

**Veredicto:** aprobado.
