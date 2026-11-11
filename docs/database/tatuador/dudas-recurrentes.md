# Dudas recurrentes — tatuador

**P: ¿El tope de 60 años de experiencia es solo del DTO o también está en la base?**
R: Está en ambas capas: `CK_Tatuador_AniosExperiencia CHECK (AniosExperiencia BETWEEN 0 AND 60)` en la base (`database/TattooStudio.sql:28`) y `[Range(0, 60)]` en el DTO (`docs/backend/tatuadores/validacion.md`) — se mantienen deliberadamente sincronizados.

**P: ¿`Especialidad` es un catálogo cerrado (enum) en la base?**
R: No. Es `NVARCHAR(50)` sin `CHECK` ni tabla de referencia — cualquier texto de hasta 50 caracteres es válido a nivel de esquema. Los valores de ejemplo del enunciado (Realismo, Tradicional, Fine Line) son una convención de uso, no una restricción impuesta por el modelo.
