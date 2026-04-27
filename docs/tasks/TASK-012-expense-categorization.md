# TASK-012 Categorización de gastos

status: done
owner: backend
priority: medium

## Goal
Asignar una categoría a cada documento procesado en función del nombre
del comercio u otras señales del ticket.

## Context
Los documentos ya se persisten con MerchantName y líneas de detalle.
Categorizar los gastos es el primer paso hacia análisis y presupuesto.
Para el MVP debe ser un clasificador simple sin ML ni servicios externos.

## Scope
- Definir entidad o campo Category en Document (string, nullable)
- Implementar IExpenseCategorizer en Application con CategorizeAsync(Document)
- Implementar un categorizador basado en diccionario configurable:
  palabras clave en MerchantName → categoría (supermercado, combustible,
  restaurante, farmacia, otros)
- Aplicar la categorización en el pipeline del Worker después de normalizar
  y antes de persistir
- Agregar migración para el campo Category

## Out of scope
- Clasificación con ML o LLM
- UI para gestionar reglas
- Categorización manual desde la API
- Subcategorías

## Acceptance Criteria
- Document tiene campo Category persistido en SQLite
- El categorizador asigna una categoría correcta para comercios conocidos
- Si no hay coincidencia, Category queda como "otros" (nunca null)
- Las reglas de categorización son configurables sin recompilar
  (por ejemplo, sección CategoryRules en appsettings)
- La categorización no rompe el pipeline si el categorizador falla

## Technical Notes
- IExpenseCategorizer vive en Application; implementación en Infrastructure
  o Application según si necesita I/O
- Las reglas pueden ser un diccionario en appsettings.json:
  {"supermercado": ["walmart","carrefour","disco"], "combustible": ["ypf","shell"]}
- Comparación case-insensitive y por substring
- Agregar Category al DTO de respuesta de GET /documents

## Suggested Files
- src/ExpenseFlow.Application/Abstractions/IExpenseCategorizer.cs
- src/ExpenseFlow.Application/Categorization/KeywordExpenseCategorizer.cs
- src/ExpenseFlow.Application/Options/CategoryOptions.cs
- src/ExpenseFlow.Domain/Entities/Document.cs
- src/ExpenseFlow.Infrastructure/Migrations/...

## Definition of Done
- build exitoso
- tests unitarios del categorizador (coincidencia, sin coincidencia, case)
- migración aplicada y campo visible en BD
