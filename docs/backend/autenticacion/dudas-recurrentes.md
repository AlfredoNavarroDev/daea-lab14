# Dudas recurrentes — autenticacion

**P: ¿Cómo se crea una cuenta con rol `Tatuador` si el registro siempre asigna `Recepcionista`?**
R: No hay ningún endpoint para eso. `RolPorDefecto` es una constante privada fija a `"Recepcionista"` (`TattooStudio.Negocio/Servicios/AutenticacionService.cs:15,38`) y `RegistroDto` no tiene un campo `Rol` (`TattooStudio.Entidades/Dtos/AutenticacionDtos.cs:5-15`). El único camino es registrar el correo por `POST api/auth/registro` y luego ejecutar a mano `UPDATE dbo.Usuario SET Rol = 'Tatuador' WHERE Correo = '...'` contra la base — tal como lo documenta el comentario en `database/TattooStudio.sql:91-95`.

**P: ¿Un usuario con rol `Tatuador` solo puede actualizar sus propias citas?**
R: No está garantizado por el código. `[Authorize(Roles = "Recepcionista,Tatuador")]` en `CitasTatuajeController.Actualizar` (`CitasTatuajeController.cs:70`) solo exige tener uno de esos dos roles. `CitaTatuajeService.ActualizarAsync` (`CitaTatuajeService.cs:61-83`) no recibe el `ClaimsPrincipal` ni compara el usuario autenticado con `dto.TatuadorId` ni con la cita. Además, `Usuario` no tiene ninguna relación (FK) con `Tatuador` (`Usuario.cs` solo tiene `Id`, `Correo`, `ClaveHash`, `Rol`) — con el modelo actual no hay forma de saber "a qué tatuador corresponde" una cuenta con rol `Tatuador`. En la práctica, cualquier cuenta `Tatuador` puede actualizar la cita de cualquier tatuador, no solo las "propias".

**P: ¿Por qué el login responde `401` genérico sin decir si el problema fue el correo o la clave?**
R: Porque `LoginAsync` devuelve `null` en ambos casos con el mismo código (`usuario is null || !BCrypt.Net.BCrypt.Verify(...)`, `AutenticacionService.cs:46-50`), y el controlador solo verifica `if (token is null) return Unauthorized();` (`AuthController.cs:53-56`) — no hay manera de distinguir ambos casos desde la respuesta HTTP.

**P: ¿Por qué `GET api/tatuadores` y `GET api/citastatuaje` no exigen un rol específico, pero sus `POST`/`PUT`/`DELETE` sí?**
R: Ambos controladores llevan `[Authorize]` a nivel de clase sin `Roles` (`TatuadoresController.cs:11`, `CitasTatuajeController.cs:11`) — alcanza con estar autenticado, cualquier rol, para los `GET`. Las acciones de escritura agregan `[Authorize(Roles = "...")]` a nivel de método, que se combina con el de la clase (`Program.cs` las compone con AND, no OR) — ese atributo adicional es lo que restringe la escritura a roles puntuales.

**P: ¿Qué pasa si dos solicitudes de registro llegan al mismo tiempo con el mismo correo?**
R: El chequeo de duplicados en `RegistrarAsync` (`ObtenerPorCorreoAsync` antes del `Add`, `AutenticacionService.cs:28-32`) no es atómico. Si ambas pasan la verificación antes de que cualquiera inserte, la segunda inserción choca con `UQ_Usuario_Correo` (`TattooStudioContext.cs:56`) y lanza un `DbUpdateException` que **no** está capturado en `AuthController.Registrar` — se propagaría como `500` sin cuerpo estructurado, no como el `409 { mensaje: "... ya está registrado." }` del camino normal.

**P: ¿`GET api/auth/perfil` refleja el estado actual del usuario en la base?**
R: No. Arma el `PerfilDto` únicamente desde los claims ya incluidos en el JWT (`User.FindFirstValue(ClaimTypes.Email)` / `(ClaimTypes.Role)`, `AuthController.cs:66-70`), sin inyectar `IUsuarioRepository` ni volver a consultar la base. Si el rol de esa cuenta cambia en la base (por el `UPDATE` manual descrito arriba) después de que el token ya fue emitido, `/perfil` sigue devolviendo el rol viejo hasta que el token expire (`Jwt:MinutosExpiracion`, 60 minutos por defecto) y el usuario vuelva a loguearse para obtener un token nuevo.
