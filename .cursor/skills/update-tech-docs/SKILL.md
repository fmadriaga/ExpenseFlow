---
name: update-tech-docs
description: Mantiene sincronizada la documentación técnica del proyecto después de cada task implementada o revisada. Usar cuando hay que actualizar README, arquitectura, visión o tasks según cambios reales del repositorio.
---

# update-tech-docs

Usa esta skill cuando una task ya fue implementada o revisada y hay que actualizar documentación técnica.

## Objetivo
Mantener README, arquitectura y backlog alineados con el estado real del repositorio.

## Documentos objetivo
- `README.md`
- `docs/architecture.md`
- `docs/product/vision.md`
- `docs/tasks/*.md`

## Checklist obligatorio
1. Revisar qué cambió realmente en código.
2. Actualizar `README.md` solo si cambió:
   - setup
   - ejecución
   - dependencias
   - comandos
3. Actualizar `docs/architecture.md` solo si cambió:
   - estructura por capas
   - flujo técnico
   - proveedores
   - persistencia
4. Actualizar la task markdown:
   - status
   - notas de implementación
   - limitaciones si existen
5. No documentar funcionalidades que aún no existen.

## Rules
- No hacer marketing.
- No inventar roadmap no acordado.
- No borrar contexto útil de decisiones previas.
- Mantener redacción breve y operativa.
- Si algo no quedó implementado del todo, dejarlo explicitado.

## Formato recomendado para tasks completadas
Agregar al final:
- Resultado
- Archivos principales tocados
- Pendientes
- Fecha de cierre si aplica

## Salida esperada
Al terminar, devolver:
- documentos actualizados
- resumen de cambios
- si falta documentación pendiente
