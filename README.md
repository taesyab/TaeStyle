# TaeStyle

Aplicación móvil para aprovechar mejor la ropa, entender la inversión en el closet e identificar prendas sin uso registrado y su posible valor de reventa.

## Estado del proyecto

Primer incremento en implementación: API .NET 10 de registro y sesiones, migración PostgreSQL y app Flutter con acceso y closet vacío. Piloto confirmado en Ecuador, con USD y Android primero. No hay despliegue público ni gestión de prendas todavía.

## Stack aprobado

- Aplicación móvil: Flutter.
- API: ASP.NET Core 10 sobre .NET 10 LTS.
- Persistencia: PostgreSQL, EF Core 10 y Npgsql compatible.
- Arquitectura: Domain, Application, Infrastructure y API; CQRS simple.
- Autenticación: JWT con renovación de sesión.
- Documentación de API: OpenAPI y Swagger.
- Contenedores: Docker.
- Control de versiones y seguimiento: GitHub.

## MVP

Registro, login y recuperación; closet digital; registro de uso; métricas financieras; prendas olvidadas; estimación de reventa mediante reglas configurables.

Marketplace, comunidad, probador virtual, IA estilista, tiendas, clima, calendario de outfits e intercambio quedan fuera. YOLOv8, OpenCV y rembg son posibilidades futuras, sin implementación actual.

## Documentación

- [Diseño funcional y técnico](docs/architecture/TaeStyle-Diseno-MVP.md).
- [Definición del proyecto y organización en GitHub](docs/project/definicion-proyecto.md).
- [Backlog de preparación](docs/project/backlog-inicial.md).
- [Primer incremento y preparación del entorno](docs/project/primer-incremento.md).
- [Contratos de autenticación](docs/api/autenticacion.md).
- [Ejecutar y verificar el primer incremento](docs/project/ejecucion-local.md).
- [Guía de contribución](CONTRIBUTING.md).

## Organización prevista

Un solo repositorio contiene backend, móvil y documentación para mantener alineados los cambios del MVP.

| Ruta | Propósito | Estado |
|---|---|---|
| docs/ | Arquitectura, decisiones y planificación | Preparado |
| .github/ | Plantillas de tareas y pull requests | Preparado |
| backend/ | Solución .NET, cuatro capas y pruebas | Implementación inicial |
| mobile/tae_style/ | Proyecto Flutter | Acceso y closet vacío |
| deploy/ | Docker, Compose y script PostgreSQL | Preparado para desarrollo |

Los comandos de ejecución y requisitos están en la guía enlazada. La recuperación de contraseña, gestión de prendas e informes se implementarán en los siguientes incrementos; no se debe publicar esta base como un MVP completo.

## Repositorio remoto

Repositorio confirmado: [taesyab/TaeStyle](https://github.com/taesyab/TaeStyle). Nombre del producto: **TaeStyle**. Rama principal prevista: main. La visibilidad del repositorio no se ha verificado ni modificado.
