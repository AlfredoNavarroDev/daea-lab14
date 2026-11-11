# Backend — TattooStudio.Api

Web API REST en 4 capas (`TattooStudio.Entidades` → `TattooStudio.Datos` → `TattooStudio.Negocio` → `TattooStudio.Api`) para la gestión de un estudio de tatuajes.

## Unidades

| Unidad | Qué cubre |
|---|---|
| [tatuadores](./tatuadores/) | CRUD de `Tatuador`, baja lógica, listado de citas de un tatuador (ruta anidada) |
| [citas-tatuaje](./citas-tatuaje/) | CRUD de `CitaTatuaje`, regla de solapamiento de horarios |

## Infraestructura compartida

### Capas y referencias

```
TattooStudio.Api       -> controladores ([ApiController], api/[controller]), Program.cs, Swagger
TattooStudio.Negocio    -> servicios (reglas de negocio), MappingProfile, ReglaNegocioException
TattooStudio.Datos      -> TattooStudioContext, repositorios (EF Core)
TattooStudio.Entidades  -> entidades (Tatuador, CitaTatuaje) + DTOs
```

Los controladores dependen solo de interfaces de servicio (`ITatuadorService`, `ICitaTatuajeService`) y de DTOs — nunca del `DbContext` ni de las entidades (`TattooStudio.Api/Controllers/*.cs`).

### `ReglaNegocioException` (`TattooStudio.Negocio/Excepciones/ReglaNegocioException.cs`)

Excepción simple (`: Exception`, un constructor con `message`) que representa una violación de regla de negocio. Cada acción de controlador que puede violarla envuelve la llamada al servicio en `try { ... } catch (ReglaNegocioException ex) { return Conflict(new { mensaje = ex.Message }); }`, devolviendo **409** con el mensaje tal cual lo lanzó el servicio. No hay un filtro global para esta excepción: el `try/catch` está repetido en cada acción (`PUT`/`DELETE` de tatuadores, `POST`/`PUT` de citas).

### `MappingProfile` (`TattooStudio.Negocio/MappingProfile.cs`)

Único perfil de AutoMapper, registrado en `Program.cs` con `builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(MappingProfile).Assembly))`. Seis `CreateMap`:

| Mapeo | Dirección | `ForMember` |
|---|---|---|
| `Tatuador → TatuadorDto` | lectura | `CantidadCitas = src.CitaTatuajes.Count` |
| `TatuadorCrearDto → Tatuador` | entrada | — |
| `TatuadorActualizarDto → Tatuador` | entrada (update) | — |
| `CitaTatuaje → CitaTatuajeDto` | lectura | `TatuadorNombre = src.Tatuador.Nombre` |
| `CitaTatuajeCrearDto → CitaTatuaje` | entrada | — |
| `CitaTatuajeActualizarDto → CitaTatuaje` | entrada (update) | — |

No usa `ReverseMap()` en ningún mapeo: las formas de lectura (agregan datos calculados/relacionados) y las de entrada (llevan Data Annotations) son intencionalmente distintas, así que van en `CreateMap` separados. Los campos que solo fija el servicio después del mapeo (`Tatuador.Activo = true` al crear, `CitaTatuaje.Estado = "Pendiente"` al crear) **no** están en el `MappingProfile` — quedan fuera de los `CreateMap` porque los DTOs de creación no los incluyen (ver `dudas-recurrentes.md` de cada unidad).

### Swagger / OpenAPI

`Program.cs` usa el generador nativo de .NET (`builder.Services.AddOpenApi()` + `app.MapOpenApi()`, que publica `/openapi/v1.json`) y solo agrega `Swashbuckle.AspNetCore.SwaggerUI` como visor, apuntado a ese documento (`options.SwaggerEndpoint("/openapi/v1.json", ...)`, `RoutePrefix = "swagger"`). No se usa el generador de Swashbuckle (`Swashbuckle.AspNetCore.SwaggerGen`/`AddSwaggerGen`) — solo su UI.

### Autenticación

No hay `[Authorize]` ni middleware de autenticación en ningún controlador (`Program.cs` no registra autenticación, solo `app.UseAuthorization()` sin esquema). Es intencional: el enunciado del laboratorio fija `Tipo de autenticación: Ninguno`.

## Known gaps

- No hay un manejador global de excepciones: solo `ReglaNegocioException` se traduce a una respuesta controlada (409); cualquier otra excepción no capturada (p. ej. una falla de conexión a SQL Server, o una violación de `CHECK` que llegara a disparar `DbUpdateException`) se propaga como `500` sin cuerpo estructurado.
- No existen pruebas automatizadas (unitarias o de integración) en la solución — no hay carpeta de tests ni proyecto `*.Tests`.
