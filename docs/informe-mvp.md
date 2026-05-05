# ExpenseFlow — Informe MVP

## Resumen ejecutivo

ExpenseFlow es una solución personal/familiar para capturar tickets de compra,
extraer sus datos mediante OCR y almacenarlos de forma confiable para análisis
posterior. El MVP resuelve el flujo completo de extremo a extremo: detectar
archivos nuevos en una carpeta local sincronizada con Drive o OneDrive, leerlos
con Azure Document Intelligence, normalizar los datos extraídos, persistirlos en
SQLite y mover cada archivo a su carpeta final según el resultado.

El desarrollo se ejecutó en nueve tasks iterativas, cada una revisada por un
ciclo Planner → Backend-Implementer → Reviewer → Docs-Keeper antes de cerrar.
Todas las tasks están en estado `done`.

---

## Arquitectura general

La solución sigue una arquitectura en capas estricta:

- **Domain** — entidades y value objects sin dependencias externas.
- **Application** — contratos (interfaces), casos de uso, DTOs y opciones.
- **Infrastructure** — implementaciones: EF Core/SQLite, Azure OCR, filesystem.
- **Worker** — orquestación periódica del pipeline mediante BackgroundService.
- **Api** — proyecto preparado para endpoints futuros (sin lógica en el MVP).

El principio central es que las capas superiores no conocen las inferiores: Domain
no depende de EF, Application no depende de Infrastructure, la orquestación vive
exclusivamente en Worker.

---

## Lo desarrollado por task

### TASK-001 — Bootstrap de la solución

Se creó la estructura base del repositorio: solución `.sln`, cinco proyectos por
capa (Domain, Application, Infrastructure, Worker, Api), referencias entre proyectos,
carpetas `docs`, `src`, `tests` y `storage`, y un `README.md` inicial. El objetivo
fue dejar la solución lista para crecer sin reorganizaciones posteriores.

**Resultado:** solución compilando, arquitectura en capas establecida, sin lógica
de negocio todavía.

---

### TASK-002 — Persistencia SQLite con EF Core

Se introdujo la capa de datos completa.

**Entidades en Domain:**
- `Document` — representa un ticket procesado. Campos: `Id`, `FilePath`,
  `FileHash`, `RawJson`, `OcrStatus`, `ErrorMessage`, `CreatedAt`.
- `DocumentLine` — línea de detalle de un ticket. Campos: `Id`, `DocumentId`,
  `Description`, `Amount`, `Currency`.
- `ProcessingJob` — registro de cada intento de procesamiento. Campos: `Id`,
  `DocumentId`, `StartedAt`, `FinishedAt`, `Status`, `ErrorMessage`.

**Infrastructure:**
- `ExpenseFlowDbContext` con `DbSet<>` para las tres entidades y mapeo Fluent.
- `AddPersistence()` con SQLite, ruta `data/expenseflow.db` resuelta desde
  `ContentRootPath`.
- `MigrateAsync()` al arranque del Worker.
- Migración inicial `InitialCreate`.

**Tests:** test de integración con SQLite real — inserta y lee un `Document`.

---

### TASK-003 — Scanner de archivos con deduplicación por hash

Se implementó el componente que detecta tickets nuevos en la carpeta `inbox`.

**Contrato en Application:**
- `IFileScanner` con `GetPendingFilesToProcessAsync()`.
- `ScanResult` (record): `FullPath`, `FileHash`, `IsAlreadyInDatabase`.
- `StorageOptions` con rutas `Inbox`, `Processed`, `Error`.

**Implementación en Infrastructure:**
- `FileScanner` enumera archivos de primer nivel, filtra extensiones `jpg`,
  `jpeg`, `png`, `pdf` (sin distinción de mayúsculas), ignora archivos vacíos
  o inaccesibles, calcula hash SHA-256 en stream y consulta `Document.FileHash`
  en SQLite para descartar duplicados.
- Logs en cada paso: candidato nuevo, duplicado descartado, archivo ignorado,
  error de lectura.

**Tests:** filtrado de extensiones, archivo vacío ignorado, duplicado por hash.

---

### TASK-004 — Proveedor OCR con Azure Document Intelligence

Se introdujo la integración con Azure para extraer datos de tickets.

**Contrato en Application:**
- `IReceiptOcrProvider` — recibe ruta de archivo y `CancellationToken`, devuelve
  `OcrResult`.
- `OcrResult` (DTO): `MerchantName`, `TransactionDate`, `TotalAmount`,
  `TaxAmount`, `Currency`, `RawJson`, `Lines` (colección de líneas con
  `Description`, `Quantity`, `UnitPrice`, `Amount`).

**Implementación en Infrastructure:**
- `AzureDocumentIntelligenceReceiptProvider` usa el modelo `prebuilt-receipt`.
- Mapea todos los campos disponibles; maneja nulos sin romper el flujo.
- Preserva la respuesta cruda como `RawJson` serializable.
- Ningún tipo del SDK de Azure se expone fuera de Infrastructure.
- `AzureDocumentIntelligenceOptions` (`Endpoint`, `ApiKey`) bindeado desde
  configuración, nunca hardcodeado.
- Logs de error con contexto de archivo, sin exponer credenciales.

**Tests:** mapeo verificado contra JSON fijo, sin llamada real a Azure.

---

### TASK-005 — Normalizador de tickets

Se implementó la capa de transformación entre el resultado OCR y el modelo
de dominio.

**Campos añadidos a Domain:**
- `Document`: `MerchantName`, `TransactionDate`, `Currency`, `TotalAmount`,
  `TaxAmount`, `Confidence`.
- `DocumentLine`: `Quantity`, `UnitPrice`.
- Migración `AddDocumentNormalizationFields`.

**Contrato en Application:**
- `IReceiptNormalizer` con `Normalize(OcrResult, filePath, fileHash) → Document`.

**Implementación (`ReceiptNormalizer`):**
- Mapea todos los campos de `OcrResult` a `Document` y crea la colección de
  `DocumentLine` si hay líneas.
- Calcula `Confidence` como porcentaje de 6 ranuras clave presentes: comercio,
  fecha, total, impuesto, moneda, al menos una línea.
- Define `OcrStatus`:
  - `Success` si hay comercio o total.
  - `Partial` si hay fecha, impuesto, moneda o líneas pero sin mínimo.
  - `Failed` si no hay señal estructurada.
- Nunca lanza excepción por campos ausentes; siempre produce un `Document` con
  estado coherente.
- `ReceiptOcrStatuses` como clase de constantes en Application.

**Tests:** mapeo completo, parcial, fallido, con y sin líneas, recorte de texto.

---

### TASK-006 — Movimiento de archivos y deduplicación

Se implementó el componente responsable de mover archivos a su carpeta final.

**Contrato en Application:**
- `IFileMover` con `MoveToProcessedAsync` y `MoveToErrorAsync`, ambos devuelven
  la ruta destino final.

**Implementación en Infrastructure (`FileMover`):**
- Mueve archivos a `{Processed|Error}/yyyy/MM/` en UTC.
- Crea los subdirectorios destino si no existen.
- Resuelve colisiones de nombre con sufijo `{nombre}_{guid}{ext}` sin
  sobrescribir ni eliminar el original.
- Logs: éxito con origen y destino, colisión resuelta, error con excepción.
- No consulta DbContext ni contiene lógica de deduplicación por hash — esa
  responsabilidad permanece en `FileScanner`.

**Tests:** movimiento a `processed`, movimiento a `error`, colisión resuelta,
árbol `yyyy/MM/` creado automáticamente.

---

### TASK-007 — Worker loop (pipeline end-to-end)

Esta task ensambló todas las piezas anteriores en un pipeline automático.

**`WorkerOptions`:** `IntervalSeconds` (mínimo 1, configurable por
`Worker:IntervalSeconds`).

**`ExpenseFlowWorker` (BackgroundService):**
- Genera `JobId` al inicio de cada ciclo.
- Usa `SemaphoreSlim(1,1)` para evitar ciclos solapados — si el ciclo anterior
  sigue activo, se registra una advertencia y se espera el intervalo.
- Pipeline por archivo:
  1. `IFileScanner.GetPendingFilesToProcessAsync()` → lista de candidatos.
  2. `IReceiptOcrProvider.AnalyzeReceiptAsync()` → `OcrResult`.
  3. `IReceiptNormalizer.Normalize()` → `Document` + `DocumentLine[]`.
  4. `ExpenseFlowDbContext.SaveChangesAsync()` → persiste `Document`,
     `DocumentLine[]` y `ProcessingJob(Success)`.
  5. `IFileMover.MoveToProcessedAsync()` → archivo a `processed/yyyy/MM/`.
- En caso de error antes de persistir: `ChangeTracker.Clear()`, persiste
  documento de error + `ProcessingJob(Failed)`, mueve a `error/yyyy/MM/`.
- En caso de error después de persistir: actualiza documento y job a fallido,
  intenta mover a `error/`.
- Log de inicio y fin de ciclo con métricas: archivos encontrados, procesados
  con éxito, fallidos.
- `CancellationToken` propagado a todos los pasos para shutdown limpio.

**Tests:** smoke test con SQLite temporal, scanner stub (inbox vacío) y OCR stub.
El host arranca y completa un ciclo sin excepción.

---

### TASK-008 — Configuración y secretos

Se formalizó la validación de configuración al arranque con fail-fast explícito.

**Validación añadida:**
- `StorageOptions`: `[Required]` en `Inbox`, `Processed`, `Error`.
- `AzureDocumentIntelligenceOptions`: `[Required]` en `Endpoint`, `ApiKey`.
- `WorkerOptions`: `[Range(1, int.MaxValue)]` en `IntervalSeconds`.
- Todas con `ValidateDataAnnotations() + ValidateOnStart()`.
- `ExpenseFlowConnectionStringValidator`: valida que `ConnectionStrings:ExpenseFlow`
  esté presente y no vacío antes de intentar conectar a SQLite. Lanza
  `InvalidOperationException` con mensaje claro si falta.

**Archivos de configuración:**
- `appsettings.json`: placeholders vacíos para Azure; connection string con
  ruta relativa para SQLite. Falla en Production sin env vars o User Secrets.
- `appsettings.Development.json`: placeholders no reales para Azure
  (`placeholder.invalid`); permite arrancar en Development para validar la
  estructura de configuración sin credenciales reales.

**Convención de variables de entorno (.NET):**

| Variable | Descripción |
|---|---|
| `AzureDocumentIntelligence__Endpoint` | URL del recurso Azure |
| `AzureDocumentIntelligence__ApiKey` | Clave del recurso Azure |
| `ConnectionStrings__ExpenseFlow` | Ruta al archivo SQLite |
| `Storage__Inbox` | Carpeta de entrada |
| `Storage__Processed` | Carpeta de archivos procesados |
| `Storage__Error` | Carpeta de archivos fallidos |
| `Worker__IntervalSeconds` | Intervalo entre ciclos |

---

### TASK-009 — Logging básico

Se elevó la calidad del logging para permitir observabilidad operativa completa.

**Correlación por `JobId`:**
- Al inicio de cada ciclo, `ExpenseFlowWorker` genera un `Guid` (`JobId`) y
  abre `ILogger.BeginScope` con ese campo.
- Todos los logs del ciclo — incluyendo los emitidos por servicios de
  Infrastructure invocados dentro del scope — llevan `JobId` automáticamente.

**Estructura de eventos y niveles:**

| Nivel | Evento |
|---|---|
| Information | Inicio de job (JobId, timestamp) |
| Information | Fin de job (JobId, duración, FilesFound, ProcessedOk, Failed) |
| Information | Archivo candidato detectado (JobId, FileName) |
| Information | Archivo movido a `processed` (JobId, destino) |
| Warning | Duplicado por hash (JobId, FileName, hash abreviado) |
| Warning | Archivo movido a `error` (JobId, FileName, motivo) |
| Error | OCR fallido (JobId, FileName, FullPath, excepción) |
| Error | Persistencia fallida (JobId, FileName, FullPath, excepción) |

**Sanitización verificada:** ningún log expone `ApiKey`, connection string ni
rutas absolutas sensibles. `FullPath` se incluye solo en eventos de error donde
aporta diagnóstico real.

**Niveles de consola en `appsettings.json`:**
- `Default`: Information
- `Microsoft`: Warning
- `System`: Warning
- `Console:IncludeScopes`: true (el `JobId` del scope es visible en consola)

---

## Estado del MVP al cierre

El flujo completo está operativo:

```
storage/familia/inbox/
        ↓  FileScanner (hash SHA-256, dedup por BD)
        ↓  AzureDocumentIntelligenceReceiptProvider (prebuilt-receipt)
        ↓  ReceiptNormalizer (Document + DocumentLines + OcrStatus + Confidence)
        ↓  ExpenseFlowDbContext.SaveChangesAsync (SQLite)
        ↓
storage/familia/processed/yyyy/MM/   ← éxito
storage/familia/error/yyyy/MM/       ← fallo
```

El Worker corre automáticamente cada N segundos (configurable), sin ciclos
solapados, con logging estructurado por `JobId` y validación de configuración
al arranque.

---

## Cobertura de tests al cierre

| Proyecto | Tests | Qué cubren |
|---|---|---|
| `ExpenseFlow.IntegrationTests` | 15 | SQLite insert/read, FileMover (4 casos), smoke del Worker |
| `ExpenseFlow.Application.Tests` | 11 | ReceiptNormalizer (6 casos: completo, parcial, fallido, líneas, solo líneas, recorte) + FileScanner (extensiones, vacío, hash dup) |

---

## Posibles próximas tasks

### Área: API y consulta

**TASK-010 — API mínima de consulta de documentos**
Exponer endpoints HTTP en el proyecto `Api` para listar documentos procesados,
consultar el detalle de un documento (incluyendo líneas y RawJson) y filtrar por
fecha o comercio. El DbContext ya está preparado; solo falta registrarlo en el
host de Api y crear los controllers o minimal APIs.

**TASK-011 — Endpoint de reproceso manual**
Permitir marcar un documento con `OcrStatus=Failed` para que vuelva a intentarse
en el próximo ciclo del Worker. Requiere lógica de estado en el pipeline y un
endpoint en Api.

---

### Área: calidad de datos y análisis

**TASK-012 — Categorización de gastos**
Asignar una categoría (supermercado, combustible, restaurante, etc.) a cada
documento basándose en el nombre del comercio o en reglas configurables. Puede
ser un clasificador simple basado en diccionario o una llamada adicional al
proveedor OCR.

**TASK-013 — Exportación a CSV**
Generar un archivo CSV con el histórico de documentos para importar en Excel,
Google Sheets u otras herramientas de análisis. Endpoint en Api o tarea
programada en el Worker.

**TASK-014 — Índice único en FileHash**
Agregar una restricción única a nivel de base de datos sobre `Document.FileHash`
para prevenir duplicados en condiciones de concurrencia extrema (señalado como
mejora no bloqueante por el Reviewer de TASK-003).

---

### Área: robustez y operación

**TASK-015 — Reintentos con backoff**
Implementar reintentos automáticos (con backoff exponencial) para fallos
transitorios de OCR (timeout, error 429, error de red). Usar Polly o la
biblioteca de resiliencia de .NET. Actualmente un fallo mueve el archivo a
`error/` sin reintento.

**TASK-016 — Validación semántica del Endpoint de Azure**
Verificar que `AzureDocumentIntelligenceOptions.Endpoint` es una URI HTTP/HTTPS
válida al arranque, además de que no está vacío. Señalado como mejora no
bloqueante por el Reviewer de TASK-008.

**TASK-017 — Tests de validación de configuración**
Agregar tests que verifiquen que la app lanza `OptionsValidationException` con
mensaje claro para cada campo de configuración faltante (Storage, Azure, Worker,
ConnectionString). Señalado por el Reviewer de TASK-008.

**TASK-018 — Observabilidad avanzada**
Integrar Serilog (o equivalente) con sink a archivo para tener logs persistentes
entre reinicios. Opcionalmente, agregar OpenTelemetry para trazas distribuidas
si el sistema crece hacia múltiples componentes.

---

### Área: producto y experiencia

**TASK-019 — UI web mínima de revisión**
Interfaz web (Blazor, React o similar) para visualizar el listado de documentos
procesados, ver el detalle de cada uno y corregir manualmente campos mal
extraídos por el OCR.

**TASK-020 — Soporte multi-familia / multi-usuario**
Introducir el concepto de `Family` o `User` para que la solución pueda gestionar
gastos de múltiples perfiles con aislamiento de datos.

**TASK-021 — División de gastos familiares**
Permitir asignar un porcentaje de cada gasto a cada miembro de la familia y
calcular balances periódicos.

**TASK-022 — App móvil**
Cliente móvil (MAUI o React Native) que permita fotografiar un ticket y subirlo
directamente a la carpeta `inbox` sincronizada, cerrando el ciclo sin intervención
de escritorio.
