import 'package:flutter/material.dart';

import '../../../app/theme.dart';
import '../data/access_api.dart';

class AccessScreen extends StatefulWidget {
  final AccessApi api;
  const AccessScreen({super.key, required this.api});
  @override
  State<AccessScreen> createState() => _AccessScreenState();
}

class _AccessScreenState extends State<AccessScreen> {
  String page = 'loading';
  String? message;
  bool busy = false, showPassword = false;
  String zone = 'America/Guayaquil';
  Map<String, dynamic>? profile;
  final email = TextEditingController();
  final password = TextEditingController();
  final confirm = TextEditingController();
  final form = GlobalKey<FormState>();

  @override
  void initState() {
    super.initState();
    restore();
  }

  @override
  void dispose() {
    email.dispose();
    password.dispose();
    confirm.dispose();
    super.dispose();
  }

  Future<void> restore() async {
    try {
      if (await widget.api.restore()) {
        profile = await widget.api.me();
        if (mounted) setState(() => page = 'closet');
      } else if (mounted) {
        setState(() => page = 'welcome');
      }
    } on AccessError catch (e) {
      if (mounted) {
        setState(() {
          page = e.status == 401 ? 'login' : 'retry';
          message = e.message;
        });
      }
    } catch (_) {
      if (mounted) {
        setState(() {
          page = 'retry';
          message = 'No pudimos abrir tu sesión. Inténtalo de nuevo.';
        });
      }
    }
  }

  void go(String target) {
    setState(() {
      page = target;
      message = null;
      password.clear();
      confirm.clear();
      showPassword = false;
    });
  }

  Future<void> submit() async {
    if (busy || !form.currentState!.validate()) return;
    final registering = page == 'register';
    setState(() {
      busy = true;
      message = null;
    });
    try {
      if (registering) {
        await widget.api.register(email.text, password.text, zone);
        if (mounted) {
          go('login');
          setState(() => message = 'Cuenta creada. Ya puedes iniciar sesión.');
        }
      } else {
        await widget.api.login(email.text, password.text);
        profile = await widget.api.me();
        if (mounted) go('closet');
      }
    } on AccessError catch (e) {
      if (mounted) setState(() => message = e.message);
    } catch (_) {
      if (mounted) {
        setState(
          () => message =
              'No se pudo completar la operación. Inténtalo de nuevo.',
        );
      }
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  Future<void> logout() async {
    setState(() => busy = true);
    try {
      final revoked = await widget.api.logout();
      if (!mounted) return;
      profile = null;
      go('welcome');
      if (!revoked) {
        setState(
          () => message = 'Sesión cerrada en este dispositivo. No se pudo confirmar la revocación en el servidor.',
        );
      }
    } catch (_) {
      if (mounted) {
        setState(
          () => message = 'No pudimos cerrar la sesión. Inténtalo de nuevo.',
        );
      }
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  Widget logo({bool large = false}) => Column(
    children: [
      CircleAvatar(
        radius: large ? 48 : 24,
        backgroundColor: plum,
        child: Text(
          'Ts',
          style: TextStyle(
            fontFamily: 'CormorantGaramond',
            color: ivory,
            fontSize: large ? 62 : 30,
          ),
        ),
      ),
      if (large) ...[
        const SizedBox(height: 12),
        const Text(
          'TaeStyle',
          style: TextStyle(fontFamily: 'CormorantGaramond', fontSize: 40),
        ),
      ],
    ],
  );

  @override
  Widget build(BuildContext context) {
    final heading = Theme.of(context).textTheme.headlineMedium;
    if (page == 'loading') {
      return const Scaffold(body: Center(child: CircularProgressIndicator()));
    }
    final authenticated = page == 'closet' || page == 'account';
    return Scaffold(
      bottomNavigationBar: authenticated
          ? NavigationBar(
              selectedIndex: page == 'account' ? 1 : 0,
              onDestinationSelected: busy
                  ? null
                  : (i) => go(i == 0 ? 'closet' : 'account'),
              destinations: const [
                NavigationDestination(
                  icon: Icon(Icons.checkroom_outlined),
                  label: 'Mi closet',
                ),
                NavigationDestination(
                  icon: Icon(Icons.person_outline),
                  label: 'Cuenta',
                ),
              ],
            )
          : null,
      body: SafeArea(
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 440),
            child: ListView(
              padding: const EdgeInsets.all(24),
              children: [
                if (page == 'register' || page == 'login')
                  Align(
                    alignment: Alignment.centerLeft,
                    child: TextButton(
                      onPressed: busy ? null : () => go('welcome'),
                      child: const Text('← Volver'),
                    ),
                  ),
                if (message != null)
                  Padding(
                    padding: const EdgeInsets.only(bottom: 16),
                    child: Semantics(
                      liveRegion: true,
                      child: Text(message!, key: const Key('feedback')),
                    ),
                  ),
                if (page == 'welcome') ...[
                  const SizedBox(height: 32),
                  logo(large: true),
                  const SizedBox(height: 12),
                  const Text(
                    'TU CLOSET, TU ESENCIA',
                    textAlign: TextAlign.center,
                    style: TextStyle(
                      color: taupe,
                      letterSpacing: 2,
                      fontSize: 12,
                    ),
                  ),
                  const SizedBox(height: 32),
                  Text(
                    'Tu ropa.\nTu historia.',
                    textAlign: TextAlign.center,
                    style: Theme.of(context).textTheme.headlineLarge,
                  ),
                  const SizedBox(height: 20),
                  const Text(
                    'Conoce tu closet, aprovecha tus prendas y descubre el valor de la ropa que no usas.',
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 36),
                  FilledButton(
                    onPressed: () => go('register'),
                    child: const Text('Crear cuenta'),
                  ),
                  const SizedBox(height: 12),
                  OutlinedButton(
                    onPressed: () => go('login'),
                    child: const Text('Ya tengo una cuenta'),
                  ),
                ],
                if (page == 'retry') ...[
                  logo(),
                  const SizedBox(height: 24),
                  FilledButton(
                    onPressed: () {
                      setState(() => page = 'loading');
                      restore();
                    },
                    child: const Text('Reintentar conexión'),
                  ),
                ],
                if (page == 'register' || page == 'login') ...[
                  logo(),
                  const SizedBox(height: 24),
                  Text(
                    page == 'register'
                        ? 'Tu closet empieza aquí'
                        : 'Qué gusto verte',
                    style: heading,
                  ),
                  const SizedBox(height: 20),
                  Form(
                    key: form,
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        TextFormField(
                          controller: email,
                          enabled: !busy,
                          keyboardType: TextInputType.emailAddress,
                          autocorrect: false,
                          decoration: const InputDecoration(
                            labelText: 'Correo electrónico',
                          ),
                          validator: (v) =>
                              v == null ||
                                  !RegExp(r'^[^\s@]+@[^\s@]+\.[^\s@]+$')
                                      .hasMatch(v.trim())
                              ? 'Escribe un correo válido.'
                              : null,
                        ),
                        const SizedBox(height: 16),
                        TextFormField(
                          controller: password,
                          enabled: !busy,
                          obscureText: !showPassword,
                          enableSuggestions: false,
                          autocorrect: false,
                          decoration: InputDecoration(
                            labelText: 'Contraseña',
                            helperText: page == 'register'
                                ? 'Entre 12 y 128 caracteres.'
                                : null,
                            suffixIcon: IconButton(
                              tooltip: showPassword
                                  ? 'Ocultar contraseña'
                                  : 'Mostrar contraseña',
                              onPressed: () =>
                                  setState(() => showPassword = !showPassword),
                              icon: Icon(
                                showPassword
                                    ? Icons.visibility_off
                                    : Icons.visibility,
                              ),
                            ),
                          ),
                          validator: (v) =>
                              v == null ||
                                  v.isEmpty ||
                                  (page == 'register' &&
                                      (v.length < 12 || v.length > 128))
                              ? 'Revisa la longitud de tu contraseña.'
                              : null,
                        ),
                        if (page == 'register') ...[
                          const SizedBox(height: 16),
                          TextFormField(
                            controller: confirm,
                            enabled: !busy,
                            obscureText: true,
                            decoration: const InputDecoration(
                              labelText: 'Confirmar contraseña',
                            ),
                            validator: (v) => v != password.text
                                ? 'Las contraseñas no coinciden.'
                                : null,
                          ),
                          const SizedBox(height: 16),
                          DropdownButtonFormField<String>(
                            initialValue: zone,
                            isExpanded: true,
                            decoration: const InputDecoration(
                              labelText: 'Zona horaria',
                            ),
                            items: const [
                              DropdownMenuItem(
                                value: 'America/Guayaquil',
                                child: Text('Ecuador continental'),
                              ),
                              DropdownMenuItem(
                                value: 'Pacific/Galapagos',
                                child: Text('Galápagos'),
                              ),
                            ],
                            onChanged: busy
                                ? null
                                : (v) => setState(() => zone = v!),
                          ),
                          const SizedBox(height: 12),
                          const Text(
                            'Moneda del closet: USD',
                            style: TextStyle(color: taupe),
                          ),
                        ],
                        const SizedBox(height: 24),
                        FilledButton(
                          onPressed: busy ? null : submit,
                          child: Text(
                            busy
                                ? 'Procesando…'
                                : page == 'register'
                                ? 'Crear cuenta'
                                : 'Iniciar sesión',
                          ),
                        ),
                      ],
                    ),
                  ),
                  TextButton(
                    onPressed: busy
                        ? null
                        : () => go(page == 'register' ? 'login' : 'register'),
                    child: Text(
                      page == 'register'
                          ? 'Ya tengo cuenta'
                          : 'Crear una cuenta',
                    ),
                  ),
                ],
                if (page == 'closet') ...[
                  logo(),
                  const SizedBox(height: 20),
                  Text('Mi closet', style: heading),
                  const SizedBox(height: 64),
                  const Icon(Icons.checkroom_outlined, size: 72, color: plum),
                  const SizedBox(height: 24),
                  Text(
                    'Un espacio para\nlo que ya tienes',
                    style: heading,
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 16),
                  const Text(
                    'Todavía no has agregado prendas.',
                    textAlign: TextAlign.center,
                  ),
                ],
                if (page == 'account') ...[
                  logo(),
                  const SizedBox(height: 20),
                  Text('Mi cuenta', style: heading),
                  const SizedBox(height: 24),
                  ListTile(
                    title: const Text('Correo electrónico'),
                    subtitle: Text(profile?['email'] as String? ?? ''),
                  ),
                  const ListTile(
                    title: Text('Moneda'),
                    subtitle: Text('USD · dólar estadounidense'),
                  ),
                  ListTile(
                    title: const Text('Zona horaria'),
                    subtitle: Text(
                      profile?['timeZone'] == 'Pacific/Galapagos'
                          ? 'Galápagos'
                          : 'Ecuador continental',
                    ),
                  ),
                  const SizedBox(height: 24),
                  OutlinedButton(
                    onPressed: busy ? null : logout,
                    child: const Text('Cerrar sesión'),
                  ),
                ],
              ],
            ),
          ),
        ),
      ),
    );
  }
}
