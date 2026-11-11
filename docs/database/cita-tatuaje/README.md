# cita-tatuaje

Representa una cita de tatuaje agendada: quién es el cliente, cuándo, cuánto dura, cuánto pagó de seña, en qué estado está y con qué tatuador.

**Files:** `TattooStudio.Entidades/CitaTatuaje.cs`, `TattooStudio.Datos/Data/TattooStudioContext.cs` (configuración en `OnModelCreating`), `database/TattooStudio.sql` (`CREATE TABLE dbo.CitaTatuaje`).

## Columns / fields

| Nombre | Tipo (SQL) | Constraints/default | Computado |
|---|---|---|---|
| `Id` | `INT IDENTITY(1,1)` | `PRIMARY KEY` | No |
| `ClienteNombre` | `NVARCHAR(100)` | `NOT NULL` | No |
| `ClienteCorreo` | `NVARCHAR(150)` | `NOT NULL` | No — sin `CHECK` de formato de correo a nivel de base; el formato lo valida solo el DTO (`[EmailAddress]`) |
| `FechaHora` | `DATETIME2` | `NOT NULL` | No |
| `EstimacionHoras` | `DECIMAL(5,2)` | `NOT NULL`, `CHECK (EstimacionHoras BETWEEN 0.5 AND 12)` | No |
| `SeñaPagada` | `DECIMAL(10,2)` | `NOT NULL DEFAULT 0`, `CHECK (SeñaPagada BETWEEN 0 AND 100000)` | No |
| `Estado` | `NVARCHAR(20)` | `NOT NULL DEFAULT 'Pendiente'`, `CHECK (Estado IN ('Pendiente','Confirmada','Cancelada'))` | No — la app también fija `"Pendiente"` explícitamente al crear (`CitaTatuajeService.CrearAsync`), duplicando el `DEFAULT` de la columna |
| `TatuadorId` | `INT` | `NOT NULL`, `FOREIGN KEY → Tatuador(Id)` | No |

Nota de nombre: la columna `SeñaPagada` lleva una `ñ` (Unicode `U+00F1`) tanto en el script SQL como en la entidad C# (`CitaTatuaje.SeñaPagada`) y en los DTOs — es un identificador válido en C# y en SQL Server (columna `NVARCHAR`), pero exige que cualquier herramienta/cliente que construya el JSON a mano use la codificación UTF-8 correcta; de lo contrario el campo no bindea y el valor queda en su default (visto durante pruebas manuales con `curl` desde una terminal con mojibake en el argumento).

## Indexes and relations

- `PK_CitaTatuaje` sobre `Id`.
- `IX_CitaTatuaje_TatuadorId` (índice no único) sobre `TatuadorId`, pensado para acelerar los filtros por tatuador (`GET /api/citastatuaje?tatuadorId=` y la ruta anidada `GET /api/tatuadores/{id}/citas`).
- `FK_CitaTatuaje_Tatuador` (`TatuadorId → Tatuador.Id`): el script SQL no define una acción `ON DELETE`, por lo que SQL Server aplica `NO ACTION` (bloquea el `DELETE` del padre si hay hijos). El modelo EF Core refleja esto con `DeleteBehavior.Restrict`, coherente con que la app nunca hace `DELETE` físico sobre `Tatuador` (solo baja lógica).
