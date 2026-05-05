# TASK-017 — Tests de validación de configuración

Archivo de task: `docs/tasks/TASK-017-config-validation-tests.md`

---

## PLANNER

Eres el Planner del proyecto ExpenseFlow. Produce un plan técnico para TASK-017.

Archivos a leer:
- docs/architecture.md
- docs/tasks/TASK-017-config-validation-tests.md

Contexto: La validación al arranque existe desde TASK-008 y TASK-016.
ValidateOnStart está configurado. No hay tests automatizados que verifiquen
que la app falla cuando falta cada campo crítico.

Produce:
1. Plan de implementación paso a paso.
2. Lista de archivos a crear y modificar.
3. Riesgos o puntos a tener en cuenta.

Reglas: no escribas código, salida concreta y accionable.

---

## IMPLEMENTER

Eres el Backend Implementer de ExpenseFlow. Implementa TASK-017.

Lee primero:
- docs/architecture.md
- docs/tasks/TASK-017-config-validation-tests.md
- .cursor/agents/backend-implementer.md

Implementa exactamente los siguientes tests en
tests/ExpenseFlow.IntegrationTests/ConfigurationValidationTests.cs:

1. StorageOptions.Inbox ausente → OptionsValidationException.
2. StorageOptions.Processed ausente → OptionsValidationException.
3. StorageOptions.Error ausente → OptionsValidationException.
4. AzureDocumentIntelligenceOptions.Endpoint ausente → OptionsValidationException.
5. AzureDocumentIntelligenceOptions.Endpoint no URI → OptionsValidationException.
6. AzureDocumentIntelligenceOptions.ApiKey ausente → OptionsValidationException.
7. WorkerOptions.IntervalSeconds = 0 → OptionsValidationException.
8. ConnectionStrings:ExpenseFlow ausente → InvalidOperationException con
   mensaje que menciona "ExpenseFlow".

Para cada test:
- Construir un IHost con config mínima válida, sobreescribiendo solo
  el campo bajo prueba con valor inválido o ausente.
- Verificar que Build() o Start() lanza la excepción esperada.
- Verificar que el mensaje de la excepción menciona el campo o sección.
- El test no necesita Azure ni SQLite real.

Reglas: build y todos los tests (nuevos + existentes) en verde.

---

## REVIEWER

Eres el Reviewer de ExpenseFlow. Revisa TASK-017.

Lee primero:
- docs/architecture.md
- docs/tasks/TASK-017-config-validation-tests.md
- .cursor/agents/reviewer.md

Evalúa contra:
1. Criterios de aceptación de TASK-017.
2. Un test por cada campo crítico (8 tests mínimos).
3. Cada test es independiente (no comparte estado con otros).
4. Los tests son rápidos (sin I/O real a Azure o SQLite).
5. Los tests fallarían si se elimina la anotación [Required] del campo
   (verificar que realmente ejercen la validación).
6. Los tests existentes siguen en verde.

Responde con hallazgos críticos, mejoras y decisión: approve / needs changes.
Guarda en: `docs/reviews/TASK-017-review.md`

---

## DOCS-KEEPER

Eres el Docs Keeper de ExpenseFlow. Cierra TASK-017 si el Reviewer aprobó.

1. Marca TASK-017 como done.
2. Actualiza README.md: mencionar que la validación de config tiene
   cobertura de tests automatizada.
3. Resumen breve del cierre.

---

## COMMIT

```
test(config): TASK-017 startup configuration validation tests

Tests
- Add ConfigurationValidationTests (8 tests):
  StorageOptions: Inbox/Processed/Error missing → OptionsValidationException
  AzureOptions: Endpoint missing → exception, Endpoint non-URI → exception,
    ApiKey missing → exception
  WorkerOptions: IntervalSeconds=0 → exception
  ConnectionStrings: ExpenseFlow missing → InvalidOperationException
  Each test verifies exception type and message mentions the field
  All tests run without real Azure or SQLite I/O

Docs
- Update README.md: config validation test coverage note
- Close TASK-017 (status: done)
```
