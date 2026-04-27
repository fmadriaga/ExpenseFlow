# TASK-008 Configuration and secrets

status: done
owner: backend
priority: high

## Goal
Centralizar configuraci贸n y credenciales del sistema.

## Context
Las rutas de storage, cadena de conexi贸n y credenciales del proveedor OCR no deben quedar hardcodeadas en el c贸digo.

## Scope
- Configurar `appsettings.json`
- Configurar `appsettings.Development.json` si aplica
- Soportar variables de entorno para secretos
- Crear clases Options para:
  - storage
  - OCR
  - worker
  - connection string
- Validar configuraci贸n al arrancar

## Out of scope
- secret manager cloud
- rotaci贸n autom谩tica de secretos

## Acceptance Criteria
- rutas y settings est谩n externalizados
- secretos del OCR no est谩n hardcodeados
- se usan `Options` tipadas
- si falta una configuraci贸n cr铆tica, la app falla al arrancar con mensaje claro
- la configuraci贸n del intervalo del worker es modificable sin tocar c贸digo

## Suggested Files
- `src/ExpenseFlow.Worker/appsettings.json`
- `src/ExpenseFlow.Worker/appsettings.Development.json`
- `src/ExpenseFlow.Application/Options/...`
- `src/ExpenseFlow.Infrastructure/...`

## Definition of Done
- build exitoso
- config validada al arranque
- README actualizado con variables necesarias



## Cierre documental

- **Entregado:** validaci髇 al arranque para StorageOptions, AzureDocumentIntelligenceOptions, WorkerOptions y ConnectionStrings:ExpenseFlow.
- **Seguridad:** el repositorio mantiene placeholders/no secretos; los valores reales se cargan por User Secrets o variables de entorno.
- **Pendiente (fuera de alcance):** secret manager cloud y rotaci髇 autom醫ica de secretos.

