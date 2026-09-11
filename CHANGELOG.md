# Changelog

Todos los cambios notables de **bookstore-microservices** se documentan en este archivo.

El formato sigue [Keep a Changelog](https://keepachangelog.com/es/1.1.0/) y el proyecto se adhiere a [Semantic Versioning](https://semver.org/lang/es/). Cada entrada relaciona su tipo con los [Conventional Commits](CONTRIBUTING.md#conventional-commits) y referencias de código/PR.

## [Unreleased]

### Added

- **Proveedor OpenID Connect / OAuth 2.0** (OpenIddict 7) en Auth.API: página de login en `/connect/authorize`, token endpoint `/connect/token` con *password*/*refresh* (cliente `cli`) y *Authorization Code + PKCE* (cliente `web-spa`), discovery en `/.well-known/*`.
- **Validación por discovery (RS256)** en Catalog/Orders/Inventory vía `OpenIddict.Validation` (SharedKernel `AddIdpAuthentication`): los servicios descargan claves y validan `iss` sin firma compartida hardcodeada.
- **API Gateway YARP** como punto único de entrada (`:5080` dev, `:80` prod) con rutas `/api/v1/*` hacia catalog, orders e inventory y endpoints OIDC (`/connect/*`, `/.well-known/*`) hacia auth.
- **Orden Saga orquestada** con `MassTransit`: state machine (pendiente → pago → reserva → `Shipped`), con **outbox/inbox transaccional** (sin pérdida ni duplicados).
- Consumidores RabbitMQ: `OrderCreatedConsumer` y `OrderStatusChangedConsumer` en Inventory; endpoint `change-order-status` en Orders.
- **Idempotencia** en la creación de pedidos vía header `Idempotency-Key` (GUID): `201` la primera vez, `200` con el mismo pedido en reintentos; clave generada automáticamente si no se envía.
- **Migraciones EF automáticas** al arrancar en Catalog, Orders, Inventory y OrderSaga.
- Script `docker/postgres/init/001-create-databases.sql` que crea `catalog_db`, `orders_db`, `inventory_db` y `order_saga_db` en entornos limpios.
- **Observabilidad**: OpenTelemetry → Jaeger (OTLP), Prometheus + Grafana provisionado, y `docker-compose.observability.yml`.
- **Docker de producción** multi-stage (`docker/docker-compose.prod.yml`, 9 contenedores, gateway en `:80`).
- **CI/CD** en GitHub Actions: `ci.yml` (build + test en cada push) y `cd.yml` (deploy por tags `v*`).
- Suite de **92 tests** (unit + integración con Testcontainers).
- **Frontend SPA** (fuera del monorepo, `~/Desktop/BookStoreWeb`): React 19 + Vite 8 + TypeScript 6, react-router 7, TanStack Query 5, axios, zod 4 y oidc-client-ts 3; consume el backend por el gateway (`:5080`) con Authorization Code + PKCE (cliente `web-spa`), login HTML en `/connect/authorize`, callback en `/callback`, renovación silenciosa (`automaticSilentRenew`) y logout con `signoutRedirect`.
- **Auth.API**: endpoint de logout `GET/POST /connect/logout` (`AuthorizationController.Logout()`): cierra la sesión de OpenIddict (`SignOutAsync`) y redirige a `post_logout_redirect_uri` (registrado `http://localhost:5173/` para el SPA) o `/`.
- **ApiGateway**: política CORS `Frontend` con `AllowedOrigins=["http://localhost:5173"]` y transform `RequestHeaderOriginalHost: true` en las rutas OIDC (`/connect/{**catch-all}`, `/.well-known/{**catch-all}`) para preservar el Host público `:5080` hacia Auth.

### Changed

- Destinos del gateway configurables por entorno: `${ReverseProxy__Clusters__*__Destinations__*__Address}` para que los contenedores del stack de prod enruten por nombre de servicio y no por `localhost`.
- Los servicios con base de datos aplican `Migrate()` al arrancar en lugar de depender de dumps de BD.
- El token se obtiene ahora por OIDC a través del gateway (`POST /connect/token`, password grant) en lugar de `POST /api/v1/auth/token`.
- El issuer público del proveedor es configurable (`OpenIddict__Issuer`; en prod default interno `http://auth:5100`; `BOOKSTORE_PUBLIC_ISSUER` lo sobreescribe para dominios reales).
- Auth.API pasa a usar su propia base `auth_db` (aplicaciones/scopes/autorizaciones OpenIddict); el compose de prod la conecta y añade `depends_on: postgres`.

### Fixed

- **CI**: la opción `cache: true` de `actions/setup-dotnet` fallaba en ausencia de `packages.lock.json`; se sustituyó por `actions/setup-dotnet@v6` sin caché y se subió `actions/checkout` a `v7`.
- Error de enrutado interno del gateway en contenedores (apuntaba a `http://localhost:5038` en vez de al nombre del servicio).