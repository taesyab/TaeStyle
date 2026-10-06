# Ejecutar el primer incremento

El proyecto implementa cuentas, registro, login, refresh rotatorio, logout, consulta de cuenta y alta de prendas con foto privada, listado y detalle. Consulta [alta de prendas](alta-prendas.md) para actualizar una instalación existente. La recuperación de contraseña corresponde a B05 y sigue pendiente; es obligatoria antes de un lanzamiento público.

## Requisitos

.NET SDK 10.0.401, Flutter 3.47.6 y PostgreSQL 18. Para Android se necesita Android SDK y un emulador o dispositivo. Docker es opcional para desarrollo local; los archivos Compose preparados requieren Docker disponible.

Las fuentes Cormorant Garamond y Manrope están incluidas con sus licencias para que la interfaz no dependa de descargarlas en ejecución. El monograma Ts se representa tipográficamente; los iconos de instalación definitivos y el logo vectorial quedan pendientes.

## Backend

Desde la raíz del repositorio, configurar ConnectionStrings__Database y Jwt__Key en el entorno o en secretos de desarrollo. La clave JWT debe ser aleatoria, con al menos 32 bytes, y no debe subirse a GitHub.

1. Ejecutar `dotnet tool restore` y `dotnet restore backend/TaeStyle.sln`.
2. Aplicar esquema con `dotnet tool run dotnet-ef database update --project backend/src/TaeStyle.Infrastructure --startup-project backend/src/TaeStyle.API`.
3. En desarrollo, fijar ASPNETCORE_ENVIRONMENT=Development.
4. Ejecutar `dotnet run --project backend/src/TaeStyle.API --no-launch-profile --urls http://127.0.0.1:5080`.
5. Abrir `/swagger` en ese servidor. `/health` comprueba el proceso, no la disponibilidad de PostgreSQL.

El SQL idempotente equivalente está en deploy/sql/initial-identity.sql. Las migraciones no se aplican automáticamente al arrancar la API.

## Docker local

Copiar deploy/.env.example a deploy/.env y completar los valores con secretos locales. Ejecutar `docker compose --env-file deploy/.env -f deploy/compose.yaml up -d db`. Aplicar la migración desde .NET contra localhost:5433 con esas credenciales; después ejecutar el mismo comando Compose con `up -d --build api`.

Compose escucha solamente en localhost y es de desarrollo. Un despliegue público requiere HTTPS, revisión de proxy y límites de acceso, manejo de secretos y respaldo. No usar el modo Development en producción.

## Flutter

Desde mobile/tae_style: `flutter pub get`, `flutter analyze` y `flutter test`.

En un emulador Android, iniciar con `flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5080`. La excepción HTTP se limita a la configuración debug y al host del emulador; release exige HTTPS. Para un dispositivo físico se debe configurar una URL HTTPS alcanzable.

La app no contiene una API simulada ni credenciales de demostración. Para usar el flujo, PostgreSQL y la API deben estar funcionando. El token se guarda con flutter_secure_storage; una renovación perdida obliga a iniciar sesión para evitar reutilizar un token consumido.

## Pruebas

Configurar TAESTYLE_TEST_DATABASE con una base PostgreSQL exclusiva de pruebas. Ejecutar `dotnet test backend/TaeStyle.sln`; las pruebas aplican migraciones y crean usuarios ficticios únicos, no eliminan bases existentes.

Para probar el cliente Flutter contra la API real: `flutter test --dart-define=TEST_API_URL=http://127.0.0.1:5080 test/live_api_test.dart`. Usa la misma base aislada de pruebas. Si no se define TEST_API_URL, este escenario se omite explícitamente.

GitHub Actions compila y prueba backend con PostgreSQL, analiza y prueba Flutter e intenta generar un APK debug. El estado de un workflow solo confirma su resultado cuando termina; un archivo de workflow por sí solo no equivale a una ejecución exitosa.

## Límites de este incremento

La protección actual de acceso usa límites por IP (30 solicitudes por minuto) y bloqueo temporal por cuenta tras 5 errores de contraseña. La revisión de enumeración y tiempos de respuesta, rate limiting distribuido y despliegue detrás de proxy requiere endurecimiento antes de producción. Los errores se devuelven como Problem Details con código y traceId; la validación detallada por campo aún debe ampliarse.

ASP.NET Core Identity gestiona usuarios a través de sus stores EF. El repositorio específico de prendas se incorporará con ese agregado; no se introduce un repositorio genérico encima de Identity. Las decisiones de proveedor de correo, borrado de cuenta y privacidad continúan pendientes antes de publicación.
