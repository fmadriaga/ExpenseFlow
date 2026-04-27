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
| `tests/ExpenseFlow.IntegrationTests` | Pruebas (persistencia SQLite, escáner de inbox) |
| `data/` | Datos locales: base SQLite `expenseflow.db` (generada; no commitear) |
| `storage/familia/` (subcarpetas `inbox`, `processed`, `error`) | Inbox y salidas de archivos (según arquitectura) |

## Compilar y probar

```bash
dotnet build ExpenseFlow.sln -c Release
dotnet test tests/ExpenseFlow.IntegrationTests/ExpenseFlow.IntegrationTests.csproj
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

## Rutas de almacenamiento y escáner (TASK-003)

- La sección `Storage` en `src/ExpenseFlow.Worker/appsettings.json` define por defecto (relativas al
  `ContentRoot` del Worker) `Inbox`, `Processed` y `Error` bajo `../../storage/familia/...`.
- El `IFileScanner` usa solo la ruta de **inbox** para listar y filtrar archivos; *processed* y
  *error* se reservan a tasks de movimiento de archivos. Sin OCR en esta fase.
- Puedes sobrescribir rutas (absoluta o relativa al proyecto Worker) o seguir los valores
  predeterminados, coherentes con la estructura `storage/familia/...` del repositorio.
- La carpeta de **inbox** debe existir para procesar ficheros (p. ej. creada por sincronización
  o manualmente); si no existe, el escáner registra una advertencia y no devuelve candidatos.

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

## Ejecutar

- Worker: `dotnet run --project src/ExpenseFlow.Worker` (migración, escáner al arranque,
  luego el `BackgroundService` de demostración; sin OCR aún)
- API: `dotnet run --project src/ExpenseFlow.Api`

## Referencia de capas

- `Application` referencia `Domain`
- `Infrastructure` referencia `Application` y `Domain`
- `Worker` y `Api` referencian `Application` e `Infrastructure`

Más detalle: `docs/architecture.md`.
