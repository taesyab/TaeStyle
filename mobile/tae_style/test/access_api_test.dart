import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:tae_style/features/auth/data/access_api.dart';

class MemoryStore implements SessionStore {
  String? value;
  @override
  Future<String?> read() async => value;
  @override
  Future<void> write(String v) async {
    value = v;
  }

  @override
  Future<void> clear() async {
    value = null;
  }
}

void main() {
  test(
    'Concurrent 401 responses rotate once and retry with the new token',
    () async {
      var rotations = 0;
      final store = MemoryStore()
        ..value = jsonEncode({'accessToken': 'old', 'refreshToken': 'refresh'});
      final api = AccessApi(
        base: Uri.parse('https://example.test'),
        store: store,
        client: MockClient((r) async {
          if (r.url.path.endsWith('/refresh')) {
            rotations++;
            await Future<void>.delayed(const Duration(milliseconds: 10));
            return http.Response(
              jsonEncode({'accessToken': 'new', 'refreshToken': 'new-refresh'}),
              200,
            );
          }
          return r.headers['Authorization'] == 'Bearer new'
              ? http.Response('{"email":"a@example.com"}', 200)
              : http.Response('{"title":"Expired"}', 401);
        }),
      );
      await api.restore();
      final profiles = await Future.wait([api.me(), api.me()]);
      expect(rotations, 1);
      expect(profiles.every((p) => p['email'] == 'a@example.com'), isTrue);
      expect(store.value, contains('new-refresh'));
    },
  );
  test('Logout clears storage even if the server cannot be reached', () async {
    final store = MemoryStore()
      ..value = '{"accessToken":"a","refreshToken":"b"}';
    final api = AccessApi(
      base: Uri.parse('https://example.test'),
      store: store,
      client: MockClient((_) async => throw http.ClientException('offline')),
    );
    await api.restore();
    expect(await api.logout(), isFalse);
    expect(store.value, isNull);
  });
  test('Rejected refresh removes persisted credentials', () async {
    final store = MemoryStore()
      ..value = '{"accessToken":"a","refreshToken":"b"}';
    final api = AccessApi(
      base: Uri.parse('https://example.test'),
      store: store,
      client: MockClient(
        (_) async => http.Response('{"title":"Expired"}', 401),
      ),
    );
    await api.restore();
    await expectLater(api.me(), throwsA(isA<AccessError>()));
    expect(store.value, isNull);
  });
}
