# Flujo — citas-tatuaje

## GET `api/citastatuaje`

`ObtenerTodos` → `CitaTatuajeService.ObtenerTodosAsync(tatuadorId, estado)` → `CitaTatuajeRepository.ObtenerTodosAsync`: parte de `CitaTatuajes.AsNoTracking().Include(Tatuador)`, filtra por `TatuadorId` si se pasó y por `Estado` (comparación exacta, sin `Contains`) si se pasó, ordena por `FechaHora`. Siempre `200`.

## GET `api/citastatuaje/{id}`

Busca con `CitaTatuajeRepository.ObtenerPorIdAsync` (`AsNoTracking`, `Include(Tatuador)`). `404` si no existe, `200` con `CitaTatuajeDto` (incluye `TatuadorNombre`) si existe.

## POST `api/citastatuaje`

`Crear` valida `ModelState` (`400` si falla). Si es válido, dentro de `try/catch(ReglaNegocioException)`, `CitaTatuajeService.CrearAsync(dto)`:

1. **Verifica que el tatuador exista:** `_tatuadorRepository.ObtenerPorIdAsync(dto.TatuadorId)` — si no existe, el servicio retorna `null` (no lanza excepción) y el controlador responde `404` con `{ mensaje: "El tatuador indicado no existe." }`. Este `404` es el único caso en todo el proyecto donde un DTO de entrada referencia una FK y se valida con un `404` en vez de un `400`.
2. **Valida solapamiento** con `ValidarSolapamientoAsync(dto.TatuadorId, dto.FechaHora, dto.EstimacionHoras, excluirCitaId: null)` (ver sección "Regla de solapamiento" abajo). Si hay choque, lanza `ReglaNegocioException` → el controlador responde `409`.
3. Mapea `CitaTatuajeCrearDto → CitaTatuaje` con AutoMapper y fija `cita.Estado = "Pendiente"` a mano — igual que con `Tatuador.Activo`, el DTO de creación no incluye `Estado` porque toda cita nueva nace Pendiente.
4. Persiste (`CrearAsync`: `Add` + `SaveChangesAsync`), y **vuelve a leer la cita por id** (`ObtenerPorIdAsync`, que hace `Include(Tatuador)`) antes de mapear al DTO de salida — necesario para que `CitaTatuajeDto.TatuadorNombre` (que viene de `src.Tatuador.Nombre`) no quede nulo: la entidad recién insertada en memoria no tiene cargada la navegación `Tatuador`.

Éxito: `201` vía `CreatedAtRoute("ObtenerCitaPorId", new { id = creada.Id }, creada)`.

## PUT `api/citastatuaje/{id}`

`Actualizar` valida `ModelState` (`400` si falla). Si es válido, dentro de `try/catch(ReglaNegocioException)`, `CitaTatuajeService.ActualizarAsync(id, dto)`:

1. Busca la cita con `ObtenerPorIdConTatuadorAsync` (con tracking, `Include(Tatuador)`) — si no existe, `null` → `404`.
2. Verifica que el `TatuadorId` del DTO exista (permite reasignar la cita a otro tatuador) — si no, `null` → `404`.
3. **Solo si `dto.Estado != "Cancelada"`**, vuelve a correr `ValidarSolapamientoAsync`, esta vez con `excluirCitaId: id` (para no chocar contra sí misma). Si `dto.Estado == "Cancelada"`, la validación de solapamiento se **omite por completo** — cancelar una cita nunca puede disparar un `409` por horario, aunque el nuevo horario del DTO choque con otra cita activa.
4. `_mapper.Map(dto, cita)` actualiza la entidad trackeada (incluye `Estado`, `FechaHora`, `EstimacionHoras`, `TatuadorId`, etc.) y `CitaTatuajeRepository.ActualizarAsync` guarda.

Éxito: `204`.

## DELETE `api/citastatuaje/{id}`

`Eliminar` → `CitaTatuajeService.EliminarAsync(id)`: busca la cita, si no existe retorna `false` → `404`; si existe, `CitaTatuajeRepository.EliminarAsync` hace `Remove` + `SaveChangesAsync` (borrado físico, sin excepción de negocio posible — no hay `try/catch` en esta acción del controlador).

Éxito: `204`.

## Regla de solapamiento (`ValidarSolapamientoAsync`, `CitaTatuajeService.cs:97-116`)

1. Calcula la ventana de la cita nueva/editada: `nuevoInicio = FechaHora`, `nuevoFin = FechaHora.AddHours((double)EstimacionHoras)`.
2. Trae las citas "activas" del mismo tatuador con `CitaTatuajeRepository.ObtenerActivasPorTatuadorAsync(tatuadorId, excluirCitaId)` — repositorio define "activa" como `Estado != "Cancelada"` (es decir, Pendiente **o** Confirmada), excluyendo opcionalmente la propia cita que se está editando.
3. Para cada cita activa calcula su propia ventana (`inicioExistente`, `finExistente = FechaHora.AddHours(EstimacionHoras)`) y prueba solapamiento de intervalos semiabiertos: `nuevoInicio < finExistente && inicioExistente < nuevoFin`. Dos citas que empiezan exactamente cuando termina la otra (`nuevoInicio == finExistente`) **no** se consideran solapadas (la condición usa `<` estricto).
4. Si alguna coincide, lanza `ReglaNegocioException("El tatuador ya tiene otra cita activa que se solapa con el horario indicado.")`.

La comparación es puramente en memoria (`citasActivas.Any(...)` sobre una `List<CitaTatuaje>` ya traída de la base) — no se traduce a una condición SQL de rango; con pocas citas por tatuador esto no es un problema de rendimiento, pero significa que el cálculo de `AddHours` ocurre en CLR, no en SQL Server.
