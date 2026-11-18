# Flujo — autenticacion

## POST `api/auth/registro`

`AuthController.Registrar` valida `ModelState` (`400` con `ValidationProblem` si falla). Si es válido, dentro de un `try/catch(ReglaNegocioException)`, `AutenticacionService.RegistrarAsync`:

1. Busca un usuario existente con `UsuarioRepository.ObtenerPorCorreoAsync(dto.Correo)`. Si ya existe, lanza `ReglaNegocioException($"El correo '{dto.Correo}' ya está registrado.")` → el controlador la atrapa y responde `409` con `{ mensaje }` (`AutenticacionService.cs:28-32`, `AuthController.cs:36-39`).
2. Si no existe, construye un `Usuario` nuevo a mano (no hay `AutoMapper` para esta entidad): `Correo = dto.Correo`, `ClaveHash = BCrypt.Net.BCrypt.HashPassword(dto.Clave)`, `Rol = RolPorDefecto` — una constante privada fija a `"Recepcionista"` (`AutenticacionService.cs:15,34-39`). **No hay forma de que el DTO pida otro rol**: `RegistroDto` no tiene campo `Rol`.
3. `UsuarioRepository.AgregarAsync` hace `Add` + `SaveChangesAsync`.

Éxito: `201` sin cuerpo (`StatusCode(StatusCodes.Status201Created)`, `AuthController.cs:34`) — a diferencia de `POST api/tatuadores`, que responde `201` con `CreatedAtRoute` y el recurso creado, aquí no se devuelve el `Usuario` creado ni un header `Location`.

## POST `api/auth/login`

`Login` valida `ModelState` (`400` si falla). Luego `AutenticacionService.LoginAsync`:

1. Busca el usuario por `dto.Correo`. Si no existe, **o** si existe pero `BCrypt.Net.BCrypt.Verify(dto.Clave, usuario.ClaveHash)` es `false`, el método devuelve `null` en ambos casos sin distinguirlos (`AutenticacionService.cs:46-50`).
2. El controlador traduce `null` a `401 Unauthorized` sin cuerpo (`AuthController.cs:53-56`) — mismo código de estado y misma respuesta tanto para "correo no existe" como para "clave incorrecta".
3. Si las credenciales son válidas, `GenerarToken(usuario)`:
   - Lee la sección `Jwt` de la configuración (`Clave`, `Emisor`, `Audiencia`, `MinutosExpiracion`).
   - `MinutosExpiracion` se parsea con `int.Parse(...)` (sin `try/catch`) y se usa para calcular `Expira = DateTime.UtcNow.AddMinutes(minutos)`.
   - Construye 3 claims: `sub` = `usuario.Id` (string), `email` = `usuario.Correo`, `ClaimTypes.Role` = `usuario.Rol` (`AutenticacionService.cs:62-67`).
   - Firma con `HmacSha256` usando una `SymmetricSecurityKey` derivada de `Jwt:Clave` (UTF-8).
   - Devuelve `TokenDto { Token, Expira }`.

Éxito: `200` con el `TokenDto` (`AuthController.cs:58`).

## GET `api/auth/perfil`

`[Authorize]` sin `Roles` — exige solo estar autenticado (token válido), cualquier rol pasa. El método **no** vuelve a consultar la base de datos: arma el `PerfilDto` leyendo los claims ya validados por el middleware JWT desde el `ClaimsPrincipal` (`User.FindFirstValue(ClaimTypes.Email)`, `User.FindFirstValue(ClaimTypes.Role)`, `AuthController.cs:66-70`), con `string.Empty` como *fallback* si algún claim faltara. Si el rol del usuario cambia en la base después de emitido el token, este endpoint sigue devolviendo el rol viejo hasta que el token expire y el usuario vuelva a loguearse.

## Configuración en `Program.cs`

1. `AddAuthentication` fija `JwtBearer` como esquema por defecto (autenticación y *challenge*).
2. `AddJwtBearer` configura `TokenValidationParameters` con las 4 validaciones activas: `ValidateIssuer`/`ValidAudience` contra `Jwt:Emisor`/`Jwt:Audiencia`, `ValidateLifetime` (rechaza tokens expirados), `ValidateIssuerSigningKey` con la misma clave simétrica (`Jwt:Clave`) usada para firmar en `AutenticacionService`.
3. `AddAuthorization()` se llama sin policies adicionales — solo habilita el middleware para que `[Authorize]`/`[Authorize(Roles=...)]` funcionen.
4. El pipeline llama `app.UseAuthentication()` **antes** de `app.UseAuthorization()` (orden correcto: sin esto, `[Authorize]` no tendría un `ClaimsPrincipal` poblado para evaluar).
5. `BearerSecuritySchemeTransformer` (clase al final de `Program.cs`) es un `IOpenApiDocumentTransformer` registrado en `AddOpenApi`: agrega el esquema de seguridad `"Bearer"` (HTTP, `bearer`, `JWT`) al documento OpenAPI y le agrega un `Security` requirement a **todas** las operaciones, incluidas `registro`/`login` que son `[AllowAnonymous]` — es puramente cosmético (hace aparecer el candado en Swagger UI para todos los endpoints) y no afecta la autorización real, que la decide `[Authorize]`/`[AllowAnonymous]` en cada acción.

## Autorización en `TatuadoresController` / `CitasTatuajeController`

Ambos controladores llevan `[Authorize]` a nivel de clase (sin `Roles`), así que **todo** método exige al menos un token válido — las acciones `GET` no agregan ninguna restricción adicional, por lo que basta cualquier rol autenticado (`Recepcionista` o `Tatuador`) para leer.

Las acciones de escritura agregan `[Authorize(Roles = "...")]` a nivel de método, que se combina con el de la clase (ambos deben cumplirse, pero el de la clase ya queda satisfecho con cualquier rol válido):

| Acción | Atributo de método |
|---|---|
| `TatuadoresController.Crear` / `Actualizar` / `Desactivar` | `[Authorize(Roles = "Recepcionista")]` |
| `CitasTatuajeController.Crear` / `Eliminar` | `[Authorize(Roles = "Recepcionista")]` |
| `CitasTatuajeController.Actualizar` (`PUT`) | `[Authorize(Roles = "Recepcionista,Tatuador")]` |

**Importante:** este `[Authorize(Roles = "Recepcionista,Tatuador")]` solo exige que el usuario tenga uno de esos dos roles — `CitaTatuajeService.ActualizarAsync` no recibe el `ClaimsPrincipal` ni compara nada con `dto.TatuadorId` o con la cita. Cualquier cuenta con rol `Tatuador` puede actualizar la cita de **cualquier** tatuador, no solo las propias (ver `dudas-recurrentes.md`).
