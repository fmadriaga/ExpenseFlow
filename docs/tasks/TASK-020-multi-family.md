# TASK-020 Soporte multi-familia / multi-usuario

status: done
owner: backend
priority: low

## Goal
Introducir el concepto de Family (o Profile) para que la solución pueda
gestionar gastos de múltiples perfiles con aislamiento de datos.

## Context
El MVP está diseñado para un solo usuario/familia. Para escalar a uso
compartido se necesita un modelo de datos que aísle documentos por
perfil y una forma de identificar a qué perfil pertenece cada carpeta
de inbox.

## Scope
- Agregar entidad Family (Id, Name, InboxPath, ProcessedPath, ErrorPath)
- Asociar Document a Family mediante FamilyId
- Migración de base de datos para las nuevas entidades y relaciones
- Adaptar StorageOptions para soportar múltiples configuraciones de carpetas
  (una por familia) o leer la familia desde el path del archivo
- Filtrar endpoints de Api por FamilyId (query param o header)
- Mantener compatibilidad con datos existentes (familia default)

## Out of scope
- Autenticación de usuarios
- Permisos entre familias
- UI multi-familia
- Facturación o planes

## Acceptance Criteria
- Existe la entidad Family en Domain con migración aplicada
- Document tiene FamilyId y la relación está correctamente mapeada
- El Worker puede procesar archivos para múltiples familias configuradas
- Los endpoints de Api filtran por FamilyId correctamente
- Los datos existentes migran a una familia default sin pérdida

## Technical Notes
- FamilyId en Document puede ser nullable durante la migración para
  compatibilidad con datos existentes; luego requerir con default
- La configuración de múltiples familias puede ser una lista en
  appsettings: [{ Name, InboxPath, ProcessedPath, ErrorPath }]
- El Worker itera sobre familias configuradas en cada ciclo

## Suggested Files
- src/ExpenseFlow.Domain/Entities/Family.cs
- src/ExpenseFlow.Infrastructure/Data/ExpenseFlowDbContext.cs
- src/ExpenseFlow.Infrastructure/Migrations/...
- src/ExpenseFlow.Application/Options/StorageOptions.cs

## Definition of Done
- build exitoso
- migración aplicada sobre BD existente sin pérdida de datos
- Worker procesa dos familias con carpetas distintas en el mismo ciclo
- endpoints de Api filtran por FamilyId correctamente

---

## Resultado
- Entidad `Family` y `Document.FamilyId` (FK, restrict). Índice único `(FamilyId, FileHash)` (sustituye al único solo por `FileHash`).
- Las familias y rutas viven en **SQLite**; la migración `AddFamiliesAndDocumentFamilyScope` crea `Families`, inserta dos filas (Default bajo `../../storage/familia/...` y "Familia 2" bajo `../../storage/familia2/...`) y asigna `Document.FamilyId = 1` por defecto.
- **Worker:** en cada ciclo lee `Families`, resuelve rutas con `ContentRootPathResolver` (misma idea que `Storage` relativo al `ContentRoot`) y llama a `IFileScanner` por familia; `IFileMover` / movimientos usan las raíces de esa familia vía `ScanResult`.
- **API:** query `familyId` (defecto `1`) en listado, detalle, PATCH, export y reprocess. Reproceso: `IFileRestorer` con `ErrorPath` / `InboxPath` de la **Family** del documento (resueltos con el `ContentRoot` de la Api).
- **Web:** `ExpenseFlowApi:FamilyId` en `appsettings` y parámetro en las peticiones.
- `StorageOptions` se mantiene para Api (reproceso/compat) y raíces por defecto de `FileMover`/`FileRestorer` cuando se usan las sobrecargas sin raíz explícita.

## Archivos principales
- `src/ExpenseFlow.Domain/Entities/Family.cs`, `Document.cs`
- `src/ExpenseFlow.Infrastructure/Configuration/ContentRootPathResolver.cs`
- `src/ExpenseFlow.Infrastructure/Migrations/20260427210850_AddFamiliesAndDocumentFamilyScope.cs`
- `FileScanner`, `FileMover`, `FileRestorer`, `ExpenseFlowWorker`, `DocumentsEndpoints`
- `src/ExpenseFlow.Web` (FamilyId en consultas)
- Tests de integración actualizados (Migrate + alinear rutas de Family 1 en fábricas de prueba)

## Pendientes
- UI multi-familia (selector) sigue fuera de alcance; solo parámetro de configuración.
- Añadir o editar familias: hoy vía SQL o migración; sin endpoint CRUD de familias.

**Cierre:** 2026-04-27
