# Pull Requests — Plataforma de Incidencias

## PR A — feature/busqueda-algolia → main

**Estado:** Fusionado ✅  
**Rama origen:** `feature/busqueda-algolia`  
**Rama destino:** `main`  
**Ancestro común:** COMMIT0 (`d724701`)

### Cambios incluidos:
- Nuevo servicio `AlgoliaSearchService` (server-side). La `AdminKey` nunca se expone al navegador.
- `OperacionesController.Incidencias` acepta parámetro `?q=texto`:
  - Sin texto → listado directo desde DB.
  - Con texto → consulta Algolia, filtra resultados por `Estado == "Abierta"` en DB.
- Vista actualizada con formulario de búsqueda GET.
- `<h1>` cambiado a: `Incidencias abiertas encontradas`

---

## PR B — feature/cache-redis → main

**Estado:** Fusionado ✅  
**Rama origen:** `feature/cache-redis`  
**Rama destino:** `main`  
**Ancestro común:** COMMIT0 (`d724701`)

### Cambios incluidos:
- Caché del listado general con `IDistributedCache` + Redis (TTL 60 s).
- Búsqueda de Algolia salta la caché: siempre consulta en directo.
- Al cerrar incidencia: invalida la clave de caché antes de redirigir.
- Log explícito indicando si la lectura vino de Redis o de DB.
- `<h1>` cambiado a: `Incidencias abiertas con consulta rápida`

### Conflicto resuelto al hacer merge de main → feature/cache-redis:
- **Archivo:** `Views/Operaciones/Incidencias.cshtml`
- **Conflicto en `<h1>`:** rama A tenía "encontradas", rama B tenía "con consulta rápida".
- **Resolución:** `<h1>Incidencias abiertas encontradas con consulta rápida</h1>`

---

## PR C — feature/websocket-piehost → main

**Estado:** Fusionado ✅  
**Rama origen:** `feature/websocket-piehost`  
**Rama destino:** `main`  
**Ancestro común:** COMMIT0 (`d724701`)

### Cambios incluidos:
- Al cerrar incidencia: persiste en DB → publica evento `IncidenciaActualizada` a PieHost.
- Vista conecta WebSocket al canal de PieHost; al recibir evento, elimina la fila del DOM sin recargar.
- Al reconectar (`onopen`), recarga el estado actual del listado.
- `<h1>` cambiado a: `Incidencias abiertas en tiempo real`

### Conflicto resuelto al hacer merge de main → feature/websocket-piehost:
- **Archivo:** `Views/Operaciones/Incidencias.cshtml`
- **Conflicto en `<h1>`:** main (A+B) tenía "encontradas con consulta rápida", C tenía "en tiempo real".
- **Resolución:** `<h1>Incidencias abiertas en tiempo real</h1>`
