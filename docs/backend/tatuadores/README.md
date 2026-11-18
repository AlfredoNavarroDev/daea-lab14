# tatuadores

Alta, listado, edición y baja lógica de los tatuadores del estudio; también expone, bajo su propia ruta, las citas agendadas con un tatuador puntual.

**Files:** `TattooStudio.Api/Controllers/TatuadoresController.cs`, `TattooStudio.Negocio/Servicios/ITatuadorService.cs` / `TatuadorService.cs`, `TattooStudio.Datos/Repositorios/ITatuadorRepository.cs` / `TatuadorRepository.cs`, `TattooStudio.Entidades/Tatuador.cs`, `TattooStudio.Entidades/Dtos/TatuadorDtos.cs`.

## Entidades / Tablas

| Entidad | Tabla | Notas |
|---|---|---|
| `Tatuador` | `dbo.Tatuador` | Lado "1" de la relación 1:N con `CitaTatuaje` (navegación `CitaTatuajes`) |

## Endpoints

| Método | Ruta | Auth | Input | Output |
|---|---|---|---|---|
| GET | `api/tatuadores?especialidad=&activo=` | Autenticado (cualquier rol) | query `especialidad` (substring), `activo` (bool) | `200` `List<TatuadorDto>` |
| GET | `api/tatuadores/{id}` | Autenticado (cualquier rol) | `id` | `200` `TatuadorDto` / `404` |
| GET | `api/tatuadores/{tatuadorId}/citas` | Autenticado (cualquier rol) | `tatuadorId` | `200` `List<CitaTatuajeDto>` / `404` si el tatuador no existe |
| POST | `api/tatuadores` | `Recepcionista` | `TatuadorCrearDto` | `201` (`CreatedAtRoute`) `TatuadorDto` / `400` |
| PUT | `api/tatuadores/{id}` | `Recepcionista` | `id`, `TatuadorActualizarDto` | `204` / `400` / `404` / `409` |
| DELETE | `api/tatuadores/{id}` | `Recepcionista` | `id` | `204` / `404` / `409` |

Autenticación/roles vienen de `[Authorize]` (clase) + `[Authorize(Roles = "Recepcionista")]` (acciones de escritura) — ver [autenticacion](../autenticacion/).
