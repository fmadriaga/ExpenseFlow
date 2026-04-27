# TASK-011 Endpoint de reproceso manual

status: done
owner: backend
priority: medium

## Goal
Permitir marcar un documento fallido para que el Worker lo reintente
en el próximo ciclo de procesamiento.

## Context
Actualmente los archivos que fallan se mueven a error/yyyy/MM/ y no
vuelven a procesarse. Se necesita un mecanismo manual para reactivar
documentos sin intervención directa en el filesystem.

## Scope
- Endpoint POST /documents/{id}/reprocess
- Validar que el documento existe y tiene OcrStatus = Failed o Partial
- Actualizar OcrStatus a un estado que el Worker reconozca como pendiente
  de reproceso (por ejemplo, OcrStatus = Pending)
- Mover el archivo de vuelta a inbox desde error/ (si el archivo existe)
- El Worker recogerá el archivo en el próximo ciclo por hash no registrado
  como Success

## Out of scope
- Reproceso automático / reintentos con backoff (TASK-015)
- Cambio de proveedor OCR en el reproceso
- Historial de intentos detallado

## Acceptance Criteria
- POST /documents/{id}/reprocess devuelve 200 si el documento puede reactivarse
- Devuelve 404 si el documento no existe
- Devuelve 422 si el documento ya tiene OcrStatus = Success
- El OcrStatus del documento se actualiza correctamente en BD
- Si el archivo existe en error/, se mueve de vuelta a inbox/
- Si el archivo no existe en error/, se registra advertencia pero la
  operación no falla
- El Worker procesa el documento reactivado en el siguiente ciclo

## Technical Notes
- Agregar OcrStatus = Pending si no existe todavía
- El movimiento de vuelta a inbox debe respetar la misma seguridad
  de IFileMover (sin sobreescrituras)
- Considerar introducir IFileRestorer en Application o extender IFileMover

## Suggested Files
- src/ExpenseFlow.Api/Endpoints/DocumentsEndpoints.cs
- src/ExpenseFlow.Application/Abstractions/IFileRestorer.cs
- src/ExpenseFlow.Infrastructure/Storage/FileRestorer.cs
- src/ExpenseFlow.Application/Ocr/ReceiptOcrStatuses.cs

## Definition of Done
- build exitoso
- endpoint verificado con documento en estado Failed
- Worker procesa el documento reactivado en el ciclo siguiente
