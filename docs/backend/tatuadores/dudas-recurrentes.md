# Dudas recurrentes — tatuadores

**P: ¿Por qué `TatuadorCrearDto` no tiene un campo `Activo`?**
R: Porque un tatuador siempre nace activo — el servicio lo fija a mano (`tatuador.Activo = true;` en `TatuadorService.CrearAsync`, `TattooStudio.Negocio/Servicios/TatuadorService.cs:35`) después del mapeo, en vez de dejar que el cliente lo decida.

**P: ¿El `DELETE` borra la fila de la base de datos?**
R: No. `TatuadoresController.Desactivar` llama a `TatuadorService.DesactivarAsync`, que pone `Activo = false` y hace `Update` (`TatuadorService.cs:78-79`) — es una baja lógica. La fila sigue en `dbo.Tatuador` y sigue apareciendo si se filtra `activo=false`.

**P: ¿Por qué `PUT` valida la regla de citas activas solo "si estaba activo y lo quiero desactivar", pero `DELETE` la valida siempre?**
R: Es el comportamiento real del código, no una inconsistencia accidental: en `ActualizarAsync` la condición es `tatuador.Activo && !dto.Activo` (`TatuadorService.cs:48`) porque el `PUT` puede estar actualizando otros campos sin tocar `Activo`, o reactivando un tatuador — en ninguno de esos casos tiene sentido exigir "sin citas activas". `DesactivarAsync` (el `DELETE`) siempre va a desactivar, así que siempre valida (`TatuadorService.cs:71`).

**P: ¿Por qué `GET /api/tatuadores/{tatuadorId}/citas` no filtra por estado?**
R: Porque `CitaTatuajeRepository.ObtenerPorTatuadorAsync` no recibe ni aplica ningún filtro de `Estado` (`CitaTatuajeRepository.cs:33-41`) — devuelve Pendientes, Confirmadas y Canceladas por igual. Si se necesita solo las activas, hay que filtrar en el cliente o usar `GET /api/citastatuaje?tatuadorId=&estado=`.
