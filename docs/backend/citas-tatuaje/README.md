# citas-tatuaje

Agenda de citas de tatuaje: alta, listado con filtros, edición (reprogramar / cambiar estado) y eliminación. Impone la regla de no solapar horarios de un mismo tatuador.

**Files:** `TattooStudio.Api/Controllers/CitasTatuajeController.cs`, `TattooStudio.Negocio/Servicios/ICitaTatuajeService.cs` / `CitaTatuajeService.cs`, `TattooStudio.Datos/Repositorios/ICitaTatuajeRepository.cs` / `CitaTatuajeRepository.cs`, `TattooStudio.Entidades/CitaTatuaje.cs`, `TattooStudio.Entidades/Dtos/CitaTatuajeDtos.cs`.

## Entidades / Tablas

| Entidad | Tabla | Notas |
|---|---|---|
| `CitaTatuaje` | `dbo.CitaTatuaje` | Lado "N" de la relación con `Tatuador` vía `TatuadorId` (FK) |

## Endpoints

| Método | Ruta | Auth | Input | Output |
|---|---|---|---|---|
| GET | `api/citastatuaje?tatuadorId=&estado=` | Ninguna | query `tatuadorId` (int), `estado` (string exacto) | `200` `List<CitaTatuajeDto>` |
| GET | `api/citastatuaje/{id}` | Ninguna | `id` | `200` `CitaTatuajeDto` / `404` |
| POST | `api/citastatuaje` | Ninguna | `CitaTatuajeCrearDto` | `201` (`CreatedAtRoute`) / `400` / `404` (tatuador inexistente) / `409` (solapamiento) |
| PUT | `api/citastatuaje/{id}` | Ninguna | `id`, `CitaTatuajeActualizarDto` | `204` / `400` / `404` / `409` |
| DELETE | `api/citastatuaje/{id}` | Ninguna | `id` | `204` / `404` |

A diferencia de `Tatuador`, `CitaTatuaje` no tiene baja lógica: `DELETE` borra la fila físicamente (no hay columna `Activo` en esta entidad).
