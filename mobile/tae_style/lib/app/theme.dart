import 'package:flutter/material.dart';

const plum = Color(0xFF5A2A4A);
const ivory = Color(0xFFFDFBF7);
const cream = Color(0xFFF4F1EA);
const espresso = Color(0xFF322824);
const taupe = Color(0xFF70615B);

ThemeData taeTheme() {
  final base = ThemeData(
    useMaterial3: true,
    colorScheme: ColorScheme.fromSeed(seedColor: plum, surface: ivory),
    fontFamily: 'Manrope',
  );
  return base.copyWith(
    scaffoldBackgroundColor: ivory,
    textTheme: base.textTheme
        .apply(bodyColor: espresso, displayColor: espresso)
        .copyWith(
          headlineLarge: const TextStyle(
            fontFamily: 'CormorantGaramond',
            fontSize: 44,
            height: 1.1,
            color: espresso,
          ),
          headlineMedium: const TextStyle(
            fontFamily: 'CormorantGaramond',
            fontSize: 34,
            height: 1.15,
            color: espresso,
          ),
        ),
    inputDecorationTheme: InputDecorationTheme(
      border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
      filled: true,
      fillColor: ivory,
    ),
    filledButtonTheme: FilledButtonThemeData(
      style: FilledButton.styleFrom(
        backgroundColor: plum,
        foregroundColor: Colors.white,
        minimumSize: const Size.fromHeight(50),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      ),
    ),
    outlinedButtonTheme: OutlinedButtonThemeData(
      style: OutlinedButton.styleFrom(
        minimumSize: const Size.fromHeight(50),
        foregroundColor: plum,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      ),
    ),
  );
}
