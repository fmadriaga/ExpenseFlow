# ExpenseFlow Agents Context

## Objetivo del proyecto
Procesar tickets de compra desde una carpeta sincronizada, extraer datos con OCR,
normalizarlos y guardarlos en una base SQLite para su posterior uso en una app de gastos.

## Alcance MVP
- Fuente: carpeta local sincronizada con Drive/OneDrive
- Tipos: JPG, PNG, PDF
- OCR: Azure Document Intelligence Receipt
- Persistencia: SQLite
- Flujo: inbox -> processed/error
- Salida: registros estructurados y JSON crudo

## Restricciones
- No implementar aún app móvil ni presupuestos
- No hacer multiusuario en MVP
- No introducir mensajería, colas ni microservicios
- No usar n8n

## Principios
- Simplicidad primero
- Arquitectura extensible
- OCR desacoplado por interfaz
- Cambios pequeños
- Documentación mínima siempre actualizada

## Roles sugeridos de agentes
- Planner: selecciona la próxima task lista para implementar y propone plan técnico
- Backend Implementer: implementa una sola task acotada por vez
- Reviewer: revisa cambios contra arquitectura, criterios y calidad
- Docs Keeper: mantiene README y documentación técnica/funcional

## Flujo recomendado de trabajo
1. Planner lee `docs/product/vision.md`, `docs/architecture.md` y `docs/tasks/*.md`
2. Planner selecciona una task `ready`
3. Backend Implementer ejecuta únicamente esa task
4. Reviewer revisa el cambio
5. Docs Keeper actualiza docs y marca task como `done` al cerrar el trabajo

## Convenciones
- Una task por branch o PR
- No mezclar cambios funcionales y refactors grandes
- Si cambia la arquitectura, actualizar `docs/architecture.md`
- Si cambia el setup, actualizar `README.md`
- Mantener el procesamiento desacoplado de la UI futura
