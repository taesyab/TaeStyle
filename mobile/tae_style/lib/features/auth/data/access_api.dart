import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class AccessError implements Exception {
  final String message;
  final int status;
  const AccessError(this.message, [this.status = 0]);
  @override
  String toString() => message;
}

abstract class SessionStore {
  Future<String?> read();
  Future<void> write(String value);
  Future<void> clear();
}

class SecureSessionStore implements SessionStore {
  final FlutterSecureStorage storage;
  SecureSessionStore([this.storage = const FlutterSecureStorage()]);
  @override
  Future<String?> read() => storage.read(key: 'taestyle.session');
  @override
  Future<void> write(String value) =>
      storage.write(key: 'taestyle.session', value: value);
  @override
  Future<void> clear() => storage.delete(key: 'taestyle.session');
}

class AccessApi {
  final Uri base;
  final http.Client client;
  final SessionStore store;
  Map<String, dynamic>? _tokens;
  Future<void>? _renewing;
  int _generation = 0;
  AccessApi({required this.base, required this.store, http.Client? client})
    : client = client ?? http.Client();

  Future<Map<String, dynamic>> _request(
    String path, {
    Map<String, dynamic>? body,
    String? token,
  }) async {
    final uri = base.resolve('/api/v1$path');
    final headers = {
      'Content-Type': 'application/json',
      if (token != null) 'Authorization': 'Bearer $token',
    };
    try {
      final response =
          await (body == null
                  ? client.get(uri, headers: headers)
                  : client.post(uri, headers: headers, body: jsonEncode(body)))
              .timeout(const Duration(seconds: 15));
      final decoded = response.body.isEmpty
          ? <String, dynamic>{}
          : jsonDecode(response.body);
      if (response.statusCode >= 400) {
        throw AccessError(
          response.statusCode == 429
              ? 'Espera un minuto antes de volver a intentarlo.'
              : decoded['title'] as String? ??
                    'No se pudo completar la solicitud.',
          response.statusCode,
        );
      }
      return Map<String, dynamic>.from(decoded as Map);
    } on AccessError {
      rethrow;
    } on TimeoutException {
      throw const AccessError(
        'La conexión tardó demasiado. Inténtalo de nuevo.',
      );
    } on http.ClientException {
      throw const AccessError('No pudimos conectarnos. Revisa tu conexión.');
    } on FormatException {
      throw const AccessError('El servidor devolvió una respuesta inesperada.');
    }
  }

  Future<void> register(String email, String password, String zone) async {
    await _request(
      '/auth/register',
      body: {
        'email': email.trim(),
        'password': password,
        'currency': 'USD',
        'timeZone': zone,
      },
    );
  }

  Future<void> login(String email, String password) async {
    final tokens = await _request(
      '/auth/login',
      body: {'email': email.trim(), 'password': password},
    );
    await store.write(jsonEncode(tokens));
    _generation++;
    _tokens = tokens;
  }

  Future<bool> restore() async {
    final saved = await store.read();
    if (saved == null) return false;
    try {
      final tokens = Map<String, dynamic>.from(jsonDecode(saved) as Map);
      if (tokens['refreshToken'] is! String ||
          tokens['accessToken'] is! String) {
        throw const FormatException();
      }
      _tokens = tokens;
      return true;
    } catch (_) {
      await store.clear();
      return false;
    }
  }

  Future<void> _renew() async {
    final existing = _renewing;
    if (existing != null) return existing;
    final operation = _rotate();
    _renewing = operation;
    try {
      await operation;
    } finally {
      _renewing = null;
    }
  }

  Future<void> _rotate() async {
    final generation = _generation;
    final refresh = _tokens?['refreshToken'];
    if (refresh == null) {
      throw const AccessError('Inicia sesión nuevamente.', 401);
    }
    try {
      final tokens = await _request(
        '/auth/refresh',
        body: {'refreshToken': refresh},
      );
      if (_generation != generation) {
        throw const AccessError('La sesión ha cambiado.', 401);
      }
      await store.write(jsonEncode(tokens));
      _tokens = tokens;
    } catch (_) {
      // A lost refresh response is ambiguous: do not reuse a possibly consumed token.
      if (_generation == generation) {
        _tokens = null;
        await store.clear();
      }
      throw const AccessError(
        'No pudimos renovar tu sesión. Inicia sesión nuevamente.',
        401,
      );
    }
  }

  Future<Map<String, dynamic>> me() async {
    final sentToken = _tokens?['accessToken'] as String?;
    if (sentToken == null) {
      throw const AccessError('Inicia sesión nuevamente.', 401);
    }
    try {
      return await _request('/me', token: sentToken);
    } on AccessError catch (error) {
      if (error.status != 401) rethrow;
      if (_tokens?['accessToken'] == sentToken) await _renew();
      return _request('/me', token: _tokens?['accessToken'] as String?);
    }
  }

  Future<bool> logout() async {
    // Wait for in-flight rotation so the newest token is revoked as well.
    try {
      await _renewing;
    } catch (_) {
      /* Already invalidated. */
    }
    final refresh = _tokens?['refreshToken'];
    _generation++;
    _tokens = null;
    await store.clear();
    if (refresh == null) return true;
    try {
      await _request('/auth/logout', body: {'refreshToken': refresh});
      return true;
    } catch (_) {
      return false;
    }
  }

  void dispose() => client.close();
}
