# Notas — tatuadores

## Dependencias

- `TatuadoresController` consume `ITatuadorService` **y** `ICitaTatuajeService` (para la ruta anidada `/citas`) — es el único controlador que depende de un servicio que no es "el suyo".
- `TatuadorService` depende solo de `ITatuadorRepository` y `IMapper`.

## Notas

- El `DbSet` se llama `Tatuadors` (pluralización automática de Scaffold, inglés mal aplicado sobre una palabra española) en vez de `Tatuadores` — se dejó tal como lo generó `dotnet ef dbcontext scaffold` para no reintroducir Fluent API manual; no afecta el comportamiento, solo la lectura del código (`TattooStudioContext.cs:17`).
