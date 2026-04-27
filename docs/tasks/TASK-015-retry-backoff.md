# TASK-015 Reintentos con backoff para OCR

status: done
owner: backend
priority: medium

## Goal
Implementar reintentos automáticos con backoff exponencial para fallos
transitorios del proveedor OCR (timeout, error 429, error de red).

## Context
Actualmente cualquier fallo de OCR mueve el archivo a error/ sin
reintento. Errores transitorios de red o cuotas de Azure son recuperables
y no deberían causar fallos permanentes.

## Scope
- Agregar política de reintentos en la llamada a IReceiptOcrProvider
- Reintentar solo errores transitorios (timeout, 429, 503) — no errores
  de contenido (archivo inválido, formato no soportado)
- Máximo de reintentos y backoff base configurables en opciones
- Registrar cada reintento con nivel Warning (intento N de M, motivo)
- Después de agotar reintentos, continuar con el flujo de error existente

## Out of scope
- Reintentos para persistencia o movimiento de archivos
- Circuit breaker
- Reintentos distribuidos o con cola

## Acceptance Criteria
- Fallos transitorios se reintentan hasta el máximo configurado
- Errores no transitorios fallan inmediatamente sin reintento
- Cada reintento queda registrado en logs con número de intento y excepción
- Al agotar reintentos, el archivo va a error/ como antes
- El intervalo entre reintentos crece con backoff exponencial
- MaxRetries y BaseDelaySeconds son configurables sin recompilar

## Technical Notes
- Usar Microsoft.Extensions.Http.Resilience (Polly integrado en .NET 8+)
  o Polly directamente en Infrastructure
- La política se aplica en la capa Infrastructure al invocar el cliente Azure,
  no en el Worker
- Distinguir HttpRequestException / TaskCanceledException (transitorio) de
  errores de parsing o formato (no transitorio)
- No exponer Polly ni tipos de resiliencia fuera de Infrastructure

## Suggested Files
- src/ExpenseFlow.Infrastructure/Ocr/AzureDocumentIntelligenceReceiptProvider.cs
- src/ExpenseFlow.Application/Options/AzureDocumentIntelligenceOptions.cs
- src/ExpenseFlow.Infrastructure/DependencyInjection.cs

## Definition of Done
- build exitoso
- test que simula un fallo transitorio y verifica reintentos y logs
- test que simula error no transitorio y verifica que no se reintenta
