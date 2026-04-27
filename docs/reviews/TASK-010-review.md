# TASK-010 — Revisión (Reviewer)

## Criterios de aceptación

| Criterio | Estado |
| --- | --- |
| GET /documents lista paginada con campos mínimos (sin RawJson en listado) | Cumple: `DocumentSummaryDto` + `DocumentsListResponseDto` |
| GET /documents/{id} detalle con líneas y RawJson | Cumple: `DocumentDetailDto` + `Include` de líneas |
| Filtros `from` / `to` / `status` | Cumple en query EF |
| 404 claro si no existe | Cumple: JSON con `error` e `id` |
| DTOs en Application, sin exponer entidades de dominio en HTTP | Cumple |
| Build y tests | `dotnet build` y `dotnet test` correctos |

## Hallazgos críticos

Ninguno bloqueante para merge. Durante la implementación se corrigió:

- Orden del listado: SQLite con EF Core 9 no soporta `ORDER BY` sobre `DateTimeOffset` en servidor; la implementación ordena por `Id` descendente (autoincremental, alineado con inserción reciente). Documentado en `docs/architecture.md` y comentario en código.
- Pruebas con `WebApplicationFactory`: la cadena de conexión debe coincidir exactamente con la del host (`UseSetting` + propiedad en memoria); el entorno `Testing` evita `Migrate()` en `Program` y el test prepara el esquema con `EnsureCreated` sobre el mismo fichero SQLite que el host.

## Mejoras recomendadas (no bloqueantes)

- Si hace falta orden estrictamente por `CreatedAt`, valorar columna auxiliar numérica (p. ej. ticks UTC) o proyección SQL específica para SQLite.
- Añadir `Microsoft.AspNetCore.OpenApi` / Swagger en Development para pruebas manuales sin curl (opcional).

## Decisión

**approve**
