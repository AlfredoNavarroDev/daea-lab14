# Dudas recurrentes — cita-tatuaje

**P: ¿Por qué `Estado` tiene un `DEFAULT 'Pendiente'` en la base si la app siempre lo fija a mano al crear?**
R: Son dos redes de seguridad independientes. La app lo fija en `CitaTatuajeService.CrearAsync` (`cita.Estado = "Pendiente";`) porque `CitaTatuajeCrearDto` no expone ese campo. El `DEFAULT` de la columna (`database/TattooStudio.sql:39`) protegería ante un `INSERT` que no pase por la API (p. ej. un script de carga de datos) y que tampoco especifique `Estado`.

**P: ¿Se puede insertar una cita con `SeñaPagada` negativa directamente en SQL?**
R: No. `CK_CitaTatuaje_SeñaPagada CHECK (SeñaPagada BETWEEN 0 AND 100000)` lo bloquea a nivel de base, con los mismos límites que el `[Range(0, 100000)]` del DTO (ver `docs/backend/citas-tatuaje/validacion.md`) — ya no depende únicamente de que la escritura pase por la API.

**P: ¿Qué pasa si se intenta borrar un `Tatuador` directamente en SQL (fuera de la API) y tiene citas?**
R: La `FOREIGN KEY FK_CitaTatuaje_Tatuador` no tiene `ON DELETE CASCADE` ni `SET NULL` (`TatuadorId` es `NOT NULL`, no podría ser `SET NULL` aunque se configurara) — SQL Server rechaza el `DELETE` con un error de violación de clave foránea. La única forma soportada de "eliminar" un tatuador es la baja lógica de la API (`docs/backend/tatuadores/`).
