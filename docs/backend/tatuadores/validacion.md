# Validación — tatuadores

`TatuadorCrearDto` y `TatuadorActualizarDto` (`TattooStudio.Entidades/Dtos/TatuadorDtos.cs`):

| Campo | Reglas |
|---|---|
| `Nombre` | `[Required]`, `[StringLength(100, MinimumLength = 3)]` |
| `Especialidad` | `[Required]`, `[StringLength(50)]` — texto libre, no hay un `enum`/lista cerrada aunque el dominio sugiere valores fijos (Realismo, Tradicional, Fine Line, ...) |
| `AniosExperiencia` | `[Range(0, 60)]` |
| `Activo` | Solo en `TatuadorActualizarDto` (tipo `bool`, sin `[Required]` porque `bool` no admite ausencia — `false` por defecto si se omite en el JSON) |

**DB vs DTO:** la tabla `dbo.Tatuador` tiene `CK_Tatuador_AniosExperiencia CHECK (AniosExperiencia BETWEEN 0 AND 60)`, igual que el `[Range(0, 60)]` del DTO — ambas capas coinciden exactamente. `Nombre` y `Especialidad` no tienen `CHECK` en la base más allá del `NOT NULL` y el `NVARCHAR(n)` (que trunca/rechaza a nivel de ancho de columna, no de longitud mínima) — el mínimo de 3 caracteres en `Nombre` solo se aplica en el DTO.
