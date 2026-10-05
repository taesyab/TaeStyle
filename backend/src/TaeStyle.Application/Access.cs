using System.ComponentModel.DataAnnotations;

namespace TaeStyle.Application;

public sealed record RegisterCommand(string Email, string Password, string Currency, string TimeZone);
public sealed record LoginCommand(string Email, string Password);
public sealed record RefreshCommand(string RefreshToken);
public sealed record CurrentUserQuery(Guid UserId);
public sealed record UserDto(Guid Id, string Email, string Currency, string TimeZone);
public sealed record TokensDto(string AccessToken, string RefreshToken, string TokenType,
    DateTimeOffset AccessTokenExpiresAt, DateTimeOffset RefreshTokenExpiresAt);

public sealed class AppException(int status, string code, string message) : Exception(message)
{
    public int Status { get; } = status;
    public string Code { get; } = code;
}

public static class AccessValidation
{
    public static string Email(string? email)
    {
        var value = email?.Trim() ?? "";
        if (value.Length > 254 || !new EmailAddressAttribute().IsValid(value))
            throw new AppException(400, "validation_failed", "Escribe un correo electrónico válido.");
        return value;
    }
    public static void Register(RegisterCommand command)
    {
        Email(command.Email);
        if (command.Password is null || command.Password.Length is < 12 or > 128)
            throw new AppException(400, "validation_failed", "La contraseña debe tener entre 12 y 128 caracteres.");
        if (command.Currency != "USD")
            throw new AppException(400, "validation_failed", "La moneda del piloto es USD.");
        if (command.TimeZone is not ("America/Guayaquil" or "Pacific/Galapagos"))
            throw new AppException(400, "validation_failed", "Selecciona una zona horaria de Ecuador.");
    }
}

public interface IAccessService
{
    Task<UserDto> Register(RegisterCommand command, CancellationToken ct);
    Task<TokensDto> Login(LoginCommand command, CancellationToken ct);
    Task<TokensDto> Refresh(RefreshCommand command, CancellationToken ct);
    Task Logout(RefreshCommand command, CancellationToken ct);
    Task<UserDto> Current(CurrentUserQuery query, CancellationToken ct);
}

// A single explicit handler per operation keeps CQRS simple without a mediator dependency.
public sealed class RegisterHandler(IAccessService access)
{
    public Task<UserDto> Handle(RegisterCommand command, CancellationToken ct)
    { AccessValidation.Register(command); return access.Register(command, ct); }
}
public sealed class LoginHandler(IAccessService access)
{
    public Task<TokensDto> Handle(LoginCommand command, CancellationToken ct)
    {
        AccessValidation.Email(command.Email);
        if (string.IsNullOrEmpty(command.Password) || command.Password.Length > 128)
            throw new AppException(401, "invalid_credentials", "El correo o la contraseña no son correctos.");
        return access.Login(command, ct);
    }
}
