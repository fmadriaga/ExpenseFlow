# TASK-017 Tests de validación de configuración

status: done
owner: backend
priority: low

## Goal
Agregar tests que verifiquen que la aplicación lanza OptionsValidationException
con mensaje claro cuando falta cada campo de configuración crítico.

## Context
La validación al arranque existe desde TASK-008 pero no tiene cobertura
automatizada. Sin tests, un refactor de las opciones o del registro DI
podría romper silenciosamente el comportamiento de fail-fast.

## Scope
- Test por cada campo requerido de StorageOptions (Inbox, Processed, Error)
- Test por cada campo requerido de AzureDocumentIntelligenceOptions
  (Endpoint, ApiKey)
- Test para WorkerOptions con IntervalSeconds = 0 (fuera de rango)
- Test para ConnectionStrings:ExpenseFlow ausente
- Cada test verifica que se lanza la excepción correcta con mensaje
  que identifica el campo faltante

## Out of scope
- Tests de validación semántica (eso es TASK-016)
- Tests de conectividad real
- Tests de otros campos no críticos

## Acceptance Criteria
- Un test por cada campo crítico ausente
- Cada test verifica tipo de excepción y que el mensaje menciona
  el campo o sección correspondiente
- Los tests no requieren Azure ni SQLite real (host en memoria)
- Los tests existentes no se rompen

## Technical Notes
- Usar WebApplicationFactory o IHostBuilder en modo test con config
  sobreescrita para forzar el campo ausente
- Verificar OptionsValidationException o InvalidOperationException
  según el campo (Connection String lanza tipo diferente)
- Mantener cada test independiente y rápido (sin I/O real)

## Suggested Files
- tests/ExpenseFlow.IntegrationTests/ConfigurationValidationTests.cs

## Definition of Done
- build exitoso
- todos los tests en verde
- los tests fallan si se elimina la anotación [Required] del campo
  correspondiente (verificar manualmente o con mutation)
