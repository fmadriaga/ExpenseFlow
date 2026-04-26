# ExpenseFlow

Procesamiento de tickets (OCR) desde una carpeta sincronizada, con persistencia SQLite. MVP documentado en `AGENTS.md` y `docs/architecture.md`.

## Requisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download) (misma línea major que el target de los proyectos)

## Estructura del repositorio

| Ruta | Uso |
| --- | --- |
| `src/ExpenseFlow.Domain` | Entidades y reglas de dominio |
| `src/ExpenseFlow.Application` | Casos de uso, contratos, DTOs internos |
| `src/ExpenseFlow.Infrastructure` | EF Core, integraciones, filesystem |
| `src/ExpenseFlow.Worker` | Proceso por lotes en segundo plano |
| `src/ExpenseFlow.Api` | Host HTTP para evolución futura |
| `docs/` | Visión, arquitectura y tasks |
| `tests/` | Pruebas (aún no hay proyectos) |
| `data/` | Datos locales (p. ej. base SQLite) |
| `storage/familia/` (subcarpetas `inbox`, `processed`, `error`) | Inbox y salidas de archivos (según arquitectura) |

## Compilar

```bash
dotnet build ExpenseFlow.sln -c Release
```

## Ejecutar (placeholder)

- Worker: `dotnet run --project src/ExpenseFlow.Worker`
- API: `dotnet run --project src/ExpenseFlow.Api`

Aún no hay lógica de negocio ni integración OCR; el arranque valida el host mínimo.

## Referencia de capas

- `Application` referencia `Domain`
- `Infrastructure` referencia `Application` y `Domain`
- `Worker` y `Api` referencian `Application` e `Infrastructure`

Más detalle: `docs/architecture.md`.
