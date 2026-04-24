# TASK-008 Configuration and secrets

status: ready
owner: backend
priority: high

## Goal
Centralizar configuración y credenciales del sistema.

## Context
Las rutas de storage, cadena de conexión y credenciales del proveedor OCR no deben quedar hardcodeadas en el código.

## Scope
- Configurar `appsettings.json`
- Configurar `appsettings.Development.json` si aplica
- Soportar variables de entorno para secretos
- Crear clases Options para:
  - storage
  - OCR
  - worker
  - connection string
- Validar configuración al arrancar

## Out of scope
- secret manager cloud
- rotación automática de secretos

## Acceptance Criteria
- rutas y settings están externalizados
- secretos del OCR no están hardcodeados
- se usan `Options` tipadas
- si falta una configuración crítica, la app falla al arrancar con mensaje claro
- la configuración del intervalo del worker es modificable sin tocar código

## Suggested Files
- `src/ExpenseFlow.Worker/appsettings.json`
- `src/ExpenseFlow.Worker/appsettings.Development.json`
- `src/ExpenseFlow.Application/Options/...`
- `src/ExpenseFlow.Infrastructure/...`

## Definition of Done
- build exitoso
- config validada al arranque
- README actualizado con variables necesarias
