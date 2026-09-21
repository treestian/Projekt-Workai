namespace TeamsTimeBot.Api.Services;

using Microsoft.EntityFrameworkCore;
using TeamsTimeBot.Api.Data;

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

                var db = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                var result = await syncService.SyncUsersAsync();

                _logger.LogInformation(
                    "User synchronization completed. " +
                    "Total: {Total}, Added: {Added}, " +
                    "Updated: {Updated}, Deactivated: {Deactivated}",
                    result.TotalFromGraph,
                    result.Added,
                    result.Updated,
                    result.Deactivated);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error occurred during user synchronization.");
            }

            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                var intervalHours = await db.SyncSettings
                    .Select(x => (int?)x.UserSyncIntervalHours)
                    .FirstOrDefaultAsync(stoppingToken) ?? 24;

                await Task.Delay(
                    TimeSpan.FromHours(intervalHours),
                    stoppingToken);
            }
        }
    }
}