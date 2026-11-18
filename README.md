# TattooStudio API

Web API REST en capas (ASP.NET Core 10 + Entity Framework Core 10, Database First) para la gestión de un estudio de tatuajes: tatuadores y sus citas agendadas. Desde la Semana 14, protegida con autenticación JWT y roles propios del negocio.

## Dominio

- **Tatuador**: `Id`, `Nombre`, `Especialidad` (Realismo, Tradicional, Fine Line, ...), `AniosExperiencia`, `Activo`.
- **CitaTatuaje**: `Id`, `ClienteNombre`, `ClienteCorreo`, `FechaHora`, `EstimacionHoras`, `SeñaPagada`, `Estado` (Pendiente, Confirmada, Cancelada), `TatuadorId` (FK).
- Relación **1:N**: un Tatuador tiene muchas CitaTatuaje.

### Reglas de negocio (409 Conflict)

1. No se puede crear/editar una cita si el tatuador ya tiene otra cita **activa** (Pendiente o Confirmada) que se solapa en el tiempo (ventana `FechaHora` → `FechaHora + EstimacionHoras`).
2. No se puede eliminar (desactivar) un tatuador si tiene citas en estado `Pendiente` o `Confirmada`.

Ambas reglas viven en la capa Negocio y lanzan `ReglaNegocioException`; el controlador la traduce a `409 Conflict`.

## Autenticación JWT (Semana 14)

- **Usuario**: `Id`, `Correo` (único), `ClaveHash` (BCrypt), `Rol`.
- Roles propios del negocio: **Recepcionista** (agenda citas, gestiona tatuadores) y **Tatuador** (cuenta del artista; puede actualizar citas existentes). La autorización es por rol, no por pertenencia: `Usuario` no tiene clave foránea hacia `Tatuador`, así que cualquier cuenta con rol `Tatuador` puede actualizar cualquier cita, no solo las de un tatuador en particular (ver `docs/backend/autenticacion/`). El cliente nunca envía el rol: `POST api/auth/registro` siempre asigna `Recepcionista` por defecto; para dar de alta una cuenta de `Tatuador` se promueve con un `UPDATE` sobre la tabla `Usuario` (ver `database/TattooStudio.sql`).
- El login devuelve un JWT firmado (HMAC-SHA256) con los claims `sub` (Id), `email` (Correo) y `role` (Rol); expira a los 60 minutos (configurable en `Jwt:MinutosExpiracion`).
- `AuthController`:
  - `POST api/auth/registro` — público, crea el usuario con clave hasheada; `409 Conflict` si el correo ya existe.
  - `POST api/auth/login` — público, devuelve `{ token, expira }` o `401 Unauthorized` (mismo código si el correo no existe o si la clave es incorrecta, para no filtrar cuál correo existe).
  - `GET api/auth/perfil` — requiere estar autenticado; devuelve correo y rol leídos de los claims del token, no de la base de datos.

### Matriz de permisos

| Endpoint | Acceso |
|---|---|
| `POST /api/auth/registro` | Público |
| `POST /api/auth/login` | Público |
| `GET /api/auth/perfil` | Autenticado (cualquier rol) |
| `GET /api/tatuadores` | Autenticado (cualquier rol) |
| `GET /api/tatuadores/{id}` | Autenticado (cualquier rol) |
| `GET /api/tatuadores/{id}/citas` | Autenticado (cualquier rol) |
| `POST /api/tatuadores` | Rol `Recepcionista` |
| `PUT /api/tatuadores/{id}` | Rol `Recepcionista` |
| `DELETE /api/tatuadores/{id}` | Rol `Recepcionista` |
| `GET /api/citastatuaje` | Autenticado (cualquier rol) |
| `GET /api/citastatuaje/{id}` | Autenticado (cualquier rol) |
| `POST /api/citastatuaje` | Rol `Recepcionista` |
| `PUT /api/citastatuaje/{id}` | Roles `Recepcionista` o `Tatuador` |
| `DELETE /api/citastatuaje/{id}` | Rol `Recepcionista` |

Swagger expone el botón **Authorize** (esquema `Bearer`, definido con un `IOpenApiDocumentTransformer` sobre el documento OpenAPI) para probar los endpoints protegidos.

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
│   ├── Usuario.cs
│   └── Dtos/
│       ├── TatuadorDtos.cs        # TatuadorDto, TatuadorCrearDto, TatuadorActualizarDto
│       ├── CitaTatuajeDtos.cs     # CitaTatuajeDto, CitaTatuajeCrearDto, CitaTatuajeActualizarDto
│       └── AutenticacionDtos.cs   # RegistroDto, LoginDto, TokenDto, PerfilDto
├── TattooStudio.Datos/
│   ├── Data/TattooStudioContext.cs
│   └── Repositorios/
│       ├── ITatuadorRepository.cs / TatuadorRepository.cs
│       ├── ICitaTatuajeRepository.cs / CitaTatuajeRepository.cs
│       └── IUsuarioRepository.cs / UsuarioRepository.cs
├── TattooStudio.Negocio/
│   ├── MappingProfile.cs
│   ├── Excepciones/ReglaNegocioException.cs
│   └── Servicios/
│       ├── ITatuadorService.cs / TatuadorService.cs
│       ├── ICitaTatuajeService.cs / CitaTatuajeService.cs
│       └── IAutenticacionService.cs / AutenticacionService.cs
├── TattooStudio.Api/
│   ├── Controllers/
│   │   ├── TatuadoresController.cs
│   │   ├── CitasTatuajeController.cs
│   │   └── AuthController.cs
│   ├── Program.cs                      # AddAuthentication+JwtBearer, [Authorize] global, transformer Bearer en Swagger
│   ├── appsettings.json                # sin secretos, va al repositorio
│   └── appsettings.Development.json    # ConnectionStrings + Jwt reales; gitignored, no se commitea
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

El script `database/TattooStudio.sql` crea la base `TattooStudio`, las tablas `Tatuador`, `CitaTatuaje` y `Usuario` (con FK, `CHECK` de `Estado`/`Rol` y datos de ejemplo), y el modelo de EF Core se generó desde esa base con Scaffold.

## Configuración y secretos

La cadena de conexión y la sección `Jwt` (clave, emisor, audiencia, minutos de expiración) **no se commitean**: viven en `TattooStudio.Api/appsettings.Development.json`, que está en `.gitignore`. Quien clone el repositorio debe crear ese archivo a mano con esta forma antes de correr la API:

```json
{
  "ConnectionStrings": {
    "TattooStudioConnection": "Server=.\\SQLEXPRESS;Database=TattooStudio;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Clave": "una-clave-secreta-de-al-menos-32-caracteres",
    "Emisor": "TattooStudio.Api",
    "Audiencia": "TattooStudio.Clientes",
    "MinutosExpiracion": "60"
  }
}
```

`appsettings.json` (sin secretos) solo trae `Logging` y `AllowedHosts`, y sí se commitea.

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

# Semana 14: autenticación JWT
dotnet add TattooStudio.Negocio package BCrypt.Net-Next
dotnet add TattooStudio.Negocio package System.IdentityModel.Tokens.Jwt --version "8.*"
dotnet add TattooStudio.Api package Microsoft.AspNetCore.Authentication.JwtBearer --version "10.*"
```

Tras agregar los paquetes: tabla `Usuario` a mano en `TattooStudio.Entidades/Usuario.cs` + `TattooStudioContext` (no se usó `--force` del scaffold para no perder los ajustes ya hechos sobre `Tatuador`/`CitaTatuaje`), sección `Jwt` en `appsettings.Development.json` (ver [Configuración y secretos](#configuración-y-secretos)), `AuthController`, y `AddAuthentication().AddJwtBearer(...)` + `UseAuthentication()` en `Program.cs` (antes de `UseAuthorization()`).

Tras el scaffold, `Tatuador.cs` y `CitaTatuaje.cs` se movieron de `TattooStudio.Datos/Models` a `TattooStudio.Entidades/` (namespace ajustado a `TattooStudio.Entidades`); `TattooStudioContext.cs` quedó en `TattooStudio.Datos/Data/`.

## Verificación de códigos de respuesta probados

| Código | Caso probado |
|---|---|
| 200 | `GET /api/tatuadores`, `GET /api/tatuadores/{id}/citas`, `POST /api/auth/login` válido, `GET /api/auth/perfil` |
| 201 | `POST /api/citastatuaje` con datos válidos, `POST /api/auth/registro` con correo nuevo |
| 204 | `PUT`/`DELETE` exitosos sobre tatuador y cita |
| 400 | `POST /api/citastatuaje` con correo inválido, nombre corto y horas en 0 |
| 401 | `POST /api/auth/login` con correo inexistente o clave incorrecta; cualquier endpoint protegido sin token o con token vencido |
| 403 | `POST /api/tatuadores` o `POST /api/citastatuaje` con un token de rol `Tatuador` (acción reservada a `Recepcionista`) |
| 404 | `GET`/`DELETE` sobre id inexistente |
| 409 | Cita solapada para el mismo tatuador; desactivar tatuador con citas Pendiente/Confirmada; `POST /api/auth/registro` con correo ya registrado |
