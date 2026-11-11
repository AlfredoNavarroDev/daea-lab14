# tatuador

Representa a cada artista del estudio: su nombre, especialidad, años de experiencia y si sigue activo.

**Files:** `TattooStudio.Entidades/Tatuador.cs`, `TattooStudio.Datos/Data/TattooStudioContext.cs` (configuración en `OnModelCreating`), `database/TattooStudio.sql` (`CREATE TABLE dbo.Tatuador`).

## Columns / fields

| Nombre | Tipo (SQL) | Constraints/default | Computado |
|---|---|---|---|
| `Id` | `INT IDENTITY(1,1)` | `PRIMARY KEY` | No — autogenerado por SQL Server (`IDENTITY`), no por la app |
| `Nombre` | `NVARCHAR(100)` | `NOT NULL` | No |
| `Especialidad` | `NVARCHAR(50)` | `NOT NULL` | No |
| `AniosExperiencia` | `INT` | `NOT NULL`, `CHECK (AniosExperiencia BETWEEN 0 AND 60)` | No |
| `Activo` | `BIT` | `NOT NULL DEFAULT 1` | No — la app lo fija explícitamente en `true` al crear (ver `docs/backend/tatuadores/dudas-recurrentes.md`); el `DEFAULT 1` de la columna solo actuaría si algo insertara sin pasar por la API |

## Indexes and relations

- `PK_Tatuador` sobre `Id`.
- Sin índices adicionales ni restricciones `UNIQUE` (p. ej. `Nombre` no es único — dos tatuadores pueden llamarse igual).
- Relación 1:N hacia `CitaTatuaje` vía `CitaTatuaje.TatuadorId` (ver `../cita-tatuaje/`). En el modelo EF Core, navegación inversa `Tatuador.CitaTatuajes` (`ICollection<CitaTatuaje>`), generada por el Scaffold.
