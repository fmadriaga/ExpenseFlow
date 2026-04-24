# TASK-001 Bootstrap solution

status: ready
owner: backend
priority: high

## Goal
Crear la solución base de ExpenseFlow con la estructura de proyectos,
documentación mínima y un build local exitoso.

## Context
Este es el punto de partida del proyecto. La meta es dejar la solución lista
para comenzar a implementar persistencia, scanner de archivos y OCR sin
reorganizaciones grandes posteriores.

## Scope
- Crear solución `.sln`
- Crear proyectos `Domain`, `Application`, `Infrastructure`, `Worker`, `Api`
- Referencias básicas entre proyectos
- Crear `README.md` inicial
- Dejar estructura de carpetas `docs`, `src`, `tests`, `storage`, `data`

## Acceptance Criteria
- Existe una solución `.sln`
- Existen los proyectos por capa definidos
- La solución compila localmente
- `README.md` inicial creado
- La estructura base del repositorio queda consistente con `docs/architecture.md`

## Out of Scope
- Persistencia EF Core
- Integración OCR
- Lógica de negocio real

## Technical Notes
- Mantener arquitectura por capas desde el inicio
- Evitar agregar dependencias innecesarias
- Preparar la solución para crecimiento futuro sin sobreingeniería
