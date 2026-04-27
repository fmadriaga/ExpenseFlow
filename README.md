# ExpenseFlow

Procesamiento de tickets (OCR) desde una carpeta sincronizada, con persistencia SQLite. MVP documentado en `AGENTS.md` y `docs/architecture.md`.

## Requisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download) (misma línea major que el target de los proyectos)
- [Herramienta `dotnet-ef`](https://learn.microsoft.com/ef/core/cli/dotnet) (para migraciones: `dotnet tool install --global dotnet-ef` o manifiesto local bajo `.config/`)

## Estructura del repositorio

| Ruta | Uso |
| --- | --- |
| `src/ExpenseFlow.Domain` | Entidades y reglas de dominio |
| `src/ExpenseFlow.Application` | Casos de uso, contratos, DTOs internos |
| `src/ExpenseFlow.Infrastructure` | EF Core (`ExpenseFlowDbContext`, migraciones), integraciones, filesystem |
| `src/ExpenseFlow.Worker` | Proceso por lotes en segundo plano |
| `src/ExpenseFlow.Api` | Host HTTP para evolución futura |
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

- Fichero por defecto: `data/expenseflow.db` (dos niveles arriba del `ContentRoot` del Worker: raíz del repo). El directorio `data` se crea si no existe.
- El fichero de base generado se ignora en el control de versiones (vía `.gitignore`); no lo subas al repositorio.
- Override opcional: cadena completa SQLite o ruta a fichero (sin `=`) en `ConnectionStrings:ExpenseFlow` (ver `appsettings` o variables de entorno bajo el prefijo de configuración estándar).
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
- Configura credenciales en `src/ExpenseFlow.Worker/appsettings.json` (o, preferiblemente, en
  User Secrets / variables de entorno):

```json
{
  "AzureDocumentIntelligence": {
    "Endpoint": "https://<tu-recurso>.cognitiveservices.azure.com/",
    "ApiKey": "<tu-key>"
  }
}
```

- No incluyas keys reales en el repositorio. Si faltan `Endpoint` o `ApiKey`, el provider falla
  con un error claro al resolverse desde DI.
- Variables de entorno equivalentes:
  - `AzureDocumentIntelligence__Endpoint`
  - `AzureDocumentIntelligence__ApiKey`

## Worker: ejecución local del pipeline completo (TASK-007)

1. **Carpeta inbox:** debe existir la ruta configurada en `Storage:Inbox` (por defecto
   `storage/familia/inbox` relativa al `ContentRoot` del proyecto Worker). Coloca ahí un ticket
   válido (`jpg`, `jpeg`, `png`, `pdf`, no vacío).
2. **Azure Document Intelligence** (obligatorio para procesar archivos reales): no dejes `Endpoint`
   ni `ApiKey` vacíos. Opciones:
   - User Secrets del proyecto Worker: `dotnet user-secrets set "AzureDocumentIntelligence:Endpoint" "https://..."` y `dotnet user-secrets set "AzureDocumentIntelligence:ApiKey" "..."`.
   - Variables de entorno (doble guion bajo = jerarquía en .NET):
     - `AzureDocumentIntelligence__Endpoint`
     - `AzureDocumentIntelligence__ApiKey`
3. **Intervalo entre ciclos:** en `appsettings.json`, sección `Worker` → `IntervalSeconds` (por
   defecto 60), o variable `Worker__IntervalSeconds`.
4. **Base de datos (opcional):** `ConnectionStrings__ExpenseFlow` si no usas el valor por defecto
   del `appsettings` (SQLite bajo `data/expenseflow.db` desde la raíz del repo).
5. **Rutas de almacenamiento (opcional):** `Storage__Inbox`, `Storage__Processed`, `Storage__Error`
   si no quieres las rutas relativas por defecto.

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

## Ejecutar (referencia rápida)

- Worker: `dotnet run --project src/ExpenseFlow.Worker` — detalle y variables en la sección anterior.
- API: `dotnet run --project src/ExpenseFlow.Api`

## Referencia de capas

- `Application` referencia `Domain`
- `Infrastructure` referencia `Application` y `Domain`
- `Worker` y `Api` referencian `Application` e `Infrastructure`

Más detalle: `docs/architecture.md`.
