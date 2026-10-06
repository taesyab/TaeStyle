using TaeStyle.Domain;

namespace TaeStyle.Application;

public sealed record CreateGarmentCommand(Guid Id, Guid PhotoId, string Name, string Category,
    string Color, string? Brand, decimal? PurchasePrice, DateOnly? PurchaseDate, string Condition);
public sealed record GarmentDto(Guid Id, Guid PhotoId, string Name, string Category, string Color,
    string? Brand, decimal? PurchasePrice, DateOnly? PurchaseDate, string Condition, bool Archived, DateTimeOffset CreatedAt);
public sealed record GarmentPage(IReadOnlyList<GarmentDto> Items, int Page, bool HasMore);
public sealed record PhotoDto(Guid Id);

public interface IGarmentRepository
{
    Task<GarmentDto> Create(Guid owner, CreateGarmentCommand command, DateTimeOffset now, CancellationToken ct);
    Task<GarmentPage> List(Guid owner, int page, CancellationToken ct);
    Task<GarmentDto> Get(Guid owner, Guid id, CancellationToken ct);
    Task<DateOnly> Today(Guid owner, DateTimeOffset now, CancellationToken ct);
    Task AddPhoto(GarmentPhoto photo, CancellationToken ct);
    Task<byte[]> Photo(Guid owner, Guid id, DateTimeOffset now, CancellationToken ct);
    Task CleanPhotos(DateTimeOffset now, CancellationToken ct);
}
public interface IPhotoProcessor { byte[] Process(byte[] content); }

public static class GarmentValidation
{
    public static readonly string[] Categories = ["Blusa", "Camisa", "Pantalón", "Jean", "Vestido", "Chaqueta", "Abrigo", "Falda", "Zapatos", "Accesorios"];
    public static readonly string[] Conditions = ["Excelente", "Bueno", "Regular"];
    public static CreateGarmentCommand Validate(CreateGarmentCommand c, DateOnly today)
    {
        if (c.Id == Guid.Empty || c.PhotoId == Guid.Empty) Fail("Selecciona una foto.");
        if (string.IsNullOrWhiteSpace(c.Name) || c.Name.Trim().Length > 100) Fail("El nombre es obligatorio y admite hasta 100 caracteres.");
        if (!Categories.Contains(c.Category)) Fail("Selecciona una categoría válida.");
        if (string.IsNullOrWhiteSpace(c.Color) || c.Color.Trim().Length > 40) Fail("Indica un color de hasta 40 caracteres.");
        if (c.Brand?.Trim().Length > 100) Fail("La marca admite hasta 100 caracteres.");
        if (!Conditions.Contains(c.Condition)) Fail("Selecciona el estado de conservación.");
        if (c.PurchasePrice is decimal price && (price < 0 || price > 9999999999.99m || decimal.Round(price, 2) != price))
            Fail("El precio debe ser positivo o cero y tener como máximo dos decimales.");
        if (c.PurchaseDate > today) Fail("La fecha de compra no puede ser futura.");
        return c with { Name = c.Name.Trim(), Color = c.Color.Trim(), Brand = string.IsNullOrWhiteSpace(c.Brand) ? null : c.Brand.Trim() };
    }
    private static void Fail(string message) => throw new AppException(400, "validation_failed", message);
}
public sealed class CreateGarmentHandler(IGarmentRepository repository, TimeProvider clock)
{
    public async Task<GarmentDto> Handle(Guid owner, CreateGarmentCommand command, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        command = GarmentValidation.Validate(command, await repository.Today(owner, now, ct));
        return await repository.Create(owner, command, now, ct);
    }
}
public sealed class UploadPhotoHandler(IGarmentRepository repository, IPhotoProcessor processor, TimeProvider clock)
{
    public async Task<PhotoDto> Handle(Guid owner, byte[] content, CancellationToken ct)
    {
        if (content.Length > 8 * 1024 * 1024) throw new AppException(413, "photo_too_large", "La foto debe pesar como máximo 8 MB.");
        var photo = new GarmentPhoto { Id = Guid.NewGuid(), UserId = owner, Content = processor.Process(content), ExpiresAt = clock.GetUtcNow().AddHours(24) };
        await repository.AddPhoto(photo, ct);
        return new(photo.Id);
    }
}
