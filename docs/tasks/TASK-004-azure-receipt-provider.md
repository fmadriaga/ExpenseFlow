# TASK-004 Azure receipt provider

status: done
owner: backend
priority: high

## Goal
Implementar el proveedor OCR para tickets usando Azure Document Intelligence Receipt.

## Context
El MVP necesita una integración inicial con un proveedor que devuelva información
estructurada para tickets, reduciendo al mínimo el parsing manual.

## Scope
- Crear interfaz `IReceiptOcrProvider` en Application
- Implementar `AzureDocumentIntelligenceReceiptProvider` en Infrastructure
- Mapear comercio, fecha, total, impuestos y líneas si están disponibles
- Manejar errores y logging de fallos

## Acceptance Criteria
- Existe la interfaz `IReceiptOcrProvider`
- Existe la implementación Azure en Infrastructure
- El provider devuelve un resultado normalizado utilizable por el Worker
- Se registra logging útil ante errores de OCR
- La configuración de endpoint y key queda externalizada

## Out of Scope
- Reintentos avanzados
- Fallback a otros OCR providers
- Clasificación de gastos

## Technical Notes
- No exponer tipos del SDK de Azure fuera de Infrastructure
- Preservar respuesta cruda en una estructura serializable
- Dejar espacio para agregar otros providers más adelante

## Cierre documental (Docs Keeper)

- Estado: `status: done` (confirmado tras aprobación de Reviewer).
- Implementado: contrato `IReceiptOcrProvider` + DTO `OcrResult` en Application; provider
  `AzureDocumentIntelligenceReceiptProvider` en Infrastructure; opciones tipadas
  `AzureDocumentIntelligenceOptions`; registro DI con `AddOcrProviders`; test de mapeo
  (`AzureReceiptResultMapperTests`) sin llamada real a Azure.
- Pendiente para tasks futuras (fuera de alcance de TASK-004): invocación del provider dentro
  del flujo del Worker, normalización/persistencia completa del OCR en pipeline, y soporte para
  providers alternativos.
