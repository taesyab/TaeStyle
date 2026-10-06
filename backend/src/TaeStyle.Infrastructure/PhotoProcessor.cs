using SkiaSharp;
using TaeStyle.Application;

namespace TaeStyle.Infrastructure;

public sealed class PhotoProcessor : IPhotoProcessor
{
    public byte[] Process(byte[] content)
    {
        using var data = SKData.CreateCopy(content);
        using var codec = SKCodec.Create(data);
        if (codec == null || codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png))
            throw Invalid("Selecciona una imagen JPEG o PNG válida.");
        var w = codec.Info.Width;
        var h = codec.Info.Height;
        if (w < 1 || h < 1 || w > 8192 || h > 8192 || (long)w * h > 24000000)
            throw Invalid("La foto admite hasta 24 megapíxeles y 8192 píxeles por lado.");
        using var bitmap = new SKBitmap(new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Premul));
        if (codec.GetPixels(bitmap.Info, bitmap.GetPixels()) != SKCodecResult.Success)
            throw Invalid("No se pudo leer la foto completa. Selecciona otra imagen.");
        var origin = (int)codec.EncodedOrigin;
        var orientedW = origin >= 5 ? h : w;
        var orientedH = origin >= 5 ? w : h;
        var scale = Math.Min(1f, 1600f / Math.Max(orientedW, orientedH));
        using var output = new SKBitmap(Math.Max(1, (int)(orientedW * scale)), Math.Max(1, (int)(orientedH * scale)));
        using (var canvas = new SKCanvas(output))
        {
            canvas.Clear(SKColors.White);
            canvas.Scale(scale);
            switch (origin)
            {
                case 2: canvas.Translate(w, 0); canvas.Scale(-1, 1); break;
                case 3: canvas.Translate(w, h); canvas.RotateDegrees(180); break;
                case 4: canvas.Translate(0, h); canvas.Scale(1, -1); break;
                case 5: canvas.RotateDegrees(90); canvas.Scale(1, -1); break;
                case 6: canvas.Translate(h, 0); canvas.RotateDegrees(90); break;
                case 7: canvas.Translate(h, w); canvas.RotateDegrees(90); canvas.Scale(-1, 1); break;
                case 8: canvas.Translate(0, w); canvas.RotateDegrees(270); break;
            }
            canvas.DrawBitmap(bitmap, 0, 0, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None));
        }
        // Encoding new pixels drops EXIF/location and all source metadata.
        using var image = SKImage.FromBitmap(output);
        using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, 85);
        return encoded.ToArray();
    }
    private static AppException Invalid(string message) => new(400, "invalid_photo", message);
}
