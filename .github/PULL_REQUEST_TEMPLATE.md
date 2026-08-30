## Descripción

<!-- Qué hace este PR y por qué. Referencia el issue si existe (closes #NN). -->

## Tipo de cambio

- [ ] `feat` — nueva funcionalidad
- [ ] `fix` — corrección de bug
- [ ] `refactor` — cambio sin alterar comportamiento
- [ ] `docs` — solo documentación
- [ ] `test` — solo tests
- [ ] `build` / `ci` — build, dependencias o pipelines
- [ ] `chore` — mantenimiento

<br/>

## Cambios principales

- <!-- resumen punto a punto -->
- 
- 

## Cómo se probó

- [ ] `dotnet build BookStore.slnx` sin nuevos warnings
- [ ] `dotnet test BookStore.slnx` (suite completa en verde)
- [ ] Comprobación manual (cURL / Swagger / gateway) — describe el flujo:
      <!-- p. ej. token → crear pedido con Idempotency-Key → Shipped -->

## Checklist (DoD)

- [ ] Conventional Commit final claro y con scope (`orders`, `inventory`, `gateway`, `ci`, …)
- [ ] Tests nuevos para el cambio (happy path + borde)
- [ ] Migración EF incluida si cambió el modelo (revisada, con backfill si hay datos)
- [ ] Documentación actualizada (README, Swagger, ADD/ADR si procede)
- [ ] Sin secretos: claves y credenciales fuera de `appsettings.json`

## Sugerencias para el reviewer

- <!-- puntos conflictivos o de decisión de arquitectura -->