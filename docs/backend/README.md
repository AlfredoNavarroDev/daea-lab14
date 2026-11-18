# Backend — TattooStudio.Api

Web API REST en 4 capas (`TattooStudio.Entidades` → `TattooStudio.Datos` → `TattooStudio.Negocio` → `TattooStudio.Api`) para la gestión de un estudio de tatuajes.

## Unidades

| Unidad | Qué cubre |
|---|---|
| [tatuadores](./tatuadores/) | CRUD de `Tatuador`, baja lógica, listado de citas de un tatuador (ruta anidada) |
| [citas-tatuaje](./citas-tatuaje/) | CRUD de `CitaTatuaje`, regla de solapamiento de horarios |
| [autenticacion](./autenticacion/) | Registro/login con JWT (`Usuario`), roles `Recepcionista`/`Tatuador`, configuración de `[Authorize]` consumida por `tatuadores` y `citas-tatuaje` |

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

Desde la Semana 14, `Program.cs` registra `AddAuthentication` + `AddJwtBearer` (esquema por defecto `JwtBearer`, valida emisor/audiencia/firma/expiración contra la sección `Jwt` de `appsettings.json`) y `AddAuthorization()`; el pipeline llama `app.UseAuthentication()` antes de `app.UseAuthorization()`. `TatuadoresController` y `CitasTatuajeController` llevan `[Authorize]` a nivel de clase (cualquier rol autenticado para los `GET`) y `[Authorize(Roles = "...")]` en sus acciones de escritura. Ver [autenticacion](./autenticacion/) para el detalle completo (registro, login, emisión del JWT, y la brecha de que el rol `Tatuador` no está atado a un `Tatuador.Id` puntual).

## Known gaps

- No hay un manejador global de excepciones: solo `ReglaNegocioException` se traduce a una respuesta controlada (409); cualquier otra excepción no capturada (p. ej. una falla de conexión a SQL Server, una violación de `CHECK` que llegara a disparar `DbUpdateException`, o un correo duplicado en un registro concurrente — ver `autenticacion/dudas-recurrentes.md`) se propaga como `500` sin cuerpo estructurado.
- No existen pruebas automatizadas (unitarias o de integración) en la solución — no hay carpeta de tests ni proyecto `*.Tests`.
- El rol `Tatuador` es global, no está asociado a un `Tatuador.Id`: `[Authorize(Roles = "Recepcionista,Tatuador")]` en `PUT api/citastatuaje/{id}` no impide que una cuenta `Tatuador` actualice la cita de **cualquier** tatuador, no solo las propias (ver `autenticacion/dudas-recurrentes.md`).
- No hay endpoint ni mecanismo para crear una cuenta con rol `Tatuador` ni para revocar un JWT antes de su expiración (sin refresh token, sin logout) — ambos requieren intervención manual sobre la base o esperar a que el token expire.
