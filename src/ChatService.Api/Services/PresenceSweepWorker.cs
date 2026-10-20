using ChatService.Application.Dto;
using ChatService.Application.Services;
using Microsoft.Extensions.Options;

namespace ChatService.Api.Services;

public class PresenceOptions
{
    public const string SectionName = "Presence";
    public int IdleMinutes { get; set; } = 5;
}

public class PresenceSweepWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly PresenceOptions _options;
    private readonly ILogger<PresenceSweepWorker> _logger;

    public PresenceSweepWorker(
        IServiceProvider serviceProvider,
        IOptions<PresenceOptions> options,
        ILogger<PresenceSweepWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var presenceService = scope.ServiceProvider.GetRequiredService<IPresenceService>();
                var notifier = scope.ServiceProvider.GetRequiredService<IConversationRealtimeNotifier>();
                var idleThreshold = TimeSpan.FromMinutes(_options.IdleMinutes);

                await presenceService.SweepIdleAsync(idleThreshold, stoppingToken);

                var online = await scope.ServiceProvider
                    .GetRequiredService<ChatService.Domain.Repositories.IUserPresenceRepository>()
                    .GetAllOnlineAsync(stoppingToken);

                foreach (var presence in online.Where(p => p.Status == ChatService.Domain.Enums.PresenceStatus.Away))
                {
                    await notifier.NotifyPresenceChangedAsync(new PresenceDto
                    {
                        ParticipantId = presence.UserId.UserId,
                        Status = presence.Status,
                        LastSeenAt = presence.LastSeenAt,
                        LastActivityAt = presence.LastActivityAt
                    }, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Presence sweep failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
