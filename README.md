# ExpenseFlow

Procesamiento de tickets (OCR) desde una carpeta sincronizada, con persistencia SQLite. MVP documentado en `AGENTS.md` y `docs/architecture.md`.

## Requisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download) (misma línea major que el target de los proyectos)
- [Herramienta `dotnet-ef`](https://learn.microsoft.com/ef/core/cli/dotnet) (para migraciones: `dotnet tool install --global dotnet-ef` o manifiesto local bajo `.config/`)

## Configuración y variables de entorno (TASK-008)

El Worker valida al arranque la cadena SQLite y las opciones tipadas (`Storage`, `AzureDocumentIntelligence`, `Worker`). **No subas al repositorio** `ApiKey` ni endpoints reales de Azure: usa User Secrets o variables de entorno.

En .NET, la jerarquía en variables de entorno usa **doble guion bajo** (`__`) como separador de sección y clave.

| Variable | Obligatoria | Descripción |
| --- | --- | --- |
| `ConnectionStrings__ExpenseFlow` | Sí (Worker y Api) | Ruta o cadena SQLite (p. ej. `../../data/expenseflow.db` relativa al `ContentRoot` del proyecto, o `Data Source=...;`). Debe existir en configuración mergeada; si está vacía o ausente, el proceso termina con error claro. |
| `Storage__Inbox` | Sí* | Ruta del inbox (*tras bind: no vacía; valores por defecto en `appsettings.json`). |
| `Storage__Processed` | Sí* | Raíz de procesados. |
| `Storage__Error` | Sí* | Raíz de errores. |
| `AzureDocumentIntelligence__Endpoint` | Sí* | URL del recurso (en `Production` no puede quedar vacío; en **Development** aplica `appsettings.Development.json` con placeholder). |
| `AzureDocumentIntelligence__ApiKey` | Sí* | Clave del servicio (misma nota que `Endpoint`). |
| `Worker__IntervalSeconds` | Sí* | Entero ≥ 1 (por defecto 60 en `appsettings.json`). |

\*Tras la carga de `appsettings.json` + `appsettings.{Environment}.json` + User Secrets + variables de entorno. Con `DOTNET_ENVIRONMENT=Production` y solo el `appsettings.json` base, **Azure** debe definirse por entorno o el arranque falla con `OptionsValidationException`.

### User Secrets (setup local recomendado)

Desde la carpeta del Worker:

```bash
dotnet user-secrets init --project src/ExpenseFlow.Worker
dotnet user-secrets set "AzureDocumentIntelligence:Endpoint" "https://<tu-recurso>.cognitiveservices.azure.com/" --project src/ExpenseFlow.Worker
dotnet user-secrets set "AzureDocumentIntelligence:ApiKey" "<tu-key>" --project src/ExpenseFlow.Worker
```

User Secrets tienen prioridad sobre `appsettings` y no se versionan. Para sustituir la cadena SQLite o rutas de `Storage`, puedes usar la misma convención (`ConnectionStrings:ExpenseFlow`, `Storage:Inbox`, etc.).

### Entorno Development vs Production

- **`dotnet run --project src/ExpenseFlow.Worker`** usa `launchSettings.json` con `DOTNET_ENVIRONMENT=Development`, carga `appsettings.Development.json` (placeholders de Azure **no válidos para OCR real**) y permite arrancar sin variables extra; para procesar tickets, configura User Secrets con credenciales reales.
- **Production:** configura todas las claves críticas por entorno o por ficheros desplegados fuera del repo; sin `Endpoint`/`ApiKey` el proceso falla al arranque con mensaje de validación explícito.

### Migraciones EF y entorno

Si ejecutas `dotnet ef` con entorno `Production` y sin Azure configurado, la validación puede fallar. Usa Development al aplicar migraciones en local, por ejemplo (PowerShell):

```powershell
$env:DOTNET_ENVIRONMENT = 'Development'
dotnet ef database update --project src/ExpenseFlow.Infrastructure --startup-project src/ExpenseFlow.Worker
```

## Estructura del repositorio

| Ruta | Uso |
| --- | --- |
| `src/ExpenseFlow.Domain` | Entidades y reglas de dominio |
| `src/ExpenseFlow.Application` | Casos de uso, contratos, DTOs internos |
| `src/ExpenseFlow.Infrastructure` | EF Core (`ExpenseFlowDbContext`, migraciones), integraciones, filesystem |
| `src/ExpenseFlow.Worker` | Proceso por lotes en segundo plano |
| `src/ExpenseFlow.Api` | API HTTP de consulta de documentos (TASK-010) |
| `docs/` | Visión, arquitectura y tasks |
| `tests/ExpenseFlow.IntegrationTests` | Pruebas de integración (SQLite, escáner, mapper OCR, FileMover, Worker) |
| `tests/ExpenseFlow.Application.Tests` | Pruebas unitarias (p. ej. normalizador de recibos) |
| `data/` | Datos locales: base SQLite `expenseflow.db` (generada; no commitear) |
| `storage/familia/` (subcarpetas `inbox`, `processed`, `error`) | Inbox y salidas de archivos (según arquitectura) |

## Compilar y probar

```bash
dotnet build ExpenseFlow.sln -c Release
dotnet test ExpenseFlow.sln
```

## Base de datos (SQLite)

- La cadena **`ConnectionStrings:ExpenseFlow` es obligatoria** (ver `appsettings.json`: por defecto
  `../../data/expenseflow.db` relativa al `ContentRoot` del Worker). El directorio `data` se crea
  al resolver la ruta si no existe.
- El fichero de base generado se ignora en el control de versiones (vía `.gitignore`); no lo subas al repositorio.
- Override: cadena completa SQLite o ruta a fichero (sin `=`) vía `ConnectionStrings__ExpenseFlow` o User Secrets.
- Migraciones: proyecto de modelos `src/ExpenseFlow.Infrastructure`, host de arranque `src/ExpenseFlow.Worker`:

```bash
dotnet ef migrations add Nombre --project src/ExpenseFlow.Infrastructure --startup-project src/ExpenseFlow.Worker
dotnet ef database update --project src/ExpenseFlow.Infrastructure --startup-project src/ExpenseFlow.Worker
```

El Worker aplica `Migrate()` al arrancar para mantener el esquema al día en desarrollo.

- **TASK-005:** migración `AddDocumentNormalizationFields` (normalización en `Documents` /
  `DocumentLines`). No cambia el procedimiento habitual: al ejecutar el Worker se aplica sola; solo
  necesitas `dotnet ef database update` manual si mantienes la SQLite sin arrancar el Worker.

## Rutas de almacenamiento y escáner (TASK-003)

- La sección `Storage` en `src/ExpenseFlow.Worker/appsettings.json` define por defecto (relativas al
  `ContentRoot` del Worker) `Inbox`, `Processed` y `Error` bajo `../../storage/familia/...`.
- El `IFileScanner` usa solo la ruta de **inbox** para listar y filtrar archivos; *processed* y
  *error* las usa `IFileMover` al cerrar cada ítem del pipeline (TASK-007).
- Puedes sobrescribir rutas (absoluta o relativa al proyecto Worker) o seguir los valores
  predeterminados, coherentes con la estructura `storage/familia/...` del repositorio.
- La carpeta de **inbox** debe existir para procesar ficheros (p. ej. creada por sincronización
  o manualmente); si no existe, el escáner registra una advertencia y no devuelve candidatos.

## Movimiento a processed/error (TASK-006)

- `IFileMover` / `FileMover` mueven archivos bajo las rutas raíz `Processed` y `Error` de la
  sección `Storage`, creando automáticamente las subcarpetas `yyyy/MM` (UTC) y, si hace falta,
  las propias raíces; **no** es necesario crear esa jerarquía a mano antes de un movimiento.
- Colisiones de nombre en el destino se resuelven con un nombre alternativo sin sobrescribir.
- El bucle del Worker invoca el mover tras persistir cada documento (ver «Worker: ejecución local
  del pipeline completo» más abajo).

## OCR Azure (TASK-004)

- El proveedor OCR implementa `IReceiptOcrProvider` y usa Azure Document Intelligence
  (`prebuilt-receipt`).
- **No pongas credenciales reales en `appsettings.json` del repositorio.** En `Production` los
  campos vacíos del archivo base hacen fallar la validación al arranque; en **Development**,
  `appsettings.Development.json` trae placeholders no secretos solo para poder arrancar el host
  (sustituye por User Secrets para llamadas reales a Azure). Tabla de variables: sección
  **Configuración y variables de entorno** arriba.

## Worker: ejecución local del pipeline completo (TASK-007)

1. **Carpeta inbox:** debe existir la ruta configurada en `Storage:Inbox` (por defecto
   `storage/familia/inbox` relativa al `ContentRoot` del proyecto Worker). Coloca ahí un ticket
   válido (`jpg`, `jpeg`, `png`, `pdf`, no vacío).
2. **Azure Document Intelligence:** para OCR real, configura User Secrets o variables de entorno
   (ver sección de configuración). Los placeholders de Development no son credenciales válidas.
3. **Intervalo entre ciclos:** sección `Worker` → `IntervalSeconds` en `appsettings.json` o
   `Worker__IntervalSeconds`.
4. **Base de datos:** `ConnectionStrings:ExpenseFlow` obligatoria (valor por defecto en
   `appsettings.json` apuntando a `data/expenseflow.db`).
5. **Rutas de almacenamiento (opcional):** `Storage__Inbox`, `Storage__Processed`, `Storage__Error`.

**Comando:**

```bash
dotnet run --project src/ExpenseFlow.Worker
```

Al arranque se aplican migraciones SQLite. A continuación `ExpenseFlowWorker` ejecuta ciclos: escaneo
→ OCR → normalización → persistencia → movimiento a `processed` o `error` (sin reintentos
automáticos).

**Cómo comprobar que el ciclo corre:** en consola (nivel Information) deberías ver, en cada ciclo,
mensajes equivalentes a: inicio de job con timestamp, número de archivos pendientes devueltos por
el escáner, y al cerrar el ciclo una línea con **Files found**, **processed OK** y **failed**. Si un
ciclo anterior sigue en curso, verás una advertencia de que se omite el solapamiento. Tras un
procesamiento correcto, el archivo desaparece del inbox y aparece bajo `processed/yyyy/MM/`; si
falla el pipeline del archivo, acaba en `error/yyyy/MM/` (salvo errores extremos de disco).

## Logging básico (TASK-009)

- Cada corrida del Worker incluye un `JobId` de correlación (scope de logging en consola).
- Para diagnosticar un archivo de punta a punta, busca por `JobId` y luego por `FileName`.
- Eventos clave esperables:
  - `Information`: inicio/fin de job, candidato detectado, movido a `processed`.
  - `Warning`: duplicado por hash, movido a `error` con motivo breve.
  - `Error`: OCR fallido, persistencia fallida, fallo de movimiento.
- Niveles de consola en `appsettings.json`: `Default=Information`, `Microsoft=Warning`,
  `System=Warning`, `Console.IncludeScopes=true`.
- Seguridad: los logs no deben contener `ApiKey` ni connection strings.

## Api de consulta (TASK-010)

El proyecto `ExpenseFlow.Api` usa la misma cadena SQLite y la sección `Storage` que el Worker
(`appsettings.json`). Al arrancar aplica migraciones y expone:

- `GET /documents` — listado paginado (`page`, `pageSize`, opcionalmente `from`, `to`, `status`).
- `GET /documents/{id}` — detalle con líneas y `RawJson` (el listado no incluye `RawJson`).
- `POST /documents/{id}/reprocess` — marca el documento para reproceso (`OcrStatus` = `Pending`) y, si el fichero está bajo `error/`, lo vuelve a colocar en `inbox/` (mismo hash en base de datos). `422` si el documento ya está en `Success`.

**Ejecutar en local:**

```bash
dotnet run --project src/ExpenseFlow.Api
```

Con el perfil `http` de `launchSettings.json` la URL base suele ser `http://localhost:5287`
(mira la consola al arrancar). Ejemplos:

```bash
curl -s http://localhost:5287/documents
curl -s http://localhost:5287/documents/1
curl -s -X POST http://localhost:5287/documents/1/reprocess
```

Si la base está vacía, el listado devuelve `items` vacío y `totalCount` 0. Tras procesar tickets con el Worker, los mismos documentos son visibles por la API.

## Ejecutar (referencia rápida)

- Worker: `dotnet run --project src/ExpenseFlow.Worker` — detalle y variables en la sección anterior.
- API: `dotnet run --project src/ExpenseFlow.Api` — ver sección **Api de consulta** arriba.

## Referencia de capas

- `Application` referencia `Domain`
- `Infrastructure` referencia `Application` y `Domain`
- `Worker` y `Api` referencian `Application` e `Infrastructure`

Más detalle: `docs/architecture.md`.
