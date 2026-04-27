# Architecture

## Resumen
ExpenseFlow MVP se compone de una solución .NET con arquitectura en capas,
un Worker Service que procesa tickets desde una carpeta local sincronizada
con Drive/OneDrive, un proveedor OCR basado en Azure Document Intelligence
Receipt y una base SQLite para persistir resultados.

## Componentes principales

### Source
Carpeta local sincronizada con Google Drive Desktop o OneDrive Desktop.

Estructura esperada:
- `storage/familia/inbox`
- `storage/familia/processed`
- `storage/familia/error`

### Worker
Servicio en segundo plano que:
1. escanea la carpeta `inbox`
2. filtra archivos válidos (`jpg`, `jpeg`, `png`, `pdf`)
3. calcula hash para evitar reprocesos
4. llama al proveedor OCR
5. normaliza la extracción
6. guarda el documento y sus líneas
7. mueve el archivo a `processed` o `error`

### OCR Provider
Abstracción `IReceiptOcrProvider` definida en Application.
Implementación inicial en Infrastructure:
- `AzureDocumentIntelligenceReceiptProvider`
- **Contrato y DTO interno (TASK-004):** `IReceiptOcrProvider` recibe ruta de archivo y devuelve
  `OcrResult` (comercio, fecha de transacción, total, impuestos, moneda opcional cuando el campo
  `Total` la expone, líneas y `RawJson` serializable).
- **Proveedor Azure (TASK-004):** usa `Azure.AI.DocumentIntelligence` con modelo
  `prebuilt-receipt`. Endpoint y key se toman desde configuración (`AzureDocumentIntelligence`:
  `Endpoint`, `ApiKey`) mediante opciones tipadas; no se exponen tipos del SDK fuera de
  Infrastructure.
- **Registro DI:** `AddOcrProviders` registra el provider en Infrastructure; el Worker lo deja
  disponible por DI sin invocarlo aún (la orquestación queda para tasks posteriores).
- **Extensibilidad:** Application depende solo de `IReceiptOcrProvider`; para incorporar nuevos
  OCR providers se agrega implementación y registro en Infrastructure sin cambiar el contrato.

### Storage
SQLite mediante EF Core. El fichero de base de datos local por defecto vive en
`data/expenseflow.db` (ruta resuelta desde el host; configurable con
`ConnectionStrings:ExpenseFlow`). Al arranque, el Worker aplica las migraciones
emplazadas en `Infrastructure/Migrations` (`ExpenseFlowDbContext`).

- **Contexto y DI:** `ExpenseFlowDbContext` (namespace `ExpenseFlow.Infrastructure.Data`) se
  configura con SQLite. El registro ocurre vía
  `ExpenseFlow.Infrastructure.DependencyInjection.AddPersistence` (`AddDbContext`); el
  `Program` del **Worker** invoca ese método. La **Api** no registra aún el DbContext (no
  requerido en el corte actual).
- **Entidades (Domain):** `Document` (campos: `FilePath`, `FileHash`, datos normalizados del
  ticket: `MerchantName`, `TransactionDate`, `Currency`, `TotalAmount`, `TaxAmount`, `Confidence`,
  más `RawJson` para auditoría OCR, `OcrStatus`, `ErrorMessage`, `CreatedAt`), `DocumentLine`
  (`Description`, `Quantity`, `UnitPrice`, `Amount`, `Currency`), `ProcessingJob`.
- **Normalización (TASK-005):** contrato `IReceiptNormalizer` (`Abstractions/`) e implementación
  `ReceiptNormalizer` (`Application/Services/`) mapean `OcrResult` más `FilePath`/`FileHash` a
  `Document` y `DocumentLines` (sin persistir). Se copia `RawJson` al documento. Constantes de
  estado en `ReceiptOcrStatuses` (`Success`, `Partial`, `Failed`).
  - **Confidence:** porcentaje 0–100 sobre 6 ranuras igualmente ponderadas: comercio, fecha de
    transacción, total, impuesto, moneda, y “hay al menos una línea” en `OcrResult.Lines`.
  - **OcrStatus:** `Success` si hay comercio no vacío o total numérico; si no, `Partial` si hay
    fecha, impuesto, moneda o líneas; en caso contrario `Failed`.
  - **Migración EF:** `AddDocumentNormalizationFields` (columnas nuevas en `Documents` y
    `DocumentLines`). Registro DI: `AddReceiptNormalization` en Infrastructure; el Worker lo
    invoca junto al OCR para uso en la orquestación posterior.
- **Escáner de inbox (TASK-003):** `IFileScanner` en `ExpenseFlow.Application.Abstractions` con
  DTO `ScanResult` (ruta, `FileHash` SHA-256 hex, `IsAlreadyInDatabase`). La implementación
  `FileScanner` en `ExpenseFlow.Infrastructure.Scanning` enumera solo el primer nivel de la
  carpeta inbox, filtra extensiones (`jpg`, `jpeg`, `png`, `pdf`) sin distinguir mayúsculas,
  ignora archivos vacíos, calcula hash y consulta `Document.FileHash` vía
  `ExpenseFlowDbContext` para excluir duplicados. Rutas: `StorageOptions` (sección
  `Storage` en config: `Inbox`, `Processed`, `Error`), resueltas a absolutas con
  `IPostConfigureOptions<StorageOptions>`; registro: `AddFileScanning` (además de
  `AddPersistence`). El método `GetPendingFilesToProcessAsync` devuelve solo rutas cuyo
  contenido tiene un hash SHA-256 que **no** coincide con ningún `Document.FileHash` ya
  guardado; si coincide, el archivo se descarta para reproceso y queda trazado en log.
- **Application (TASK-003):** contrato `IFileScanner`, DTO `ScanResult`, POCO `StorageOptions`
  (sección de configuración `Storage`).

Tablas:
- `Documents`
- `DocumentLines`
- `ProcessingJobs`

### API
No es obligatoria para el primer corte funcional, pero la solución ya debe quedar
preparada para exponer una API mínima más adelante.

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

### Infrastructure
- EF Core / SQLite
- OCR providers
- Servicios de filesystem
- Hashing / utilidades técnicas

### Worker
- Orquestación periódica del proceso batch

### Api
- Endpoints futuros para consultar documentos y reprocesar

## Principios arquitectónicos
- Simplicidad primero
- Integraciones externas desacopladas detrás de interfaces
- Preservar OCR crudo para auditoría y evolución futura
- Un solo flujo claro: `inbox -> processed/error`
- Evitar reprocesos por hash y por estado persistido

## Evolución futura prevista
- API para consulta de documentos
- UI web para revisión manual
- Clasificación de gastos
- Presupuestos y objetivos
- División de gastos familiares
- App móvil
