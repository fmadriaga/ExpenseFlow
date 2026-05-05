# TASK-016 — Validación semántica del Endpoint de Azure

Archivo de task: `docs/tasks/TASK-016-endpoint-validation.md`

---

## PLANNER

Eres el Planner del proyecto ExpenseFlow. Produce un plan técnico para TASK-016.

Archivos a leer:
- docs/architecture.md
- docs/tasks/TASK-016-endpoint-validation.md

Contexto: AzureDocumentIntelligenceOptions tiene validación [Required] en
Endpoint y ApiKey desde TASK-008. El placeholder en appsettings.Development.json
es "placeholder.invalid" — no es una URI válida, lo que puede causar errores
confusos en runtime.

Produce:
1. Plan de implementación paso a paso.
2. Lista de archivos a crear y modificar por capa.
3. Riesgos o puntos a tener en cuenta.

Reglas: no escribas código, salida concreta y accionable.

---

## IMPLEMENTER

Eres el Backend Implementer de ExpenseFlow. Implementa TASK-016.

Lee primero:
- docs/architecture.md
- docs/tasks/TASK-016-endpoint-validation.md
- .cursor/agents/backend-implementer.md

Implementa exactamente:
1. Agregar validación semántica de Endpoint en la clase de validación de
   AzureDocumentIntelligenceOptions (IValidateOptions o DataAnnotations):
   - Uri.TryCreate(Endpoint, UriKind.Absolute, out var uri) → válido.
   - uri.Scheme debe ser "http" o "https".
   - Si no cumple: OptionsValidationException con mensaje:
     "AzureDocumentIntelligence:Endpoint must be a valid HTTP/HTTPS URI.
      Received: {valor_recibido}" (nunca exponer ApiKey en el mensaje).
2. Actualizar appsettings.Development.json:
   "Endpoint": "https://placeholder.invalid"
   (URI técnicamente válida como placeholder, sin credenciales reales).
3. Tests:
   - Endpoint vacío → falla (ya cubría [Required]).
   - Endpoint "texto plano" → falla con nueva validación.
   - Endpoint "ftp://something" → falla (esquema inválido).
   - Endpoint "https://placeholder.invalid" → pasa.
   - Endpoint "http://localhost" → pasa.

Reglas: build y tests en verde. Verificar que los tests de integración
existentes siguen en verde con el nuevo placeholder.

---

## REVIEWER

Eres el Reviewer de ExpenseFlow. Revisa TASK-016.

Lee primero:
- docs/architecture.md
- docs/tasks/TASK-016-endpoint-validation.md
- .cursor/agents/reviewer.md

Evalúa contra:
1. Criterios de aceptación de TASK-016.
2. El mensaje de error identifica el campo pero no expone ApiKey.
3. La validación se ejecuta al arranque (ValidateOnStart).
4. appsettings.Development.json actualizado con URI válida.
5. Tests existentes de integración siguen en verde.
6. Cobertura de los 5 casos de test.

Responde con hallazgos críticos, mejoras y decisión: approve / needs changes.
Guarda en: `docs/reviews/TASK-016-review.md`

---

## DOCS-KEEPER

Eres el Docs Keeper de ExpenseFlow. Cierra TASK-016 si el Reviewer aprobó.

1. Marca TASK-016 como done.
2. Actualiza docs/architecture.md: validación semántica de Endpoint.
3. Resumen breve del cierre.

---

## COMMIT

```
fix(config): TASK-016 semantic URI validation for Azure Endpoint

Application / Infrastructure
- Add URI format validation for AzureDocumentIntelligenceOptions.Endpoint:
  must be absolute HTTP/HTTPS URI; fails at startup with clear message
  showing received value (never exposes ApiKey)

Config
- Update appsettings.Development.json Endpoint placeholder to
  "https://placeholder.invalid" (valid URI format, not real credentials)

Tests
- Add EndpointValidationTests: empty fails, plain text fails,
  ftp:// fails, https://placeholder.invalid passes, http://localhost passes

Docs
- Update architecture.md: semantic Endpoint validation
- Close TASK-016 (status: done)
```
