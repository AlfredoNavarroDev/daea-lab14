# Notas — citas-tatuaje

## Dependencias

- `CitaTatuajeService` depende de **ambos** repositorios (`ICitaTatuajeRepository` e `ITatuadorRepository`) porque necesita validar que el `TatuadorId` del DTO exista — es el único servicio que cruza repositorios de otra unidad.
- `TatuadoresController` consume `ICitaTatuajeService` para su ruta anidada (ver `docs/backend/tatuadores/notas.md`) — dependencia cruzada en el sentido inverso, a nivel de controlador.

## Notas

- `EliminarAsync` hace un `DELETE` físico (`CitaTatuajeRepository.cs:80-84`), a diferencia de `Tatuador`, que solo se desactiva. La entidad `CitaTatuaje` no tiene columna `Activo`, así que no hay una baja lógica posible sin agregar esa columna — el enunciado del laboratorio tampoco la pide para esta entidad.
- `ObtenerActivasPorTatuadorAsync` define "activa" como `Estado != "Cancelada"` (`CitaTatuajeRepository.cs:53-65`), es decir, trata `Pendiente` y `Confirmada` como equivalentes para efectos de solapamiento — coincide con la definición de "cita activa" que usa `TatuadorRepository.TieneCitasActivasAsync` para la regla de baja de tatuadores, aunque son dos consultas independientes (una filtra `!= "Cancelada"`, la otra filtra `== "Pendiente" || == "Confirmada"` — equivalentes en resultado dado que `Estado` solo tiene esos tres valores posibles, pero escritas de formas distintas en cada repositorio).
