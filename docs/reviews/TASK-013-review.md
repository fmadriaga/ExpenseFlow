# Review TASK-013 — Exportación a CSV

## Acceptance criteria

| Criterio | Estado |
|----------|--------|
| GET `/documents/export` → CSV con encabezados | OK |
| Filtros `from` / `to` / `status` | OK — misma lógica que listado (`ApplyDocumentFilters`) |
| Content-Disposition para descarga | OK |
| Vacío → solo cabecera | OK — integración |
| Escape RFC 4180 (comas, comillas) | OK — pruebas unitarias + integración |

## Arquitectura

- `ICsvExporter` y `DocumentCsvExporter` en Application; sin dependencia de EF.
- API proyecta a `DocumentExportRow`, streaming con `AsAsyncEnumerable`.
- Delimitador `,` o `;` vía query `delimiter` (sin nuevas options en host).

**Veredicto:** aprobado.
