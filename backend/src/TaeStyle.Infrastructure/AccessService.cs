using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using TaeStyle.Application;
using TaeStyle.Domain;
namespace TaeStyle.Infrastructure;

public sealed class JwtOptions
{
    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "TaeStyle";
    public string Audience { get; set; } = "TaeStyle.Mobile";
}
public sealed class AccessService(AppDbContext db, UserManager<Account> users, IOptions<JwtOptions> jwt, TimeProvider clock) : IAccessService
{
    private static AppException InvalidSession() => new(401, "invalid_session", "Inicia sesión nuevamente.");
    private static AppException InvalidCredentials() => new(401, "invalid_credentials", "El correo o la contraseña no son correctos.");
    private static UserDto Dto(Account a) => new(a.Id, a.Email!, a.Currency, a.TimeZone);
    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    public async Task<UserDto> Register(RegisterCommand command, CancellationToken ct)
    {
        var email = AccessValidation.Email(command.Email);
        var account = new Account { Id = Guid.NewGuid(), UserName = email, Email = email, Currency = command.Currency, TimeZone = command.TimeZone };
        IdentityResult result;
        try { result = await users.CreateAsync(account, command.Password); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        { throw new AppException(409, "account_already_exists", "Ya existe una cuenta con este correo."); }
        if (!result.Succeeded)
        {
            if (result.Errors.Any(x => x.Code.StartsWith("Duplicate"))) throw new AppException(409, "account_already_exists", "Ya existe una cuenta con este correo.");
            throw new AppException(400, "validation_failed", "No se pudo crear la cuenta con estos datos.");
        }
        return Dto(account);
    }
    public async Task<TokensDto> Login(LoginCommand command, CancellationToken ct)
    {
        var account = await users.FindByEmailAsync(command.Email.Trim());
        if (account is null || await users.IsLockedOutAsync(account)) throw InvalidCredentials();
        if (!await users.CheckPasswordAsync(account, command.Password))
        { await users.AccessFailedAsync(account); throw InvalidCredentials(); }
        await users.ResetAccessFailedCountAsync(account);
        var tokens = Issue(account, Guid.NewGuid(), clock.GetUtcNow().AddDays(30));
        await db.SaveChangesAsync(ct);
        return tokens;
    }
    public async Task<TokensDto> Refresh(RefreshCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken) || command.RefreshToken.Length > 256) throw InvalidSession();
        var hash = Hash(command.RefreshToken);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var session = await db.Sessions.SingleOrDefaultAsync(x => x.TokenHash == hash, ct);
        if (session is null) throw InvalidSession();
        // Serialize all mutations of a token family, including reuse and logout.
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({session.FamilyId.ToString()}, 0))", ct);
        await db.Entry(session).ReloadAsync(ct);
        if (session.RevokedAt is not null)
        { await Revoke(session.FamilyId, ct); await transaction.CommitAsync(ct); throw InvalidSession(); }
        if (session.ExpiresAt <= clock.GetUtcNow()) throw InvalidSession();
        var account = await db.Users.SingleAsync(x => x.Id == session.UserId, ct);
        session.RevokedAt = clock.GetUtcNow();
        var result = Issue(account, session.FamilyId, session.ExpiresAt);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return result;
    }
    public async Task Logout(RefreshCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken) || command.RefreshToken.Length > 256) throw new AppException(400, "validation_failed", "Falta una sesión válida.");
        var hash = Hash(command.RefreshToken);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var session = await db.Sessions.AsNoTracking().SingleOrDefaultAsync(x => x.TokenHash == hash, ct);
        if (session is not null)
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({session.FamilyId.ToString()}, 0))", ct);
            await Revoke(session.FamilyId, ct);
        }
        await transaction.CommitAsync(ct);
    }
    private Task<int> Revoke(Guid family, CancellationToken ct) => db.Sessions.Where(x => x.FamilyId == family && x.RevokedAt == null)
        .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAt, clock.GetUtcNow()), ct);
    public async Task<UserDto> Current(CurrentUserQuery query, CancellationToken ct)
    {
        var account = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == query.UserId, ct);
        return account is null ? throw InvalidSession() : Dto(account);
    }
    private TokensDto Issue(Account account, Guid family, DateTimeOffset expires)
    {
        // PostgreSQL stores microseconds; return exactly the precision persisted.
        expires = new DateTimeOffset(expires.Ticks - expires.Ticks % 10, expires.Offset);
        var now = clock.GetUtcNow();
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var session = new Session { UserId = account.Id, FamilyId = family, TokenHash = Hash(raw), ExpiresAt = expires };
        db.Sessions.Add(session);
        var accessExpiry = now.AddMinutes(15);
        var claims = new[] { new Claim("sub", account.Id.ToString()), new Claim("sid", session.Id.ToString()), new Claim("jti", Guid.NewGuid().ToString()), new Claim("sv", account.SecurityVersion.ToString()) };
        var token = new JwtSecurityToken(jwt.Value.Issuer, jwt.Value.Audience, claims, now.UtcDateTime, accessExpiry.UtcDateTime,
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Value.Key)), SecurityAlgorithms.HmacSha256));
        return new(new JwtSecurityTokenHandler().WriteToken(token), raw, "Bearer", accessExpiry, expires);
    }
}
