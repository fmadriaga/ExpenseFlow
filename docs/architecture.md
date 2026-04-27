# Architecture

## Resumen
ExpenseFlow MVP se compone de una soluci?n .NET con arquitectura en capas,
un Worker Service que procesa tickets desde una carpeta local sincronizada
con Drive/OneDrive, un proveedor OCR basado en Azure Document Intelligence
Receipt y una base SQLite para persistir resultados.

## Componentes principales

### Source
Carpeta local sincronizada con Google Drive Desktop o OneDrive Desktop.

Estructura esperada:
- `storage/familia/inbox` (debe existir para que el esc?ner liste archivos)
- `storage/familia/processed` y `storage/familia/error` (ra?ces configurables en `Storage`;
  no hace falta crear manualmente `yyyy/MM`: se crean al mover con `IFileMover`, ver TASK-006)

### Worker
Flujo **end-to-end** del MVP (un ?tem por archivo pendiente en cada ciclo):

1. **Scan:** `IFileScanner.GetPendingFilesToProcessAsync` (inbox, extensiones v?lidas, hash SHA-256,
   exclusi?n por `Document.FileHash` ya persistido).
2. **OCR:** `IReceiptOcrProvider.AnalyzeReceiptAsync` ? `OcrResult`.
3. **Normalize:** `IReceiptNormalizer.Normalize` ? `Document` + `DocumentLines` (mapeo en memoria).
4. **Persist:** `SaveChanges` sobre `Document`, l?neas y un `ProcessingJob` con estado
   `ProcessingJobStatuses.Success` o `Failed` seg?n el caso.
5. **Move:** `IFileMover.MoveToProcessedAsync` si el ?tem termin? bien tras persistir;
   `MoveToErrorAsync` en rutas de error (fallo antes de persistir, o fallo al mover a processed tras
   guardar, con actualizaci?n del documento/job a fallido cuando aplica).

**Bucle y operaci?n (TASK-007):**

- **Host:** `ExpenseFlowWorker` (`BackgroundService`), registrado con `AddHostedService`.
- **Intervalo:** `WorkerOptions` en Application (`IntervalSeconds`, secci?n `Worker` en configuraci?n;
  m?nimo efectivo 1 s entre fin de un ciclo e inicio del siguiente).
- **Guard de solapamiento:** `SemaphoreSlim(1,1)` + intento de adquisici?n inmediata; si un ciclo
  sigue en curso, no se inicia otro en paralelo (advertencia en log y espera al intervalo).
- **M?tricas por ciclo** (log de fin): archivos encontrados (candidatos devueltos por el esc?ner),
  procesados OK (persistencia + move a processed), fallidos (cualquier error tratado como fallo de
  ?tem). Inicio de ciclo con timestamp en log; si el esc?ner lanza, se registra fin con m?tricas en
  cero.
- **Correlaci?n de logs (TASK-009):** cada ciclo genera `JobId` y abre un scope de logging para
  correlar eventos de Worker/Infrastructure en consola local. Los eventos por archivo incluyen
  `FileName` y, cuando aporta diagn?stico, `FullPath` y hash abreviado.
- **Niveles de log (TASK-009):**
  - `Information`: inicio/fin de job con m?tricas, candidato detectado, archivo movido a processed.
  - `Warning`: duplicado por hash, archivo movido a error por fallo recuperable del pipeline.
  - `Error`: OCR/persistencia/movimiento fallidos con excepci?n y contexto de archivo.
- **Cancelaci?n:** `CancellationToken` del host en `Task.Delay`, `WaitAsync`, esc?ner, EF y mover.

Los pasos 1?2 (listado y filtrado en inbox, hash y dedup) siguen encapsulados en `FileScanner`; la
orquestaci?n solo consume contratos en Application e Infrastructure.

### OCR Provider
Abstracci?n `IReceiptOcrProvider` definida en Application.
Implementaci?n inicial en Infrastructure:
- `AzureDocumentIntelligenceReceiptProvider`
- **Contrato y DTO interno (TASK-004):** `IReceiptOcrProvider` recibe ruta de archivo y devuelve
  `OcrResult` (comercio, fecha de transacci?n, total, impuestos, moneda opcional cuando el campo
  `Total` la expone, l?neas y `RawJson` serializable).
- **Proveedor Azure (TASK-004):** usa `Azure.AI.DocumentIntelligence` con modelo
  `prebuilt-receipt`. Endpoint y key se toman desde configuraci?n (`AzureDocumentIntelligence`:
  `Endpoint`, `ApiKey`) mediante opciones tipadas; no se exponen tipos del SDK fuera de
  Infrastructure.
- **Registro DI:** `AddOcrProviders` registra el provider en Infrastructure; `ExpenseFlowWorker`
  lo resuelve por scope y lo invoca en el pipeline por archivo.
- **Extensibilidad:** Application depende solo de `IReceiptOcrProvider`; para incorporar nuevos
  OCR providers se agrega implementaci?n y registro en Infrastructure sin cambiar el contrato.

### Storage
SQLite mediante EF Core. El fichero de base de datos local por defecto vive en
`data/expenseflow.db` (ruta resuelta desde el host; configurable con
`ConnectionStrings:ExpenseFlow`). Al arranque, el Worker aplica las migraciones
emplazadas en `Infrastructure/Migrations` (`ExpenseFlowDbContext`).

- **Contexto y DI:** `ExpenseFlowDbContext` (namespace `ExpenseFlow.Infrastructure.Data`) se
  configura con SQLite. El registro ocurre v?a
  `ExpenseFlow.Infrastructure.DependencyInjection.AddPersistence` (`AddDbContext`); el
  `Program` del **Worker** invoca ese m?todo. El `Program` de la **Api** (TASK-010) tambi?n
  invoca `AddPersistence` y `AddFileScanning` para compartir la misma base SQLite; aplica
  `Migrate()` al arranque **salvo** en el entorno `Testing` (pruebas con `WebApplicationFactory`,
  que preparan el esquema de forma aislada).
- **Configuraci?n y secretos (TASK-008):** `ConnectionStrings:ExpenseFlow` es obligatoria (no vac?a)
  antes de registrar EF; `StorageOptions`, `AzureDocumentIntelligenceOptions` y `WorkerOptions` usan
  validaci?n con anotaciones (`ValidateDataAnnotations` + `ValidateOnStart`) y resoluci?n forzada al
  inicio del Worker antes de `Migrate()`. En `Production`, `AzureDocumentIntelligence` debe
  configurarse v?a entorno o secretos; `appsettings.Development.json` incluye placeholders no
  reales solo para desarrollo local y herramientas (sustituir por User Secrets para OCR real).
  Convenci?n de entorno .NET: Seccion__Clave (ejemplos:
  AzureDocumentIntelligence__Endpoint, AzureDocumentIntelligence__ApiKey,
  ConnectionStrings__ExpenseFlow).
- **Entidades (Domain):** `Document` (campos: `FilePath`, `FileHash`, datos normalizados del
  ticket: `MerchantName`, `TransactionDate`, `Currency`, `TotalAmount`, `TaxAmount`, `Confidence`,
  m?s `RawJson` para auditor?a OCR, `OcrStatus`, `ErrorMessage`, `CreatedAt`), `DocumentLine`
  (`Description`, `Quantity`, `UnitPrice`, `Amount`, `Currency`), `ProcessingJob`.
- **Normalizaci?n (TASK-005):** contrato `IReceiptNormalizer` (`Abstractions/`) e implementaci?n
  `ReceiptNormalizer` (`Application/Services/`) mapean `OcrResult` m?s `FilePath`/`FileHash` a
  `Document` y `DocumentLines` (sin persistir). Se copia `RawJson` al documento. Constantes de
  estado en `ReceiptOcrStatuses` (`Success`, `Partial`, `Failed`).
  - **Confidence:** porcentaje 0?100 sobre 6 ranuras igualmente ponderadas: comercio, fecha de
    transacci?n, total, impuesto, moneda, y ?hay al menos una l?nea? en `OcrResult.Lines`.
  - **OcrStatus:** `Success` si hay comercio no vac?o o total num?rico; si no, `Partial` si hay
    fecha, impuesto, moneda o l?neas; en caso contrario `Failed`.
  - **Migraci?n EF:** `AddDocumentNormalizationFields` (columnas nuevas en `Documents` y
    `DocumentLines`). Registro DI: `AddReceiptNormalization` en Infrastructure; el Worker invoca
    el normalizador tras el OCR en cada archivo procesado.
- **Esc?ner de inbox (TASK-003):** `IFileScanner` en `ExpenseFlow.Application.Abstractions` con
  DTO `ScanResult` (ruta, `FileHash` SHA-256 hex, `IsAlreadyInDatabase`). La implementaci?n
  `FileScanner` en `ExpenseFlow.Infrastructure.Scanning` enumera solo el primer nivel de la
  carpeta inbox, filtra extensiones (`jpg`, `jpeg`, `png`, `pdf`) sin distinguir may?sculas,
  ignora archivos vac?os, calcula hash y consulta `Document.FileHash` v?a
  `ExpenseFlowDbContext` para excluir duplicados. Rutas: `StorageOptions` (secci?n
  `Storage` en config: `Inbox`, `Processed`, `Error`), resueltas a absolutas con
  `IPostConfigureOptions<StorageOptions>`; registro: `AddFileScanning` (adem?s de
  `AddPersistence`). El m?todo `GetPendingFilesToProcessAsync` devuelve solo rutas cuyo
  contenido tiene un hash SHA-256 que **no** coincide con ning?n `Document.FileHash` ya
  guardado; si coincide, el archivo se descarta para reproceso y queda trazado en log.
- **Application (TASK-003):** contrato `IFileScanner`, DTO `ScanResult`, POCO `StorageOptions`
  (secci?n de configuraci?n `Storage`).
- **Movimiento de archivos (TASK-006):** contrato `IFileMover` en `Application.Abstractions` con
  `MoveToProcessedAsync` y `MoveToErrorAsync` (destino bajo `StorageOptions.Processed` o `Error`,
  subcarpetas `yyyy/MM` en UTC). Implementaci?n `FileMover` en `Infrastructure.Storage`: crea
  el ?rbol de directorios destino si falta, resuelve colisiones de nombre con sufijo ?nico
  (`{nombre}_{guid}{ext}`) sin sobrescribir archivos existentes, logs de movimiento exitoso,
  error al mover y colisi?n de nombre evitada. **No** consulta base de datos ni implementa
  deduplicaci?n por hash: eso sigue siendo responsabilidad exclusiva de `IFileScanner` (comparaci?n
  con `Document.FileHash`). Registro DI: `AddFileStorage` (tras `AddFileScanning` para tener
  `StorageOptions`). Orquestaci?n: `ExpenseFlowWorker` (TASK-007).

Tablas:
- `Documents`
- `DocumentLines`
- `ProcessingJobs`

### API (TASK-010)
Host `ExpenseFlow.Api` (ASP.NET Core, minimal API) con:

- **Registro:** `AddPersistence` + `AddFileScanning` (misma base y secci?n `Storage` que el Worker
  en `appsettings.json`); validaci?n de cadena v?a `ExpenseFlowConnectionStringValidator` al inicio;
  `Migrate()` al arranque excepto en entorno `Testing`.
- **DTOs (Application):** `DocumentSummaryDto` (listado, sin `FilePath` ni `RawJson`),
  `DocumentDetailDto` (incluye `FilePath`, `RawJson`, l?neas), `DocumentLineDto`,
  `DocumentsListResponseDto` (`items`, `page`, `pageSize`, `totalCount`). `Category` reservada
  (null hasta que exista en dominio).
- **Endpoints** (`MapDocumentsEndpoints`): `GET /documents` (paginaci?n `page` / `pageSize` m?x. 100,
  filtros opcionales `from` / `to` en `DateOnly`, `status` = `OcrStatus`); orden del listado por
  `Id` descendente (SQLite/EF no ordenan por `DateTimeOffset` en servidor; el orden se alinea con
  inserciones recientes). `GET /documents/{id}` con l?neas y `RawJson`; 404 con cuerpo JSON
  `error` + `id` si no existe.

## Capas

### Domain
- Entidades
- Value Objects
- Reglas del dominio

### Application
- Casos de uso
- Interfaces
- DTOs internos
- Contratos para OCR, filesystem y repositorios
- Opciones de host batch: `WorkerOptions` (`Worker` en configuraci?n) para el intervalo del ciclo

### Infrastructure
- EF Core / SQLite
- OCR providers
- Servicios de filesystem
- Hashing / utilidades t?cnicas

### Worker
- Orquestaci?n peri?dica del proceso batch

### Api
- Consulta de documentos (TASK-010); reproceso y m?s funciones en tasks posteriores

## Principios arquitect?nicos
- Simplicidad primero
- Integraciones externas desacopladas detr?s de interfaces
- Preservar OCR crudo para auditor?a y evoluci?n futura
- Un solo flujo claro: `inbox -> processed/error`
- Evitar reprocesos por hash y por estado persistido

## Evoluci?n futura prevista
- API para consulta de documentos
- UI web para revisi?n manual
- Clasificaci?n de gastos
- Presupuestos y objetivos
- Divisi?n de gastos familiares
- App m?vil

