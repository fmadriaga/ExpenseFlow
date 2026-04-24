# TASK-005 Receipt normalizer

status: ready
owner: backend
priority: high

## Goal
Transformar la salida del provider OCR en un modelo interno consistente para persistencia.

## Context
Ya existe o existirá un provider OCR para tickets, pero la respuesta del proveedor no debe propagarse al dominio ni a la base sin una capa de normalización intermedia.

## Scope
- Crear un normalizador para mapear la respuesta OCR a `Document` y `DocumentLines`.
- Persistir el JSON crudo como `RawJson`.
- Calcular `Confidence` y `OcrStatus`.
- Mapear, cuando existan:
  - comercio
  - fecha
  - moneda
  - total
  - impuestos
  - líneas

## Out of scope
- categorización de gastos
- UI
- exportación CSV
- reproceso

## Acceptance Criteria
- existe una clase o servicio `ReceiptNormalizer`
- el provider OCR no expone DTOs externos fuera de Infrastructure/Application
- se mapean correctamente comercio, fecha, moneda, total e impuestos
- si existen líneas, se crean `DocumentLines`
- se guarda `RawJson`
- se define `Confidence` y `OcrStatus`
- si falta información clave, el documento no rompe el flujo y queda con estado coherente

## Suggested Files
- `src/ExpenseFlow.Application/Abstractions/...`
- `src/ExpenseFlow.Application/Services/ReceiptNormalizer.cs`
- `src/ExpenseFlow.Domain/...`
- `src/ExpenseFlow.Infrastructure/...`

## Definition of Done
- build exitoso
- tests unitarios básicos del normalizador
- docs mínimas actualizadas si cambia arquitectura
