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
| GET | `api/tatuadores?especialidad=&activo=` | Ninguna | query `especialidad` (substring), `activo` (bool) | `200` `List<TatuadorDto>` |
| GET | `api/tatuadores/{id}` | Ninguna | `id` | `200` `TatuadorDto` / `404` |
| GET | `api/tatuadores/{tatuadorId}/citas` | Ninguna | `tatuadorId` | `200` `List<CitaTatuajeDto>` / `404` si el tatuador no existe |
| POST | `api/tatuadores` | Ninguna | `TatuadorCrearDto` | `201` (`CreatedAtRoute`) `TatuadorDto` / `400` |
| PUT | `api/tatuadores/{id}` | Ninguna | `id`, `TatuadorActualizarDto` | `204` / `400` / `404` / `409` |
| DELETE | `api/tatuadores/{id}` | Ninguna | `id` | `204` / `404` / `409` |
