# Contratos de autenticación de TaeStyle

Estado: especificación propuesta para implementar el primer incremento. No son endpoints ya disponibles. Prefijo /api/v1, HTTPS y cuerpos JSON. Las respuestas de autenticación incluyen Cache-Control no-store. Nunca registrar contraseñas, tokens o encabezados Authorization.

## Reglas comunes

- Email: normalizar de forma consistente, recortar espacios externos y validar formato; máximo 254 caracteres. No modificar silenciosamente una contraseña.
- Contraseña: mínimo propuesto de 12 caracteres; máximo propuesto de 128. Permitir espacios, pegado y gestores. Confirmación de contraseña es una validación del formulario y no se envía a la API.
- Campos requeridos vacíos o inválidos: 400 con errores por campo. JSON mal formado: 400. Tipo de contenido no admitido: 415.
- Fechas de expiración: instante UTC con formato ISO 8601. Idioma inicial de mensajes: español. Los clientes toman decisiones por códigos estables, no por el texto traducido.
- Límites de intentos de acceso y recuperación configurables por IP y cuenta normalizada; evitar bloqueos permanentes que un tercero pueda provocar. Devolver 429 y Retry-After cuando aplique.
- Los límites numéricos de intentos se fijarán y verificarán antes de publicar; no están configurados actualmente.

## Registro

POST /auth/register. Acceso anónimo.

| Campo de solicitud | Regla |
|---|---|
| email | Requerido, email válido |
| password | Requerido, longitud admitida |
| currency | USD, única moneda del piloto confirmado |
| timeZone | Zona IANA válida; sugerida por el dispositivo, confirmada por la persona |

Éxito: 201 con id, email, currency y timeZone. No devolver password, hash ni tokens. No aceptar un id de propietario elegido por el cliente. El cliente muestra confirmación y dirige a login.

Email duplicado: 409 con código account_already_exists. Esta decisión facilita corregir el registro, pero permite inferir que una cuenta existe; aplicar límites de intentos y no reutilizar este comportamiento en recuperación de contraseña. Si se decide ocultar también la existencia durante registro, deberá rediseñarse el flujo antes de implementarlo.

La unicidad depende de la base de datos, no solo de una consulta previa. Si dos solicitudes compiten, solo una crea la cuenta. Si el cliente pierde la respuesta, puede iniciar sesión o repetir el alta y recibir el conflicto, sin crear cuentas duplicadas.

## Login

POST /auth/login. Acceso anónimo. Solicitud: email y password.

Éxito: 200 con accessToken, refreshToken, tokenType igual a Bearer, accessTokenExpiresAt y refreshTokenExpiresAt. Access token propuesto: 15 minutos. Refresh token propuesto: 30 días desde login como duración absoluta de la familia; una rotación no prolonga indefinidamente ese plazo.

Credenciales incorrectas: 401 con código invalid_credentials y mensaje genérico, tanto si el email no existe como si la contraseña es incorrecta. No devolver detalles de validación interna de Identity.

El JWT incluirá sub, identificador de sesión, jti, emisor, audiencia, tiempos de emisión/expiración y versión de seguridad. Validar firma, algoritmo permitido, emisor, audiencia y expiración. Evitar datos de perfil innecesarios dentro del JWT.

## Renovación

POST /auth/refresh. No requiere un access token vigente; requiere refreshToken en el cuerpo.

Éxito: 200 con los mismos campos de login y un refresh token nuevo. La transacción consume el token anterior y vincula el sustituto. Guardar solo el hash del token en servidor.

Token desconocido, vencido o revocado: 401 con código invalid_session. Reutilización de token ya consumido: revocar su familia y solicitar nuevo login. No devolver el motivo preciso al cliente.

Flutter serializa las renovaciones concurrentes. Si una renovación se pierde por timeout después de ser consumida en servidor, solicitar login cuando no pueda recuperarse de forma segura; no reintentar indefinidamente el mismo token. La política prioriza revocación verificable sobre continuidad de una sesión ambigua.

## Cierre de sesión

POST /auth/logout. Cuerpo: refreshToken de la sesión actual. El token actúa como credencial para revocar su propia familia, incluso si el access token venció. No admite un usuario ni una familia arbitraria enviados por el cliente.

Éxito: 204 sin contenido, tanto para un token válido como para uno desconocido o ya revocado; un cuerpo ausente o mal formado sigue siendo 400. No indica si una sesión ajena existe.

El cliente limpia credenciales locales. La revocación impide nuevas renovaciones, pero un JWT de acceso ya emitido puede permanecer válido hasta 15 minutos. El cierre de todas las sesiones no forma parte de este endpoint.

## Cuenta actual

GET /me. Requiere Authorization Bearer válido. No acepta UsuarioId como filtro.

Éxito: 200 con id, email, currency y timeZone de la identidad autenticada. Sin sesión válida: 401. No devolver tablas de Identity, hashes, reglas internas ni tokens.

## Recuperación y cambio de contraseña

Incluidos en B05, después del primer incremento, y obligatorios antes del lanzamiento. POST /auth/forgot-password recibe email y responde 202 con un mensaje uniforme para cuentas existentes o inexistentes. La solicitud válida no confirma entrega de correo.

POST /auth/reset-password recibe token y newPassword. Éxito: 204. Token inválido, consumido o vencido: 400 con código invalid_reset_token. El cambio consume el token de un solo uso, incrementa la versión de seguridad y revoca sesiones dentro de una transacción.

El middleware compara la versión de seguridad del JWT con la versión actual de la cuenta para invalidar access tokens previos al cambio; validar solo la firma y expiración no basta para garantizar este efecto. En el MVP se puede consultar una proyección mínima de cuenta; cualquier caché futura requiere invalidación explícita.

El enlace debe abrir una pantalla real para introducir la contraseña. Se decidirá el enlace móvil y su alternativa web antes de B05, junto con el proveedor de correo. No se fija un dominio inexistente.

## Formato de errores

Problem Details con type, title, status, detail, instance cuando corresponda, y extensiones code, traceId y errors para validaciones por campo. No exponer stack traces, consultas SQL ni secretos. type puede ser about:blank hasta contar con un catálogo público de errores.

| Código | HTTP | Comportamiento del cliente |
|---|---|---|
| validation_failed | 400 | Señalar campos y conservar datos no sensibles |
| account_already_exists | 409 | Ofrecer login; recuperación cuando esté disponible |
| invalid_credentials | 401 | Mostrar mensaje genérico, sin renovar sesión |
| invalid_session | 401 | Limpiar sesión y solicitar login |
| rate_limited | 429 | Respetar Retry-After y permitir intentar después |
| unexpected_error | 500 | Mostrar error y referencia de soporte; no revelar detalles internos |

El interceptor móvil intenta renovar ante 401 de recursos privados, una sola vez por solicitud y mediante una renovación compartida. No intenta renovar ante 401 de login, refresh o solicitudes anónimas. Reenviar mutaciones exige revisar su idempotencia; no habilitar un reintento indiscriminado para todos los endpoints futuros.
