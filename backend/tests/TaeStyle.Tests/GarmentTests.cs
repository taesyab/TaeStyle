using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using TaeStyle.Application;
using TaeStyle.Infrastructure;

namespace TaeStyle.Tests;

public class GarmentTests
{
    private static byte[] Image()
    {
        using var bitmap = new SKBitmap(32, 48);
        bitmap.Erase(SKColors.Plum);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    [Fact]
    public void Photo_is_decoded_and_reencoded_and_invalid_files_rejected()
    {
        var processor = new PhotoProcessor();
        var converted = processor.Process(Image());
        using var data = SKData.CreateCopy(converted);
        using var codec = SKCodec.Create(data);
        Assert.Equal(SKEncodedImageFormat.Jpeg, codec.EncodedFormat);
        Assert.Equal(32, codec.Info.Width);
        Assert.Equal(48, codec.Info.Height);
        Assert.Throws<AppException>(() => processor.Process([1, 2, 3]));
        Assert.Throws<AppException>(() => processor.Process(Image()[..30]));
    }

    [Fact]
    public void Purchase_fields_preserve_unknown_and_reject_invalid_values()
    {
        var today = new DateOnly(2026, 10, 6);
        var c = new CreateGarmentCommand(Guid.NewGuid(), Guid.NewGuid(), " Blusa ", "Blusa", " Azul ", " ", null, null, "Bueno");
        var clean = GarmentValidation.Validate(c, today);
        Assert.Null(clean.PurchasePrice); Assert.Null(clean.PurchaseDate); Assert.Null(clean.Brand);
        Assert.Equal("Blusa", clean.Name);
        Assert.Throws<AppException>(() => GarmentValidation.Validate(c with { PurchasePrice = -1 }, today));
        Assert.Throws<AppException>(() => GarmentValidation.Validate(c with { PurchasePrice = 1.001m }, today));
        Assert.Throws<AppException>(() => GarmentValidation.Validate(c with { PurchaseDate = today.AddDays(1) }, today));
        Assert.Throws<AppException>(() => GarmentValidation.Validate(c with { Category = "Invalid" }, today));
        Assert.Equal(0, GarmentValidation.Validate(c with { PurchasePrice = 0 }, today).PurchasePrice);
    }

    [Fact]
    public async Task Garments_and_photos_are_private_and_creation_is_idempotent()
    {
        var connection = Environment.GetEnvironmentVariable("TAESTYLE_TEST_DATABASE") ?? throw new InvalidOperationException("Set TAESTYLE_TEST_DATABASE.");
        await using var factory = new AccessFactory(connection);
        using var client = factory.CreateClient();
        await using (var scope = factory.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/garments")).StatusCode);
        async Task<string> Login()
        {
            var email = $"garment-{Guid.NewGuid():N}@example.com";
            (await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand(email, "A test password", "USD", "America/Guayaquil"))).EnsureSuccessStatusCode();
            var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, "A test password"));
            return (await login.Content.ReadFromJsonAsync<TokensDto>())!.AccessToken;
        }
        var owner = await Login();
        var other = await Login();
        client.DefaultRequestHeaders.Authorization = new("Bearer", owner);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/v1/garment-photos", new ByteArrayContent([1, 2, 3]))).StatusCode);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, (await client.PostAsync("/api/v1/garment-photos", new ByteArrayContent(new byte[8 * 1024 * 1024 + 1]))).StatusCode);
        var upload = await client.PostAsync("/api/v1/garment-photos", new ByteArrayContent(Image()));
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        var photo = (await upload.Content.ReadFromJsonAsync<PhotoDto>())!;
        var command = new CreateGarmentCommand(Guid.NewGuid(), photo.Id, "Mi blusa", "Blusa", "Azul", null, null, null, "Excelente");
        client.DefaultRequestHeaders.Authorization = new("Bearer", other);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/garment-photos/{photo.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/garments", command)).StatusCode);
        client.DefaultRequestHeaders.Authorization = new("Bearer", owner);
        var responses = await Task.WhenAll(client.PostAsJsonAsync("/api/v1/garments", command), client.PostAsJsonAsync("/api/v1/garments", command));
        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/v1/garments", command with { Name = "Otro nombre" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/garments", command with { Id = Guid.NewGuid() })).StatusCode);
        var list = (await client.GetFromJsonAsync<GarmentPage>("/api/v1/garments"))!;
        Assert.Single(list.Items); Assert.Null(list.Items[0].PurchasePrice); Assert.False(list.HasMore);
        var detail = (await client.GetFromJsonAsync<GarmentDto>($"/api/v1/garments/{command.Id}"))!;
        Assert.Equal(command.Name, detail.Name);
        var jpeg = await client.GetAsync($"/api/v1/garment-photos/{photo.Id}");
        Assert.Equal("image/jpeg", jpeg.Content.Headers.ContentType!.MediaType);
        Assert.Contains("no-store", jpeg.Headers.CacheControl!.ToString());
        client.DefaultRequestHeaders.Authorization = new("Bearer", other);
        Assert.Empty((await client.GetFromJsonAsync<GarmentPage>("/api/v1/garments"))!.Items);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/garments/{command.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/garment-photos/{photo.Id}")).StatusCode);
        await using var finalScope = factory.Services.CreateAsyncScope();
        var repository = finalScope.ServiceProvider.GetRequiredService<IGarmentRepository>();
        var db = finalScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var linked = await db.GarmentPhotos.SingleAsync(x => x.Id == photo.Id);
        var temporary = new TaeStyle.Domain.GarmentPhoto { Id = Guid.NewGuid(), UserId = linked.UserId, Content = Image(), ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1) };
        await repository.AddPhoto(temporary, default);
        await repository.CleanPhotos(DateTimeOffset.UtcNow, default);
        Assert.False(await db.GarmentPhotos.AnyAsync(x => x.Id == temporary.Id));
        Assert.True(await db.GarmentPhotos.AnyAsync(x => x.Id == photo.Id));
    }
}
