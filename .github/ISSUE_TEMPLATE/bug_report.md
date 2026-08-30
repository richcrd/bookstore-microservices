---
name: Reporte de bug
about: Algo no funciona como debería
title: "bug(<scope>): descripción breve"
labels: bug
assignees: ''
---

**Describe el bug**
Qué ocurre, de forma clara y concisa.

**Reproducción**
Pasos para reproducirlo:
1. Arranco los servicios con `...`
2. Ejecuto `curl ...`
3. Veo `...`

**Resultado esperado**
Qué debería haber pasado.

**Resultado real**
Qué pasó (adjunta logs/stack trace, capturas de Jaeger/Prometheus si aplica).

**Entorno**
- Servicio/scope implicado: [`orders`, `inventory`, `catalog`, `auth`, `gateway`, `saga`, `ci`…]
- Origen del error: [ ] Dev (procesos locales)  [ ] Stack Docker de prod
- Versión de rama o tag: 

**Contexto adicional**
- ¿Es intermitente o reproducible siempre?
- ¿Afecta a un sólo servicio o a varios (mensajería/saga)?
- ¿Cambiaste algo del entorno (puertos, env vars, secrets) antes del fallo?