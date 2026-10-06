import 'dart:math';
import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';

import '../../auth/data/access_api.dart';

const categories = [
  'Blusa',
  'Camisa',
  'Pantalón',
  'Jean',
  'Vestido',
  'Chaqueta',
  'Abrigo',
  'Falda',
  'Zapatos',
  'Accesorios',
];
const conditions = ['Excelente', 'Bueno', 'Regular'];

class ClosetView extends StatefulWidget {
  final AccessApi api;
  final VoidCallback onSessionExpired;
  final String timeZone;
  const ClosetView({
    super.key,
    required this.api,
    required this.onSessionExpired,
    required this.timeZone,
  });
  @override
  State<ClosetView> createState() => _ClosetViewState();
}

class _ClosetViewState extends State<ClosetView> {
  final List<Map<String, dynamic>> items = [];
  bool loading = true, more = false;
  int page = 0;
  String? error;
  @override
  void initState() {
    super.initState();
    load(reset: true);
    recoverPhoto();
  }

  Future<void> recoverPhoto() async {
    try {
      final response = await ImagePicker().retrieveLostData();
      if (!mounted || response.isEmpty) return;
      if (response.files?.isNotEmpty == true) {
        await add(recovered: response.files!.first);
      } else {
        setState(
          () => error = 'No se pudo recuperar la foto. Vuelve a seleccionarla.',
        );
      }
    } catch (_) {
      /* Plugin may be unavailable outside Android. */
    }
  }

  Future<void> load({bool reset = false}) async {
    setState(() {
      loading = true;
      error = null;
    });
    try {
      final next = reset ? 1 : page + 1;
      final result = await widget.api.garments(next);
      if (!mounted) return;
      setState(() {
        if (reset) items.clear();
        final incoming = (result['items'] as List).map(
          (e) => Map<String, dynamic>.from(e as Map),
        );
        for (final garment in incoming) {
          if (!items.any((g) => g['id'] == garment['id'])) items.add(garment);
        }
        page = next;
        more = result['hasMore'] as bool;
      });
    } on AccessError catch (e) {
      if (mounted) {
        if (e.status == 401) {
          widget.onSessionExpired();
        } else {
          setState(() => error = e.message);
        }
      }
    } catch (_) {
      if (mounted) {
        setState(
          () => error = 'No pudimos cargar tu closet. Inténtalo de nuevo.',
        );
      }
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  Future<void> add({XFile? recovered}) async {
    final saved = await Navigator.of(context).push<bool>(
      MaterialPageRoute(
        builder: (_) => GarmentForm(
          api: widget.api,
          timeZone: widget.timeZone,
          recovered: recovered,
        ),
      ),
    );
    if (saved == true && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Prenda agregada a tu closet.')),
      );
      await load(reset: true);
    }
  }

  @override
  Widget build(BuildContext context) => Column(
    crossAxisAlignment: CrossAxisAlignment.stretch,
    children: [
      Row(
        children: [
          Expanded(
            child: Text(
              'Mi closet',
              style: Theme.of(context).textTheme.headlineMedium,
            ),
          ),
          IconButton(
            tooltip: 'Actualizar closet',
            onPressed: loading ? null : () => load(reset: true),
            icon: const Icon(Icons.refresh),
          ),
        ],
      ),
      const SizedBox(height: 16),
      FilledButton.icon(
        onPressed: () => add(),
        icon: const Icon(Icons.add),
        label: const Text('Agregar prenda'),
      ),
      const SizedBox(height: 24),
      if (error != null) ...[
        Text(error!, semanticsLabel: error),
        TextButton(
          onPressed: loading ? null : () => load(reset: items.isEmpty),
          child: const Text('Reintentar'),
        ),
      ],
      if (!loading && error == null && items.isEmpty) ...[
        const SizedBox(height: 32),
        const Icon(Icons.checkroom_outlined, size: 72),
        const SizedBox(height: 24),
        Text(
          'Un espacio para\nlo que ya tienes',
          textAlign: TextAlign.center,
          style: Theme.of(context).textTheme.headlineMedium,
        ),
        const SizedBox(height: 16),
        const Text(
          'Agrega tu primera prenda para empezar.',
          textAlign: TextAlign.center,
        ),
      ],
      ...items.map(
        (g) => Card(
          margin: const EdgeInsets.only(bottom: 16),
          clipBehavior: Clip.antiAlias,
          child: InkWell(
            onTap: () => Navigator.of(context).push(
              MaterialPageRoute<void>(
                builder: (_) => GarmentDetail(api: widget.api, garment: g),
              ),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                PrivatePhoto(api: widget.api, id: g['photoId'] as String),
                Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        g['name'] as String,
                        style: Theme.of(context).textTheme.titleLarge,
                      ),
                      Text('${g['category']} · ${g['color']}'),
                      Text(
                        g['purchasePrice'] == null
                            ? 'Precio no registrado'
                            : '${(g['purchasePrice'] as num).toStringAsFixed(2)} USD',
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
      if (loading) const Center(child: CircularProgressIndicator()),
      if (more && !loading)
        OutlinedButton(onPressed: load, child: const Text('Ver más prendas')),
    ],
  );
}

class PrivatePhoto extends StatefulWidget {
  final AccessApi api;
  final String id;
  const PrivatePhoto({super.key, required this.api, required this.id});
  @override
  State<PrivatePhoto> createState() => _PrivatePhotoState();
}

class _PrivatePhotoState extends State<PrivatePhoto> {
  late Future<Uint8List> photo;
  @override
  void initState() {
    super.initState();
    photo = widget.api.garmentPhoto(widget.id);
  }

  @override
  void didUpdateWidget(covariant PrivatePhoto oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.id != widget.id) photo = widget.api.garmentPhoto(widget.id);
  }

  @override
  Widget build(BuildContext context) => SizedBox(
    height: 220,
    child: FutureBuilder<Uint8List>(
      future: photo,
      builder: (context, snapshot) {
        if (snapshot.hasError) {
          return Center(
            child: TextButton(
              onPressed: () =>
                  setState(() => photo = widget.api.garmentPhoto(widget.id)),
              child: const Text('Reintentar foto'),
            ),
          );
        }
        if (!snapshot.hasData) {
          return const Center(child: CircularProgressIndicator());
        }
        return Image.memory(
          snapshot.data!,
          fit: BoxFit.contain,
          semanticLabel: 'Foto de la prenda',
          errorBuilder: (_, error, stack) =>
              const Center(child: Text('No se pudo mostrar la foto.')),
        );
      },
    ),
  );
}

class GarmentDetail extends StatelessWidget {
  final AccessApi api;
  final Map<String, dynamic> garment;
  const GarmentDetail({super.key, required this.api, required this.garment});
  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Tu prenda')),
    body: ListView(
      padding: const EdgeInsets.all(24),
      children: [
        PrivatePhoto(api: api, id: garment['photoId'] as String),
        const SizedBox(height: 24),
        Text(
          garment['name'] as String,
          style: Theme.of(context).textTheme.headlineMedium,
        ),
        ListTile(
          title: const Text('Categoría'),
          subtitle: Text(garment['category'] as String),
        ),
        ListTile(
          title: const Text('Color'),
          subtitle: Text(garment['color'] as String),
        ),
        ListTile(
          title: const Text('Conservación'),
          subtitle: Text(garment['condition'] as String),
        ),
        ListTile(
          title: const Text('Marca'),
          subtitle: Text(garment['brand'] as String? ?? 'No registrada'),
        ),
        ListTile(
          title: const Text('Precio de compra'),
          subtitle: Text(
            garment['purchasePrice'] == null
                ? 'No registrado'
                : '${(garment['purchasePrice'] as num).toStringAsFixed(2)} USD',
          ),
        ),
        ListTile(
          title: const Text('Fecha de compra'),
          subtitle: Text(garment['purchaseDate'] as String? ?? 'No registrada'),
        ),
      ],
    ),
  );
}

class GarmentForm extends StatefulWidget {
  final AccessApi api;
  final String timeZone;
  final XFile? recovered;
  const GarmentForm({
    super.key,
    required this.api,
    required this.timeZone,
    this.recovered,
  });
  @override
  State<GarmentForm> createState() => _GarmentFormState();
}

class _GarmentFormState extends State<GarmentForm> {
  final form = GlobalKey<FormState>();
  final name = TextEditingController(),
      color = TextEditingController(),
      brand = TextEditingController(),
      price = TextEditingController();
  String? category, condition, error, photoId;
  DateTime? date;
  Uint8List? photo;
  bool busy = false;
  Map<String, dynamic>? pending;
  late final String id = newId();
  static String newId() {
    final random = Random.secure();
    final bytes = List<int>.generate(16, (_) => random.nextInt(256));
    bytes[6] = (bytes[6] & 15) | 64;
    bytes[8] = (bytes[8] & 63) | 128;
    final h = bytes.map((v) => v.toRadixString(16).padLeft(2, '0')).join();
    return '${h.substring(0, 8)}-${h.substring(8, 12)}-${h.substring(12, 16)}-${h.substring(16, 20)}-${h.substring(20)}';
  }

  @override
  void initState() {
    super.initState();
    if (widget.recovered != null) acceptPhoto(widget.recovered!);
  }

  @override
  void dispose() {
    name.dispose();
    color.dispose();
    brand.dispose();
    price.dispose();
    super.dispose();
  }

  Future<void> acceptPhoto(XFile file) async {
    try {
      if (await file.length() > 8 * 1024 * 1024) {
        throw const AccessError('Selecciona una foto de hasta 8 MB.');
      }
      final bytes = await file.readAsBytes();
      if (mounted) {
        setState(() {
          photo = bytes;
          photoId = null;
          error = null;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(
          () =>
              error = e is AccessError ? e.message : 'No pudimos leer la foto.',
        );
      }
    }
  }

  Future<void> pick(ImageSource source) async {
    setState(() {
      busy = true;
      error = null;
    });
    try {
      final file = await ImagePicker().pickImage(
        source: source,
        maxWidth: 1600,
        maxHeight: 1600,
        imageQuality: 85,
      );
      if (file != null) await acceptPhoto(file);
    } catch (_) {
      if (mounted) {
        setState(
          () => error = 'No pudimos abrir la cámara o la galería. Revisa los permisos e inténtalo de nuevo.',
        );
      }
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  Future<void> save() async {
    if (busy || !form.currentState!.validate()) return;
    if (photo == null) {
      setState(() => error = 'Agrega una foto de la prenda.');
      return;
    }
    setState(() {
      busy = true;
      error = null;
    });
    try {
      photoId ??= await widget.api.uploadPhoto(photo!);
      pending ??= {
        'id': id,
        'photoId': photoId,
        'name': name.text.trim(),
        'category': category,
        'color': color.text.trim(),
        'brand': brand.text.trim(),
        'condition': condition,
        'purchasePrice': price.text.trim().isEmpty
            ? null
            : num.parse(price.text.trim().replaceAll(',', '.')),
        'purchaseDate': date == null
            ? null
            : '${date!.year.toString().padLeft(4, '0')}-${date!.month.toString().padLeft(2, '0')}-${date!.day.toString().padLeft(2, '0')}',
      };
      await widget.api.createGarment(pending!);
      if (mounted) Navigator.pop(context, true);
    } on AccessError catch (e) {
      if (mounted) {
        setState(() {
          error = e.message;
          if (e.status == 400 || e.status == 413) {
            pending = null;
            photoId = null;
          }
        });
      }
    } catch (_) {
      if (mounted) {
        setState(
          () => error = 'No pudimos confirmar el guardado. Reintenta sin cerrar este formulario.',
        );
      }
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  Future<void> chooseDate() async {
    final utc = DateTime.now().toUtc().subtract(
      Duration(hours: widget.timeZone == 'Pacific/Galapagos' ? 6 : 5),
    );
    final today = DateTime(utc.year, utc.month, utc.day);
    final chosen = await showDatePicker(
      context: context,
      initialDate: date ?? today,
      firstDate: DateTime(1900),
      lastDate: today,
      helpText: 'Fecha de compra',
      cancelText: 'Cancelar',
      confirmText: 'Aceptar',
    );
    if (chosen != null && mounted) setState(() => date = chosen);
  }

  @override
  Widget build(BuildContext context) {
    final editable = !busy && pending == null;
    return PopScope(
      canPop: !busy,
      child: Scaffold(
        appBar: AppBar(title: const Text('Agregar prenda')),
        body: Form(
          key: form,
          child: ListView(
            padding: const EdgeInsets.all(24),
            children: [
              const Text(
                'Foto, nombre, categoría, color y conservación son obligatorios.',
              ),
              const SizedBox(height: 16),
              if (photo != null)
                Image.memory(
                  photo!,
                  height: 220,
                  fit: BoxFit.contain,
                  errorBuilder: (_, error, stack) => const Text(
                    'Formato no compatible. Selecciona JPEG o PNG.',
                  ),
                ),
              Wrap(
                spacing: 12,
                children: [
                  OutlinedButton.icon(
                    onPressed: editable
                        ? () => pick(ImageSource.gallery)
                        : null,
                    icon: const Icon(Icons.photo_library_outlined),
                    label: const Text('Galería'),
                  ),
                  OutlinedButton.icon(
                    onPressed: editable ? () => pick(ImageSource.camera) : null,
                    icon: const Icon(Icons.camera_alt_outlined),
                    label: const Text('Cámara'),
                  ),
                ],
              ),
              const Text('JPEG o PNG · Hasta 8 MB'),
              const SizedBox(height: 20),
              TextFormField(
                controller: name,
                enabled: editable,
                maxLength: 100,
                decoration: const InputDecoration(labelText: 'Nombre'),
                validator: (v) =>
                    v == null || v.trim().isEmpty ? 'Escribe el nombre.' : null,
              ),
              const SizedBox(height: 16),
              DropdownButtonFormField<String>(
                initialValue: category,
                decoration: const InputDecoration(labelText: 'Categoría'),
                items: categories
                    .map((v) => DropdownMenuItem(value: v, child: Text(v)))
                    .toList(),
                onChanged: editable
                    ? (v) => setState(() => category = v)
                    : null,
                validator: (v) =>
                    v == null ? 'Selecciona una categoría.' : null,
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: color,
                enabled: editable,
                maxLength: 40,
                decoration: const InputDecoration(
                  labelText: 'Color',
                  hintText: 'Ej. azul, multicolor u otro',
                ),
                validator: (v) =>
                    v == null || v.trim().isEmpty ? 'Indica el color.' : null,
              ),
              const SizedBox(height: 16),
              DropdownButtonFormField<String>(
                initialValue: condition,
                decoration: const InputDecoration(labelText: 'Conservación'),
                items: conditions
                    .map((v) => DropdownMenuItem(value: v, child: Text(v)))
                    .toList(),
                onChanged: editable
                    ? (v) => setState(() => condition = v)
                    : null,
                validator: (v) =>
                    v == null ? 'Selecciona la conservación.' : null,
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: brand,
                enabled: editable,
                maxLength: 100,
                decoration: const InputDecoration(
                  labelText: 'Marca (opcional)',
                ),
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: price,
                enabled: editable,
                keyboardType: const TextInputType.numberWithOptions(
                  decimal: true,
                ),
                decoration: const InputDecoration(
                  labelText: 'Precio de compra en USD (opcional)',
                ),
                validator: (v) {
                  final text = v?.trim() ?? '';
                  if (text.isEmpty) return null;
                  return RegExp(r'^\d{1,10}([.,]\d{1,2})?$').hasMatch(text)
                      ? null
                      : 'Indica un precio válido con hasta dos decimales.';
                },
              ),
              const SizedBox(height: 16),
              OutlinedButton(
                onPressed: editable ? chooseDate : null,
                child: Text(
                  date == null
                      ? 'Fecha de compra (opcional)'
                      : '${date!.day}/${date!.month}/${date!.year}',
                ),
              ),
              if (date != null)
                TextButton(
                  onPressed: editable
                      ? () => setState(() => date = null)
                      : null,
                  child: const Text('Quitar fecha'),
                ),
              if (error != null)
                Padding(
                  padding: const EdgeInsets.symmetric(vertical: 16),
                  child: Semantics(liveRegion: true, child: Text(error!)),
                ),
              if (pending != null && !busy)
                const Text(
                  'Conservamos los datos para reintentar sin duplicar tu prenda.',
                ),
              const SizedBox(height: 24),
              FilledButton(
                onPressed: busy ? null : save,
                child: Text(busy ? 'Guardando…' : 'Guardar prenda'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
