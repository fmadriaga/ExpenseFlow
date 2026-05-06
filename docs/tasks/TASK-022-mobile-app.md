# TASK-022 App móvil

status: done
owner: mobile
priority: low

## Goal
Cliente móvil que permita fotografiar un ticket y subirlo directamente
a la carpeta inbox sincronizada, cerrando el ciclo de captura sin
necesidad de intervención desde el escritorio.

## Context
Hoy el flujo requiere que el ticket llegue físicamente a la carpeta
inbox (via Drive/OneDrive). Una app móvil permite fotografiar el ticket
en el momento de la compra y encolarlo para procesamiento automático.

## Scope
- Aplicación móvil (iOS y Android) con cámara para fotografiar tickets
- Subir la foto a la carpeta inbox de Drive/OneDrive del usuario
- Vista de historial de tickets enviados (estado: pendiente, procesado,
  error) consultando la API de ExpenseFlow
- Notificación local cuando un ticket pasa a estado procesado

## Out of scope
- OCR en el dispositivo
- Edición de datos desde la app
- Autenticación propia (usa la cuenta de Drive/OneDrive del usuario)
- Android < API 26 o iOS < 15

## Acceptance Criteria
- La app abre la cámara y permite tomar una foto
- La foto se guarda en la carpeta inbox de Drive/OneDrive correctamente
- El Worker procesa el archivo en el siguiente ciclo
- El historial muestra el estado correcto del ticket
- La notificación local se emite cuando el estado cambia a procesado
- La app funciona en iOS y Android

## Technical Notes
- Tecnología sugerida: .NET MAUI (reutiliza el stack .NET del proyecto)
  o React Native si se prefiere separar el stack móvil
- Integración con Google Drive API o Microsoft Graph para subir archivos
  a la carpeta inbox configurada
- Polling o webhook desde la API de ExpenseFlow para actualizar estados
- La API debe exponer autenticación básica o token para que la app
  pueda consultar el historial (depende de TASK-010)

## Suggested Files
- src/ExpenseFlow.Mobile/... (proyecto nuevo MAUI o RN)
- src/ExpenseFlow.Api/Endpoints/... (webhook o polling de estado)

## Definition of Done
- build exitoso en iOS y Android (simulador aceptable)
- foto tomada desde la app aparece en inbox y es procesada por el Worker
- historial muestra el estado correcto del ticket procesado
