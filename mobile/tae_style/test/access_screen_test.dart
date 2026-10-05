import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:tae_style/main.dart';
import 'package:tae_style/features/auth/data/access_api.dart';

import 'access_api_test.dart' show MemoryStore;

void main() {
  testWidgets('Registration validates before sending credentials', (
    tester,
  ) async {
    var requests = 0;
    final api = AccessApi(
      base: Uri.parse('https://example.test'),
      store: MemoryStore(),
      client: MockClient((_) async {
        requests++;
        return http.Response('{}', 201);
      }),
    );
    await tester.pumpWidget(TaeStyleApp(api: api));
    await tester.pumpAndSettle();
    await tester.scrollUntilVisible(find.text('Crear cuenta'), 200);
    await tester.tap(find.text('Crear cuenta'));
    await tester.pumpAndSettle();
    await tester.ensureVisible(
      find.widgetWithText(FilledButton, 'Crear cuenta'),
    );
    await tester.tap(find.widgetWithText(FilledButton, 'Crear cuenta'));
    await tester.pumpAndSettle();
    expect(find.text('Escribe un correo válido.'), findsOneWidget);
    expect(requests, 0);
    expect(tester.takeException(), isNull);
  });
}
