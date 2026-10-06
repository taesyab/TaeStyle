using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TaeStyle.Application;
using TaeStyle.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOptions<JwtOptions>().BindConfiguration("Jwt")
    .Validate(o => Encoding.UTF8.GetByteCount(o.Key) >= 32, "Configure Jwt__Key with at least 32 random bytes.").ValidateOnStart();
builder.Services.AddDbContext<AppDbContext>((sp, o) => o.UseNpgsql(
    sp.GetRequiredService<IConfiguration>().GetConnectionString("Database")
        ?? throw new InvalidOperationException("Configure ConnectionStrings__Database.")));
builder.Services.AddIdentityCore<Account>(o =>
{
    o.User.RequireUniqueEmail = true; o.Password.RequiredLength = 12;
    o.Password.RequireDigit = o.Password.RequireLowercase = o.Password.RequireUppercase = o.Password.RequireNonAlphanumeric = false;
    o.Lockout.MaxFailedAccessAttempts = 5; o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
}).AddEntityFrameworkStores<AppDbContext>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IAccessService, AccessService>();
builder.Services.AddScoped<RegisterHandler>(); builder.Services.AddScoped<LoginHandler>();
builder.Services.AddScoped<IGarmentRepository, GarmentRepository>();
builder.Services.AddSingleton<IPhotoProcessor, PhotoProcessor>();
builder.Services.AddScoped<CreateGarmentHandler>();
builder.Services.AddScoped<UploadPhotoHandler>();
builder.Services.AddHostedService<PhotoCleanup>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new();
    o.MapInboundClaims = false;
    o.TokenValidationParameters = new()
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
        ValidateIssuer = true,
        ValidIssuer = jwt.Issuer,
        ValidateAudience = true,
        ValidAudience = jwt.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(10),
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
    };
    o.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            if (!Guid.TryParse(context.Principal?.FindFirstValue("sub"), out var id) || !int.TryParse(context.Principal?.FindFirstValue("sv"), out var version)) { context.Fail("Invalid session"); return; }
            var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            if (!await db.Users.AnyAsync(x => x.Id == id && x.SecurityVersion == version, context.HttpContext.RequestAborted)) context.Fail("Invalid session");
        }
    };
});
builder.Services.AddAuthorization(); builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer(); builder.Services.AddSwaggerGen();
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("access", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "60";
        await Results.Problem(statusCode: 429, title: "Espera antes de intentarlo de nuevo.", extensions: new Dictionary<string, object?> { ["code"] = "rate_limited" }).ExecuteAsync(context.HttpContext);
    };
});
var app = builder.Build();
app.Use(async (context, next) =>
{
    context.Response.Headers.CacheControl = "no-store";
    try { await next(context); }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
        var known = ex as AppException;
        var status = known?.Status ?? (ex is BadHttpRequestException bad ? bad.StatusCode : 500);
        if (status == 500) app.Logger.LogError("Request failed. Trace {TraceId}; type {Type}", context.TraceIdentifier, ex.GetType().Name);
        await Results.Problem(statusCode: status, title: known?.Message ?? "No se pudo completar la solicitud.", extensions: new Dictionary<string, object?> { ["code"] = known?.Code ?? (status == 400 ? "validation_failed" : "unexpected_error"), ["traceId"] = context.TraceIdentifier }).ExecuteAsync(context);
    }
});
app.UseStatusCodePages(async status =>
{
    var code = status.HttpContext.Response.StatusCode;
    await Results.Problem(statusCode: code, extensions: new Dictionary<string, object?> { ["code"] = code == 401 ? "invalid_session" : "request_failed", ["traceId"] = status.HttpContext.TraceIdentifier }).ExecuteAsync(status.HttpContext);
});
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing")) app.UseHttpsRedirection();
app.UseRateLimiter(); app.UseAuthentication(); app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
var auth = app.MapGroup("/api/v1/auth").RequireRateLimiting("access");
auth.MapPost("/register", async (RegisterCommand command, RegisterHandler handler, CancellationToken ct) => Results.Json(await handler.Handle(command, ct), statusCode: 201));
auth.MapPost("/login", async (LoginCommand command, LoginHandler handler, CancellationToken ct) => Results.Ok(await handler.Handle(command, ct)));
auth.MapPost("/refresh", async (RefreshCommand command, IAccessService service, CancellationToken ct) => Results.Ok(await service.Refresh(command, ct)));
auth.MapPost("/logout", async (RefreshCommand command, IAccessService service, CancellationToken ct) => { await service.Logout(command, ct); return Results.NoContent(); });
app.MapGet("/api/v1/me", async (ClaimsPrincipal user, IAccessService service, CancellationToken ct) => Results.Ok(await service.Current(new(Guid.Parse(user.FindFirstValue("sub")!)), ct))).RequireAuthorization();
var closet = app.MapGroup("/api/v1").RequireAuthorization();
closet.MapGet("/garments", async (ClaimsPrincipal user, IGarmentRepository repository, CancellationToken ct, int page = 1) =>
    Results.Ok(await repository.List(Guid.Parse(user.FindFirstValue("sub")!), page, ct)));
closet.MapGet("/garments/{id:guid}", async (Guid id, ClaimsPrincipal user, IGarmentRepository repository, CancellationToken ct) =>
    Results.Ok(await repository.Get(Guid.Parse(user.FindFirstValue("sub")!), id, ct)));
closet.MapPost("/garments", async (CreateGarmentCommand command, ClaimsPrincipal user, CreateGarmentHandler handler, CancellationToken ct) =>
{
    var result = await handler.Handle(Guid.Parse(user.FindFirstValue("sub")!), command, ct);
    return Results.Created($"/api/v1/garments/{result.Id}", result);
});
// Raw bytes avoid multipart buffering and keep the upload bounded before decoding.
closet.MapPost("/garment-photos", async (HttpRequest request, ClaimsPrincipal user, UploadPhotoHandler handler, CancellationToken ct) =>
{
    const int limit = 8 * 1024 * 1024;
    if (request.ContentLength > limit) throw new AppException(413, "photo_too_large", "La foto debe pesar como máximo 8 MB.");
    using var data = new MemoryStream();
    var buffer = new byte[81920];
    int read;
    while ((read = await request.Body.ReadAsync(buffer, ct)) > 0)
    {
        if (data.Length + read > limit) throw new AppException(413, "photo_too_large", "La foto debe pesar como máximo 8 MB.");
        await data.WriteAsync(buffer.AsMemory(0, read), ct);
    }
    return Results.Json(await handler.Handle(Guid.Parse(user.FindFirstValue("sub")!), data.ToArray(), ct), statusCode: 201);
}).RequireRateLimiting("access");
closet.MapGet("/garment-photos/{id:guid}", async (Guid id, ClaimsPrincipal user, IGarmentRepository repository, TimeProvider clock, HttpResponse response, CancellationToken ct) =>
{
    response.Headers["X-Content-Type-Options"] = "nosniff";
    return Results.File(await repository.Photo(Guid.Parse(user.FindFirstValue("sub")!), id, clock.GetUtcNow(), ct), "image/jpeg");
});
app.Run();
public partial class Program;
