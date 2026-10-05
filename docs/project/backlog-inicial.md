# Backlog inicial para GitHub

Esta lista es una preparación local, no una lista de issues ya creadas. Las estimaciones y criterios funcionales detallados permanecen en el diseño.

| ID | Trabajo | Sprint | Dependencias |
|---|---|---|---|
| B01 | Cerrar decisiones, prototipo y contratos | 0 | — |
| B02 | Solución por capas, Docker y CI | 1 | B01 |
| B03 | Esquema, migraciones y catálogos | 1 | B02 |
| B04 | Registro, login, renovación y logout | 1 | B03 |
| B05 | Recuperación y correo | 2 | B04 |
| B06 | Base Flutter, navegación y sesión segura | 1 | B01, B04 |
| B07 | Fotos privadas y limpieza | 2 | B03, B04 |
| B08 | Alta y edición de prendas | 2 | B06, B07 |
| B09 | Listado, detalle, búsqueda y filtros | 3 | B08 |
| B10 | Archivar, restaurar y eliminar | 3 | B09 |
| B11 | Registrar y deshacer uso | 3 | B09 |
| B12 | Métricas y costo por uso | 4 | B10, B11 |
| B13 | Prendas olvidadas | 4 | B11 |
| B14 | Reventa y configuración versionada | 4 | B03, B12 |
| B15 | Accesibilidad, usabilidad y estados de red | 5 | Funciones principales completas |
| B16 | Aislamiento, carga y restauración | 5 | B15 |
| B17 | Piloto y distribución | 6 | B16 |

## Primera tarea concreta

Título: B01 Definir las decisiones de lanzamiento del MVP.

Resultado: confirmar moneda, plataformas móviles y reglas de registro; revisar el prototipo y los contratos; registrar decisiones y fecha. .NET 10 LTS ya está aprobado y no requiere volver a decidirse.

Aceptación: cada decisión tiene una respuesta explícita; las contradicciones con el diseño se corrigen; el primer incremento técnico tiene un alcance verificable.
