# Validación — citas-tatuaje

`CitaTatuajeCrearDto` y `CitaTatuajeActualizarDto` (`TattooStudio.Entidades/Dtos/CitaTatuajeDtos.cs`):

| Campo | Reglas | Solo en |
|---|---|---|
| `ClienteNombre` | `[Required]`, `[StringLength(100, MinimumLength = 3)]` | ambos |
| `ClienteCorreo` | `[Required]`, `[EmailAddress]`, `[StringLength(150)]` | ambos |
| `FechaHora` | `[Required]` (tipo `DateTime`, sin cota de pasado/futuro) | ambos |
| `EstimacionHoras` | `[Range(0.5, 12)]` | ambos |
| `SeñaPagada` | `[Range(0, 100000)]` | ambos |
| `Estado` | `[Required]`, `[RegularExpression("^(Pendiente\|Confirmada\|Cancelada)$")]` | solo `Actualizar` — `Crear` no lo expone, el servicio fija `"Pendiente"` |
| `TatuadorId` | `[Required]` (no hay validación de que exista; eso lo hace el servicio contra el repositorio, ver `flujo.md`) | ambos |

**DB vs DTO:**

- `EstimacionHoras`: `CK_CitaTatuaje_EstimacionHoras CHECK (EstimacionHoras BETWEEN 0.5 AND 12)` coincide exactamente con el `[Range(0.5, 12)]` del DTO.
- `SeñaPagada`: `CK_CitaTatuaje_SeñaPagada CHECK (SeñaPagada BETWEEN 0 AND 100000)` coincide exactamente con el `[Range(0, 100000)]` del DTO — ya no se puede insertar un valor negativo ni por fuera de la API.
- `Estado`: `CK_CitaTatuaje_Estado CHECK (Estado IN ('Pendiente','Confirmada','Cancelada'))` coincide con la lista de la `[RegularExpression]` del DTO de actualización.
- `FechaHora`: ninguna de las dos capas valida que la fecha no sea pasada; una cita puede agendarse en cualquier fecha, pasada o futura (el enunciado del laboratorio no pide esa regla).

Las tres columnas numéricas con `CHECK` ahora reflejan exactamente los mismos límites que sus `[Range]` en el DTO — la base de datos es la última línea de defensa si algo escribe sin pasar por la API, en vez de ser más permisiva que la API.
