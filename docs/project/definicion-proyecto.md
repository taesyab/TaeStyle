# Definición del proyecto TaeStyle

Fecha: 5 de octubre de 2026.

## Objetivo

Preparar un MVP móvil que permita registrar prendas, medir su uso y comprender la inversión registrada y el potencial orientativo de reventa. El diseño funcional y técnico es la referencia para aceptar las funcionalidades.

## Repositorio

Repositorio confirmado: [taesyab/TaeStyle](https://github.com/taesyab/TaeStyle), con rama principal prevista main. El nombre aprobado del producto es TaeStyle. Facilita coordinar API, móvil, contratos y documentación con un único backlog. No se creará una rama develop permanente para este MVP. La visibilidad no se ha verificado ni modificado.

Propietario confirmado: taesyab. El repositorio respondió sin referencias de ramas publicadas al consultarlo. La copia local está conectada mediante origin. Quedan pendientes los accesos de colaboradores y la configuración de seguimiento en GitHub.

## Flujo de trabajo

1. Definir una tarea con resultado esperado, criterios de aceptación y dependencias.
2. Crear una rama corta desde main: docs/descripcion, feat/descripcion, fix/descripcion o chore/descripcion.
3. Preparar cambios acotados y abrir un pull request vinculado a la tarea.
4. Revisar el resultado y las comprobaciones aplicables.
5. Integrar mediante squash y eliminar la rama terminada.

Se propone impedir eliminaciones y force push de main y exigir resolución de conversaciones. Exigir una revisión externa solo cuando haya un segundo colaborador que pueda realizarla; no bloquear a una persona trabajando sola con una aprobación imposible.

La protección automática de ramas privadas depende del plan de GitHub. Verificar disponibilidad antes de prometerla; si el plan no la permite, documentar que el flujo se sigue como convención y no como control obligatorio. [Documentación oficial](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches).

No se configurarán comprobaciones obligatorias que aún no existan. La CI de backend y Flutter se añadirá al crear proyectos ejecutables, con compilación, análisis y pruebas pertinentes.

## Seguimiento en GitHub

Propuesta de GitHub Project: TaeStyle MVP. Vistas: tablero de estado y tabla por sprint. Estados: Backlog, Listo, En curso, En revisión, Terminado. Campo separado Bloqueado y explicación en la tarea; conservar el estado real del trabajo.

Campos: prioridad, área, sprint, estimación y responsable. Etiquetas propuestas: tipo:historia, tipo:tarea, tipo:error; area:backend, area:mobile, area:ux, area:infra, area:docs; prioridad:P0 y prioridad:P1. Solo asignar responsables cuando se conozca el equipo.

Hitos: Sprint 0 Diseño; Sprint 1 Base y acceso; Sprint 2 Prendas; Sprint 3 Organización y uso; Sprint 4 Métricas; Sprint 5 Calidad; Sprint 6 Piloto. No fijar fechas hasta confirmar capacidad y fecha de inicio. Los 93 puntos del diseño son provisionales.

Los elementos B01–B17 del diseño serán las tareas principales. Las historias HU01–HU14 se vincularán a sus tareas sin duplicar estimaciones. Una plantilla local no crea una issue, un hito ni un tablero remoto.

## Decisiones aprobadas y pendientes

Aprobadas: alcance MVP del diseño, arquitectura por capas y .NET 10 LTS. GitHub será el servicio de control de versiones solicitado.

Pendientes: verificar visibilidad del repositorio; moneda y país del piloto; Android primero o Android/iOS; reglas propuestas sobre fechas, precio desconocido y archivo; alojamiento y correo. La preparación documental puede avanzar mientras se resuelven; la implementación de funcionalidades dependientes debe respetar sus respuestas.

## Información y acceso

No versionar credenciales, claves de firma, datos de usuarios, fotos reales ni respaldos. Los ejemplos futuros contendrán únicamente valores ficticios. Los secretos del servidor pertenecerán al entorno de ejecución; una aplicación móvil no debe contener secretos de servidor.

No se asignará una licencia de código abierto sin decisión de la propietaria. No publicar el repositorio ni añadir colaboradores automáticamente.

## Fuente de verdad

El Markdown de arquitectura dentro de docs/architecture será la versión mantenida en el repositorio. El Word entregado anteriormente sigue siendo una exportación de revisión; deberá regenerarse cuando cambie el diseño. Esta preparación no modifica aquella entrega.

## Criterio de preparación completa

README, diseño, guía de contribución, plantillas y exclusiones revisados; propietario y repositorio confirmados; primer contenido incorporado respetando el historial existente; tablero y permisos comprobados en GitHub. La documentación local está preparada y origin configurado; el tablero, los permisos y las protecciones todavía no se han configurado.
