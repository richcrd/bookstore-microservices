# bookstore-microservices

**Microservicios de una librería online en .NET 10 con Clean Architecture**: un sistema completo y desplegable compuesto por 5 APIs + una saga orquestada por mensajes, con autenticación OpenID Connect, resiliencia, observabilidad y pipeline CI/CD.

---

## Badges

| CI (main) | Stack |
|---|---|
| [![CI](https://github.com/richcrd/bookstore-microservices/actions/workflows/ci.yml/badge.svg)](https://github.com/richcrd/bookstore-microservices/actions/workflows/ci.yml) | ![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4) ![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-336791) ![RabbitMQ](https://img.shields.io/badge/RabbitMQ-4-FF6600) ![Docker](https://img.shields.io/badge/Docker-✓-2496ED) ![License](https://img.shields.io/badge/license-MIT-blue) |

> _Banner / demo del producto: pendiente de añadir._

## Tabla de contenidos

- [Features](#features)
- [Arquitectura](#arquitectura) · [detalle](docs/architecture.md)
- [Requisitos previos](#requisitos-previos)
- [Quickstart (desarrollo)](#quickstart-desarrollo)
- [Configuración](#configuración)
- [Uso / API](#uso--api)
- [Estructura del proyecto](#estructura-del-proyecto)
- [Testing](#testing)
- [Deploy / CI-CD](#deploy--ci-cd)
- [Observabilidad](#observabilidad)
- [Aspire (AppHost)](#aspire-apphost)
- [Fases de desarrollo](docs/phases.md) · [Arquitectura de detalle](docs/architecture.md)
- [Changelog](#changelog)
- [Contributing](#contributing)
- [Licencia](#licencia)

## Features

- **Un solo punto de entrada**: gateway YARP que enruta `/api/v1/*` y los endpoints OIDC (`/connect/*`, `/.well-known/*`) a los servicios internos (y reescribe destinos en prod).
- **Autenticación OpenID Connect** centralizada (`Auth.API` actúa de **proveedor OIDC** con OpenIddict — Authorization Code + PKCE para SPA, *password*/*refresh* para clientes confidenciales; **el gateway y los servicios** validan los tokens por *discovery*, y las rutas `orders`/`stock-items` exigen token **en el borde** — 401 sin él — mientras `catalog` y `auth` siguen públicas).
- **Rate limiting en el gateway**: fixed window **20 req/15 s por IP** (`QueueLimit 0`); al superarlo responde **429** con `Retry-After: 15`.
- **Saga distribuida** con `MassTransit` + patrón **Outbox/Inbox** transaccional (sin pérdida ni duplicado de mensajes).
- **Resiliencia**: reintentos exponenciales y circuit breaker (`Microsoft.Extensions.Http.Resilience`/Polly).
- **Idempotencia**: header `Idempotency-Key` en `POST /orders` (retry seguro sin duplicar pedidos).
- **Migraciones de esquema automáticas** en el arranque de cada servicio (EF Core).
- **Observabilidad**: OpenTelemetry → collector → Jaeger (trazas) + Seq (logs); Prometheus + Grafana con dashboards y alertas provisionadas (métricas `/metrics` y scrape de RabbitMQ) — **también desplegada en el stack de producción** con alertas (webhook configurable por entorno).
- **Docker de producción**: multi-stage, compose con Postgres/RabbitMQ propios, observabilidad incluida (collector/Jaeger/Seq/Prometheus/Grafana) y solo `:80` expuesto al exterior.
- **CI/CD**: GitHub Actions (build+test en cada push) y deploy por tags `v*`.
- **94 tests** de backend (unit + integración con Testcontainers); el **SPA** suma **12 tests propios** (Vitest + React Testing Library, fuera del monorepo).

## Arquitectura

> **Documento completo**: [docs/architecture.md](docs/architecture.md) — componentes, enrutado YARP, mensajería y contratos, saga paso a paso, outbox/inbox, persistencia, observabilidad y despliegue.

```mermaid
flowchart TB
    C["Cliente (REST + OIDC token)"] -->|"/api/v1/*"| GW["ApiGateway (YARP)<br/>dev :5080 · prod :80"]
    GW --> AUTH["Auth.API :5100<br/>OIDC provider"]
    GW --> CAT["Catalog.API :5038"]
    GW --> ORD["Orders.API :5248"]
    GW --> INV["Inventory.API :5208"]

    ORD -. "GetBook (HTTP)" .-> CAT

    ORD -->|"OrderCreated (outbox)"| RQ[(RabbitMQ)]
    RQ --> SAGA["OrderSaga.Worker<br/>state machine + pago simulado"]
    RQ --> ORD
    RQ --> INV
    SAGA --> RQ

    CAT --> PGC[("catalog_db")]
    ORD --> PGO[("orders_db")]
    INV --> PGI[("inventory_db")]
    SAGA --> PGS[("order_saga_db")]
    AUTH --> PGA[("auth_db")]

    subgraph Observabilidad
        J["Jaeger :16686 (OTLP)"]
        P["Prometheus :9090"]
        G["Grafana :3000"]
    end
```

**Flujo típico**: el cliente obtiene un token con su flujo OIDC (Authorization Code + PKCE desde el SPA o *password grant* desde la CLI) → crea un pedido (Orders valida contra Catalog con el snapshot de precio) → el evento viaja por el outbox a RabbitMQ → la saga coordina pago (simulado) → Inventory reserva y descuenta stock → el pedido pasa a `Shipped`.

### Frontend (SPA)

La interfaz web (React) **vive fuera del monorepo** (carpeta local del autor `~/Desktop/BookStoreWeb`) y se conecta al backend **solo a través del gateway**:

- **Arranque**: `npm run dev` en `http://localhost:5173`; el gateway habilita CORS para ese origen (política `Frontend`).
- **Login**: `customer/customer123` (rol `customer`) o `admin/admin123` (rol `admin`), con el flujo OIDC de Authorization Code + PKCE (cliente `web-spa`) contra `http://localhost:5080`.
- **Configuración**: se define por entorno con `VITE_GATEWAY_URL` y `VITE_OIDC_CLIENT_ID` (`.env`/`.env.production`; plantilla en `.env.example`) — **configuración, no secretos**; la seguridad del flujo reside en Authorization Code + PKCE (cliente público `web-spa`).
- **Carrito y paginación**: el carrito persiste en `localStorage` (clave `bookstore.cart`) y el checkout crea el pedido con `Idempotency-Key`; la home busca con debounce y los listados de libros/pedidos pagan con el componente `Pagination`.
- **Seguimiento en vivo**: «Mis pedidos» enlaza al detalle (`/orders/:id`), con timeline (`OrderTracking`: `Pending → Paid → Shipped → Delivered`) e indicador pulsante de «actualización en vivo»; `useOrder`/`useMyOrders` hacen *polling* de `GET /api/v1/orders/{id}` cada 2 s mientras el pedido no esté en estado final, y el badge usa los **estados reales de Orders** (`Pending/Paid/Shipped/Delivered/Cancelled` — la saga termina en `Shipped`, nunca emite `Delivered`). Tests del SPA: `npm test` (**12 tests**, Vitest + React Testing Library).
- **API**: todas las llamadas pasan por el gateway `http://localhost:5080` (`/api/v1/*` y endpoints OIDC `/connect/*`, `/.well-known/*`).

## Requisitos previos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- PostgreSQL 17 y RabbitMQ 4 (recomendado: vía Docker, ver Quickstart)
- Puertos libres (tabla abajo)

| Puerto | Uso |
|---|---|
| 5038 / 5248 / 5208 / 5100 / 5080 | Servicios (dev) |
| 5432 / 5672 / 15692 | Postgres / RabbitMQ (+ métricas Prometheus) |
| 4317 / 4318 / 16686 / 9090 / 3000 / 5341 | Collector OTLP (gRPC / HTTP) / Jaeger / Prometheus / Grafana / Seq |

## Quickstart (desarrollo)

1. **Clonar** el repositorio:
   ```bash
   git clone https://github.com/richcrd/bookstore-microservices.git
   cd bookstore-microservices
   ```

2. **Levantar la infraestructura** (Postgres + RabbitMQ):
   ```bash
   docker run -d --name bookstore-postgres \
     -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres -p 5432:5432 postgres:17-alpine
   docker run -d --name bookstore-rabbitmq -p 5672:5672 -p 15672:15672 -p 15692:15692 rabbitmq:4-management
   ```

3. **Compilar**:
   ```bash
   dotnet build BookStore.slnx
   ```

4. **Arrancar los servicios** (cada uno en su terminal; las migraciones EF se aplican solas al iniciar). Es **obligatorio** `ASPNETCORE_ENVIRONMENT=Development` para que se carguen las credenciales de desarrollo (`appsettings.Development.json`): sin ella el entorno por defecto es `Production` y los placeholders `CHANGE_ME` de `appsettings.json` hacen fallar el arranque a propósito (fail-fast).
   ```bash
   ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5100 dotnet run --project src/Services/Auth/Auth.API
   ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5038 dotnet run --project src/Services/Catalog/Catalog.API
   ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5248 dotnet run --project src/Services/Orders/Orders.API
   ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5208 dotnet run --project src/Services/Inventory/Inventory.API
   ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Services/OrderSaga/OrderSaga.Worker
   ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5080 dotnet run --project src/ApiGateway/ApiGateway
   ```

5. **Swagger** en `http://localhost:<puerto>/swagger` por cada servicio (o por el gateway sin `/swagger`).

6. **(Opcional) Observabilidad**:
   ```bash
   docker compose -f docker/docker-compose.observability.yml up -d
   ```

## Configuración

Los servicios usan la **precedencia estándar de ASP.NET Core** — variables de entorno (`Config__Clave`) > `appsettings.{Environment}.json` > `appsettings.json` > defaults de código. Los secretos siguen la **Fase 21 (patrón 12-factor)**: `appsettings.json` **no contiene credenciales** (placeholders `CHANGE_ME` con fail-fast), las credenciales de desarrollo viven en `appsettings.Development.json` y en QA/prod los secretos llegan por **variables de entorno** (`.env`/CD) sin tocar el compose. La tabla muestra los valores de *desarrollo*:

| Variable | Default (desarrollo) | Descripción |
|---|---|---|
| `ConnectionStrings__AuthDb` | `Host=localhost;Database=auth_db;Username=postgres;Password=postgres` | BD de Auth (OpenIddict) |
| `ConnectionStrings__CatalogDb` | `Host=localhost;Database=catalog_db;Username=postgres;Password=postgres` | BD de catálogo |
| `ConnectionStrings__OrdersDb` | `Host=localhost;Database=orders_db;...` | BD de órdenes |
| `ConnectionStrings__InventoryDb` | `Host=localhost;Database=inventory_db;...` | BD de stock |
| `ConnectionStrings__OrderSagaDb` | `Host=localhost;Database=order_saga_db;...` | BD de la saga |
| `RabbitMQ__Host` | `rabbitmq://localhost` | Broker de mensajería (en prod el compose inyecta `rabbitmq://${RABBITMQ_USER}:${RABBITMQ_PASSWORD}@rabbitmq`) |
| `CatalogApi__BaseAddress` | `http://localhost:5038` | HTTP a Catalog usado por Orders |
| `OpenIddict__Issuer` | `http://localhost:5080` | Issuer público del proveedor OIDC; **los servicios y el gateway** lo usan para descubrir claves y validar el `iss` (en dev es el Host del gateway; en prod default `http://auth:5100` resoluble en la red interna; para dominios reales sobreescribe con `BOOKSTORE_PUBLIC_ISSUER`) |
| `OpenIddict__SpaClientId`/`SpaRedirectUri` | `web-spa` / `http://localhost:5173/callback` | Cliente público SPA (Authorization Code + PKCE) sembrado al arrancar |
| `OpenIddict__CliClientId`/`CliClientSecret` | `cli` / `cli-dev-secret` *(dev)* | Cliente confidencial para la CLI (*password*/*refresh* grant); en prod `OpenIddict__CliClientSecret` se inyecta desde `CLI_CLIENT_SECRET` |
| `OpenTelemetry__Endpoint` | `http://localhost:4318` | Endpoint OTLP HTTP/protobuf (trazas, métricas y logs) hacia el collector; lo sobreescribe la env `OTEL_EXPORTER_OTLP_ENDPOINT` (la inyecta Aspire) |
| `ReverseProxy__Clusters__<name>__Destinations__<dest>__Address` | `http://localhost:<puerto>` | Destinos YARP por servicio (sobrescritos en prod) |

### Secretos por entorno

| Entorno | Fuente de secretos | Cómo se inyecta |
|---|---|---|
| **dev** (dotnet run / tests) | `appsettings.Development.json`: connection strings `Password=postgres`, usuarios demo, `CliClientSecret: cli-dev-secret` (opcional: *user-secrets* para overrides locales) | `ASPNETCORE_ENVIRONMENT=Development` — solo así se carga el archivo (el entorno por defecto es `Production`) |
| **qa / prod (compose manual)** | `docker/.env` (plantilla versionable `docker/.env.prod.example`) | `cp docker/.env.prod.example docker/.env` y rellenar; el compose interpola `${VAR}` y **falla al arrancar si falta una variable obligatoria** (fail-fast) |
| **qa / prod (CD)** | GitHub Secrets | `cd.yml` inyecta los 6 secretos por `env` + `envs:` del `appleboy/ssh-action`; el compose los interpola desde la sesión remota, sin `.env` en el servidor |

> ⚠️ **`appsettings.json` no contiene secretos**: los placeholders `CHANGE_ME` de `ConnectionStrings.*` y `OpenIddict__CliClientSecret` son deliberados (fail-fast si el entorno no los sobreescribe) y **jamás** deben rellenarse con credenciales reales. Las credenciales demo (`admin/admin123`, `customer/customer123`) son **solo desarrollo**; en prod no hay usuarios por defecto (se inyectan opcionalmente por env `AuthUsers__Users__N__*`).

## Uso / API

La documentación completa de cada contrato está en el **Swagger** de cada servicio. Endpoints principales tras el gateway:

| Método | Ruta | Servicio |
|---|---|---|
| POST | `/connect/token` · `/connect/authorize` · `/connect/logout` | Auth (endpoints OIDC) |
| GET | `/.well-known/openid-configuration` · `/.well-known/jwks` | Auth (discovery) |
| GET/POST | `/api/v1/books`, `/api/v1/categories` | Catalog |
| GET/POST/PATCH | `/api/v1/orders/...` | Orders |
| GET/POST | `/api/v1/stock-items/...` | Inventory |

Las rutas `orders` y `stock-items` exigen **token Bearer** (el gateway valida en el borde y responde **401** sin token); `books`, `categories`, `/connect/*` y `/.well-known/*` son públicas. El gateway aplica además **rate limiting** de 20 peticiones/15 s por IP (429 con `Retry-After: 15`).

**Credenciales demo (solo desarrollo)**: `admin/admin123` (rol `admin`) y `customer/customer123` (rol `customer`) se seedan desde `appsettings.Development.json`; en QA/prod **no existen usuarios por defecto** (ver [Secretos por entorno](#secretos-por-entorno)).

```bash
# Discovery OIDC a través del gateway
curl -s http://localhost:5080/.well-known/openid-configuration

# Token con el cliente confidencial "cli" (password grant)
TOKEN=$(curl -s -X POST http://localhost:5080/connect/token \
  -d "grant_type=password&client_id=cli&client_secret=cli-dev-secret&username=customer&password=customer123&scope=openid profile roles offline_access" \
  | jq -r .access_token)

# Crear pedido (idempotente: misma Idempotency-Key → mismo pedido)
curl -s -X POST http://localhost:5080/api/v1/orders \
  -H "Authorization: Bearer $TOKEN" \
  -H "Idempotency-Key: $(uuidgen)" \
  -H "Content-Type: application/json" \
  -d '{"customerId":"22222222-2222-2222-2222-222222222222",
       "items":[{"bookId":"<bookId>","quantity":1}]}'
```

## Estructura del proyecto

```
src/
├── ApiGateway/                        # Punto único de entrada (YARP + rutas)
├── BuildingBlocks/SharedKernel/       # OpenIddict (validación), telemetría y utilidades compartidas
└── Services/
    ├── Auth/Auth.API/                 # Proveedor OpenID Connect (login, tokens y discovery)
    ├── Catalog/                       # Catálogo: API + Application + Domain + Infrastructure
    ├── Orders/                        # Órdenes: API + Application + Domain + Infrastructure
    ├── Inventory/                     # Stock: API + Application + Domain + Infrastructure
    └── OrderSaga/OrderSaga.Worker/    # Saga state machine + pago simulado
tests/                                 # Tests unit y de integración por servicio
docker/                                # Dockerfiles + compose (dev, observabilidad, prod)
.github/workflows/                     # CI (build+test) y CD (deploy por tags)
```

Cada servicio sigue **Clean Architecture** (Dominio → Application → Infrastructure → API) con su propio DbContext y migraciones EF.

## Testing

```bash
dotnet test BookStore.slnx
```

- **Unit**: domain + application de cada servicio.
- **Integración**: `WebApplicationFactory` + **Testcontainers** (Postgres efímero) — requiere Docker en el runner.

## Deploy / CI-CD

- **CI** (`.github/workflows/ci.yml`): en cada push/PR a `main` → `dotnet restore/build/test`. Estado: [![CI](https://github.com/richcrd/bookstore-microservices/actions/workflows/ci.yml/badge.svg)](https://github.com/richcrd/bookstore-microservices/actions/workflows/ci.yml)
- **CD** (`.github/workflows/cd.yml`): al crear un tag `v*` → SSH al servidor → `docker compose build` + `up -d` del stack de producción; inyecta los **6 secretos de entorno** (`POSTGRES_PASSWORD`, `RABBITMQ_USER`, `RABBITMQ_PASSWORD`, `CLI_CLIENT_SECRET`, `GRAFANA_ADMIN_PASSWORD` y `GRAFANA_ALERT_WEBHOOK_URL`) por `env` + `envs:` del `appleboy/ssh-action`, de modo que el compose los interpole en la sesión remota **sin `.env` en el servidor**.

Necesita secrets en el repo: `SERVER_HOST`, `SERVER_USER`, `SSH_PRIVATE_KEY`, `POSTGRES_PASSWORD`, `RABBITMQ_USER`, `RABBITMQ_PASSWORD`, `CLI_CLIENT_SECRET`, `GRAFANA_ADMIN_PASSWORD`, `GRAFANA_ALERT_WEBHOOK_URL` y la variable `DEPLOY_DIR`.

**Stack de producción** (`docker/docker-compose.prod.yml`): **13 contenedores** — Postgres + RabbitMQ propios, los 6 proyectos ejecutables (5 servicios + gateway) y **5 de observabilidad** (otel-collector, Jaeger, Seq, Prometheus y Grafana) — con el gateway publicado en **`:80`** y las UIs de observabilidad (`16686`, `5341`, `9090`, `3000`, `4317`, `4318`) publicadas **solo para inspección**; en servidores reales el firewall debe mantener únicamente `:80` abierto.

Para levantar el stack **fuera del CD** (p. ej. QA local): `cp docker/.env.prod.example docker/.env`, rellena los valores obligatorios y `docker compose -f docker/docker-compose.prod.yml up -d --build`. ⚠️ **Rotar `POSTGRES_PASSWORD` en un volumen existente exige `docker compose -f docker/docker-compose.prod.yml down -v` (destruye los datos del volumen) u `ALTER USER` en la BD**: la variable `POSTGRES_PASSWORD` de Postgres solo aplica en el primer `init` del volumen.

Historial completo de **fases construidas** (qué se añadió, qué no y por qué) en [docs/phases.md](docs/phases.md).

## Observabilidad

Los servicios emiten **trazas + métricas + logs** por OTLP **HTTP/protobuf** al **collector** apuntando al **path por señal** (`/v1/traces`, `/v1/metrics`, `/v1/logs`): configura el endpoint base **sin path** (dev `http://localhost:4318`; prod `http://otel-collector:4318`, nombre de servicio del compose) — el SDK añade `/v1/{signal}`. El collector enruta las trazas a Jaeger y los logs a Seq. En paralelo, Prometheus scrapea `/metrics` de los servicios y el endpoint de métricas de RabbitMQ (`:15692`), y Grafana sirve dashboards y alertas provisionadas con webhook configurable por entorno.

| Herramienta | URL | Qué ver |
|---|---|---|
| otel-collector | `:4317` (gRPC) / `:4318` (OTLP HTTP) | Recepción de telemetría (trazas, métricas y logs) y enrutado a Jaeger/Seq. |
| Jaeger | `http://localhost:16686` | Trazas distribuidas end-to-end (una orden completa). |
| Seq | `http://localhost:5341` | Logs agregados (ingest OTLP vía collector; auth deshabilitada en dev). |
| Prometheus | `http://localhost:9090` | Métricas `/metrics` (scrape 10s) de los servicios + **RabbitMQ `:15692`** (`Status → Targets`). |
| Grafana | `http://localhost:3000` (`admin/admin`) | Dashboards provisionados `BookStore · API` y `BookStore · RabbitMQ` + alertas (latencia p95 > 1s, ratio 5xx > 5%, colas RabbitMQ > 200) con contact point webhook `${GRAFANA_ALERT_WEBHOOK_URL}`. |

El contenedor dev de RabbitMQ debe exponer el puerto `15692` (métricas Prometheus, ver comando `docker run` del Quickstart). Ejemplo de query Prometheus: `rate(http_server_request_duration_seconds_count[5m])` o filtrada por servicio con `{service_name="Orders.API"}`.

**En producción** (Fase 20) la observabilidad vive dentro del propio stack (`docker-compose.prod.yml`): otel-collector, Jaeger, Seq, Prometheus y Grafana se despliegan junto a los servicios, con `prometheus.prod.yml` scrapeando por **nombre de contenedor** (`apigateway:5080`, `auth:5100`, `catalog:5038`, `orders:5248`, `inventory:5208`) + RabbitMQ (`rabbitmq:15692`, `/metrics/per-object`), y el contact point de alertas resolviéndose desde `GRAFANA_ALERT_WEBHOOK_URL` (default placeholder `http://host.docker.internal:3001/hooks/none`; el CD lo propaga como secret) y el admin de Grafana desde `GRAFANA_ADMIN_PASSWORD` (**obligatoria desde la Fase 21**, sin default en prod; en dev el stack observability usa `admin/admin`). Las UIs (`16686`, `5341`, `9090`, `3000`, `4317`, `4318`) se publican en el compose **solo para inspección**: el gateway sigue siendo el único puerto público `:80` y en servidores reales el firewall debe mantener solo `:80` abierto. En desarrollo se sigue usando `docker compose -f docker/docker-compose.observability.yml up -d`.

## Aspire (AppHost)

Alternativa al arranque de los 6 `dotnet run` (no a la vez): el proyecto `src/AppHost` (.NET Aspire) orquesta **todos los servicios** con sus puertos clásicos reutilizando **la infra dev existente** — los contenedores `bookstore-postgres` y `bookstore-rabbitmq` (no gestiona contenedores, solo inyecta sus connection strings por entorno):

```bash
dotnet run --project src/AppHost
```

- **Dashboard** en `https://localhost:17017` (HTTPS): recursos, logs, trazas y métricas; inyecta `OTEL_EXPORTER_OTLP_ENDPOINT` a los servicios.
- Fija los puertos HTTP clásicos (gateway `5080`, auth `5100`, catalog `5038`, orders `5248`, inventory `5208`, saga worker) e inyecta `ConnectionStrings__CatalogDb/OrdersDb/InventoryDb/OrderSagaDb` → `Host=localhost;Port=5432;...` y `RabbitMQ__Host` → `rabbitmq://localhost:5672`.
- Requiere la plantilla: `dotnet new install Aspire.ProjectTemplates` (el workload `aspire` del SDK no está instalado porque exige sudo).

## Changelog

El historial de cambios sigue [Keep a Changelog](CHANGELOG.md) + Conventional Commits y el proyecto respeta [SemVer](https://semver.org/lang/es/).

## Contributing

Ver [CONTRIBUTING.md](CONTRIBUTING.md). Regla básica: **Conventional Commits** + `feat:`/`fix:`/`chore:` y descripción clara en cada PR; los cambios de infraestructura y contratos requieren revisión explícita.

## Licencia

Distribuido bajo la [licencia MIT](LICENSE), © 2026 Richard Rodriguez.

La seguridad y forma de reportar vulnerabilidades se documentan en [SECURITY.md](SECURITY.md); el historial de cambios en [CHANGELOG.md](CHANGELOG.md).