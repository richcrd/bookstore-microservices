# ADR-0001 — Autenticación con OpenID Connect (OpenIddict) en vez de JWT casero

- **Estado**: Aceptado
- **Fecha**: 2026-09-11
- **Decisores**: Richard Rodriguez · revisión de arquitectura
- **Referencias**: [architecture.md §12](../architecture.md), [Fase 14 de desarrollo](../phases.md), [README](../README.md)

## Contexto

El sistema tenía `Auth.API` emitiendo **JWT HMAC-SHA256** firmados con una clave compartida (`Jwt__Issuer/Jwt__Audience/Jwt__SigningKey`) que los servicios validaban por firma. Ese enfoque tenía límites para un sistema tipo *bookstore* que necesita:

1. **Múltiples tipos de cliente**: un SPA (frontend del futuro) y clientes confidenciales/scripts (`cli`).
2. **Flujos estándar**: refresh tokens, consentimiento, logout y futura revocación.
3. **Rotación de claves y discovery**: los consumidores no deberían conocer la clave de firma; en un despliegue real la clave se rota periódicamente.
4. **Seguridad de tokens**: HMAC con clave compartida obliga a distribuir el secreto a todos los validadores (amplia superficie de ataque) y no soporta asimetría ni *okta*-como OIDC out of the box.

Se evaluó sustituir el login propio por un IdP externo (Keycloak / IdentityServer / Auth0), pero el repo es un sistema educativo autocontenido que arranca con un `docker compose` propio: externalizar el IdP rompía el espíritu de *unstackeable locally*.

## Decisión

Adoptar **OpenIddict 7** dentro de `Auth.API` como **proveedor OpenID Connect**:

- **Endpoints OIDC estándar**: `/connect/authorize`, `/connect/token`, `/connect/logout` y discovery/`jwks` en `/.well-known/*`.
- **Flujos habilitados**:
  - `web-spa` (cliente **público**): **Authorization Code + PKCE** y refresh, con página de login propia en `Auth.API`.
  - `cli` (cliente **confidencial**): **password grant** y refresh (scripts/E2E).
- **Persistencia** de aplicaciones, scopes y autorizaciones en una nueva BD `auth_db` (OpenIddict + EF Core), sembrada al arrancar.
- **Validación por discovery (RS256)** en Catalog/Orders/Inventory vía `OpenIddict.Validation` (`AddIdpAuthentication` en `SharedKernel`): cada servicio descarga el documento de discovery del issuer y valida `iss`, firma y claves. Eliminada la clave de firma compartida de la configuración.
- **Gateway YARP** expone `/connect/{**catch-all}` y `/.well-known/{**catch-all}` hacia `Auth.API`, de modo que el **issuer público** sea la URL del gateway (en dev `http://localhost:5100` directo; en prod `BOOKSTORE_PUBLIC_ISSUER`), único puerto expuesto.
- **Tests de integración** migrados a un **esquema de autenticación de test** (`TestAuthHandler`): la suite no depende de la firma real ni de un IdP en marcha.

Se eligió **OpenIddict frente a IdentityServer**: IdentityServer 4 requirió licenciamiento desde v5 y no encaja con la filosofía MIT del proyecto; OpenIddict es libre, activo y se integra con EF Core y ASP.NET Core de forma directa.

## Consecuencias

**Positivas:**
- Contratos de autenticación **estándar OIDC**: cualquier cliente/SPA (incluso un futuro frontend en otra pila) se integra sin código propietario.
- **Layering de seguridad** correcto: los validadores no conocen material de firma (solo el issuer); el *remote node* OpenIddict resuelve claves por discovery.
- Base para rotación de claves, refresh tokens reales y (con trabajo futuro) revocación y logout.
- Testability: los flujos se verifican sin depender de un servidor externo.

**Negativas / deuda:**
- `auth_db` añade una BD más y el requisito de que **Auth arranque después de Postgres** (gestionado con `depends_on` en prod).
- **Certificados de desarrollo** (`AddDevelopment*Certificate`): un despliegue real exige certificados de firma/cifrado persistentes y **HTTPS** (deuda documentada en architecture.md §16).
- El issuer debe ser **único y estable** por entorno; cambiarlo invalida tokens y discovery (por eso es una env var, no config magic).
- El password grant se mantiene únicamente para la CLI/scripts (no es recomendable para SPAs); el frontend real usará Authorization Code + PKCE.

## Alternativas consideradas

1. **Mantener JWT HMAC mejorado (rotación, audience)**: rechazado — seguía abriendo el vector de la clave compartida y no aportaba flujos estándar.
2. **Keycloak en contenedor**: descartado por peso (JVM, base de datos propia, provisioning extra) y porque centraliza la autenticación en un componente externo al dominio de la app.
3. **IdentityServer**: descartado por el modelo de licencias desde v5 (MVC/advertencias de licencia) frente a OpenIddict (libre, MIT, integración EF nativa).
4. **Auth0/Cognito (SaaS)**: descartado por requerir credenciales externas y no ser autocontenido para el entorno local/prod del curso.