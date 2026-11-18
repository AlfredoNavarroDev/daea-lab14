# autenticacion

Registro y login con JWT para las cuentas de acceso a la API (`Usuario`), y la configuración de autenticación/autorización (`Program.cs`) que `TatuadoresController` y `CitasTatuajeController` consumen. Dos roles: `Recepcionista` (asignado por defecto al autoregistrarse) y `Tatuador` (debe asignarse a mano).

**Files:** `TattooStudio.Api/Controllers/AuthController.cs`, `TattooStudio.Negocio/Servicios/IAutenticacionService.cs` / `AutenticacionService.cs`, `TattooStudio.Datos/Repositorios/IUsuarioRepository.cs` / `UsuarioRepository.cs`, `TattooStudio.Entidades/Usuario.cs`, `TattooStudio.Entidades/Dtos/AutenticacionDtos.cs`, `TattooStudio.Api/Program.cs` (bloque `AddAuthentication`/`AddJwtBearer`/`AddAuthorization` + `BearerSecuritySchemeTransformer`), `TattooStudio.Api/appsettings.json` (sección `Jwt`).

## Entidades / Tablas

| Entidad | Tabla | Notas |
|---|---|---|
| `Usuario` | `dbo.Usuario` | Sin relación (FK) con `Tatuador` — una cuenta solo tiene `Correo`/`ClaveHash`/`Rol`, no referencia a qué tatuador "es" si su rol es `Tatuador` |

## Endpoints

| Método | Ruta | Auth | Input | Output |
|---|---|---|---|---|
| POST | `api/auth/registro` | `[AllowAnonymous]` | `RegistroDto` | `201` (sin cuerpo) / `400` / `409` (correo ya registrado) |
| POST | `api/auth/login` | `[AllowAnonymous]` | `LoginDto` | `200` `TokenDto` / `400` / `401` |
| GET | `api/auth/perfil` | `[Authorize]` (cualquier rol autenticado) | — (token Bearer) | `200` `PerfilDto` |

## Roles y dónde se exigen

| Rol | Se asigna | Puede (además de `GET`s, abiertos a cualquier rol autenticado) |
|---|---|---|
| `Recepcionista` | Automático al registrarse (`RolPorDefecto` en `AutenticacionService`) | `POST`/`PUT`/`DELETE` de `api/tatuadores`; `POST`/`PUT`/`DELETE` de `api/citastatuaje` |
| `Tatuador` | Manual — `UPDATE dbo.Usuario SET Rol = 'Tatuador' ...` (no hay endpoint) | `PUT api/citastatuaje/{id}` únicamente (no `POST`, no `DELETE`, no escritura sobre `api/tatuadores`) |

Ver `dudas-recurrentes.md` — el `PUT` de citas **no** verifica que el tatuador autenticado sea el dueño de esa cita puntual; "Tatuador" es un rol global, no una cuenta atada a un `Tatuador.Id`.
