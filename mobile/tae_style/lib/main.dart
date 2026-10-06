import 'package:flutter/material.dart';

import 'app/config.dart';
import 'app/theme.dart';
import 'features/auth/data/access_api.dart';
import 'features/auth/presentation/access_screen.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(
    TaeStyleApp(
      api: AccessApi(base: apiBaseUrl(), store: SecureSessionStore()),
    ),
  );
}

class TaeStyleApp extends StatelessWidget {
  final AccessApi api;
  const TaeStyleApp({super.key, required this.api});
  @override
  Widget build(BuildContext context) => MaterialApp(
    title: 'TaeStyle',
    debugShowCheckedModeBanner: false,
    theme: taeTheme(),
    home: AccessScreen(api: api),
  );
}
