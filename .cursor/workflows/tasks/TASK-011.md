# TASK-011 — Endpoint de reproceso manual

Archivo de task: `docs/tasks/TASK-011-reprocess-endpoint.md`

---

## PLANNER

Eres el Planner del proyecto ExpenseFlow. Lee los archivos indicados y produce
un plan técnico para ejecutar TASK-011.

Archivos a leer:
- docs/architecture.md
- docs/tasks/TASK-011-reprocess-endpoint.md

Contexto: TASK-010 completada. Existen endpoints GET /documents y
GET /documents/{id}. IFileMover tiene MoveToProcessedAsync y MoveToErrorAsync.
OcrStatus tiene valores Success, Partial, Failed definidos en ReceiptOcrStatuses.

Produce:
1. Plan de implementación paso a paso.
2. Lista de archivos a crear y modificar por capa.
3. Dependencias NuGet si aplica.
4. Riesgos o puntos a tener en cuenta.

Reglas: no escribas código, salida concreta y accionable.

---

## IMPLEMENTER

Eres el Backend Implementer de ExpenseFlow. Implementa TASK-011.

Lee primero:
- docs/architecture.md
- docs/tasks/TASK-011-reprocess-endpoint.md
- .cursor/agents/backend-implementer.md

Implementa exactamente:
1. Agregar constante OcrStatus = "Pending" a ReceiptOcrStatuses si no existe.
2. Definir IFileRestorer en Application/Abstractions/ con:
   RestoreToInboxAsync(string sourcePath, CancellationToken) → ruta destino.
3. Implementar FileRestorer en Infrastructure/Storage/ que:
   - Mueva el archivo desde error/ de vuelta a inbox/.
   - Resuelva colisiones de nombre (mismo mecanismo que FileMover).
   - Registre log de éxito o advertencia si el archivo no existe.
4. Registrar IFileRestorer en AddFileStorage().
5. Endpoint POST /documents/{id}/reprocess en DocumentsEndpoints:
   - 404 si el documento no existe.
   - 422 si OcrStatus = Success (ya procesado correctamente).
   - Actualiza OcrStatus a Pending y limpia ErrorMessage en BD.
   - Llama a IFileRestorer.RestoreToInboxAsync si el archivo existe en error/.
   - Si el archivo no existe, solo actualiza BD con advertencia en log.
   - Devuelve 200 con mensaje confirmando la reactivación.
6. Tests:
   - POST /documents/{id}/reprocess con documento Failed → 200 y OcrStatus=Pending.
   - POST /documents/{id}/reprocess con documento Success → 422.
   - POST /documents/{id}/reprocess con id inexistente → 404.

Reglas: build y tests en verde antes de cerrar.

---

## REVIEWER

Eres el Reviewer de ExpenseFlow. Revisa TASK-011.

Lee primero:
- docs/architecture.md
- docs/tasks/TASK-011-reprocess-endpoint.md
- .cursor/agents/reviewer.md

Evalúa contra:
1. Criterios de aceptación de TASK-011.
2. IFileRestorer en Application; FileRestorer en Infrastructure.
3. Respuestas HTTP correctas: 200, 404, 422.
4. El Worker recogerá el documento en el próximo ciclo (verificar que
   OcrStatus=Pending no lo excluye del scanner).
5. Manejo del caso: archivo no existe en error/ (advertencia, no fallo).
6. Cobertura de tests.

Responde con hallazgos críticos, mejoras y decisión: approve / needs changes.
Guarda en: `docs/reviews/TASK-011-review.md`

---

## DOCS-KEEPER

Eres el Docs Keeper de ExpenseFlow. Cierra TASK-011 si el Reviewer aprobó.

1. Marca TASK-011 como done.
2. Actualiza docs/architecture.md: IFileRestorer, FileRestorer, endpoint
   POST /documents/{id}/reprocess, flujo de reactivación.
3. Actualiza README.md si aplica.
4. Resumen breve del cierre.

---

## COMMIT

```
feat(api): TASK-011 manual reprocess endpoint

Application
- Add OcrStatus = "Pending" to ReceiptOcrStatuses
- Add IFileRestorer with RestoreToInboxAsync

Infrastructure
- Add FileRestorer: moves file from error/ back to inbox/,
  resolves name collisions, logs success or missing-file warning
- Register IFileRestorer in AddFileStorage()

Api
- Add POST /documents/{id}/reprocess endpoint:
  404 if not found, 422 if already Success,
  updates OcrStatus=Pending + clears ErrorMessage,
  restores file to inbox/ if exists in error/

Tests
- POST reprocess: Failed→200, Success→422, missing→404

Docs
- Update architecture.md: reprocess flow, IFileRestorer
- Close TASK-011 (status: done)
```
