import 'dart:convert';
import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:tae_style/features/auth/data/access_api.dart';
import 'package:tae_style/features/closet/presentation/closet_view.dart';

import 'access_api_test.dart' show MemoryStore;

void main() {
  test(
    'Photo upload retries after token renewal with identical bytes',
    () async {
      final store = MemoryStore()
        ..value = '{"accessToken":"old","refreshToken":"refresh"}';
      final image = Uint8List.fromList([1, 2, 3, 4]);
      var uploads = 0;
      final api = AccessApi(
        base: Uri.parse('https://example.test'),
        store: store,
        client: MockClient((request) async {
          if (request.url.path.endsWith('/refresh')) {
            return http.Response(
              '{"accessToken":"new","refreshToken":"new-refresh"}',
              200,
            );
          }
          uploads++;
          expect(request.bodyBytes, image);
          expect(request.headers['Content-Type'], 'application/octet-stream');
          return request.headers['Authorization'] == 'Bearer new'
              ? http.Response('{"id":"photo-id"}', 201)
              : http.Response('{}', 401);
        }),
      );
      await api.restore();
      expect(await api.uploadPhoto(image), 'photo-id');
      expect(uploads, 2);
      api.dispose();
    },
  );
  testWidgets(
    'Closet distinguishes network error from empty closet and retries',
    (tester) async {
      var requests = 0;
      final store = MemoryStore()
        ..value = '{"accessToken":"token","refreshToken":"refresh"}';
      final api = AccessApi(
        base: Uri.parse('https://example.test'),
        store: store,
        client: MockClient((request) async {
          requests++;
          if (requests == 1) throw http.ClientException('offline');
          return http.Response(
            jsonEncode({'items': [], 'page': 1, 'hasMore': false}),
            200,
          );
        }),
      );
      await api.restore();
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: SingleChildScrollView(
              child: ClosetView(
                api: api,
                timeZone: 'America/Guayaquil',
                onSessionExpired: () {},
              ),
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();
      expect(find.text('Reintentar'), findsOneWidget);
      expect(find.text('Agrega tu primera prenda para empezar.'), findsNothing);
      await tester.tap(find.text('Reintentar'));
      await tester.pumpAndSettle();
      expect(
        find.text('Agrega tu primera prenda para empezar.'),
        findsOneWidget,
      );
      expect(find.text('Agregar prenda'), findsOneWidget);
      api.dispose();
    },
  );
  testWidgets('Garment form requires fields before calling the API', (
    tester,
  ) async {
    var requests = 0;
    final api = AccessApi(
      base: Uri.parse('https://example.test'),
      store: MemoryStore(),
      client: MockClient((_) async {
        requests++;
        return http.Response('{}', 500);
      }),
    );
    await tester.pumpWidget(
      MaterialApp(
        home: GarmentForm(api: api, timeZone: 'America/Guayaquil'),
      ),
    );
    await tester.scrollUntilVisible(
      find.text('Guardar prenda'),
      300,
      scrollable: find.byType(Scrollable).first,
    );
    await tester.tap(find.text('Guardar prenda'));
    await tester.pumpAndSettle();
    expect(requests, 0);
    await tester.scrollUntilVisible(
      find.text('Escribe el nombre.'),
      -300,
      scrollable: find.byType(Scrollable).first,
    );
    expect(find.text('Escribe el nombre.'), findsOneWidget);
    api.dispose();
  });
}
