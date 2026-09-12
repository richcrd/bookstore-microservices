# Fases de desarrollo — bitácora del proyecto

> Historial profesional de las fases construidas, con el **qué se añadió**, el **qué no se añadió y por qué**, y la **verificación** de cada una. La referencia canónica de arquitectura está en [architecture.md](architecture.md); los cambios versionados en el [CHANGELOG](../CHANGELOG.md).

**Solución**: `BookStore.slnx` · **Target**: .NET 10 · **Enfoque**: Clean Architecture + microservicios + eventos.

## Resumen por fases

| Fase | Entregable principal | Verificación | Estado |
|---|---|---|---|
| 0. Inicialización | Estructura del monorepo | `git log` limpio | ✔ |
| 1. Catalog | API con Clean Architecture completa + tests | Swagger + tests unit | ✔ |
| 2. Orders | API de pedidos + snapshot de precio (HTTP→Catalog) | Tests unit + integración (Testcontainers) | ✔ |
| 3. Inventory | Modelo de stock con reservas y operaciones | Tests unit | ✔ |
| 4. Mensajería | RabbitMQ + MassTransit + outbox/inbox | Tests unit de bus + consumo real | ✔ |
| 5. Saga | Orquestador de pedidos con compensación | Flujo E2E de una orden | ✔ |
| 6. Gateway | Punto único de entrada (YARP) | Enrutado `/api/v1/*` | ✔ |
| 7. Seguridad | Autenticación y autorización JWT | Login + roles protegidos | ✔ |
| 8. Resiliencia | Reintentos + circuit breaker (HTTP) | Fallo transitorio tolerado | ✔ |
| 9. Observabilidad | Trazas (Jaeger) y métricas (Prometheus/Grafana) | Traza end-to-end de una orden | ✔ |
| 10. CI/CD + Docker | Stack prod (9 contenedores) + pipelines | E2E en prod + CI verde | ✔ |
| 11. Idempotencia | Retry-safe en creación de pedidos | 201/200 con misma key | ✔ |
| 12. Estabilización CI | Pipeline reproducible | CI verde en GitHub | ✔ |
| 13. Documentación | Repo enterprise (README, CONTRIBUTING, etc.) | Docs enlazadas | ✔ |
| 14. Seguridad OIDC | Proveedor OpenID Connect + validación por discovery | Login OIDC + 401/403 + suite verde | ✔ |
| 15. Frontend SPA | Frontend SPA (React + OIDC) | Login + pedido end-to-end vía gateway; build/tsc OK | ✔ |
| 16. Frontend SPA v2 | SPA v2: búsqueda, carrito y paginación | tsc + build OK; flujo añadir → carrito → pedido validado | ✔ |
| 17. Gateway: auth en el borde | OIDC en el edge + rate limiting testeado (deuda Fase 6) | 401 en orders/stock-items sin token; ráfaga 30 → 20 OK + 429; 94 tests | ✔ |
| 18. Frontend SPA v3 | Seguimiento del pedido en vivo (polling) + tests del frontend | npm test 12/12; badge con estados reales; E2E Pending → Shipped por polling | ✔ |
| 19. Observabilidad 2 + Aspire | Stack observability v2 (collector + Seq + dashboards/alertas) + .NET Aspire AppHost | Trazas en Jaeger, logs en Seq, dashboards/alertas en Grafana, AppHost levantando los 6 servicios | ✔ |

**Estado final**: 94 tests en verde · CI/CD operativo · stack prod validado · gateway con auth OIDC en el borde y rate limiting testeado · observabilidad v2 (logs en Seq, dashboards y alertas en Grafana, scrape de RabbitMQ) y orquestación opcional con .NET Aspire · frontend SPA v3 (React + OIDC) validado end-to-end contra el gateway (búsqueda, carrito persistente, paginación y seguimiento del pedido en vivo por polling), con 12 tests propios (Vitest).

---

## Fase 0 — Inicialización del repositorio

**Qué se añadió**: el esqueleto del monorepo (`.gitignore`, estructura `src/`, solución de trabajo) para iterar por fases con historial limpio.

**Qué no se añadió y por qué**: no se creó código de negocio todavía; se priorizó sentar la estructura y dejar el historial listo para un desarrollo incremental por fases.

**Verificación**: historial con commits atómicos y mensajes Conventional Commits.

---

## Fase 1 — Catalog: Clean Architecture completa

**Qué se añadió**:
- **Domain**: `Book`, `Author`, `StockAuthor`→`Publisher`, valores como `ISBN` y `Money` como *value objects* con invariantes.
- **Application**: casos de uso (commands/queries), DTOs, interfaces de repositorio.
- **Infrastructure**: EF Core + PostgreSQL, repositorios y configuración de entidades.
- **API**: controladores, middleware de errores, Swagger legible.
- **Testing**: unit per capa (reglas de dominio y lógica de aplicación).
- Ajuste: carga *perezosa* de la connection string (2ce25b9) para no levantar configuración innecesariamente en tests.

**Qué no se añadió y por qué**: ni autenticación ni mensajería — la primera fase impuso el estándar (Clean Architecture + tests) antes de la complejidad; el control de acceso se decidió diferir hasta la fase de seguridad (7) para no acoplar el contrato antes de estabilizarlo.

**Verificación**: endpoints servidos por Swagger; suite de tests unit en verde.

---

## Fase 2 — Orders: pedidos con snapshot de precio

**Qué se añadió**:
- **Domain**: `Order`, `OrderItem` y la máquina de estados `Pending → Paid → Shipped → Cancelled` con transiciones válidas inviolables.
- **Application**: `CreateOrderCommand`, validación de entrada, interfaces.
- **Infrastructure**: DbContext propio, repositorios, Unit of Work.
- **API**: contratos HTTP completos (JSON) y un **HttpClient síncrono hacia Catalog** (`GET /books/{id}`) que hace **snapshot del precio** en el momento de crear el pedido.
- **Testing**: unit (domain/application) + **integración** con `WebApplicationFactory` y **Testcontainers** (Postgres efímero).

**Qué no se añadió y por qué**:
- No se embebió el catálogo en Orders (se consulta), preservando el **ownership de datos por servicio**.
- La sincronización de stock con Inventory se dejó para la fase de mensajería (4): Orders no llama a Inventory por HTTP para evitar caminos síncronos largos y acoplamiento fuerte.

**Verificación**: tests unit + integración verdes; el precio devuelto queda fijo en el pedido aunque el catálogo cambie después.

---

## Fase 3 — Inventory: stock con reservas

**Qué se añadió**:
- **Domain**: `StockItem` con `QuantityOnHand`, `ReservedQuantity` y `Available`, con invariantes (no descontar más de lo disponible, no reservar cantidades negativas).
- **Operations de dominio**: *reserve* / *deduct reserved* / *release*.
- **Application + Infrastructure + API** + tests unit.

**Qué no se añadió y por qué**: **no se conectó todavía con Orders** por HTTP; la integración por eventos llegó en la fase 4. La decisión deliberada de modelar primero un dominio correcto de stock (semántica de reservas) evita diseñar el transporte sobre un modelo defectuoso.

**Verificación**: tests unit en verde sobre las invariantes de stock.

---

## Fase 4 — Mensajería: RabbitMQ + MassTransit

**Qué se añadió**:
- **Orders como productor**: al crear el pedido se publica `OrderCreatedMessage`; endpoint `change-order-status` para aplicar `Paid/Shipped/Cancelled` y publicar `OrderStatusChangedMessage`.
- **Inventory como consumidor**: `OrderCreatedConsumer` (reserva stock por ítem) y `OrderStatusChangedConsumer` (descuenta en `Shipped`, libera en `Cancelled`).
- **Patrón outbox/inbox transaccional (MassTransit)**: los mensajes se persisten en la misma transacción que el agregado y se entregan tras el commit; el inbox deduplica en los consumidores.
- Registro de SQL (logging de consultas) y **tests unit del bus** (contratos y consumidores).

**Qué no se añadió y por qué**:
- **No hay DLQ / políticas de retención**: se asume redelivery del broker; una dead-letter cola sería una mejora de gestión de errores futura.
- **No se introdujeron sagas aún**: primero aseguramos el transporte y las garantías de entrega; la orquestación llegó en la fase siguiente como evolución natural.

**Verificación**: creación de pedido → evento llega a Inventory → reserva aplicada; consumo real contra RabbitMQ en el entorno dev.

---

## Fase 5 — Saga del pedido (orquestación)

**Qué se añadió**: `OrderSaga.Worker` — máquina de estados con **correlación por `OrderId`** y **persistencia del estado** en `order_saga_db`:

```
OrderCreated → AwaitingPayment → (PaymentApproved) → ShipmentRequested → Completed
                                  └── retry ≤ 3 ──→ Cancelled (compensación)
```

- `RequestPaymentCommand`/`PaymentCompleted` por request/response hacia el **payment-gateway simulado** (éxito si `Total < 10.000`, 250 ms).
- Hasta **3 intentos de pago** y **compensación automática**: `Cancelled` → se libera el stock (via `OrderStatusChangedMessage`).

**Qué no se añadió y por qué**:
- **No se usó 2PC / transacciones distribuidas**: se eligió **saga con consistencia eventual** (escalable, sin locks distribuidos); la durabilidad corre a cargo del outbox.
- **No se usó choreografía**: se escogió **orquestación** con state machine para tener trazabilidad y control central del flujo.
- **No hay pago real ni notificación al cliente**: el payment-gateway es un stub intencional (mismo contrato que un proveedor real) y el seguimiento es futuro trabajo de frontend.

**Verificación**: flujo E2E completo de una orden hasta `Shipped`, con devolución de stock verificada en caso de cancelación.

---

## Fase 6 — API Gateway (YARP)

**Qué se añadió**: proyecto `ApiGateway` con **YARP**: 5 rutas `/api/v1/*` (`books`, `categories`, `orders`, `stock-items`, `auth`) hacia los clusters de servicios, dando un **punto único de entrada** con URL estable.

**Qué no se añadió y por qué**:
- **No se hizo rate limiting ni autenticación en el gateway**: la validación de tokens se mantiene en cada servicio (firma JWT compartida) y el límite de tráfico se dejó como deuda de producción — **cerrada en la Fase 17** (auth OIDC en el borde + rate limiting extraído y testeado).
- **No se configuró CORS**: pendiente intencionalmente hasta definir el cliente web (frontend).

**Verificación**: enrutado `:5080` (dev) y `:80` (prod) hacia los 4 servicios; `/health` operativo.

---

## Fase 7 — Seguridad: JWT

**Qué se añadió**: servicio **Auth** que emite tokens **JWT HMAC-SHA256** (`Jwt__Issuer`, `Jwt__Audience`, `Jwt__SigningKey`, caducidad 30 min) con roles `admin`/`customer`; validación por firma en el resto de servicios y **políticas de autorización** protegidas (p. ej. inventario restringido a `AdminOnly`).

**Qué no se añadió y por qué**:
- **No hay refresh tokens ni revocación**: se mitiga con caducidad corta; un IdP externo (Keycloak/IdentityServer) era excesivo para el curso.
- **Clave de firma única no rotada**: reconocido como deuda técnica de producción (ver [SECURITY](../SECURITY.md)).

**Verificación**: login `admin/admin123` y `customer/customer123` → token → acceso a endpoints según rol; 401/403 correctos.

---

## Fase 8 — Resiliencia

**Qué se añadió**: cliente HTTP resiliente (`Microsoft.Extensions.Http.Resilience`) para `Orders → Catalog`: **reintentos exponenciales** + **circuit breaker** (y timeout), de modo que fallos transitorios del catálogo no tumben la creación de pedidos.

**Qué no se añadió y por qué**: **no se implementó bulkhead/semáforo ni pruebas de caos**: los mecanismos de aislamiento de cargas y la inyección de fallos quedaron fuera del alcance del curso; retry+circuit son los esenciales para la integración HTTP actual.

**Verificación**: simulación de fallo en Catalog (servicio parado) → el pedido responde con degradación controlada y el circuito se reabre al recuperarse.

---

## Fase 9 — Observabilidad

**Qué se añadió**:
- **Trazas distribuidas** con OpenTelemetry (HTTP, MassTransit, Npgsql/EF) → **Jaeger** (OTLP).
- **Métricas** `/metrics` (Prometheus) → **Grafana** con datasource provisionado y scrape de los servicios dev.
- `docker-compose.observability.yml` (Jaeger, Prometheus, Grafana).

**Qué no se añadió y por qué**: **no se configuraron alertas ni dashboards custom en Grafana**, y tampoco agregación de logs (ELK/Seq); se priorizó la correlación de trazas y las métricas base. Las alertas son trabajo de producción pendiente.

**Verificación**: traza end-to-end en Jaeger (gateway → orders → bus → saga → inventory) y métricas visibles en Grafana.

---

## Fase 10 — CI/CD y Docker de producción

**Qué se añadió**:
- **Dockerfiles multi-stage** por servicio y `docker-compose.prod.yml`: **9 contenedores** (Postgres y RabbitMQ propios, 6 servicios y el gateway) en red interna, con **único puerto público `:80`** y destinos YARP inyectados por entorno (resolución por nombre de servicio).
- **Auto-migraciones EF** (`db.Database.Migrate()` al arrancar en los 4 servicios con BD) + script `docker/postgres/init/001-create-databases.sql` que crea las 4 bases en entornos limpios.
- **CI** (`ci.yml`): restore/build/test en cada push; **CD** (`cd.yml`): deploy por tag `v*` vía SSH + `docker compose up -d`.

**Qué no se añadió y por qué**:
- **No hay TLS/HTTPS** (sin certificado en el ámbito del curso) ni **secrets management** en el host (documentado como requisito de producción).
- **El CD no se ha disparado end-to-end**: requiere configurar los secrets (`SERVER_HOST`, `SERVER_USER`, `SSH_PRIVATE_KEY`) en un servidor real — listo para usarlo.

**Verificación**: `docker compose --project-name bookstore-prod up -d` → 9 contenedores `healthy`, E2E completo en prod (auth → catálogo → stock → pedido → `Shipped`, stock 5→4) y CI verde en GitHub.

---

## Fase 11 — Idempotencia en la creación de pedidos

**Qué se añadió**: header `Idempotency-Key` (GUID) en `POST /api/v1/orders`:
- 1.ª vez → `201`; reintento con la misma clave → `200` con el mismo pedido; sin clave → GUID interno.
- Columna `IdempotencyKey` con índice único + migración con **backfill** (`gen_random_uuid()`) para filas existentes.
- Tests unit + integración cubriendo los tres escenarios.

**Qué no se añadió y por qué**: la idempotencia solo se aplicó donde había riesgo real (creación); los cambios de estado y stock son de bajo impacto y quedan protegidos por el outbox/inbox. No se añadió TTL/limpieza de claves (mejora futura).

**Verificación**: doble POST con la misma key devuelve el mismo `OrderId`; suite completa 92/92.

---

## Fase 12 — Estabilización del CI

**Qué se añadió**: fix del pipeline que fallaba por `cache: true` en `actions/setup-dotnet` sin `packages.lock.json` → `setup-dotnet@v6` sin caché y `checkout@v7`. Resultado: **CI reproducible y verde** de forma estable.

**Qué no se añadió y por qué**: no se adoptó `packages.lock.json` ni la caché de NuGet para mantener el build simple; tampoco *test-reports* como artefactos (pendiente de decidir).

**Verificación**: sucesivas runs en GitHub Actions en verde tras la corrección.

---

## Fase 13 — Documentación enterprise

**Qué se añadió**: la base documental completa del repo:
- `README.md` canónico (badges, arquitectura, quickstart, configuración, uso/API, testing, CI/CD, observabilidad).
- `CONTRIBUTING.md`, `SECURITY.md`, `CHANGELOG.md`, `LICENSE` (MIT).
- `.github/`: plantillas de PR e issues; `docs/architecture.md` (arquitectura de detalle) y esta bitácora.
- Ignorado `/.claude/` (archivos locales del asistente, nunca en commits).

**Qué no se añadió y por qué**:
- **ADR** (`docs/adr/`) aún por redactar: se prefirió primero consolidar la arquitectura y luego fijar las decisiones formales.
- **Banner/demo del producto**: pendiente de capturas con el frontend futuro.

**Verificación**: docs coherentes y enlazadas entre sí; `git status` limpio tras el commit.

---

## Fase 14 — Seguridad: OpenID Connect (OpenIddict)

**Qué se añadió**: migración del JWT firmado por HMAC a un **proveedor OIDC estándar** con **OpenIddict 7** en `Auth.API`:

- Token endpoint (`/connect/token`) con **password** y **refresh** (cliente confidencial `cli`) y **Authorization Code + PKCE** (cliente público `web-spa`); página de login HTML en `/connect/authorize`.
- Persistencia de aplicaciones, scopes y autorizaciones en `auth_db` (OpenIddict EF Core), sembradas al arranque.
- **Validación por discovery (RS256)** en Catalog/Orders/Inventory vía `OpenIddict.Validation` (`AddIdpAuthentication` en SharedKernel): sin clave de firma compartida configurada; cada servicio descarga el documento y comprueba `iss`.
- **Gateway YARP** expone también `/.well-known/{**cat-catch-all}` y `/connect/{**catch-all}` (la antigua ruta `/api/v1/auth/*` se eliminó, ya sin controladores).
- Tests de integración migrados al esquema de test (`TestAuthHandler`): la suite no depende de la firma real.

**Qué no se añadió y por qué**:
- **No hay certificados de producción ni HTTPS**: OpenIddict usa `AddDevelopment*Certificates`; el transporte seguro y los certificados persistentes quedan como requisito del despliegue real (deuda documentada en architecture.md §16).
- **No hay revocación de tokens de acceso ni logout del frontend**: se mitiga con vida corta (30 min) y refresh tokens *reference*; la revocación por sesión es trabajo futuro con el frontend.
- **El SPA aún no existe**: el cliente `web-spa` está sembrado y listo (PKCE) para el frontend futuro.

**Verificación**: flujo Authorization Code + PKCE completo validado (login HTML → `code` → `access_token`); password grant por `cli` desde el gateway; discovery pública en `/.well-known/openid-configuration`; checkpoint sin token → **401**, admin → **201**, customer → **403**; suite **92/92** en verde.

---

## Fase 15 — Frontend SPA (React + OpenID Connect)

**Qué se añadió**: el **SPA de la librería**, que **vive fuera del monorepo** (carpeta local del autor `~/Desktop/BookStoreWeb`): este repositorio contiene solo el backend y el gateway; el frontend no forma parte del repo y consume todo a través de él.

- **Stack**: React 19, Vite 8, TypeScript 6, react-router 7, TanStack Query 5, axios, zod 4 y oidc-client-ts 3.
- **Arquitectura por capas** en `src/`: `app/` (main, router de rutas, providers, config), `domain/` (models + ports), `application/` (hooks React Query + AuthProvider), `infrastructure/` (httpClient axios con interceptor Bearer/401, oidc.ts, schemas zod, DI), `presentation/` (pages/components/guards/layout) y `shared/` (constantes, `formatMoney` y `customerId` determinista derivado de `sub`).
- **Flujo OIDC**: Authorization Code + **PKCE** con el cliente público `web-spa`; login HTML custom en `/connect/authorize`; captura del callback en `/callback`; renovación silenciosa (`automaticSilentRenew`, `silent_redirect_uri = /callback`); logout con `signoutRedirect` a `/connect/logout` con `post_logout_redirect_uri=http://localhost:5173/`.
- **Pantallas**: Inicio/catálogo (consume `GET /api/v1/books`), Nuevo pedido (cantidades por libro; header `Idempotency-Key` GUID; cliente derivado de `sub`), Mis pedidos (`GET /api/v1/orders`) y Stock (admin, `GET /api/v1/stock-items` y `POST` add).
- **Backend acompañante** (ya commiteado en este repo):
  - **Auth.API**: los endpoints OIDC (`connect/authorize`, `connect/token`, `connect/logout`) usan rutas **relativas** para que la *discovery* respete el Host de la petición; nuevo `Logout()` en `Controllers/AuthorizationController.cs` (GET/POST `~/connect/logout` → `SignOutAsync(OpenIddictServerAspNetCoreDefaults)` + redirect a `post_logout_redirect_uri` o `/`).
  - **ApiGateway**: rutas `/connect/{**catch-all}` y `/.well-known/{**catch-all}` con transform `RequestHeaderOriginalHost: true` (preserva el Host original `:5080` hacia Auth); política CORS `"Frontend"` con `AllowedOrigins=["http://localhost:5173"]`.
  - Issuer dev de Auth en `http://localhost:5080/` (los 4 servicios validan por discovery contra ese issuer).
  - Cliente `web-spa` (público, PKCE) sembrado en `auth_db` al arrancar, con `post_logout_redirect_uri` registrado.

**Qué no se añadió y por qué** (pasa a la **Fase 16**): búsqueda avanzada en el catálogo, carrito persistente (localStorage), paginación de la UI, tests del frontend y hosting del SPA. El SPA se mantiene fuera del monorepo por alcance (el repo sigue siendo el backend de referencia) y su despliegue queda desacoplado del stack Docker de producción.

**Verificación**: `tsc --noEmit` limpio y `vite build` OK; flujo completo validado a mano (login customer/admin, creación de pedido → saga → `Shipped`, logout con 302).

---

## Fase 16 — Frontend SPA v2 (búsqueda, carrito y paginación)

**Qué se añadió** (evolución del SPA de la Fase 15; el frontend sigue viviendo **fuera del monorepo** en `~/Desktop/BookStoreWeb`):

- **Configuración por entorno (`.env`)**: variables `VITE_GATEWAY_URL` y `VITE_OIDC_CLIENT_ID` en `.env` (dev), `.env.production` (prod) y `.env.example` (plantilla versionable), con tipos en `src/vite-env.d.ts` y consumo en `src/app/config.ts`. Son **configuración por entorno, NO secretos**: un SPA embute estas variables en el bundle en *build time* y el navegador siempre puede leerlas; nada sensible (signing keys, credenciales) viaja al frontend. La seguridad del flujo se apoya en **Authorization Code + PKCE** con el cliente público `web-spa` (*client_id* y URLs no son secretos por diseño de OAuth2).
- **Carrito persistente (localStorage)**: clave `bookstore.cart` (`src/shared/storageKeys.ts`); dominio `Cart`/`CartItem` (bookId, title, author, unitPrice, currency, quantity) y puerto `CartStoragePort { load, save, clear }` (`src/domain/models.ts` / `ports.ts`); implementación `CartStorageImpl` con localStorage directo (JSON + cast, sin guard defensivo); `CartContext.tsx` con `CartProvider` + hook `useCart()` (add, setQuantity, remove, clear, count, total, currency) y persistencia automática.
- **Nueva página de carrito (`CartPage.tsx`)**: +/−, quitar, vaciar, aviso de login si no hay sesión y checkout que crea el pedido con header `Idempotency-Key` (GUID) y **vacía el carrito al confirmar**. Es el nuevo flujo de creación de pedido (reemplaza a la antigua página «Nuevo pedido»); la ruta `/orders/new` redirige a `/cart` y la nav muestra «Carrito (n)» con el contador.
- **Búsqueda en catálogo**: input en la home con debounce (`useDeferredValue`) y reseteo de página al cambiar el término; el `queryKey` de TanStack Query combina `search + page`.
- **Paginación**: componente reutilizable `Pagination` (Anterior · Página X de Y · Siguiente) aplicado a libros (home) y pedidos («Mis pedidos»), con `keepPreviousData` de TanStack Query y `page` en el `queryKey`; el backend ya paginaba (`page`/`pageSize`) y la UI ahora lo explota.
- **Convención de código acordada** para el SPA y futuros cambios: código directo, sin escritura defensiva ni helpers de más (nada de `parse`/`normalize`/`safe`/`fallback`/`resolve` en nombres propios; el `.parse()` de zod es API de librería, no nombre propio).

**Qué no se añadió y por qué**:
- **Tests del frontend**: el SPA aún no tiene infraestructura de testing (ni runner ni tests); la validación de esta fase se hizo con `tsc --noEmit` + `vite build` y verificación manual del flujo. Pendiente junto con el kit de testing del frontend.
- **Code-splitting por rutas** (lazy loading de páginas): todas las rutas se mantienen en un solo bundle por simplicidad; con una aplicación aún pequeña la división de código aportaría poco hoy. Pendiente como optimización futura.

**Verificación**: `tsc` limpio y `vite build` OK; flujo completado a mano: búsqueda con debounce → añadir al carrito → recarga (el carrito persiste en `localStorage`) → ajuste de cantidades +/− → crear pedido con `Idempotency-Key` → carrito vaciado y pedido visible en «Mis pedidos» con paginación.

---

## Fase 17 — Gateway: auth OIDC en el borde + rate limiting testeado

**Qué se añadió** (cierra la deuda de la **Fase 6**):
- **Validación de tokens en el borde (edge)**: el ApiGateway valida ahora en la entrada con `AddIdpAuthentication(builder.Configuration)` de SharedKernel (`OpenIddict.Validation`) y una política `authenticated` (`RequireAuthenticatedUser`). Las rutas YARP `orders` (`/api/v1/orders/{**catch-all}`) e `inventory` (`/api/v1/stock-items/{**catch-all}`) exigen token; `catalog-books`, `catalog-categories`, `auth-connect` y `auth-wellknown` siguen públicas (preservando la semántica de cada servicio: Catalog lee con `[AllowAnonymous]`, Auth es el IdP).
- **Orden de middleware** en `Program.cs`: `UseServiceTelemetry → UseRateLimiter → UseCors → UseAuthentication → UseAuthorization → MapReverseProxy`.
- **Issuer del gateway**: `OpenIddict__Issuer: http://localhost:5080` en `appsettings.json` (dev) — el gateway proxya su propio `/.well-known` al auth, *bootstrap* sin bucle.
- **Rate limiting extraído y testeable**: el limiter global (fixed window **20 req/15 s por IP**, `QueueLimit 0`, **429** con `Retry-After: 15`) que ya existía en el gateway se movió a `src/ApiGateway/RateLimitPolicies.cs` (`CreateGlobalLimiter()`) sin cambiar su comportamiento.
- **Tests**: nuevo proyecto `tests/ApiGateway.UnitTests` (2 tests: rechazo al superar el límite y partición por IP). Suite total: 92 → **94 tests** en verde; build sin errores nuevos.
- **Compose prod**: el servicio `apigateway` recibe `OpenIddict__Issuer: ${BOOKSTORE_PUBLIC_ISSUER:-http://auth:5100}` y `OpenIddict__DisableTransportSecurityRequirement: "true"` (igual que los demás servicios con validación).

**Qué no se añadió y por qué**:
- **No se sustituyó la validación por discovery de los servicios**: el edge añade una primera barrera (401 temprano y política central única) pero Orders/Inventory/la saga siguen validando en origen; no se centralizó toda la autorización ni las políticas por rol en el gateway.
- **El rate limiter sigue siendo global y fijo por IP**: no hay políticas por ruta ni por usuario, ni cola (`QueueLimit 0` → rechazo directo); es un límite de abuso básico, no un plan de QoS por cliente (mejora futura).

**Verificación**: E2E manual en dev (gateway `:5080`): `GET /api/v1/orders` sin token → **401**; con token (cliente `cli`, *password grant*) → **200**; `/.well-known/openid-configuration` y `/api/v1/books` → **200** públicos; ráfaga de 30 GET → 20 OK y luego **429** con `Retry-After: 15`; suite **94/94** en verde.

---

## Fase 18 — Frontend SPA v3: seguimiento del pedido en vivo + tests del frontend

**Qué se añadió** (evolución del SPA de las Fases 15/16; el frontend sigue viviendo **fuera del monorepo** en `~/Desktop/BookStoreWeb`. **El backend no cambió**: `GET /api/v1/orders/{id}` ya devolvía `status` + `updatedAt` en `OrderDto` y el seguimiento se implementa por polling):

- **Corrección de incoherencia en el badge de estado**: la versión anterior pintaba los estados internos de la saga (`AwaitingPayment`/`PaymentApproved`/`ShipmentRequested`/`Completed`), que **nunca llegan a la API de orders** — por eso `Paid` se renderizaba en gris. El SPA usa ahora los estados reales que produce la saga en Orders (`Pending/Paid/Shipped/Delivered/Cancelled`, validado en `Orders.Domain/Enums/OrderStatus.cs`): la saga publica `ChangeOrderStatusCommand` con `Paid`, `Shipped` y `Cancelled`, pero nunca `Delivered`.
- **Módulo puro `src/application/orders/orderStatus.ts`**: `ORDER_STATUS_LABEL` (Pendiente/Pagado/Enviado/Entregado/Cancelado), `ORDER_STEPS` (`Pending → Paid → Shipped → Delivered`), `orderStatusLabel`, `orderStatusStep` e `isOrderFinished` (`Shipped`/`Delivered`/`Cancelled` son finales porque la saga termina en `Shipped`).
- **Polling de 2 s**: `useOrder(id)` hace *refetch* cada 2 s mientras el pedido no esté en estado final; `useMyOrders` hace lo mismo mientras haya pedidos sin terminar. Nuevo `OrdersPort.getOrder(id)` implementado en el repositorio con `GET /api/v1/orders/{id}`; queryKey `orders.byId`.
- **Página `/orders/:id` (`OrderDetailPage`)**: badge + indicador pulsante «actualización en vivo» + componente `OrderTracking` (timeline con las 4 etapas resaltando la actual, o aviso rojo si está `Cancelled`) + total, fechas y artículos; enlace «← Mis pedidos». En «Mis pedidos», cada pedido enlaza a su detalle y el subtítulo explica el seguimiento en vivo.
- **Tests del frontend (tooling nuevo en el SPA)**: Vitest 5 + React Testing Library 16 + jsdom + jest-dom; script `npm test` (`vitest run`); configuración en `vite.config.ts` (`test.environment: 'jsdom'`, `setupFiles: src/test/setup.ts` con cleanup explícito tras cada test). **4 ficheros / 12 tests**: `orderStatus.test.ts` (labels, steps, estado final), `CartStorageImpl.test.ts` (roundtrip localStorage), `CartContext.test.tsx` (añadir/acumular/quitar/vaciar + persistencia) y `Pagination.test.tsx` (render + navegación + disabled).

**Qué no se añadió y por qué**:

- **El backend no se tocó**: el polling aprovecha el `GET /api/v1/orders/{id}` existente (el DTO ya exponía `status`/`updatedAt`); no se añadieron streaming (SSE/WebSockets), endpoint de eventos ni cambios de contrato HTTP/mensajes (el contrato de órdenes es estable).
- **`Delivered` no lo emite la saga** (termina en `Shipped`), pero se mantiene en el timeline y en el enum de Orders (`Shipped → Delivered` es transición válida); el SPA lo refleja como etapa del contrato, no como estado alcanzable hoy.
- **Los tests del frontend viven fuera del monorepo** (junto al SPA): el CI del repo (`ci.yml`) sigue cubriendo solo los **94 tests** de backend; el recuento de **12 tests** corresponde al SPA.
- **Sin *code-splitting* por rutas ni hosting del SPA** (arrastrado de la Fase 16): bundle único y despliegue desacoplado del stack Docker de producción.

**Verificación**: `npm test` **12/12 verdes**, `tsc --noEmit` limpio y `npm run build` OK en el SPA; E2E real vía gateway: pedido creado en `Pending` y observado pasar a `Shipped` por polling de `GET /api/v1/orders/{id}`; el SPA sirve `/` y `/orders/:id` en dev. Suite de backend del repo intacta (94 tests).

---

## Fase 19 — Observabilidad 2: dashboards + alertas + logs (Seq) + .NET Aspire

**Qué se añadió**:

- **Telemetría SharedKernel** (`src/BuildingBlocks/SharedKernel/Telemetry/TelemetryExtensions.cs`): `AddServiceTelemetry` exporta ahora **trazas + métricas + logs** por OTLP **HTTP/protobuf** (protocolo explícito). El endpoint se resuelve: env `OTEL_EXPORTER_OTLP_ENDPOINT` (la inyecta Aspire) > config `OpenTelemetry:Endpoint` > default `http://localhost:4318` (collector). El worker `OrderSaga.Worker/Program.cs` replica el patrón inline, incluyendo logs OTLP (`WithLogging`). Paquetes OpenTelemetry actualizados **1.12.0 → 1.18.0** en `SharedKernel.csproj` (fix de la vulnerabilidad **NU1902**; `OpenTelemetry.Instrumentation.EntityFrameworkCore` y `OpenTelemetry.Exporter.Prometheus.AspNetCore` en `1.18.0-beta.1`).
- **Stack observability v2** (`docker/docker-compose.observability.yml`, `docker/otel-collector.yml`, `docker/prometheus.yml`):
  - **otel-collector** (`otel/opentelemetry-collector-contrib:0.118.0`) recibiendo OTLP en `:4317` (gRPC) / `:4318` (HTTP) — receivers con `endpoint: 0.0.0.0` explícito (en 0.118 el default liga a loopback) — y enrutando **trazas → Jaeger** y **logs → Seq** (`http://seq:80/ingest/otlp`). Los host ports `14317/14318` quedaron descartados.
  - **Seq** (`datalust/seq:latest`): UI `:5341`, volumen de datos `seq-data` y auth deshabilitada (`SEQ_FIRSTRUN_NOAUTHENTICATION`); la imagen de Jaeger dejó de publicar `4317/4318` (Jaeger solo expone `16686`).
  - **Grafana**: datasource Prometheus con **uid fijo `prometheus`**, provider de dashboards (`provisioning/dashboards/provider.yml`) y **2 dashboards JSON provisionados** — `BookStore · API` (uid `bookstore-api`; request rate, latencia p95, error rate 5xx, peticiones activas; template var por `instance`) y `BookStore · RabbitMQ` (uid `bookstore-rabbitmq`; ready/unacked, consumidores, publ/deliv). **Alertas provisionadas** (`provisioning/alerting/`): `rules.yml` con 3 reglas (latencia p95 > 1 s, ratio 5xx > 5 %, colas RabbitMQ > 200), `contact-points.yml` (webhook placeholder `http://host.docker.internal:3001/hooks/none`) y `policies.yml`.
  - **Prometheus**: nuevo job `rabbitmq` scrapeando `host.docker.internal:15692` (`metrics_path /metrics/per-object`) → dashboards de colas; el contenedor dev `bookstore-rabbitmq` expone ahora el puerto **15692**.
- **.NET Aspire AppHost** (`src/AppHost/`): proyecto `BookStore.AppHost` (SDK `Aspire.AppHost.Sdk/13.5.3`, `AspireUseCliBundle=true`) añadido a la solución; orquesta los 6 proyectos con `AddProject<Projects.*>` fijando los puertos HTTP clásicos con `WithHttpEndpoint` (gateway 5080, auth 5100, catalog 5038, orders 5248, inventory 5208, saga worker) y `WaitFor` en el gateway; inyecta envs para la infra dev existente (docker-run, **no gestiona contenedores**): `ConnectionStrings__CatalogDb/OrdersDb/InventoryDb/OrderSagaDb` → `Host=localhost;Port=5432;Database=<db>;Username=postgres;Password=postgres` y `RabbitMQ__Host` → `rabbitmq://localhost:5672`. El dashboard (`https://localhost:17017`) inyecta `OTEL_EXPORTER_OTLP_ENDPOINT` a los servicios. Requiere `dotnet new install Aspire.ProjectTemplates` (el workload `aspire` del SDK **no** está instalado porque exige sudo); es **alternativa** al flujo de 6 `dotnet run` (no ambos a la vez).

**Qué no se añadió y por qué**:

- **Aspire no gestiona la infraestructura**: el AppHost reutiliza los contenedores dev (`bookstore-postgres`, `bookstore-rabbitmq`) vía connection strings por entorno; no se usan recursos `AddPostgres`/`AddRabbitMQ` de Aspire para no duplicar el ciclo de vida de una infra ya cubierta por Docker.
- **El contact point de alertas es un placeholder**: `webhook-local` apunta a `http://host.docker.internal:3001/hooks/none` (sin receptor real en `:3001`); las 3 reglas y las políticas quedan operativas y listas para conectar un canal real (Slack/Teams/PagerDuty) en producción.
- **El stack observability v2 es solo dev**: el compose de producción no despliega collector/Seq/Grafana; los contenedores de prod mantienen `:80` como único puerto público y la observabilidad queda para el entorno de desarrollo (o un futuro stack de observabilidad independiente).
- **Seq no sustituye a los logs de consola**: los servicios siguen logueando a stdout (capturado por Docker); Seq agrega los logs OTLP en dev para correlacionarlos con las trazas.

**Verificación**: `docker compose -f docker/docker-compose.observability.yml up -d` → el collector recibe OTLP en `4317/4318`; traza end-to-end de una orden en Jaeger; logs de los servicios visibles en Seq (UI `:5341`); dashboards `BookStore · API` / `BookStore · RabbitMQ` en Grafana con datos por `instance` y de colas; alertas provisionadas visibles en `Alerting`; `dotnet run --project src/AppHost` levanta los 6 servicios con dashboard en `https://localhost:17017`; suite de backend intacta (94 tests).
