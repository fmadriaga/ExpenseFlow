# TASK-003 File scanner

status: ready
owner: backend
priority: high

## Goal
Implementar un scanner de la carpeta `inbox` que detecte archivos válidos,
calcule hash y evite reprocesos.

## Context
El flujo depende de poder encontrar tickets nuevos en una carpeta local
sin procesar repetidamente los mismos archivos.

## Scope
- Enumerar archivos desde `storage/familia/inbox`
- Filtrar extensiones válidas: `jpg`, `jpeg`, `png`, `pdf`
- Ignorar archivos vacíos o inválidos
- Calcular hash de archivo
- Verificar si ya existe un documento con el mismo hash

## Acceptance Criteria
- Lista archivos válidos en `inbox`
- Ignora archivos inválidos o vacíos
- Calcula hash por archivo
- Evita reprocesos por hash ya existente
- Deja trazas de log útiles

## Out of Scope
- OCR
- Movimiento a processed/error
- Reglas avanzadas por proveedor

## Technical Notes
- Centralizar rutas en options/configuración
- Mantener el scanner desacoplado del OCR
- El resultado del scanner debe ser reutilizable por el Worker
