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

**Estado final**: 92 tests en verde · 33 commits · CI/CD operativo · stack prod validado.

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
- **No se hizo rate limiting ni autenticación en el gateway**: la validación de tokens se mantiene en cada servicio (firma JWT compartida) y el límite de tráfico se dejó como deuda de producción.
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
