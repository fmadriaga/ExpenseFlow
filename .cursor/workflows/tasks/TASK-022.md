# TASK-022 — App móvil

Archivo de task: `docs/tasks/TASK-022-mobile-app.md`

---

## PLANNER

Eres el Planner del proyecto ExpenseFlow. Produce un plan técnico para TASK-022.

Archivos a leer:
- docs/architecture.md
- docs/tasks/TASK-022-mobile-app.md

Contexto: La Api expone GET /documents y GET /documents/{id} (TASK-010).
El Worker procesa archivos desde inbox/. La carpeta inbox está sincronizada
con Drive/OneDrive. No existe proyecto móvil todavía.

Produce:
1. Plan de implementación paso a paso.
2. Tecnología recomendada (.NET MAUI) con justificación.
3. Lista de archivos a crear y modificar por proyecto.
4. Dependencias NuGet y permisos de plataforma necesarios.
5. Riesgos o puntos a tener en cuenta.

Reglas: no escribas código, salida concreta y accionable.

---

## IMPLEMENTER

Eres el Backend Implementer de ExpenseFlow. Implementa TASK-022.

Lee primero:
- docs/architecture.md
- docs/tasks/TASK-022-mobile-app.md
- .cursor/agents/backend-implementer.md

Implementa exactamente:

**Api (backend):**
1. Endpoint GET /documents/recent?limit=20: devuelve los N documentos
   más recientes con status (Pending, Success, Failed) para el historial
   de la app móvil. Reutiliza DocumentSummaryDto.

**Mobile (nuevo proyecto src/ExpenseFlow.Mobile, .NET MAUI):**
2. Crear proyecto MAUI para iOS y Android.
3. Pantallas:
   - MainPage: botón "Fotografiar ticket" que abre la cámara.
     Al confirmar la foto, la guarda como .jpg en la carpeta inbox
     de Drive/OneDrive (configurada en appsettings de la app móvil).
   - HistoryPage: lista de últimos 20 tickets vía GET /documents/recent.
     Muestra FileName, TransactionDate, TotalAmount, OcrStatus con color
     (verde=Success, amarillo=Pending, rojo=Failed).
4. Servicio ExpenseFlowApiClient para consumir la Api.
5. Servicio InboxUploader que guarda la foto en la carpeta inbox local
   sincronizada (File.Copy a la ruta configurada — Drive/OneDrive la
   sincroniza automáticamente desde el escritorio).
6. Configuración en appsettings de la app: ApiBaseUrl, InboxLocalPath.
7. Permisos: Camera, ReadExternalStorage/WriteExternalStorage (Android),
   NSCameraUsageDescription (iOS).
8. Notificación local cuando un ticket pasa de Pending a Success/Failed
   (polling cada 30s desde HistoryPage).
9. Agregar ExpenseFlow.Mobile al .sln.

Reglas: build en simulador (iOS o Android). Sin autenticación.
La foto se guarda localmente en la carpeta sincronizada — no hay
integración directa con la API de Drive/OneDrive en esta task.

---

## REVIEWER

Eres el Reviewer de ExpenseFlow. Revisa TASK-022.

Lee primero:
- docs/architecture.md
- docs/tasks/TASK-022-mobile-app.md
- .cursor/agents/reviewer.md

Evalúa contra:
1. Criterios de aceptación de TASK-022.
2. La foto se guarda en la carpeta inbox configurada (no hardcodeada).
3. HistoryPage consume la Api por HTTP (no acceso directo a BD).
4. Permisos de cámara declarados correctamente en ambas plataformas.
5. El polling de estado no bloquea la UI.
6. El proyecto compila en simulador.
7. ApiBaseUrl e InboxLocalPath son configurables.

Responde con hallazgos críticos, mejoras y decisión: approve / needs changes.
Guarda en: `docs/reviews/TASK-022-review.md`

---

## DOCS-KEEPER

Eres el Docs Keeper de ExpenseFlow. Cierra TASK-022 si el Reviewer aprobó.

1. Marca TASK-022 como done.
2. Actualiza docs/architecture.md: proyecto ExpenseFlow.Mobile, MAUI,
   InboxUploader, ExpenseFlowApiClient, endpoint GET /documents/recent.
3. Actualiza README.md: cómo configurar y ejecutar la app móvil en simulador.
4. Resumen breve del cierre.

---

## COMMIT

```
feat(mobile): TASK-022 .NET MAUI mobile app for ticket capture

Api
- Add GET /documents/recent?limit=N endpoint for mobile history feed

Mobile (new project: src/ExpenseFlow.Mobile, .NET MAUI iOS+Android)
- MainPage: camera capture, saves photo as .jpg to configured inbox path
- HistoryPage: recent 20 tickets list with OcrStatus color coding,
  30s polling for status updates, local notification on state change
- ExpenseFlowApiClient: typed HTTP client for Api consumption
- InboxUploader: saves captured photo to local inbox folder
  (Drive/OneDrive syncs automatically from desktop)
- Configurable: ApiBaseUrl, InboxLocalPath in app settings
- Camera permissions: Android (READ/WRITE_EXTERNAL_STORAGE),
  iOS (NSCameraUsageDescription)
- Added ExpenseFlow.Mobile to ExpenseFlow.sln

Docs
- Update architecture.md: Mobile project, MAUI, capture flow
- Update README.md: mobile setup and simulator instructions
- Close TASK-022 (status: done)
```
