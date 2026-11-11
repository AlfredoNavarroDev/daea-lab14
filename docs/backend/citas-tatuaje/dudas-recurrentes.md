# Dudas recurrentes — citas-tatuaje

**P: ¿Por qué cancelar una cita no valida solapamiento, pero confirmar o dejarla Pendiente sí?**
R: `CitaTatuajeService.ActualizarAsync` solo llama a `ValidarSolapamientoAsync` cuando `dto.Estado != "Cancelada"` (`CitaTatuajeService.cs:75-78`). Tiene sentido: una cita que se está cancelando está dejando de "ocupar" el horario, así que no importa si su horario (ya irrelevante) choca con otra.

**P: ¿Qué pasa si `TatuadorId` en el `POST`/`PUT` no existe?**
R: El servicio responde con `null` (no lanza `ReglaNegocioException`) y el controlador lo traduce a `404` — en `POST` con el mensaje explícito `"El tatuador indicado no existe."` (`CitasTatuajeController.cs:54`), en `PUT` con un `404` sin cuerpo (`CitasTatuajeController.cs:79`). Es un `404`, no un `400`, porque el formato del DTO es válido (un entero), solo que no referencia un recurso existente.

**P: ¿Dos citas que empiezan justo cuando termina la anterior se consideran solapadas?**
R: No. `ValidarSolapamientoAsync` usa comparación estricta (`nuevoInicio < finExistente && inicioExistente < nuevoFin`, `CitaTatuajeService.cs:108`) — una cita que termina a las 13:00 y otra que empieza a las 13:00 no se solapan.

**P: ¿Por qué `CrearAsync` vuelve a leer la cita de la base después de insertarla, en vez de usar la misma instancia que acaba de mapear?**
R: Porque `CitaTatuajeDto.TatuadorNombre` se mapea desde `src.Tatuador.Nombre` (`MappingProfile.cs:19`), y la entidad recién creada con `_mapper.Map<CitaTatuaje>(dto)` solo tiene `TatuadorId` seteado — su propiedad de navegación `Tatuador` está `null` hasta que se vuelve a consultar con `Include(Tatuador)` (`CitaTatuajeService.cs:57`, `CitaTatuajeRepository.ObtenerPorIdAsync`). Sin ese segundo `SELECT`, el mapeo de salida fallaría con `NullReferenceException` al intentar leer `Tatuador.Nombre`.
