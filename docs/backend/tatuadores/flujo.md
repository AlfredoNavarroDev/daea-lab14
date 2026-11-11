# Flujo — tatuadores

## GET `api/tatuadores`

`TatuadoresController.ObtenerTodos` → `TatuadorService.ObtenerTodosAsync` → `TatuadorRepository.ObtenerTodosAsync`.

El repositorio parte de `Tatuadors.AsNoTracking().Include(t => t.CitaTatuajes)` (necesita incluir las citas porque `TatuadorDto.CantidadCitas` se calcula con `AutoMapper` sobre esa colección — sin el `Include` la colección llega vacía y el conteo sale en `0`), aplica `Where(Especialidad.Contains(...))` si se pasó `especialidad` y `Where(Activo == ...)` si se pasó `activo`, y ordena por `Nombre`. Siempre `200`, lista vacía si no hay coincidencias.

## GET `api/tatuadores/{id}`

`ObtenerPorId` → `TatuadorService.ObtenerPorIdAsync` → `TatuadorRepository.ObtenerPorIdConCitasAsync` (con `Include(CitaTatuajes)`, sin `AsNoTracking`). Si no existe: `404`. Si existe: `200` con `TatuadorDto` (incluye `CantidadCitas`).

## GET `api/tatuadores/{tatuadorId}/citas` (ruta anidada)

`ObtenerCitasDelTatuador` primero verifica que el tatuador exista llamando a `_tatuadorService.ObtenerPorIdAsync(tatuadorId)` — si no, `404`. Si existe, pide las citas con `_citaService.ObtenerPorTatuadorAsync(tatuadorId)`, que internamente usa `CitaTatuajeRepository.ObtenerPorTatuadorAsync` (`Include(Tatuador)`, ordenado por `FechaHora`, **sin** filtrar por estado: devuelve Pendientes, Confirmadas y Canceladas). `200` con la lista (vacía si el tatuador no tiene citas).

## POST `api/tatuadores`

`Crear` valida `ModelState` (`400` con `ValidationProblem` si falla). Si es válido, `TatuadorService.CrearAsync`:
1. Mapea `TatuadorCrearDto → Tatuador` con AutoMapper (el DTO no trae `Activo`).
2. Fija `tatuador.Activo = true` explícitamente en el servicio — **no** es un default de AutoMapper ni de la entidad (la entidad no tiene inicializador para `Activo`, y el `CreateMap<TatuadorCrearDto, Tatuador>` no tiene `ForMember` para esa propiedad).
3. Persiste con `TatuadorRepository.CrearAsync` (`Add` + `SaveChangesAsync`).

Responde `201` vía `CreatedAtRoute("ObtenerTatuadorPorId", new { id = creado.Id }, creado)`.

## PUT `api/tatuadores/{id}`

`Actualizar` valida `ModelState` (`400` si falla). Si es válido, dentro de un `try/catch(ReglaNegocioException)`:

1. `TatuadorService.ActualizarAsync(id, dto)` busca el tatuador con `ObtenerPorIdAsync` (sin tracking) — si no existe, retorna `false` → controlador responde `404`.
2. **Regla de negocio (409):** solo si el tatuador está actualmente activo (`tatuador.Activo == true`) y el DTO pide desactivarlo (`dto.Activo == false`), se consulta `TieneCitasActivasAsync(id)` (existe alguna `CitaTatuaje` con `Estado` `Pendiente` o `Confirmada`); si hay alguna, lanza `ReglaNegocioException` con el nombre del tatuador en el mensaje. El controlador la atrapa y responde `409` con `{ mensaje }`.
   - Si el tatuador ya estaba inactivo, o si el DTO mantiene/activa `Activo`, esta verificación **no corre** — reactivar un tatuador nunca está bloqueado por esta regla.
3. Si pasa la validación, `_mapper.Map(dto, tatuador)` actualiza la entidad trackeada in-place (incluye `Nombre`, `Especialidad`, `AniosExperiencia` y `Activo`) y `TatuadorRepository.ActualizarAsync` guarda.

Éxito: `204`.

## DELETE `api/tatuadores/{id}` (baja lógica)

`Desactivar` → `TatuadorService.DesactivarAsync(id)`, dentro de un `try/catch(ReglaNegocioException)` en el controlador:

1. Busca el tatuador; si no existe, `false` → `404`.
2. **Regla de negocio (409):** siempre consulta `TieneCitasActivasAsync(id)` (sin condicionarlo al estado actual de `Activo`, a diferencia de `ActualizarAsync`) — si tiene alguna cita `Pendiente` o `Confirmada`, lanza `ReglaNegocioException` → `409`.
3. Si no hay citas activas, fija `tatuador.Activo = false` y guarda. No es un `DELETE` físico: la fila permanece en `dbo.Tatuador`.

Éxito: `204`. Nota: `DELETE` sobre un tatuador que ya está inactivo y sin citas activas vuelve a guardarlo con `Activo = false` sin error (operación idempotente en los hechos, aunque no hay un chequeo explícito de "ya estaba inactivo").
