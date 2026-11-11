# Database — TattooStudio (SQL Server)

Base de datos creada **database-first**: `database/TattooStudio.sql` crea la base `TattooStudio`, las tablas y los datos de ejemplo; el modelo de EF Core (`TattooStudio.Entidades/Tatuador.cs`, `CitaTatuaje.cs`, `TattooStudio.Datos/Data/TattooStudioContext.cs`) se generó después con `dotnet ef dbcontext scaffold` a partir de esa base (ver `README.md` de la raíz del repo para el comando completo).

## Tablas

| Tabla | Carpeta de doc |
|---|---|
| `dbo.Tatuador` | [./tatuador/](./tatuador/) |
| `dbo.CitaTatuaje` | [./cita-tatuaje/](./cita-tatuaje/) |

## Relación

`CitaTatuaje.TatuadorId` → `Tatuador.Id` (1:N). La FK (`FK_CitaTatuaje_Tatuador`) no tiene `ON DELETE CASCADE` en el script SQL (SQL Server aplica `NO ACTION` por defecto); el modelo EF Core refleja esto explícitamente con `OnDelete(DeleteBehavior.Restrict)` (`TattooStudioContext.cs:37`), coherente con que `TatuadorId` es `NOT NULL` y con que la app nunca borra un `Tatuador` físicamente (solo baja lógica, ver `docs/backend/tatuadores/`).

## Known gaps

- **Script SQL vs. modelo EF Core — concordancia verificada:** se comparó columna por columna el `CREATE TABLE` de `database/TattooStudio.sql` contra `OnModelCreating` de `TattooStudioContext.cs` y coinciden (mismos `HasMaxLength`, mismo `HasColumnType("decimal(p,s)")`, mismos valores por defecto, mismo comportamiento de borrado en la FK). No se detectó drift entre el script y el modelo generado.
