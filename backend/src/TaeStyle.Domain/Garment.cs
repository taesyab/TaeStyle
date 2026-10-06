namespace TaeStyle.Domain;

public sealed class Garment
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid PhotoId { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Color { get; set; } = "";
    public string? Brand { get; set; }
    public decimal? PurchasePrice { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public string Condition { get; set; } = "";
    public bool Archived { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class GarmentPhoto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public byte[] Content { get; set; } = [];
    // Null only once linked to a garment. Expired temporary uploads are removed.
    public DateTimeOffset? ExpiresAt { get; set; }
}
