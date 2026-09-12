# Security Policy

**bookstore-microservices** es un proyecto educativo y de referencia. Aun así, las buenas prácticas de seguridad se aplican a todo el ciclo de vida del código.

## Reportar una vulnerabilidad

**No** abras un issue público para reportar vulnerabilidades de seguridad. Escribe un correo privado a:

✉️ **fam.castro99@gmail.com**

Envía el reporte con la siguiente información:

- Descripción clara del problema y su **impacto** (¿qué se comprometería?).
- Pasos reproducibles, o payload/PoC si es necesario.
- Cómo de crítica crees que es la vulnerabilidad.
- Tu contacto (opcional) para seguir el caso.

### Qué esperar

| Plazo | Compromiso |
|---|---|
| Primer acuse de recibo | ≤ 72 h laborables |
| Triage y confirmación | ≤ 5 días laborables |
| Mitigación en staging / rama | Según severidad |

### Política de divulgación

- 🟢 **Severidad baja/media**: se corrige en una rama normal y se documenta en el [CHANGELOG](CHANGELOG.md).
- 🟠 **Severidad alta**: el fix se prepara en una rama privada y se libera junto con una descripción pública, dando tiempo a actualizar.
- 🔴 **Crítica**: coordinación total con el reportero antes de cualquier divulgación pública (*responsible disclosure*).

## Versiones soportadas

Al no haber releases publicados aún, se soporta únicamente la rama `main`. Cuando existan tags `v*`, esta política se ampliará a las versiones en mantenimiento.

## Clarificación

Los credenciales de desarrollo (`admin/admin123`, `customer/customer123`) son **intencionales y solo para entornos locales**: viven en `appsettings.Development.json` y solo se cargan con `ASPNETCORE_ENVIRONMENT=Development`. `appsettings.json` **no contiene secretos** (placeholders `CHANGE_ME` con fail-fast). No reutilices las credenciales demo en producción; los entornos productivos deben usar secrets reales por variables de entorno (ver [CONTRIBUTING.md](CONTRIBUTING.md#seguridad-y-secrets)).