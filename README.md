# TattooStudio API

Web API REST en capas (ASP.NET Core 10 + Entity Framework Core 10, Database First) para la gestión de un estudio de tatuajes: tatuadores y sus citas agendadas.

## Dominio

- **Tatuador**: `Id`, `Nombre`, `Especialidad` (Realismo, Tradicional, Fine Line, ...), `AniosExperiencia`, `Activo`.
- **CitaTatuaje**: `Id`, `ClienteNombre`, `ClienteCorreo`, `FechaHora`, `EstimacionHoras`, `SeñaPagada`, `Estado` (Pendiente, Confirmada, Cancelada), `TatuadorId` (FK).
- Relación **1:N**: un Tatuador tiene muchas CitaTatuaje.

### Reglas de negocio (409 Conflict)

1. No se puede crear/editar una cita si el tatuador ya tiene otra cita **activa** (Pendiente o Confirmada) que se solapa en el tiempo (ventana `FechaHora` → `FechaHora + EstimacionHoras`).
2. No se puede eliminar (desactivar) un tatuador si tiene citas en estado `Pendiente` o `Confirmada`.

Ambas reglas viven en la capa Negocio y lanzan `ReglaNegocioException`; el controlador la traduce a `409 Conflict`.

## Arquitectura en capas

```
API (TattooStudio.Api)        -> controladores, Program.cs, Swagger
   -> Negocio (TattooStudio.Negocio)   -> servicios, MappingProfile, ReglaNegocioException
       -> Datos (TattooStudio.Datos)        -> DbContext, repositorios
           -> Entidades (TattooStudio.Entidades) -> entidades + DTOs
```

El controlador solo conoce servicios (por interfaz) y DTOs; nunca el `DbContext` ni las entidades.

## Estructura de carpetas

```
Lab/
├── database/
│   └── TattooStudio.sql           # script de creación + datos de ejemplo (database-first)
├── TattooStudio.Entidades/
│   ├── Tatuador.cs
│   ├── CitaTatuaje.cs
│   └── Dtos/
│       ├── TatuadorDtos.cs        # TatuadorDto, TatuadorCrearDto, TatuadorActualizarDto
│       └── CitaTatuajeDtos.cs     # CitaTatuajeDto, CitaTatuajeCrearDto, CitaTatuajeActualizarDto
├── TattooStudio.Datos/
│   ├── Data/TattooStudioContext.cs
│   └── Repositorios/
│       ├── ITatuadorRepository.cs / TatuadorRepository.cs
│       └── ICitaTatuajeRepository.cs / CitaTatuajeRepository.cs
├── TattooStudio.Negocio/
│   ├── MappingProfile.cs
│   ├── Excepciones/ReglaNegocioException.cs
│   └── Servicios/
│       ├── ITatuadorService.cs / TatuadorService.cs
│       └── ICitaTatuajeService.cs / CitaTatuajeService.cs
├── TattooStudio.Api/
│   ├── Controllers/
│   │   ├── TatuadoresController.cs
│   │   └── CitasTatuajeController.cs
│   ├── Program.cs
│   └── appsettings.json
└── TattooStudio.slnx
```

## Endpoints

### `api/Tatuadores`

| Método | Ruta | Descripción | Código éxito |
|---|---|---|---|
| GET | `/api/tatuadores?especialidad=&activo=` | Lista con filtros por query string | 200 |
| GET | `/api/tatuadores/{id}` | Detalle por id | 200 / 404 |
| GET | `/api/tatuadores/{tatuadorId}/citas` | **Ruta anidada**: citas de un tatuador | 200 / 404 |
| POST | `/api/tatuadores` | Crea un tatuador | 201 |
| PUT | `/api/tatuadores/{id}` | Actualiza (puede desactivar) | 204 / 404 / 409 |
| DELETE | `/api/tatuadores/{id}` | Baja lógica (`Activo = false`) | 204 / 404 / 409 |

### `api/CitasTatuaje`

| Método | Ruta | Descripción | Código éxito |
|---|---|---|---|
| GET | `/api/citastatuaje?tatuadorId=&estado=` | Lista con filtros por query string | 200 |
| GET | `/api/citastatuaje/{id}` | Detalle por id | 200 / 404 |
| POST | `/api/citastatuaje` | Crea una cita (valida solapamiento) | 201 / 404 / 409 |
| PUT | `/api/citastatuaje/{id}` | Actualiza una cita (valida solapamiento) | 204 / 404 / 409 |
| DELETE | `/api/citastatuaje/{id}` | Elimina una cita | 204 / 404 |

Swagger UI: `/swagger` (documento OpenAPI generado en `/openapi/v1.json`). Solo expone DTOs, nunca las entidades.

## Mapeos (AutoMapper) — sustentación

| Mapeo | Origen → Destino | ¿Por qué en ese sentido? | Configuración especial |
|---|---|---|---|
| `Tatuador → TatuadorDto` | Entidad → DTO de lectura | Es lo que la API devuelve al cliente; la entidad no sale de la API | `ForMember`: `CantidadCitas` se calcula con `MapFrom(src => src.CitaTatuajes.Count)` |
| `TatuadorCrearDto → Tatuador` | DTO de entrada → Entidad | Lo que llega del cliente al crear, se convierte en entidad para persistir | Ninguna (nombres coinciden); `Activo` se fija en `true` en el servicio tras el mapeo |
| `TatuadorActualizarDto → Tatuador` | DTO de entrada → Entidad existente | Se usa con `Map(origen, destino)` para actualizar una entidad trackeada sin perder su `Id` | Ninguna |
| `CitaTatuaje → CitaTatuajeDto` | Entidad → DTO de lectura | Es lo que la API devuelve al cliente | `ForMember`: `TatuadorNombre` toma el nombre del tatuador relacionado con `MapFrom(src => src.Tatuador.Nombre)`, en vez de devolver el objeto `Tatuador` completo |
| `CitaTatuajeCrearDto → CitaTatuaje` | DTO de entrada → Entidad | Lo que llega del cliente al crear la cita | Ninguna; `Estado` se fija en `"Pendiente"` en el servicio tras el mapeo |
| `CitaTatuajeActualizarDto → CitaTatuaje` | DTO de entrada → Entidad existente | Se usa con `Map(origen, destino)` para actualizar (incluye reprogramar o cambiar `Estado`) | Ninguna |

No se usa `ReverseMap()`: los sentidos Entidad→DTO (lectura) y DTO→Entidad (entrada) tienen formas distintas (uno agrega datos calculados/relacionados, el otro valida con Data Annotations), así que se definen por separado.

## Base de datos (Database First)

El script `database/TattooStudio.sql` crea la base `TattooStudio`, las tablas `Tatuador` y `CitaTatuaje` (con FK, `CHECK` de `Estado` y datos de ejemplo), y el modelo de EF Core se generó desde esa base con Scaffold.

Cadena de conexión (`TattooStudio.Api/appsettings.json`):

```
Server=.\SQLEXPRESS;Database=TattooStudio;Trusted_Connection=True;TrustServerCertificate=True;
```

## Comandos usados (en orden)

```bash
# Base de datos
sqlcmd -S ".\SQLEXPRESS" -E -f 65001 -i database/TattooStudio.sql

# Solución y proyectos
dotnet new sln -n TattooStudio
dotnet new classlib -n TattooStudio.Entidades
dotnet new classlib -n TattooStudio.Datos
dotnet new classlib -n TattooStudio.Negocio
dotnet new webapi -n TattooStudio.Api --use-controllers
dotnet sln add TattooStudio.Entidades TattooStudio.Datos TattooStudio.Negocio TattooStudio.Api

# Referencias (API -> Negocio -> Datos -> Entidades)
dotnet add TattooStudio.Api reference TattooStudio.Negocio
dotnet add TattooStudio.Negocio reference TattooStudio.Datos
dotnet add TattooStudio.Datos reference TattooStudio.Entidades

# Paquetes NuGet
dotnet add TattooStudio.Datos package Microsoft.EntityFrameworkCore.SqlServer --version "10.*"
dotnet add TattooStudio.Api package Microsoft.EntityFrameworkCore.Design --version "10.*"
dotnet add TattooStudio.Api package Swashbuckle.AspNetCore.SwaggerUI
dotnet add TattooStudio.Negocio package AutoMapper

# Scaffold del modelo (database-first) hacia TattooStudio.Datos
dotnet ef dbcontext scaffold "Server=.\SQLEXPRESS;Database=TattooStudio;Trusted_Connection=True;TrustServerCertificate=True;" \
  Microsoft.EntityFrameworkCore.SqlServer \
  --project TattooStudio.Datos --startup-project TattooStudio.Api \
  --output-dir Models --context TattooStudioContext --no-onconfiguring --force

# Build y ejecución
dotnet build
dotnet run --project TattooStudio.Api
```

Tras el scaffold, `Tatuador.cs` y `CitaTatuaje.cs` se movieron de `TattooStudio.Datos/Models` a `TattooStudio.Entidades/` (namespace ajustado a `TattooStudio.Entidades`); `TattooStudioContext.cs` quedó en `TattooStudio.Datos/Data/`.

## Verificación de códigos de respuesta probados

| Código | Caso probado |
|---|---|
| 200 | `GET /api/tatuadores`, `GET /api/tatuadores/{id}/citas` |
| 201 | `POST /api/citastatuaje` con datos válidos |
| 204 | `PUT`/`DELETE` exitosos sobre tatuador y cita |
| 400 | `POST /api/citastatuaje` con correo inválido, nombre corto y horas en 0 |
| 404 | `GET`/`DELETE` sobre id inexistente |
| 409 | Cita solapada para el mismo tatuador; desactivar tatuador con citas Pendiente/Confirmada |
