# Informe de configuración y setup para primera prueba E2E con Azure Document Intelligence

**Contexto:** Tasks 001–021 marcadas como done. Este documento resume lo necesario para una primera corrida real del pipeline (Worker → OCR Azure → SQLite → API → Web), según `docs/architecture.md`, `AGENTS.md` y los `appsettings.json` actuales del repositorio.

---

## 1. Credenciales Azure Document Intelligence (Worker)

### Valores exactos en configuración

En `src/ExpenseFlow.Worker/appsettings.json`, la sección base tiene **`Endpoint` y `ApiKey` vacíos** (correcto para no versionar secretos):

```json
"AzureDocumentIntelligence": {
  "Endpoint": "",
  "ApiKey": "",
  "MaxRetries": 3,
  "BaseDelaySeconds": 1.0
}
```

Para ejecutar OCR real debes aportar valores **no vacíos** tras la fusión de configuración (User Secrets, variables de entorno o ficheros locales no versionados):

| Clave JSON | Variable de entorno equivalente | Descripción |
| --- | --- | --- |
| `AzureDocumentIntelligence:Endpoint` | `AzureDocumentIntelligence__Endpoint` | URI absoluta del recurso (`http` o `https`). La validación (TASK-016) exige URI absoluta válida. |
| `AzureDocumentIntelligence:ApiKey` | `AzureDocumentIntelligence__ApiKey` | Una de las dos claves del recurso en Azure Portal. |

Opcionales ya definidas en appsettings: `MaxRetries`, `BaseDelaySeconds` (reintentos TASK-015).

### Cómo obtenerlas en Azure Portal

1. Portal Azure → **Todos los recursos** → tu recurso de **Document Intelligence** (antes “Form Recognizer”) o recurso **Cognitive Services** multi-servicio compatible con Document Intelligence.
2. En el menú lateral del recurso, abre **Claves y punto de conexión** (o **Keys and Endpoint**).
3. **Endpoint:** copia la URL completa (ejemplo de forma típica: `https://<nombre-recurso>.cognitiveservices.azure.com/`). Debe coincidir con la región donde creaste el recurso.
4. **ApiKey:** copia **KEY 1** o **KEY 2** (cualquiera de las dos sirve para llamadas API).

Notas:

- El proveedor usa el modelo **`prebuilt-receipt`** (`docs/architecture.md`); el recurso debe permitir Document Intelligence / análisis de recibos según la SKU elegida.
- En **`appsettings.Development.json`** del Worker hay placeholders (`https://placeholder.invalid` y texto placeholder para la clave): sirven solo para arrancar el host **sin** llamadas OCR válidas; para E2E real hay que sustituirlos por secretos reales (preferible User Secrets o variables de entorno).

---

## 2. Migraciones de base de datos (`data/expenseflow.db`)

### Rol del fichero

- `ConnectionStrings:ExpenseFlow` por defecto es **`../../data/expenseflow.db`** relativo al **ContentRoot** del proyecto que arranca (Worker y Api en sus respectivos `appsettings.json`).
- Arquitectura: Worker y Api aplican **`Migrate()`** al arranque (excepto Api en entorno `Testing`), por lo que el esquema suele actualizarse solo con ejecutar Worker o Api.

### Migraciones presentes en código

En `src/ExpenseFlow.Infrastructure/Migrations/` están al menos:

| Migración |
| --- |
| `20260426214511_InitialCreate` |
| `20260427001245_AddDocumentNormalizationFields` |
| `20260427193556_AddDocumentCategory` |
| `20260427194752_AddFileHashUniqueIndex` |
| `20260427210850_AddFamiliesAndDocumentFamilyScope` |
| `20260427211715_AddFamilyMembersExpenseSplitsPaidBy` |

### Comando para aplicar manualmente sobre SQLite

Desde la **raíz del repo** (PowerShell), usando entorno Development para evitar fallos de validación de opciones al construir el host del Worker (`README.md`):

```powershell
$env:DOTNET_ENVIRONMENT = 'Development'
dotnet ef database update --project src/ExpenseFlow.Infrastructure --startup-project src/ExpenseFlow.Worker
```

### Verificación

Para comprobar que no queden migraciones pendientes respecto al archivo que resuelve la cadena de conexión:

```powershell
$env:DOTNET_ENVIRONMENT = 'Development'
dotnet ef migrations list --project src/ExpenseFlow.Infrastructure --startup-project src/ExpenseFlow.Worker
```

Las migraciones **ya aplicadas** no deben aparecer con sufijo **`(Pending)`**. Si todas muestran `(Pending)`, la base `data/expenseflow.db` está vacía de historial EF o desfasada: ejecuta `dotnet ef database update` como arriba (o arranca Worker/Api una vez con cadena apuntando al mismo fichero).

---

## 3. Carpetas de storage y multi-familia (TASK-020)

### Fuente de verdad por familia

Tras TASK-020, el Worker **no** lista el inbox desde la sección JSON `Storage` para el batch principal: lee las filas de la tabla **`Families`** en SQLite (`docs/architecture.md`, resultado TASK-020). La migración `AddFamiliesAndDocumentFamilyScope` inserta datos iniciales:

| Id | Nombre | Rutas relativas al ContentRoot del Worker |
| --- | --- | --- |
| 1 | Default | `../../storage/familia/inbox`, `.../processed`, `.../error` |
| 2 | Familia 2 | `../../storage/familia2/inbox`, `.../processed`, `.../error` |

### ¿Hay que crear carpetas por familia adicional?

- **Sí, como mínimo los tres árboles por familia que vayas a usar**, en especial **`…/inbox`**: si el inbox no existe, el escáner registra advertencia y **no devuelve candidatos** para esa familia (`README.md`).
- **`processed`** y **`error`**: no es obligatorio crearlos antes; `FileMover` puede crear destinos al mover (`docs/architecture.md`, TASK-006). Aun así, crear `processed` y `error` evita sorpresas al inspeccionar el disco.
- La segunda familia (`familia2`) viene **sembrada en BD**: si quieres prueba solo familia 1, no es obligatorio crear `storage/familia2/...` hasta que proceses familia 2.

### Sección `Storage` en Worker y Api

Siguen existiendo `Storage:Inbox`, `Processed`, `Error` en `appsettings.json` (referencia y rutas por defecto para `FileMover`/`FileRestorer` cuando aplica). Mantén valores coherentes con la familia por defecto (`familia/...`) para reproceso/API (`docs/architecture.md`).

---

## 4. Arranque de proyectos (orden y comandos desde la raíz del repo)

Convención del repo (`README.md`): rutas SQLite y `storage` relativas al ContentRoot de cada proyecto (`src/...`). Ejecuta siempre desde **`c:\dev\ExpenseFlow`** (o raíz clonada equivalente).

### Orden recomendado para smoke E2E completo

1. **Aplicar migraciones** (si no confías en el primer arranque): ver §2.
2. **Configurar secretos OCR** (User Secrets o variables): §1 y §6.
3. **Worker** — alimenta el pipeline batch (OCR + persistencia + movimiento de archivos).
4. **API** — expone documentos y debe usar la **misma** base SQLite y rutas `Storage` coherentes.
5. **Web** — consume la API vía `ExpenseFlowApi:BaseUrl`; **requiere la API en marcha**.

### Comandos exactos (tres terminales desde la raíz)

Perfil HTTP por defecto en `launchSettings.json`:

```powershell
dotnet run --project src/ExpenseFlow.Worker
```

```powershell
dotnet run --project src/ExpenseFlow.Api --launch-profile http
```

```powershell
dotnet run --project src/ExpenseFlow.Web --launch-profile http
```

URLs típicas según `Properties/launchSettings.json`:

| Proyecto | HTTP |
| --- | --- |
| Api | `http://localhost:5287` |
| Web | `http://localhost:5168` |

`src/ExpenseFlow.Web/appsettings.json` define **`ExpenseFlowApi:BaseUrl`** = `http://localhost:5287`, alineado con el perfil `http` de la Api.

---

## 5. Checklist de smoke test (mínimo viable)

1. **Preparación:** carpeta `storage/familia/inbox` existente; depositar un ticket **`jpg`/`png`/`pdf`** válido y no vacío en ese inbox (primer nivel; el escáner no profundiza subcarpetas).
2. **Worker:** tras un ciclo (`Worker:IntervalSeconds`, por defecto 60 s), revisar **consola** o **`logs/expenseflow-*.log`** (Serilog TASK-018): correlación por `JobId`, métricas de fin de ciclo (**files found**, procesados OK / fallidos). Sin errores de OCR si Azure está bien configurado.
3. **Filesystem:** el archivo debe abandonar el inbox y aparecer bajo **`storage/familia/processed/<yyyy>/<MM>/`** (UTC), salvo fallo de pipeline → **`error/…`**.
4. **API:** `GET http://localhost:5287/documents` (opcional `familyId=1`) → debe listar el documento nuevo; `GET http://localhost:5287/documents/{id}` → detalle con líneas y `RawJson`.
5. **Web:** abrir `http://localhost:5168`, confirmar listado/detalle acorde a la API y misma familia (`ExpenseFlowApi:FamilyId`, defecto `1`).

---

## 6. Secretos en Development vs Production

### Patrón recomendado (alineado con `docs/architecture.md` y `README.md`)

| Entorno | Dónde poner Endpoint / ApiKey / cadena SQLite |
| --- | --- |
| **Development local** | **`dotnet user-secrets`** en el proyecto Worker (y variables equivalentes si prefieres): no se suben al repo. Ejemplo ya documentado en `README.md`: `dotnet user-secrets set "AzureDocumentIntelligence:Endpoint" "https://..." --project src/ExpenseFlow.Worker`. Para Api, si algún día necesitas secretos distintos, mismo patrón en `ExpenseFlow.Api`. |
| **Production / servidores** | **Variables de entorno** del proceso (incl. **Azure App Configuration**, **Key Vault references**, **Docker secrets**, **Kubernetes secrets**) con las claves `AzureDocumentIntelligence__Endpoint`, `AzureDocumentIntelligence__ApiKey`, `ConnectionStrings__ExpenseFlow`, etc. El **`appsettings.json` base** puede dejar Azure vacío: la validación obliga a configurarlos vía entorno en **Production** (`architecture.md`, TASK-008). |

### Qué no hacer

- No commitear **`ApiKey`** ni endpoints productivos en `appsettings.json`.
- No registrar secretos reales en **`appsettings.Development.json`** si ese archivo se comparte en equipo (preferir User Secrets por máquina).

### `.env`

El estándar documentado en el repo es **variables de entorno + User Secrets** (.NET). Un fichero **`.env`** solo es adecuado si el equipo lo usa con herramientas que lo cargan y **`*.env` está en `.gitignore`**; no está descrito como obligatorio en la documentación actual del proyecto.

---

## 7. Resumen de configuraciones que suelen faltar antes del primer E2E real

| Ítem | Estado típico |
| --- | --- |
| `AzureDocumentIntelligence:Endpoint` / `ApiKey` en el Worker | Vacíos en `appsettings.json`; placeholders en Development → sustituir por secretos reales para OCR. |
| Base SQLite migrada | Ejecutar `dotnet ef database update` o arrancar Worker/Api al menos una vez contra la misma ruta `data/expenseflow.db`. |
| Carpetas `storage/familia/inbox` (y opcionalmente familia 2) | Crear manualmente los inbox usados; BD ya puede referenciar `familia2`. |
| Api + Web | Api debe estar arriba antes que Web; `BaseUrl` de Web debe coincidir con el puerto real de la Api. |

---

*Documento generado como revisión de setup E2E; basado en `docs/architecture.md`, `AGENTS.md`, `README.md`, `src/ExpenseFlow.Worker/appsettings*.json`, `src/ExpenseFlow.Api/appsettings.json`, `src/ExpenseFlow.Web/appsettings.json`, `docs/tasks/TASK-020-multi-family.md`, y verificación con `dotnet ef migrations list` (2026-04-27).*
