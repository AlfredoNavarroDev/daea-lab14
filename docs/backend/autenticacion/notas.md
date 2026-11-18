# Notas — autenticacion

## Dependencias

- `AutenticacionService` depende de `IUsuarioRepository` y de `IConfiguration` directamente (lee `_configuration.GetSection("Jwt")` a mano) — no hay un `JwtSettings` tipado registrado con `IOptions<T>`, a diferencia de cómo se inyectarían configuraciones en otras capas.
- `Program.cs` también lee `builder.Configuration.GetSection("Jwt")` por separado, para construir `TokenValidationParameters` — la clave simétrica (`Jwt:Clave`) se lee y se convierte a bytes UTF-8 en dos lugares distintos (`Program.cs` y `AutenticacionService.GenerarToken`) en vez de compartir una sola utilidad.
- `TatuadoresController` y `CitasTatuajeController` dependen de esta unidad solo indirectamente, vía `[Authorize]`/`[Authorize(Roles=...)]` — no referencian ningún tipo de `autenticacion` directamente, la dependencia es puramente a nivel de pipeline HTTP (middleware de autenticación + atributos).

## Notas

- No hay revocación de sesión ni *refresh token*: el JWT es válido hasta su expiración (`Jwt:MinutosExpiracion`, 60 minutos en `appsettings.json`) sin ningún mecanismo para invalidarlo antes (ni una lista negra, ni un endpoint de logout). "Cerrar sesión" solo puede hacerse del lado del cliente, descartando el token.
- `BCrypt.Net.BCrypt.HashPassword(dto.Clave)` se llama sin especificar un *work factor* explícito — usa el valor por defecto de la librería (`10`). No hay forma de ajustarlo desde configuración.
- `Usuario` no tiene relación con `Tatuador` — es la causa raíz de la duda recurrente sobre si un `Tatuador` puede editar solo sus propias citas (ver `dudas-recurrentes.md`). Resolver esa brecha de forma correcta requeriría agregar una FK opcional `Usuario.TatuadorId` (o la tabla inversa) y que `CitaTatuajeService.ActualizarAsync` reciba el `ClaimsPrincipal` para comparar.
- No existen pruebas automatizadas para esta unidad (ni para el resto de la solución) — no hay carpeta de tests ni proyecto `*.Tests`.
