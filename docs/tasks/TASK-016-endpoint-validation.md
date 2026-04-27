# TASK-016 Validación semántica del Endpoint de Azure

status: done
owner: backend
priority: low

## Goal
Validar que AzureDocumentIntelligenceOptions.Endpoint es una URI
HTTP/HTTPS válida al arranque, además de verificar que no está vacío.

## Context
La validación actual de TASK-008 solo verifica que Endpoint no es vacío.
Un placeholder inválido como "placeholder.invalid" pasa la validación
pero falla en runtime al intentar conectar. Un fail-fast semántico
ahorra tiempo de diagnóstico en despliegues reales.

## Scope
- Agregar validación de formato URI (HTTP o HTTPS) sobre Endpoint
- El mensaje de error debe indicar qué valor se recibió y qué formato
  se esperaba, sin exponer la key
- La validación debe ejecutarse al arranque (ValidateOnStart)

## Out of scope
- Validación de conectividad real (ping al endpoint)
- Validación de ApiKey por formato o longitud
- Cambios en otras opciones

## Acceptance Criteria
- Un Endpoint con formato URI HTTP/HTTPS válido pasa la validación
- Un Endpoint vacío, con solo espacios o con texto plano no URI
  falla al arranque con OptionsValidationException y mensaje claro
- Los tests de integración con placeholders de Development siguen
  en verde (el placeholder de Development debe actualizarse a una
  URI válida de ejemplo: https://placeholder.invalid)
- appsettings.Development.json actualizado con placeholder URI válido

## Technical Notes
- Usar Uri.TryCreate + UriKind.Absolute + esquema http/https
- Implementar como IValidateOptions<AzureDocumentIntelligenceOptions>
  o como atributo personalizado según convención del repo
- Actualizar appsettings.Development.json: "Endpoint": "https://placeholder.invalid"

## Suggested Files
- src/ExpenseFlow.Application/Options/AzureDocumentIntelligenceOptions.cs
- src/ExpenseFlow.Infrastructure/Configuration/... (si se usa IValidateOptions)
- src/ExpenseFlow.Worker/appsettings.Development.json

## Definition of Done
- build exitoso
- test: endpoint vacío → falla; endpoint texto plano → falla;
  endpoint https://... → pasa
- appsettings.Development.json con placeholder URI válido
