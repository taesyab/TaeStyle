# Validación del primer incremento

Fecha: 5 de octubre de 2026.

## Comprobado localmente

- Solución .NET 10 compilada sin errores ni advertencias.
- Seis pruebas backend aprobadas, incluyendo una prueba de integración que aplica migraciones en PostgreSQL 18 y verifica registro, email duplicado, login incorrecto, acceso privado, aislamiento entre cuentas, rotación, detección de reutilización, logout e invalidación por versión de seguridad.
- Flutter 3.47.6: análisis sin incidencias y cuatro pruebas aprobadas de validación de formulario, renovación concurrente, eliminación de credenciales y logout sin red.
- Un escenario adicional del cliente Flutter contra la API y PostgreSQL reales aprobado: registro, login, perfil, restauración de sesión y logout. Ese escenario requiere TEST_API_URL y se omite explícitamente en el comando estándar.
- Migración inicial generada con EF Core y SQL idempotente exportado.
- Tipografías aprobadas incluidas localmente junto con sus licencias.
- Auditoría NuGet incluyendo dependencias transitivas: sin vulnerabilidades reportadas por los orígenes consultados en esta fecha; no equivale a una garantía de ausencia de vulnerabilidades futuras.

## Todavía no comprobado

- Ejecución en emulador o dispositivo Android y comportamiento real del almacén seguro Android: no hay Android SDK disponible en el equipo.
- Composición Docker local: no está instalado el motor Docker. Se incluye validación de construcción de imagen en CI.
- Despliegue, HTTPS público, correo y recuperación de contraseña: fuera de este primer incremento.
- Pruebas de carga, accesibilidad con tecnologías de asistencia y validación con usuarias.

El workflow de GitHub requiere una ejecución exitosa antes de considerar verificadas la construcción de imagen y la generación de APK. No se marca el Sprint 1 como completado hasta verificar también el recorrido en Android.
