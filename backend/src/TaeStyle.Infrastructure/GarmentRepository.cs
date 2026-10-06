using Microsoft.EntityFrameworkCore;
using TaeStyle.Application;
using TaeStyle.Domain;

namespace TaeStyle.Infrastructure;

public sealed class GarmentRepository(AppDbContext db) : IGarmentRepository
{
    private static readonly System.Linq.Expressions.Expression<Func<Garment, GarmentDto>> Projection = g =>
        new(g.Id, g.PhotoId, g.Name, g.Category, g.Color, g.Brand, g.PurchasePrice, g.PurchaseDate, g.Condition, g.Archived, g.CreatedAt);
    public async Task<DateOnly> Today(Guid owner, DateTimeOffset now, CancellationToken ct)
    {
        var zone = await db.Users.Where(x => x.Id == owner).Select(x => x.TimeZone).SingleAsync(ct);
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, TimeZoneInfo.FindSystemTimeZoneById(zone)).DateTime);
    }
    public async Task<GarmentDto> Create(Guid owner, CreateGarmentCommand c, DateTimeOffset now, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // Serializes mutations for this owner, including idempotent retries and photo linking.
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({BitConverter.ToInt64(owner.ToByteArray())})", ct);
        var existing = await db.Garments.Where(x => x.Id == c.Id && x.UserId == owner).Select(Projection).SingleOrDefaultAsync(ct);
        if (existing != null)
        {
            if (existing.PhotoId != c.PhotoId || existing.Name != c.Name || existing.Category != c.Category || existing.Color != c.Color || existing.Brand != c.Brand || existing.PurchasePrice != c.PurchasePrice || existing.PurchaseDate != c.PurchaseDate || existing.Condition != c.Condition)
                throw new AppException(409, "request_conflict", "La solicitud ya se utilizó con otros datos.");
            return existing;
        }
        if (await db.Garments.AnyAsync(x => x.Id == c.Id, ct))
            throw new AppException(409, "request_conflict", "No se puede usar ese identificador de solicitud.");
        var photo = await db.GarmentPhotos.SingleOrDefaultAsync(x => x.Id == c.PhotoId && x.UserId == owner && x.ExpiresAt > now, ct)
            ?? throw new AppException(400, "invalid_photo", "La foto no está disponible. Vuelve a seleccionarla.");
        photo.ExpiresAt = null;
        db.Garments.Add(new Garment { Id = c.Id, UserId = owner, PhotoId = c.PhotoId, Name = c.Name, Category = c.Category, Color = c.Color, Brand = c.Brand, PurchasePrice = c.PurchasePrice, PurchaseDate = c.PurchaseDate, Condition = c.Condition, CreatedAt = now });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return await Get(owner, c.Id, ct);
    }
    public async Task<GarmentPage> List(Guid owner, int page, CancellationToken ct)
    {
        if (page < 1 || page > 100000) throw new AppException(400, "validation_failed", "Página inválida.");
        var items = await db.Garments.AsNoTracking().Where(x => x.UserId == owner)
            .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).Skip((page - 1) * 20).Take(21).Select(Projection).ToListAsync(ct);
        return new(items.Take(20).ToArray(), page, items.Count > 20);
    }
    public async Task<GarmentDto> Get(Guid owner, Guid id, CancellationToken ct) =>
        await db.Garments.Where(x => x.Id == id && x.UserId == owner).Select(Projection).SingleOrDefaultAsync(ct)
        ?? throw new AppException(404, "not_found", "No se encontró la prenda.");
    public async Task AddPhoto(GarmentPhoto photo, CancellationToken ct)
    { db.GarmentPhotos.Add(photo); await db.SaveChangesAsync(ct); }
    public async Task<byte[]> Photo(Guid owner, Guid id, DateTimeOffset now, CancellationToken ct) =>
        await db.GarmentPhotos.Where(x => x.Id == id && x.UserId == owner && (x.ExpiresAt == null || x.ExpiresAt > now)).Select(x => x.Content).SingleOrDefaultAsync(ct)
        ?? throw new AppException(404, "not_found", "No se encontró la foto.");
    public Task CleanPhotos(DateTimeOffset now, CancellationToken ct) =>
        db.GarmentPhotos.Where(x => x.ExpiresAt < now).ExecuteDeleteAsync(ct);
}
