import 'package:flutter/foundation.dart';

Uri apiBaseUrl() {
  const configured = String.fromEnvironment('API_BASE_URL');
  final value = configured.isEmpty && kDebugMode
      ? 'http://10.0.2.2:5080'
      : configured;
  final uri = Uri.tryParse(value);
  if (uri == null ||
      !uri.hasAuthority ||
      (!kDebugMode && uri.scheme != 'https')) {
    throw StateError(
      'Define API_BASE_URL con una URL HTTPS para esta compilación.',
    );
  }
  return uri;
}
