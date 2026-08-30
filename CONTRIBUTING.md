# Contributing

¡Gracias por interesarte en **bookstore-microservices**! Este proyecto es un sistema de microservicios educativo y de referencia. Al contribuir mantén la misma calidad que el resto del repo: código limpio, convenciones claras y documentación al día.

Consulta primero el [README](README.md) (arquitectura, quickstart, puertos y configuración).

---

## Índice

1. [¿Cómo empezar?](#cómo-empezar)
2. [Flujo de trabajo Git / PRs](#flujo-de-trabajo-git--prs)
3. [Conventional Commits](#conventional-commits)
4. [Estándares de código](#estándares-de-código)
5. [Organización por capas](#organización-por-capas)
6. [Migraciones EF Core](#migraciones-ef-core)
7. [Testing](#testing)
8. [Mensajería y sagas](#mensajería-y-sagas)
9. [Docker y entornos](#docker-y-entornos)
10. [Documentación](#documentación)
11. [Seguridad y secrets](#seguridad-y-secrets)
12. [Definición de hecho (DoD)](#definición-de-hecho-dod)

---

## ¿Cómo empezar?

1. Clona el repo y sigue el [Quickstart (desarrollo)](README.md#quickstart-desarrollo) del README.
2. Instala las herramientas necesarias:
   - SDK de **.NET 10**
   - **Docker Desktop** (para infraestructura y tests de integración)
   - `dotnet-ef` para migraciones:
     ```bash
     dotnet tool install --global dotnet-ef
     ```
3. Crea una rama desde `main`:
   ```bash
   git checkout main
   git pull
   git checkout -b feat/orders-improve-stock-check
   ```

> Cualquier tarea que no toque código (bug reproducible, mejora, refactor) empieza siempre en un **issue** con su `steps to reproduce` cuando aplique.

## Flujo de trabajo Git / PRs

- **Modelo**: rama corta por cambio + **PR a `main`**. No se pushea directo a `main` salvo hotfixes acordados por review.
- La rama se nombra con el tipo del cambio: `feat/...`, `fix/...`, `refactor/...`, `docs/...`, `chore/...`.
- **Cada PR debe:** pasar el CI (`.github/workflows/ci.yml`), mantener TODOS los tests verdes y actualizar la documentación que toque.
- **Regla de oro**: un PR hace *una cosa* — pequeño, revisable y con títulos según [Conventional Commits](#conventional-commits). Un PR gigante se rechaza sin revisión pidiendo que se divida.
- Tras la aprobación se hace *squash merge* si el PR tiene múltiples commits de trabajo (**conventional commit** final), o *rebase* si se necesita mantener historia por etapas.
- Etiqueta el PR con su **scope** (`orders`, `catalog`, `infra`, `ci`, `docs`, etc.) cuando el repo los tenga definidos.

## Conventional Commits

Se usa [Conventional Commits](https://www.conventionalcommits.org/) **obligatorio**. Formato:

```
<tipo>(<scope opcional>): <descripción>

<cuerpo opcional, por qué y cómo>
```

Tipos permitidos:

| Tipo | Uso |
|---|---|
| `feat` | Nueva funcionalidad (feature) |
| `fix` | Corrección de bug |
| `refactor` | Cambio de código sin cambiar comportamiento |
| `docs` | Sólo documentación |
| `test` | Sólo tests |
| `build` / `ci` | Cambios de build, dependencias o pipelines |
| `chore` | Tareas de mantenimiento |
| `perf` | Optimizaciones de rendimiento |

Scopes del monorepo: `orders`, `catalog`, `inventory`, `auth`, `saga`, `gateway`, `sharedk`, `tests`, `docker`, `ci`, `docs`.

Ejemplos:

```
feat(orders): add idempotency-key support to POST /orders
fix(inventory): decrement stock atomically on ship
docs: add contributing guide
ci: bump setup-dotnet to v6
refactor(catalog): extract validation to Application layer
```

## Estándares de código

- **C# moderno**: `net10.0`, *file-scoped namespaces*, primary constructors, records para DTOs y commands, `var` cuando el tipo es evidente, `null-forgiving` solo cuando tienes certeza, colecciones inmutables (`IReadOnlyList`).
- **Sin comentarios de relleno**; el código debe leerse solo. Comentar solo el *por qué* no obvio.
- **FluentValidation** para la validación de entrada (API), nunca validación dentro del modelo de dominio salvo invariantes reales.
- **Nullable habilitado**: no silenciar warnings sin justificación.
- Formateo consistente: `dotnet format` antes de cada commit:
  ```bash
  dotnet format BookStore.slnx
  ```
- El código nuevo que no compila, no se sube. Ejecuta siempre `dotnet build BookStore.slnx`.

## Organización por capas

Cada servicio de `src/Services/*` sigue **Clean Architecture** (reglas de dependencia **siempre hacia dentro**):

```
<Servicio>.API/             → controladores, middleware, DI del servicio
<Servicio>.Application/     → commands/queries, DTOs, interfaces (repositorios, unit of work)
<Servicio>.Domain/          → entidades, value objects, eventos de dominio, invariantes
<Servicio>.Infrastructure/  → EF Core, repositorios, migraciones, consumidores externos
```

- **No** pongas `using` de infraestructura dentro de `Application`/`Domain` (p. ej. no lances tipos de EF/Npgsql desde Application).
- Los **`Program.cs`** deben mantenerse delgados; la configuración pesada vive en extensiones de DI (`AddInfrastructure()`, `AddApplication()`).
- Al tocar un contrato HTTP, actualiza el **Swagger/link** y el tabla de rutas del README.

## Migraciones EF Core

Los servicios **aplican migraciones automáticamente al arrancar** (ver nota en README). Cuando cambie el modelo:

1. Crea la migración desde la carpeta del **Infrastructure** correspondiente (cada proyecto tiene su *design-time factory*):
   ```bash
   cd src/Services/Orders/Orders.Infrastructure
   dotnet ef migrations add AddSomething
   ```
2. **Revisa el `Up()` generado**: en `ALTER`/`UPDATE` sobre tablas con datos, añade el *backfill* necesario (ej. claves únicas sobre filas existentes usan `gen_random_uuid()`).
3. No borres migraciones aplicadas; si tu rama aún no las ha introducido, usa `dotnet ef migrations remove`.
4. Un cambio de esquema **debe** incluir la migración en el mismo PR que el cambio de modelo.

## Testing

- **Unit** (`.UnitTests`): dominio y aplicación, sin IO (repuestos con NSubstitute).
- **Integración** (`.IntegrationTests`): `WebApplicationFactory` + **Testcontainers** (Postgres efímero por clase fixture). Requieren Docker.
- Ejecuta siempre la suite completa antes de empujar:
  ```bash
  dotnet test BookStore.slnx
  ```
- Al añadir un comportamiento nuevo (o fixear uno), **acompáñalo de tests**:
  - bug → test que reproduce el escenario primero;
  - feature → test del caso feliz + al menos un caso de borde (validación, autorización, idempotencia, catálogo caído).

## Mensajería y sagas

- Se usa **MassTransit + RabbitMQ** con patrones **Outbox/Inbox** (EF). Al tocar consumidores:
  - mantené réplicas por **idempotencia**: los consumidores deben tolerar recibir el mismo mensaje dos veces;
  - los **dominio-events** se persisten en el mismo `UnitOfWork` que el agregado (transacción atómica);
  - el nombre de endpoints usa *kebab-case* (`order-status-changed`).
- Cambios en los mensajes (contratos) son breaking: versiona inmediatamente o crea un ADR. Nunca rompas el contrato sin coordinar productor y consumidor (usa *contract testing* cuando el ecosistema crezca).

## Docker y entornos

- `docker/` contiene los Dockerfiles (multi-stage) y tres compose: `docker-compose.observability.yml` (Jaeger/Prometheus/Grafana), `docker-compose.prod.yml` (stack completo de producción) y scripts de init de BD en `docker/postgres/init/`.
- Si cambias puertos/envs: actualiza el **README** y el compose de prod.
- Los contenedores de infra de desarrollo se lanzan por `docker run` (ver Quickstart), no es necesario tocar compose para iterar en dev.

## Documentación

- Documentación **versionada con el código** (*docs-as-code*): si el PR cambia comportamiento, actualiza README/swagger. Un PR que rompe docs se considera incompleto.
- Las decisiones de arquitectura se registran como **ADR** en `docs/adr/` (`adr-NNNN-title.md`): cuándo se tomó, por qué y alternativas.
- Enlaces, tablas de puertos/envs y diagramas deben seguir siendo ciertos tras el cambio.

## Seguridad y secrets

- **Nunca** commitees secretos: claves JWT reales, contraseñas de entornos no-dev, tokens o certificados. Las credenciales de ejemplo de dev (`admin/admin123`) son intencionales y solo para local.
- Las claves reales de prod van en **GitHub Secrets** / Docker secrets / Vault, nunca en `appsettings.json`.
- Si detectas un vector de seguridad, abre un issue privado o contacta a los mantenedores en vez de publicarlo en un PR abierto.

## Definición de hecho (DoD)

Antes de marcar como lista una tarea, el cambio debe cumplir **todo** esto:

- [ ] Compila (`dotnet build BookStore.slnx`) sin nuevos warnings.
- [ ] Suite completa verde (`dotnet test BookStore.slnx`).
- [ ] Tests nuevos cubren el cambio (happy path + borde).
- [ ] Conventional Commit claro y con scope.
- [ ] Documentación al día (README, Swagger, ADR si es decisión).
- [ ] Sin secretos commiteados; secretos fuera de `appsettings.json`.
- [ ] Migración EF incluida si cambió el modelo.
- [ ] PR pequeño, con descripción del *qué* y *por qué*.

---

Si tienes dudas de cómo abordar un cambio, abre un issue y etiquétalo: `question` o `good first issue`. ¡Gracias por contribuir!