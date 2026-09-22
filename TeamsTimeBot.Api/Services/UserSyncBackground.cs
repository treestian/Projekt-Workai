namespace TeamsTimeBot.Api.Services;

public class UserSyncBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UserSyncBackgroundService> _logger;

    public UserSyncBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<UserSyncBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "User synchronization background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var syncService = scope.ServiceProvider
                    .GetRequiredService<UserSyncService>();

                var result = await syncService.SyncUsersAsync();

                _logger.LogInformation(
                    "User synchronization completed. " +
                    "Total: {Total}, Added: {Added}, " +
                    "Updated: {Updated}, Deactivated: {Deactivated}",
                    result.TotalFromGraph,
                    result.Added,
                    result.Updated,
                    result.Deactivated);

                var userService = scope.ServiceProvider
                    .GetRequiredService<UserService>();

                var settings = await userService.GetSyncSettingsAsync();

                await Task.Delay(
                    TimeSpan.FromHours(settings.UserSyncIntervalHours),
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error occurred during user synchronization.");

                await Task.Delay(
                    TimeSpan.FromMinutes(1),
                    stoppingToken);
            }
        }
    }
}

