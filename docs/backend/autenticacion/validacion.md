# Validación — autenticacion

`RegistroDto` y `LoginDto` (`TattooStudio.Entidades/Dtos/AutenticacionDtos.cs`):

| DTO.Campo | Reglas |
|---|---|
| `RegistroDto.Correo` | `[Required]`, `[EmailAddress]`, `[StringLength(150)]` |
| `RegistroDto.Clave` | `[Required]`, `[StringLength(100, MinimumLength = 6)]` |
| `LoginDto.Correo` | `[Required]`, `[EmailAddress]`, `[StringLength(150)]` |
| `LoginDto.Clave` | `[Required]` únicamente — **sin** mínimo de longitud, a diferencia de `RegistroDto.Clave`. Tiene sentido: en login la clave se compara contra un hash ya existente con `BCrypt.Verify`, no se está creando una cuenta nueva, así que no hay una longitud "correcta" que exigir de antemano. |

No hay un DTO de actualización de perfil ni de cambio de rol — `Rol` nunca es un campo de entrada en ningún DTO de esta unidad; solo se fija server-side (`RolPorDefecto` constante) o a mano vía SQL.

**DB vs DTO:**

- `dbo.Usuario.Correo` es `NVARCHAR(150)`, igual que `[StringLength(150)]` del DTO — coinciden. Además tiene `UQ_Usuario_Correo UNIQUE` — la unicidad se valida **dos veces**: una vez en `AutenticacionService.RegistrarAsync` (`ObtenerPorCorreoAsync` antes del `Add`) y otra vez, implícitamente, por el índice único de la base. El chequeo del servicio no es atómico; ver `dudas-recurrentes.md` para el caso de dos registros concurrentes con el mismo correo.
- `dbo.Usuario.ClaveHash` es `NVARCHAR(200)` — nunca se valida contra este límite en el DTO (el DTO valida la *clave en texto plano*, de máximo 100 caracteres; el hash de BCrypt que efectivamente se guarda tiene un largo fijo de 60 caracteres, muy por debajo de los 200 disponibles).
- `dbo.Usuario.Rol` tiene `CK_Usuario_Rol CHECK (Rol IN ('Recepcionista', 'Tatuador'))`. Como ningún DTO permite elegir `Rol`, este `CHECK` nunca lo ejercita la API en sí misma — solo protege contra un `UPDATE`/`INSERT` manual con un valor fuera de esos dos.
- `Jwt:MinutosExpiracion` se guarda como **string** en `appsettings.json` (`"60"`) y se convierte con `int.Parse` en `AutenticacionService.GenerarToken` sin manejo de excepción — un valor no numérico en ese campo de configuración rompería el login con una excepción no controlada (`FormatException`, `500`) en vez de fallar al arrancar la aplicación.
