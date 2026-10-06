using TaeStyle.Application;

public sealed class PhotoCleanup(IServiceScopeFactory scopes, TimeProvider clock, ILogger<PhotoCleanup> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<IGarmentRepository>().CleanPhotos(clock.GetUtcNow(), stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            { logger.LogWarning("Temporary photo cleanup failed; will retry. Type {Type}", ex.GetType().Name); }
        }
    }
}
