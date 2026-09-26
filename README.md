# Plataforma de Incidencias 🚲

Sistema de gestión de averías para empresas de bicicletas compartidas, desarrollado en **ASP.NET Core 8 MVC**. Permite registrar averías en estaciones, buscarlas a alta velocidad con Algolia, consultar el listado con latencia mínima gracias a Redis y recibir actualizaciones reactivas en tiempo real mediante WebSockets con PieHost.

---

## 🚀 Despliegue en Producción (Render)

- **URL Pública:** `https://plataforma-incidencias.onrender.com` (o la URL de tu Web Service en Render)
- **Hash del commit desplegado en main:** `a54cd95`
- **Ancestro común inicial (COMMIT0):** `d724701`

---

## ⚙️ Configuración de Variables de Entorno en Render

Configura las siguientes variables en el panel de **Environment** de Render (nunca en el código fuente):

| Variable de Entorno | Descripción | Ejemplo / Valor |
|---------------------|-------------|-----------------|
| `ASPNETCORE_ENVIRONMENT` | Entorno de ejecución | `Production` |
| `PORT` | Puerto de escucha HTTP | `8080` (inyectado automáticamente por Render) |
| `ConnectionStrings__DefaultConnection` | Conexión a SQLite | `Data Source=incidencias.db` |
| `Algolia__AppId` | Application ID de Algolia | *Tu App ID de Algolia* |
| `Algolia__AdminKey` | Admin API Key (server-side ONLY) | *Tu Admin Key de Algolia* |
| `Algolia__SearchKey` | Search-Only API Key | *Tu Search Key de Algolia* |
| `Algolia__IndexName` | Nombre del índice | `incidencias` |
| `Redis__ConnectionString` | Host:puerto o URI de Redis | `red-xxxxxxxxxxxx:6379` (o Redis gestionado) |
| `PieHost__ChannelUrl` | URL del canal WebSocket PieHost | `wss://ws-<cluster>.piehost.com/v1/...` |
| `PieHost__ChannelKey` | API Key del canal PieHost | *Tu API Key de PieHost* |

---

## 🌳 Árbol de Ramas y Commits (`git log --graph --oneline --all`)

El repositorio refleja fielmente la divergencia desde el commit inicial y la resolución limpia de conflictos:

```text
*   a54cd95 Merge main en websocket-piehost: resuelve conflicto de titulo, conserva busqueda, cache y websocket
|\  
| *   4233e6e Merge main en cache-redis: resuelve conflicto de titulo, conserva busqueda y cache
| |\  
| | * 9926242 feat(algolia): implementa busqueda de incidencias server-side con AlgoliaSearchService y filtro Estado=Abierta
| * | a94deed feat(redis): implementa cache distribuida en listado general con invalidacion al cerrar y logging explicito
| |/  
* / 3417047 feat(piehost): implementa publicacion de eventos WebSocket tras persistir en DB y sincronizacion reactiva DOM
|/  
* d724701 COMMIT0: feat: bootstrap PlataformaIncidencias - MVC + Identity + EF Core SQLite + modelo Incidencia + seed 8 registros + OperacionesController + vista base
*   32fc018 Merge pull request #1 from Jhordancl/feature/bootstrap-dominio
|\  
| * ced960a feat: bootstrap proyecto MVC + Identity + modelo de dominio (Cliente, SolicitudCredito) + seed inicial
|/  
* 9cbdff7 git init
```

---

## 🔀 Explicación de las Resoluciones de Conflictos

### Conflicto 1: Integración de PR B (`feature/cache-redis`) con `main` (que ya contenía PR A)
- **Archivos en conflicto:** `Program.cs`, `Controllers/OperacionesController.cs`, `Views/Operaciones/Incidencias.cshtml`.
- **Causa del conflicto en `<h1>`:**
  - En `main` (PR A - Algolia): `<h1>Incidencias abiertas encontradas</h1>`
  - En `feature/cache-redis` (PR B - Redis): `<h1>Incidencias abiertas con consulta rápida</h1>`
- **Resolución:**
  - Se unificó el encabezado a: `<h1>Incidencias abiertas encontradas con consulta rápida</h1>`.
  - En `OperacionesController.cs`: Se preservó tanto `AlgoliaSearchService` como `IDistributedCache`. Si el usuario envía un término de búsqueda (`q`), se consulta directamente Algolia y se omite la caché; si el listado es general, se atiende desde Redis por 60 segundos.
  - Al cerrar una incidencia, se invalida la clave de caché en Redis antes del redirect.
- **Commit de resolución:** `4233e6e`.

### Conflicto 2: Integración de PR C (`feature/websocket-piehost`) con `main` (que ya contenía PR A + B)
- **Archivos en conflicto:** `Controllers/OperacionesController.cs`, `Views/Operaciones/Incidencias.cshtml`.
- **Causa del conflicto en `<h1>`:**
  - En `main` (PR A + B): `<h1>Incidencias abiertas encontradas con consulta rápida</h1>`
  - En `feature/websocket-piehost` (PR C): `<h1>Incidencias abiertas en tiempo real</h1>`
- **Resolución:**
  - Se unificó el encabezado a: `<h1>Incidencias abiertas encontradas en tiempo real con consulta rápida</h1>`.
  - Se conservó la búsqueda con Algolia, la caché distribuida con Redis y la reactividad WebSocket con PieHost.
  - En la vista, se mantuvieron el formulario de búsqueda, el badge de estado WebSocket en vivo y el script reactivo que escucha eventos `IncidenciaActualizada` para eliminar filas en el DOM y consulta el listado en `onopen`.
- **Commit de resolución:** `a54cd95`.

---

## 🧪 Pruebas de Producción (Secuencia de 2 Sesiones Simultáneas)

1. **Acceso:** Iniciar sesión en dos navegadores o pestañas de incógnito diferentes usando:
   - **Usuario:** `supervisor@incidencias.com`
   - **Contraseña:** `Supervisor1234!`
2. **Pestaña 1 (Búsqueda Algolia):**
   - Escribir "freno" o "candado" en el buscador. El servidor consulta Algolia sin pasar por caché y filtra únicamente las incidencias con `Estado == "Abierta"`.
3. **Pestaña 2 (Caché Redis y Listado General):**
   - Acceder al listado general (`/Operaciones/Incidencias`). La primera petición genera un `[DATABASE HIT]` y guarda en Redis con TTL de 60s. Al refrescar dentro de ese minuto, se evidencia un `[REDIS HIT]`.
4. **Verificación en Tiempo Real (Secuencia Completa):**
   - En la Pestaña 1, hacer clic en **Cerrar** en una incidencia abierta.
   - **Flujo ejecutado en el backend:**
     1. El estado cambia a "Cerrada" y se guarda en SQLite.
     2. Se invalida la clave `incidencias_abiertas_listado` en Redis.
     3. Se publica el evento `IncidenciaActualizada` en PieHost.
   - **Efecto en la Pestaña 2:** Sin recargar la página, la fila de la incidencia cerrada se resalta en rojo y desaparece inmediatamente del DOM vía WebSocket.
   - **Efecto en la Búsqueda:** Al volver a buscar esa avería en Algolia, ya no aparece porque el controlador filtra por `Estado == "Abierta"`.
