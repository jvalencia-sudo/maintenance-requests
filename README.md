# Solicitudes de mantenimiento

Aplicación para registrar solicitudes de mantenimiento y seguirlas hasta su cierre: estados con transiciones controladas, responsable asignable e historial de cada cambio.

**Stack:** API en .NET 8 (ASP.NET Core, EF Core 8, PostgreSQL 16) con arquitectura en capas y DDD táctico; web en Next.js 16 (React 19, TypeScript, Tailwind CSS 4, TanStack Query); todo orquestado con Docker Compose.

## Contenido

1. [Requisitos](#requisitos)
2. [Ejecución](#ejecución)
3. [Variables de entorno](#variables-de-entorno)
4. [Pruebas](#pruebas)
5. [Contrato de la API](#contrato-de-la-api)
6. [Autenticación simulada](#autenticación-simulada)
7. [Supuestos de negocio](#supuestos-de-negocio)
8. [Arquitectura](#arquitectura)
9. [Decisiones técnicas](#decisiones-técnicas)
10. [Uso de IA](#uso-de-ia)

## Requisitos

- **Docker** con **Compose v2** (`docker compose`, no `docker-compose`).
- **.NET 8 SDK**, solo para correr las pruebas. Sirve cualquier versión 8.0.x.
- Puertos **3000** (web), **8080** (API) y **5432** (PostgreSQL) libres. Si alguno está ocupado, se cambia en `.env` (ver [Variables de entorno](#variables-de-entorno)).

## Ejecución

```bash
cp .env.example .env
docker compose up --build
```

El primer arranque aplica las migraciones y siembra datos de demostración: 5 usuarios y 30 solicitudes en distintos estados.

| Servicio | URL |
|---|---|
| Web | http://localhost:3000 |
| API | http://localhost:8080 |
| Swagger | http://localhost:8080/swagger |

En la web, elige un usuario en el selector **"Actuando como"** del encabezado para crear solicitudes, cambiar su estado o asignarlas.

Para empezar de cero con la base vacía (se vuelve a sembrar al arrancar):

```bash
docker compose down -v
docker compose up --build
```

Si falta el `.env`, Compose se detiene con el mensaje `Copia .env.example a .env`.

### Desarrollo local sin contenedores para la API y la web

Solo la base corre en Docker; la API y la web corren en la máquina con recarga en caliente.

```bash
# 1. Base de datos
docker compose up -d db

# 2. API en http://localhost:5184 (perfil "http": migra, siembra y habilita Swagger)
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=maintenance_requests;Username=maintenance;Password=change_me" --project src/MaintenanceRequests.Api
dotnet run --project src/MaintenanceRequests.Api --launch-profile http

# 3. Web en http://localhost:3000
cd web
echo "NEXT_PUBLIC_API_URL=http://localhost:5184" > .env.local
npm install
npm run dev
```

La cadena de conexión va en *user-secrets* (o en la variable `ConnectionStrings__Default`), nunca en `appsettings.json`. Los valores del ejemplo son los de `.env.example`; si cambiaste el `.env`, usa los tuyos.

## Variables de entorno

Todas viven en `.env`, que se crea a partir de `.env.example` y no se versiona.

| Variable | Uso | Valor de ejemplo |
|---|---|---|
| `POSTGRES_USER` | Usuario de PostgreSQL. La API arma su cadena de conexión con estas tres variables. | `maintenance` |
| `POSTGRES_PASSWORD` | Contraseña de PostgreSQL. | `change_me` |
| `POSTGRES_DB` | Nombre de la base de datos. | `maintenance_requests` |
| `DB_PORT` | Puerto del host para PostgreSQL. Solo escucha en `127.0.0.1`. | `5432` |
| `API_PORT` | Puerto del host para la API. | `8080` |
| `WEB_PORT` | Puerto del host para la web. | `3000` |
| `APPLY_MIGRATIONS` | Aplica las migraciones pendientes al arrancar la API. | `true` |
| `SEED_DEMO_DATA` | Siembra las solicitudes de demostración si la tabla está vacía. | `true` |
| `ENABLE_SWAGGER` | Publica Swagger en `/swagger`. | `true` |
| `CORS_ALLOWED_ORIGINS` | Orígenes que pueden llamar a la API, separados por coma. `*` no se acepta. Debe coincidir con `http://localhost:${WEB_PORT}`. | `http://localhost:3000` |
| `NEXT_PUBLIC_API_URL` | URL de la API que usa el navegador. Se incrusta al construir la imagen web, así que debe coincidir con `http://localhost:${API_PORT}` y cambiarla exige `docker compose up --build`. | `http://localhost:8080` |

Si cambias `API_PORT` o `WEB_PORT`, ajusta también `NEXT_PUBLIC_API_URL` o `CORS_ALLOWED_ORIGINS`.

Compose fija además, para la API, `ASPNETCORE_ENVIRONMENT=Production`, `ASPNETCORE_HTTP_PORTS=8080` y `ConnectionStrings__Default`, apuntando al servicio `db`.

## Pruebas

Desde la raíz del repositorio:

```bash
# Unitarias del dominio (no requieren Docker)
dotnet test tests/MaintenanceRequests.UnitTests

# Integración de la API contra un PostgreSQL real (requieren Docker encendido)
dotnet test tests/MaintenanceRequests.IntegrationTests

# Todas
dotnet test
```

- **Unitarias:** reglas del agregado. Cubren validaciones de título y descripción, las 7 transiciones permitidas y las 18 rechazadas, la asignación y las propiedades calculadas.
- **Integración:** levantan la API completa con `WebApplicationFactory` contra un PostgreSQL 16 efímero de Testcontainers. Cubren:
  - el flujo principal: crear (201 con `Location`), cambiar de estado, leer el historial con sus actores y rechazar una transición inválida sin tocar el historial;
  - el conflicto de concurrencia con una versión vieja;
  - que los campos que no controla el cliente (`id`, `status`, `createdAt`) se ignoren al crear;
  - el 401 sin usuario y el 400 con errores por campo;
  - que un fallo al guardar el historial no deje el cambio de estado ni la asignación a medias.

## Contrato de la API

Base: `http://localhost:8080`. Los enums viajan como texto (`"InProgress"`) y los errores siguen [ProblemDetails (RFC 9457)](https://www.rfc-editor.org/rfc/rfc9457), con un `traceId` para rastrearlos en los logs.

| Método | Ruta | Descripción | `X-User-Id` | Respuestas |
|---|---|---|---|---|
| `GET` | `/api/maintenance-requests` | Listado paginado con filtros | No | 200, 400 |
| `GET` | `/api/maintenance-requests/summary` | Totales por estado (globales) | No | 200 |
| `GET` | `/api/maintenance-requests/{id}` | Detalle con historial | No | 200, 404 |
| `POST` | `/api/maintenance-requests` | Crea una solicitud en `Pending` | Sí | 201, 400, 401 |
| `PATCH` | `/api/maintenance-requests/{id}/status` | Cambia el estado | Sí | 200, 400, 401, 404, 409 |
| `PATCH` | `/api/maintenance-requests/{id}/assignee` | Asigna o reasigna el responsable | Sí | 200, 400, 401, 404, 409 |
| `GET` | `/api/users` | Catálogo de usuarios | No | 200 |

**Parámetros del listado:** `page` (desde 1), `pageSize` (1 a 50, por defecto 10), `status`, `priority`, `category`, `search` (contiene, en el título) y `sortDirection` (`desc` por defecto o `asc`, por fecha de creación). La respuesta trae `items`, `page`, `pageSize`, `totalCount` y `totalPages`.

**Valores:**
- `status`: `Pending`, `InProgress`, `OnHold`, `Resolved`, `Cancelled`.
- `priority`: `Low`, `Medium`, `High`, `Critical`.
- `category`: `Infrastructure`, `Equipment`, `Software`, `Other`.

**Transiciones permitidas** (cualquier otra devuelve 409):

| Desde | Hacia |
|---|---|
| `Pending` | `InProgress`, `Cancelled` |
| `InProgress` | `OnHold`, `Resolved`, `Cancelled` |
| `OnHold` | `InProgress`, `Cancelled` |
| `Resolved`, `Cancelled` | Ninguna: son estados finales |

El detalle incluye `allowedTransitions` y `canAssign`, para que el cliente no tenga que replicar estas reglas, y `version`, que se reenvía en cada cambio.

### Ejemplo: crear una solicitud

```bash
curl -X POST http://localhost:8080/api/maintenance-requests \
  -H "Content-Type: application/json" \
  -H "X-User-Id: 2" \
  -d '{"title":"Aire acondicionado sin enfriar","description":"El equipo de la sala 3 enciende pero no baja la temperatura.","category":"Equipment","priority":"High"}'
```

Responde `201 Created`, con la cabecera `Location` y el detalle completo: `id`, `status: "Pending"`, `version`, `allowedTransitions` y el historial con el evento `Created`.

Para cambiar el estado se envía la `version` que devolvió la última lectura:

```bash
curl -X PATCH http://localhost:8080/api/maintenance-requests/31/status \
  -H "Content-Type: application/json" \
  -H "X-User-Id: 1" \
  -d '{"targetStatus":"InProgress","version":123456}'
```

Reemplaza `31` y `123456` por el `id` y la `version` de la respuesta anterior.

### Ejemplo: 400 con errores por campo

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "La solicitud tiene datos inválidos.",
  "status": 400,
  "errors": {
    "title": ["El título debe tener entre 5 y 120 caracteres."]
  },
  "traceId": "00-3756af1b91d07e3f3f788def9d05ef43-28260114603235e3-00"
}
```

Las claves de `errors` usan los mismos nombres que el JSON de entrada, así la web muestra cada mensaje bajo su campo.

### Ejemplo: 409

Transición no permitida:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "La operación no es válida en el estado actual.",
  "status": 409,
  "detail": "La solicitud está en Resolved, un estado final, y ya no admite cambios de estado.",
  "code": "invalid_status_transition",
  "traceId": "00-af75c1e04c7b803d758b90589c5f5e84-4c77d0005502aa02-00"
}
```

Versión vieja (otro usuario modificó la solicitud después de tu lectura):

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Conflicto de concurrencia.",
  "status": 409,
  "detail": "La solicitud fue modificada por otro usuario. Recarga e intenta de nuevo.",
  "code": "concurrency_conflict",
  "traceId": "00-9de38317eee20f879d5fe34d93c8875a-6af2b6bc00932374-00"
}
```

Códigos de 409: `invalid_status_transition`, `request_closed` (asignar en un estado final), `same_assignee` (asignar al responsable actual) y `concurrency_conflict`.

## Autenticación simulada

La prueba no pide autenticación, así que el actor de cada operación viaja en la cabecera `X-User-Id`:

- Se valida contra el catálogo sembrado de 5 usuarios. Un id ausente, no numérico o inexistente responde **401**.
- Es obligatoria en `POST` y `PATCH`, y se registra como actor en el historial. Las lecturas no la requieren.
- La web guarda el usuario elegido en `localStorage` y envía la cabecera en cada mutación.
- En Swagger se configura una vez con el botón **Authorize**.

**No es seguro en producción:** cualquier cliente puede hacerse pasar por otro usuario cambiando el número. Se reemplazaría por JWT/OIDC implementando `ICurrentUser` a partir de los *claims* del token. El dominio y los servicios no cambian, porque solo conocen esa interfaz ([ICurrentUser.cs](src/MaintenanceRequests.Application/Abstractions/ICurrentUser.cs)).

## Supuestos de negocio

- **Estados finales:** `Resolved` y `Cancelled` no admiten cambio de estado ni de responsable (409).
- **Asignar al responsable actual** es un error (409), para no generar historial vacío.
- **No se desasigna, no se edita ni se borra.** Cancelar reemplaza al borrado y deja rastro en el historial.
- **No se exige responsable para pasar a `InProgress`** (queda como mejora candidata).
- **El solicitante es el usuario actual** (`X-User-Id`). Cualquier usuario del catálogo puede ser responsable.
- **Paginación:** `pageSize` por defecto 10 y máximo 50. Una página fuera de rango devuelve `items: []` con los totales, para que el cliente pueda volver a una página válida.
- **Búsqueda:** "contiene" sobre el título, sin distinguir mayúsculas y sensible a tildes. Los filtros admiten un valor cada uno y se combinan con AND.
- **Fechas:** se guardan en UTC (`timestamptz`) y la web las muestra en la hora local del navegador.
- **El resumen muestra totales globales**, no los del listado filtrado.
- **Título** de 5 a 120 caracteres y **descripción** de 10 a 2000, contados después de quitar espacios al inicio y al final. Se cuentan caracteres Unicode (*code points*), no unidades UTF-16, así que la mayoría de los emojis cuentan como uno.

## Arquitectura

### Contenedores

El navegador descarga la web desde `web` y llama directamente a la API; solo la API habla con la base de datos.

```mermaid
flowchart LR
    browser(["Navegador"])

    subgraph compose["Docker Compose"]
        web["<b>web</b><br/>Next.js 16<br/>:3000"]
        api["<b>api</b><br/>ASP.NET Core 8<br/>:8080"]
        db[("<b>db</b><br/>PostgreSQL 16<br/>:5432")]
    end

    browser -- "páginas y JS" --> web
    browser -- "JSON + X-User-Id" --> api
    api -- "EF Core (Npgsql)" --> db
```

### Capas del backend

Cada flecha significa "depende de". El dominio no depende de nada; Application define las interfaces que Infrastructure implementa.

```mermaid
flowchart TB
    api["<b>Api</b><br/>Controladores<br/>Errores como ProblemDetails<br/>Usuario actual (X-User-Id)"]
    app["<b>Application</b><br/>Casos de uso y DTOs<br/>Interfaces de repositorio<br/>y de consultas"]
    domain["<b>Domain</b><br/>Agregado MaintenanceRequest<br/>Transiciones de estado<br/>Historial"]
    infra["<b>Infrastructure</b><br/>EF Core y migraciones<br/>Repositorio y consultas<br/>Datos demo"]

    api --> app
    app --> domain
    infra --> app
    infra --> domain
    api -. "solo composition root (registro de dependencias)" .-> infra
```

## Decisiones técnicas

### Estructura

Cuatro proyectos de código y dos de pruebas. Las dependencias apuntan hacia el dominio: Api → Application → Domain, e Infrastructure → Application y Domain. Api referencia Infrastructure solo para registrar dependencias en [Program.cs](src/MaintenanceRequests.Api/Program.cs), y ningún controlador usa el `DbContext`.

| Proyecto | Contiene |
|---|---|
| `Domain` | El agregado `MaintenanceRequest`, su historial, el mapa de transiciones, los enums y las excepciones de negocio. No depende de ningún paquete. |
| `Application` | El servicio con los casos de uso, los DTOs de entrada y salida, y las interfaces que necesita (`ICurrentUser`, repositorio, consultas). |
| `Infrastructure` | EF Core con PostgreSQL: configuración del modelo, migraciones, repositorio, consultas de lectura y datos demo. |
| `Api` | Controladores, manejo centralizado de errores, lectura de `X-User-Id` y configuración por variables de entorno. |

- **Por qué:** el dominio no depende de EF ni de ASP.NET, así que sus reglas se prueban sin web ni base de datos. Además, el compilador impide meter infraestructura en él por accidente.
- **Costo:** más proyectos y archivos que una API de un solo proyecto. Lo acepté porque el enunciado evalúa justamente esa separación.
- **Lectura y escritura separadas, sin MediatR:**
  - Las escrituras cargan el agregado con el repositorio y lo cambian solo mediante sus métodos.
  - Las lecturas proyectan directo a DTO en [MaintenanceRequestQueries.cs](src/MaintenanceRequests.Infrastructure/Persistence/MaintenanceRequestQueries.cs), porque no tienen reglas que proteger.

### SOLID y DDD

| Principio | Dónde se ve |
|---|---|
| S | El controlador ([MaintenanceRequestsController.cs](src/MaintenanceRequests.Api/Controllers/MaintenanceRequestsController.cs)) traduce HTTP, el servicio ([MaintenanceRequestService.cs](src/MaintenanceRequests.Application/Requests/MaintenanceRequestService.cs)) orquesta el caso de uso, el agregado aplica las reglas y el repositorio persiste. |
| O | Las transiciones viven en un solo mapa ([StatusTransitions.cs](src/MaintenanceRequests.Domain/Requests/StatusTransitions.cs)), no en condicionales dispersos. Un estado nuevo es un valor del enum más su línea en el mapa. El CHECK de la base se genera desde el enum ([CheckConstraintSql.cs](src/MaintenanceRequests.Infrastructure/Persistence/Configurations/CheckConstraintSql.cs)), así que solo hace falta una migración nueva. |
| L | No hay jerarquías con comportamiento que sustituir. La única herencia es `DomainException`: sus subtipos solo agregan datos (`Code`, `Field`) y no redefinen nada. |
| I | Interfaces pequeñas: `ICurrentUser` tiene una propiedad, y el repositorio del agregado tiene tres métodos (`GetByIdAsync`, `Add`, `SaveChangesAsync`), sin `Update` genérico. |
| D | Application define las interfaces que implementan Api (`CurrentUser`) e Infrastructure (repositorio y consultas). El reloj llega como `TimeProvider`, y el dominio recibe la fecha como parámetro. |

**DDD táctico, sin más:**
- `MaintenanceRequest` es un agregado con setters privados. El estado y el responsable solo cambian con `ChangeStatus` y `Assign`, que validan y agregan la entrada del historial en la misma operación ([MaintenanceRequest.cs](src/MaintenanceRequests.Domain/Requests/MaintenanceRequest.cs)).
- El historial es una entidad hija que solo el dominio puede crear: su constructor es privado y sus fábricas son `internal`.
- `User` se referencia por id.
- No usé bounded contexts, domain events ni value objects, porque el problema no los justifica.

### Patrones y librerías

| Elemento | Decisión | Motivo |
|---|---|---|
| Repositorio (solo del agregado) | Sí | Application no depende de EF, y toda escritura pasa por el agregado. |
| Servicio de consultas | Sí | Proyección directa a DTO con filtros, orden y paginación en SQL. |
| Excepciones + `IExceptionHandler` | Sí | Un solo lugar traduce los errores a ProblemDetails; los controladores no tienen `try/catch`. |
| `TimeProvider` | Sí | La fecha se puede controlar en las pruebas sin inventar una interfaz propia. |
| Swashbuckle | Sí | Viene en la plantilla de .NET 8 (MIT). |
| EFCore.NamingConventions | Sí | Tablas y columnas en snake_case con una línea (Apache 2.0). |
| Testcontainers | Sí | El proveedor InMemory no aplica transacciones, CHECKs, FKs ni `ILIKE` (MIT). |
| TanStack Query | Sí | Carga, error, reintento e invalidación del listado y del resumen tras cada cambio. |
| MediatR, AutoMapper | No | Indirección sin beneficio en una API de este tamaño; además tienen licencia comercial desde 2025. |
| FluentValidation | No | Duplicaría reglas que ya protege el dominio. |
| Repositorio genérico, Unit of Work propio | No | `DbContext` ya es la unidad de trabajo, y un `Update` genérico saltaría las reglas del agregado. |
| Patrón State, domain events | No | Cinco estados sin comportamiento distinto: un mapa basta. Nadie consumiría los eventos. |
| FluentAssertions 8 | No | Licencia comercial desde 2025; las pruebas usan los `Assert` de xUnit. |
| react-hook-form, zod, Redux | No | Hay un solo formulario, y el estado de los filtros vive en la URL. |

### Persistencia

- **Esquema:** tres tablas, `users`, `maintenance_requests` y `request_history`, con nombres en snake_case. Las fechas son `timestamptz` y los usuarios se siembran en la migración.
- **Enums como texto** con un CHECK generado desde el enum: se leen bien en la base y no se rompen si alguien reordena el enum en C#.
- **CHECK de coherencia del historial:** cada tipo de evento exige los datos que lo describen. Por ejemplo, un `StatusChanged` sin `from_status` no entra.
- **Índices, cada uno por una consulta:**
  - `(created_at, id)`: el listado y su orden, con `id` como desempate estable para paginar.
  - `(status, created_at, id)`: el listado filtrado por estado.
  - GIN con `gin_trgm_ops` sobre `title` (extensión `pg_trgm`): la búsqueda con `ILIKE '%texto%'`, que un índice B-tree no puede usar.
  - `(request_id, occurred_at, id)`: el historial del detalle, en orden.
- **Índices que no creé:**
  - Quité la convención de EF que indexa cada FK, porque ninguna consulta filtra por `requester_id` ni `assignee_id`.
  - `priority` y `category` sí son filtros, pero tienen 4 valores cada uno. Un índice propio apenas descarta filas, y con este volumen el planificador prefiere recorrer la tabla. Con datos reales lo decidiría con `EXPLAIN ANALYZE`.
- **Atomicidad:** el cambio y su entrada de historial se guardan en un solo `SaveChanges`, que EF ejecuta en una transacción. [HistoryAtomicityTests.cs](tests/MaintenanceRequests.IntegrationTests/HistoryAtomicityTests.cs) lo demuestra: un trigger hace fallar el INSERT del historial, y la prueba verifica que el cambio de estado o de responsable también se revirtió.
- **Concurrencia optimista:** `Version` se mapea a la columna de sistema `xmin` de PostgreSQL, que cambia en cada UPDATE, así que no hace falta una columna propia. El cliente reenvía la versión que leyó. Si no coincide, o si otra escritura se adelantó entre la lectura y el guardado, la API responde 409 `concurrency_conflict`.
- **Migraciones:** versionadas en el repositorio y aplicadas al arrancar si `APPLY_MIGRATIONS=true`. En producción irían en un paso del pipeline, antes del despliegue.
- **Borrado:** todas las FKs son `RESTRICT`. No hay borrado; cancelar lo reemplaza y el historial se conserva completo.

### Limitaciones y mejoras

- **Autenticación real** (JWT/OIDC) con roles. Hoy cualquier cliente puede suplantar a otro usuario, y el 401 no incluye la cabecera `WWW-Authenticate`.
- **Búsqueda sin tildes:** hoy la búsqueda distingue tildes. Se resolvería con la extensión `unaccent`.
- **Paginación por cursor:** hoy es por offset; con volumen alto conviene el cursor.
- **Desasignar, editar y comentar:** no se puede desasignar ni editar, y las transiciones no llevan comentario ni motivo.
- **Responsable obligatorio para `InProgress`:** hoy no se exige.
- **Carrera de `xmin`:** la protección ante dos escrituras simultáneas no tiene prueba automática. La comparación con la versión del cliente sí la tiene.
- **Reintentos ante fallos transitorios:** no hay `EnableRetryOnFailure`. Si la base se reinicia, la primera petición puede responder 500.
- **Detalles del contrato:**
  - Los 400 que genera el propio framework (JSON mal formado, campo faltante) tienen el título en inglés.
  - Los enums también aceptan su valor numérico.
- **Cadena de conexión:** se arma concatenando variables, así que un `;` en `POSTGRES_PASSWORD` la rompería.
- **URL de la API fija en el build:** `NEXT_PUBLIC_API_URL` queda fija al construir la imagen web.
- **Validación duplicada:** los límites de título y descripción están duplicados en la web ([validation.ts](web/src/lib/validation.ts)) para dar respuesta inmediata. La API sigue siendo la que decide.
- **Pruebas, CI y observabilidad:** el frontend no tiene pruebas automatizadas, y faltan CI con las pruebas y métricas y trazas.

## Uso de IA

Usé Claude (Anthropic) como apoyo durante toda la prueba, como lo permite el enunciado:

- **Análisis y diseño:** el análisis de lo que había que hacer y el diseño principal
  fueron míos. Ese diseño se fue ajustando con mejoras que propuso Claude.
- **Generación de código:** buena parte del código se generó con Claude Code a partir
  de ese diseño, bloque por bloque (dominio, persistencia, API, pruebas, frontend,
  Docker). Por eso varios commits tienen pocos minutos de diferencia entre sí.
- **Revisión:** después de cada bloque pedí una revisión con criterio de evaluador y
  corregí lo que salió de ella. Por ejemplo: la connection string pasó a user secrets,
  el healthcheck de PostgreSQL ahora espera por TCP, la tabla ya no se recorta en
  tablet y la descripción ya no desborda en móvil.

**Cómo verifiqué el resultado:** leí el código completo de cada capa hasta poder
explicarlo, ejecuté las pruebas unitarias y de integración, probé la API desde Swagger
(transiciones inválidas, versión vieja, datos inválidos, falta de X-User-Id), revisé la
interfaz en varios anchos de pantalla y levanté la solución con Docker Compose desde un
clon limpio.

Las decisiones de diseño son mías y puedo justificarlas; la IA aceleró la escritura
y la revisión, no reemplazó el criterio.