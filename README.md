# bookstore-microservices

**Microservicios de una librería online en .NET 10 con Clean Architecture**: un sistema completo y desplegable compuesto por 5 APIs + una saga orquestada por mensajes, con autenticación JWT, resiliencia, observabilidad y pipeline CI/CD.

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
- [Fases de desarrollo](docs/phases.md) · [Arquitectura de detalle](docs/architecture.md)
- [Changelog](#changelog)
- [Contributing](#contributing)
- [Licencia](#licencia)

## Features

- **Un solo punto de entrada**: gateway YARP que enruta `/api/v1/*` a los servicios internos (y reescribe destinos en prod).
- **Autenticación JWT** centralizada (login en `Auth.API`, validación por firma en el resto).
- **Saga distribuida** con `MassTransit` + patrón **Outbox/Inbox** transaccional (sin pérdida ni duplicado de mensajes).
- **Resiliencia**: reintentos exponenciales y circuit breaker (`Microsoft.Extensions.Http.Resilience`/Polly).
- **Idempotencia**: header `Idempotency-Key` en `POST /orders` (retry seguro sin duplicar pedidos).
- **Migraciones de esquema automáticas** en el arranque de cada servicio (EF Core).
- **Observabilidad**: OpenTelemetry → Jaeger (trazas), Prometheus + Grafana (métricas `/metrics`).
- **Docker de producción**: multi-stage, compose con Postgres/RabbitMQ propios y solo `:80` expuesto.
- **CI/CD**: GitHub Actions (build+test en cada push) y deploy por tags `v*`.
- **92 tests** (unit + integración con Testcontainers).

## Arquitectura

> **Documento completo**: [docs/architecture.md](docs/architecture.md) — componentes, enrutado YARP, mensajería y contratos, saga paso a paso, outbox/inbox, persistencia, observabilidad y despliegue.

```mermaid
flowchart TB
    C["Cliente (REST + JWT)"] -->|"/api/v1/*"| GW["ApiGateway (YARP)<br/>dev :5080 · prod :80"]
    GW --> AUTH["Auth.API :5100"]
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

    subgraph Observabilidad
        J["Jaeger :16686 (OTLP)"]
        P["Prometheus :9090"]
        G["Grafana :3000"]
    end
```

**Flujo típico**: el cliente pide un token → crea un pedido (Orders valida contra Catalog con el snapshot de precio) → el evento viaja por el outbox a RabbitMQ → la saga coordina pago (simulado) → Inventory reserva y descuenta stock → el pedido pasa a `Shipped`.

## Requisitos previos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- PostgreSQL 17 y RabbitMQ 4 (recomendado: vía Docker, ver Quickstart)
- Puertos libres (tabla abajo)

| Puerto | Uso |
|---|---|
| 5038 / 5248 / 5208 / 5100 / 5080 | Servicios (dev) |
| 5432 / 5672 | Postgres / RabbitMQ |
| 16686 / 9090 / 3000 | Jaeger / Prometheus / Grafana |

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
   docker run -d --name bookstore-rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:4-management
   ```

3. **Compilar**:
   ```bash
   dotnet build BookStore.slnx
   ```

4. **Arrancar los servicios** (cada uno en su terminal; las migraciones EF se aplican solas al iniciar):
   ```bash
   ASPNETCORE_URLS=http://localhost:5100 dotnet run --project src/Services/Auth/Auth.API
   ASPNETCORE_URLS=http://localhost:5038 dotnet run --project src/Services/Catalog/Catalog.API
   ASPNETCORE_URLS=http://localhost:5248 dotnet run --project src/Services/Orders/Orders.API
   ASPNETCORE_URLS=http://localhost:5208 dotnet run --project src/Services/Inventory/Inventory.API
   dotnet run --project src/Services/OrderSaga/OrderSaga.Worker
   ASPNETCORE_URLS=http://localhost:5080 dotnet run --project src/ApiGateway/ApiGateway
   ```

5. **Swagger** en `http://localhost:<puerto>/swagger` por cada servicio (o por el gateway sin `/swagger`).

6. **(Opcional) Observabilidad**:
   ```bash
   docker compose -f docker/docker-compose.observability.yml up -d
   ```

## Configuración

| Variable | Default | Descripción |
|---|---|---|
| `ConnectionStrings__CatalogDb` | `Host=localhost;Database=catalog_db;Username=postgres;Password=postgres` | BD de catálogo |
| `ConnectionStrings__OrdersDb` | `Host=localhost;Database=orders_db;...` | BD de órdenes |
| `ConnectionStrings__InventoryDb` | `Host=localhost;Database=inventory_db;...` | BD de stock |
| `ConnectionStrings__OrderSagaDb` | `Host=localhost;Database=order_saga_db;...` | BD de la saga |
| `RabbitMQ__Host` | `rabbitmq://localhost` | Broker de mensajería (en prod `rabbitmq://rabbitmq`) |
| `CatalogApi__BaseAddress` | `http://localhost:5038` | HTTP a Catalog usado por Orders |
| `Jwt__Issuer` | `BookstoreAuth` | Emisor del token |
| `Jwt__Audience` | `BookstoreClient` | Audiencia del token |
| `Jwt__SigningKey` | `(dev)` | Clave HMAC-SHA256 de firma |
| `Jwt__AccessTokenLifetimeMinutes` | `30` | Vida del token |
| `OpenTelemetry__Endpoint` | `http://localhost:4317` | Endpoint OTLP (Jaeger) |
| `ReverseProxy__Clusters__<name>__Destinations__<dest>__Address` | `http://localhost:<puerto>` | Destinos YARP por servicio (sobrescritos en prod) |

> ⚠️ Las claves (`Jwt__SigningKey` y contraseñas de BD) están en `appsettings.json` con valores de **desarrollo**. Para producción deben venir de *secrets* (GitHub Secrets / Docker secrets / Vault).

## Uso / API

La documentación completa de cada contrato está en el **Swagger** de cada servicio. Endpoints principales tras el gateway:

| Método | Ruta | Servicio |
|---|---|---|
| POST | `/api/v1/auth/token` | Auth |
| GET/POST | `/api/v1/books`, `/api/v1/categories` | Catalog |
| GET/POST/PATCH | `/api/v1/orders/...` | Orders |
| GET/POST | `/api/v1/stock-items/...` | Inventory |

**Credenciales demo**: `admin/admin123` (rol `admin`) y `customer/customer123` (rol `customer`).

```bash
# Token
TOKEN=$(curl -s -X POST http://localhost:5080/api/v1/auth/token \
  -H "Content-Type: application/json" \
  -d '{"username":"customer","password":"customer123"}' | jq -r .token)

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
├── BuildingBlocks/SharedKernel/       # JWT, telemetría OpenTelemetry y utilidades compartidas
└── Services/
    ├── Auth/Auth.API/                 # Login y emisión de JWT
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
- **CD** (`.github/workflows/cd.yml`): al crear un tag `v*` → SSH al servidor → `docker compose build` + `up -d` del stack de producción.

Necesita secrets en el repo: `SERVER_HOST`, `SERVER_USER`, `SSH_PRIVATE_KEY` y la variable `DEPLOY_DIR`.

**Stack de producción** (`docker/docker-compose.prod.yml`): 9 contenedores (Postgres + RabbitMQ propios, 6 servicios y el gateway) con el gateway publicado en **`:80`**.

Historial completo de **fases construidas** (qué se añadió, qué no y por qué) en [docs/phases.md](docs/phases.md).

## Observabilidad

| Herramienta | URL | Qué ver |
|---|---|---|
| Jaeger | `http://localhost:16686` | Trazas distribuidas end-to-end (una orden completa). |
| Prometheus | `http://localhost:9090` | Métricas `/metrics` scrapeadas cada 10s (+ `Status → Targets`). |
| Grafana | `http://localhost:3000` (`admin/admin`) | Dashboards sobre el datasource Prometheus provisionado. |

Ejemplo de query Prometheus: `rate(http_server_request_duration_seconds_count[5m])` o filtrada por servicio con `{service_name="Orders.API"}`.

## Changelog

El historial de cambios sigue [Keep a Changelog](CHANGELOG.md) + Conventional Commits y el proyecto respeta [SemVer](https://semver.org/lang/es/).

## Contributing

Ver [CONTRIBUTING.md](CONTRIBUTING.md). Regla básica: **Conventional Commits** + `feat:`/`fix:`/`chore:` y descripción clara en cada PR; los cambios de infraestructura y contratos requieren revisión explícita.

## Licencia

Distribuido bajo la [licencia MIT](LICENSE), © 2026 Richard Rodriguez.

La seguridad y forma de reportar vulnerabilidades se documentan en [SECURITY.md](SECURITY.md); el historial de cambios en [CHANGELOG.md](CHANGELOG.md).