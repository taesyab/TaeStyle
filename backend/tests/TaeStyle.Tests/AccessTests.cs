using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaeStyle.Application;
using TaeStyle.Infrastructure;

namespace TaeStyle.Tests;

public class ValidationTests
{
    [Theory]
    [InlineData("short", "USD", "America/Guayaquil")]
    [InlineData("a long password", "EUR", "America/Guayaquil")]
    [InlineData("a long password", "USD", "Unknown")]
    public void Invalid_registration_is_rejected(string password, string currency, string zone) =>
        Assert.Throws<AppException>(() => AccessValidation.Register(new("person@example.com", password, currency, zone)));
    [Theory]
    [InlineData("America/Guayaquil")]
    [InlineData("Pacific/Galapagos")]
    public void Pilot_zones_are_accepted(string zone) => AccessValidation.Register(new("person@example.com", "a long password", "USD", zone));
}

public class AccessTests
{
    [Fact]
    public async Task Real_postgres_access_lifecycle_and_isolation()
    {
        var connection = Environment.GetEnvironmentVariable("TAESTYLE_TEST_DATABASE")
            ?? throw new InvalidOperationException("Set TAESTYLE_TEST_DATABASE to a disposable PostgreSQL database.");
        await using var factory = new AccessFactory(connection);
        using var client = factory.CreateClient();
        await using (var scope = factory.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
        var email = $"test-{Guid.NewGuid():N}@example.com";
        const string password = "Only for tests 123";
        var registered = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand(email, password, "USD", "America/Guayaquil"));
        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);
        var user = await registered.Content.ReadFromJsonAsync<UserDto>();
        var duplicate = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand(email.ToUpperInvariant(), password, "USD", "America/Guayaquil"));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, "wrong"))).StatusCode);
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, password));
        response.EnsureSuccessStatusCode();
        var first = (await response.Content.ReadFromJsonAsync<TokensDto>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", first.AccessToken);
        var me = await client.GetFromJsonAsync<UserDto>("/api/v1/me");
        Assert.Equal(user!.Id, me!.Id);
        var otherEmail = $"test-{Guid.NewGuid():N}@example.com";
        await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterCommand(otherEmail, password, "USD", "Pacific/Galapagos"));
        var other = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(otherEmail, password));
        var otherTokens = (await other.Content.ReadFromJsonAsync<TokensDto>())!;
        client.DefaultRequestHeaders.Authorization = new("Bearer", otherTokens.AccessToken);
        Assert.NotEqual(user.Id, (await client.GetFromJsonAsync<UserDto>("/api/v1/me"))!.Id);
        var refreshed = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshCommand(first.RefreshToken));
        refreshed.EnsureSuccessStatusCode();
        var second = (await refreshed.Content.ReadFromJsonAsync<TokensDto>())!;
        Assert.NotEqual(first.RefreshToken, second.RefreshToken);
        Assert.Equal(first.RefreshTokenExpiresAt, second.RefreshTokenExpiresAt);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshCommand(first.RefreshToken))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshCommand(second.RefreshToken))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/v1/auth/logout", new RefreshCommand(otherTokens.RefreshToken))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshCommand(otherTokens.RefreshToken))).StatusCode);
        await using var finalScope = factory.Services.CreateAsyncScope();
        var db = finalScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var account = await db.Users.SingleAsync(x => x.Id == user.Id);
        account.SecurityVersion++;
        await db.SaveChangesAsync();
        client.DefaultRequestHeaders.Authorization = new("Bearer", first.AccessToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/me")).StatusCode);
    }
}
public sealed class AccessFactory(string connection) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Database"] = connection,
            ["Jwt:Key"] = "Test-only-signing-key-with-at-least-32-bytes",
            ["Logging:LogLevel:Default"] = "Warning"
        }));
    }
}
