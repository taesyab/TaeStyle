import 'package:flutter_test/flutter_test.dart';
import 'package:tae_style/features/auth/data/access_api.dart';

import 'access_api_test.dart' show MemoryStore;

void main() {
  const base = String.fromEnvironment('TEST_API_URL');
  test(
    'Flutter client registers, logs in, restores and logs out against real API',
    () async {
      final store = MemoryStore();
      final api = AccessApi(base: Uri.parse(base), store: store);
      final email =
          'flutter-${DateTime.now().microsecondsSinceEpoch}@example.com';
      const password = 'Test only password 2026';
      try {
        await api.register(email, password, 'Pacific/Galapagos');
        await api.login(email, password);
        expect((await api.me())['email'], email);
        final restored = AccessApi(base: Uri.parse(base), store: store);
        try {
          expect(await restored.restore(), isTrue);
          expect((await restored.me())['timeZone'], 'Pacific/Galapagos');
          expect(await restored.logout(), isTrue);
          expect(store.value, isNull);
        } finally {
          restored.dispose();
        }
      } finally {
        api.dispose();
      }
    },
    skip: base.isEmpty
        ? 'Requires TEST_API_URL pointing to a running test API.'
        : false,
  );
}
