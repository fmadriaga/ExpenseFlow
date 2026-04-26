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
- **Entidades (Domain):** `Document` (campos: `FilePath`, `FileHash`, `RawJson` para
  auditoría OCR, `OcrStatus`, `ErrorMessage`, `CreatedAt`), `DocumentLine`, `ProcessingJob`.

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
