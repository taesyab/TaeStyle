# Primer incremento de TaeStyle

Fecha: 5 de octubre de 2026. Estado: alcance de lanzamiento y reglas funcionales confirmados; contratos y recorridos especificados. Validación del prototipo con usuarias pendiente.

## Resultado que debe recibir la usuaria

Poder crear una cuenta, iniciar sesión, llegar a su closet vacío, mantener la sesión al reabrir la aplicación y cerrarla. Los fallos deben explicarse sin perder innecesariamente los datos del formulario.

Este documento concreta el Sprint 1. No afirma que sus funcionalidades estén implementadas. El alta de prendas, las métricas y la recuperación por correo se entregan en incrementos posteriores, conforme al roadmap.

## Estado de las decisiones

| Decisión | Estado |
|---|---|
| Nombre TaeStyle y repositorio taesyab/TaeStyle | Confirmado |
| Backend .NET 10 LTS | Confirmado |
| Arquitectura por capas y stack Flutter/PostgreSQL | Definido |
| Ecuador, USD y Android primero | Confirmado por la propietaria el 5 de octubre de 2026 |
| Precio y compra opcionales, un uso diario y reglas de archivo/olvido | Confirmado por la propietaria el 5 de octubre de 2026 |
| Proveedor de alojamiento y correo | Pendiente; no impide definir contratos |
| Validación del prototipo con usuarias | Pendiente; no realizada |

## Preparación del entorno

Inspección local realizada el 5 de octubre de 2026:

| Componente | Resultado observado | Paso necesario |
|---|---|---|
| Git | Disponible y repositorio conectado | Trabajar en ramas y revisar cambios |
| .NET SDK | 10.0.401 instalado | Fijar una versión verificada al crear la solución |
| Runtime ASP.NET Core | 10.0.12 instalado | Validar restauración y compilación cuando exista el proyecto |
| Flutter | No localizado en PATH ni en las rutas comunes consultadas | Confirmar instalación o preparar SDK estable |
| Docker | No localizado en PATH ni en la ruta habitual consultada | Confirmar instalación y disponibilidad del motor |
| Android SDK y emulador | No verificados | Revisar con el diagnóstico de Flutter una vez disponible |
| PostgreSQL | No verificado | Preparar instancia local aislada al definir Docker |

No encontrar un ejecutable en esas rutas no demuestra que no esté instalado. No se han instalado ni actualizado herramientas, aceptado licencias ni modificado configuración global.

## Secuencia del sprint

| Paso | Trabajo concreto | Evidencia de finalización |
|---|---|---|
| 1 · B02 | Crear solución y proyectos Domain, Application, Infrastructure y API; proyectos de pruebas | Compila; referencias respetan las capas; Domain no referencia infraestructura |
| 2 · B03 | Configurar persistencia de cuentas y sesiones, migración inicial | Base vacía se crea mediante migraciones; email normalizado es único |
| 3 · B04 | Registro, login, refresh, logout y consulta de cuenta | Recorrido HTTP completo y pruebas de sesiones inválidas/revocadas |
| 4 · B06 | Crear Flutter, navegación y almacenamiento seguro | Registro y login desde móvil contra API real, sin simular éxito |
| 5 · B02 | Añadir CI para los proyectos existentes | Compilación, análisis y pruebas ejecutados en GitHub con resultados visibles |
| 6 | Validar el incremento integrado | Evidencia del recorrido móvil y lista explícita de limitaciones |

Los pasos pueden subdividirse sin alterar los identificadores B01–B17 ni duplicar los 93 puntos estimados del backlog. No se asignan fechas ni responsables ficticios.

## Pantallas del primer recorrido

### Acceso

Nombre TaeStyle, una frase breve sobre organizar y aprovechar la ropa, acciones «Crear cuenta» e «Iniciar sesión». Mostrar «Recuperar contraseña» únicamente cuando el flujo esté conectado; no publicar un enlace que no funcione. La recuperación seguirá siendo obligatoria para el lanzamiento del MVP.

### Crear cuenta

Campos: correo, contraseña y confirmación de contraseña. Mostrar u ocultar la contraseña con un control accesible. Explicar el mínimo de 12 caracteres junto al campo. Mostrar USD como moneda del piloto y la zona horaria sugerida por el dispositivo, confirmada por la persona. No asumir que todo Ecuador comparte una sola zona horaria.

Al pulsar «Crear cuenta», deshabilitar el envío repetido mientras se procesa. Conservar correo y selecciones ante errores; no persistir contraseñas en disco. Tras el alta, ir a «Iniciar sesión» con correo precargado y confirmación de cuenta creada. No emitir una sesión implícita desde el endpoint de registro.

### Iniciar sesión

Correo y contraseña, acceso al registro y error genérico ante credenciales incorrectas. Con acceso correcto, abrir el closet vacío. Con error de red, permitir reintentar sin afirmar que la contraseña es incorrecta.

### Closet vacío

Título «Mi closet» y explicación de que todavía no hay prendas. Durante este incremento de pruebas se presenta como estado vacío; la acción de agregar se habilita con B08. No mostrar saldos, métricas o prendas ficticias como datos reales de la cuenta.

### Cuenta

Correo, moneda, zona horaria y acción «Cerrar sesión». Los cambios de moneda no se habilitan sin resolver sus reglas. Al cerrar sesión en línea, revocar la sesión actual y volver a Acceso. Si no hay conexión, eliminar credenciales locales e informar que la revocación remota no pudo confirmarse; no confundirlo con cierre de todas las sesiones.

## Contratos y pruebas

Los contratos de acceso se definen en [Contratos de autenticación](../api/autenticacion.md). Se mantendrán consistentes con OpenAPI cuando exista la API.

Escenarios mínimos:

1. Registro válido crea una sola cuenta y permite login posterior.
2. Correo repetido con diferente capitalización no crea otra cuenta.
3. Contraseña incorrecta devuelve un error genérico y no inicia sesión.
4. Solicitud privada sin token o con token vencido devuelve 401.
5. Renovar una sesión cambia el refresh token; el anterior no sirve nuevamente.
6. Reutilización de refresh token revoca su familia y solicita login.
7. Cerrar sesión impide renovarla; el access token puede conservar validez hasta su expiración corta prevista.
8. Dos cuentas obtienen únicamente su propia información en /me.
9. Solicitudes móviles simultáneas comparten una única renovación de sesión para no disparar reutilización accidental.
10. Un timeout no muestra éxito ficticio ni dispara reintentos ilimitados.

## Condición para iniciar y terminar

Antes de implementar: registrar las decisiones de producto que afecten al primer incremento y revisar los flujos y contratos propuestos. La arquitectura aprobada no implica que se haya probado el prototipo con usuarias.

El incremento termina cuando el recorrido funciona contra PostgreSQL real, las pruebas pertinentes pasan, la CI produce resultados reales y la app se verifica en la plataforma seleccionada. Un proyecto que solo compila no equivale a una funcionalidad terminada.
