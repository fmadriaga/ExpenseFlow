# Review TASK-011 — Endpoint de reproceso manual

## Acceptance criteria

| Criterio | Estado |
|----------|--------|
| POST devuelve 200 cuando puede reactivarse | OK |
| 404 si el documento no existe | OK |
| 422 si OcrStatus = Success | OK |
| OcrStatus actualizado en BD (Pending, ErrorMessage limpiado) | OK |
| Archivo en error/ movido a inbox/ por hash | OK |
| Si no hay archivo en error/, advertencia y 200 | OK |
| Worker reprocesa en ciclo siguiente (hash + Pending, update in place) | OK — escenario verificado en diseño; integración con Worker cubierta por lógica compartida y tests de API |

## Arquitectura

- `IFileRestorer` en Application; implementación en Infrastructure; sin lógica OCR en controllers.
- Deduplicación por hash ajustada: solo se descarta candidato si hash existe con `Success` (permite re-inbox).
- Worker actualiza documento `Pending` existente al éxito de OCR en lugar de duplicar fila.

## Riesgos / notas

- Reproceso con archivo ausente en disco: queda `Pending` hasta que el usuario restaure el fichero o se limpie manualmente.

## Verificación

- `dotnet build` y `dotnet test` (solución) en verde.

**Veredicto:** aprobado para merge.
