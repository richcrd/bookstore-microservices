# Changelog

Todos los cambios notables de **bookstore-microservices** se documentan en este archivo.

El formato sigue [Keep a Changelog](https://keepachangelog.com/es/1.1.0/) y el proyecto se adhiere a [Semantic Versioning](https://semver.org/lang/es/). Cada entrada relaciona su tipo con los [Conventional Commits](CONTRIBUTING.md#conventional-commits) y referencias de código/PR.

## [Unreleased]

### Added

- Autenticación **JWT** con roles (`admin`/`customer`): login en `POST /api/v1/auth/token` (Auth.API).
- **API Gateway YARP** como punto único de entrada (`:5080` dev, `:80` prod) con rutas `/api/v1/*` hacia catalog, orders, inventory y auth.
- **Orden Saga orquestada** con `MassTransit`: state machine (pendiente → pago → reserva → `Shipped`), con **outbox/inbox transaccional** (sin pérdida ni duplicados).
- Consumidores RabbitMQ: `OrderCreatedConsumer` y `OrderStatusChangedConsumer` en Inventory; endpoint `change-order-status` en Orders.
- **Idempotencia** en la creación de pedidos vía header `Idempotency-Key` (GUID): `201` la primera vez, `200` con el mismo pedido en reintentos; clave generada automáticamente si no se envía.
- **Migraciones EF automáticas** al arrancar en Catalog, Orders, Inventory y OrderSaga.
- Script `docker/postgres/init/001-create-databases.sql` que crea `catalog_db`, `orders_db`, `inventory_db` y `order_saga_db` en entornos limpios.
- **Observabilidad**: OpenTelemetry → Jaeger (OTLP), Prometheus + Grafana provisionado, y `docker-compose.observability.yml`.
- **Docker de producción** multi-stage (`docker/docker-compose.prod.yml`, 9 contenedores, gateway en `:80`).
- **CI/CD** en GitHub Actions: `ci.yml` (build + test en cada push) y `cd.yml` (deploy por tags `v*`).
- Suite de **92 tests** (unit + integración con Testcontainers).

### Changed

- Destinos del gateway configurables por entorno: `${ReverseProxy__Clusters__*__Destinations__*__Address}` para que los contenedores del stack de prod enruten por nombre de servicio y no por `localhost`.
- Los servicios con base de datos aplican `Migrate()` al arrancar en lugar de depender de dumps de BD.
- El login se expone como `POST /api/v1/auth/token` a través del gateway.

### Fixed

- **CI**: la opción `cache: true` de `actions/setup-dotnet` fallaba en ausencia de `packages.lock.json`; se sustituyó por `actions/setup-dotnet@v6` sin caché y se subió `actions/checkout` a `v7`.
- Error de enrutado interno del gateway en contenedores (apuntaba a `http://localhost:5038` en vez de al nombre del servicio).