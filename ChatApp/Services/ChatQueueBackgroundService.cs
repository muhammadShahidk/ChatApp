using ChatApp.Interfaces;

namespace ChatApp.Services
{
    public class ChatQueueBackgroundService : BackgroundService
    {
        private readonly ILogger<ChatQueueBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(10); // Process every 10 seconds

        public ChatQueueBackgroundService(
            ILogger<ChatQueueBackgroundService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Chat Queue Background Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var chatAssignmentService = scope.ServiceProvider.GetRequiredService<IChatAssignmentService>();
                        var teamService = scope.ServiceProvider.GetRequiredService<ITeamService>();
                        var sessionMonitorService = scope.ServiceProvider.GetRequiredService<ISessionMonitorService>();

                        // 1. Update agent shift statuses
                        teamService.UpdateAgentShiftStatus();

                        // 2. Check session activity and mark inactive sessions
                        var monitoringResult = await sessionMonitorService.CheckSessionActivityAsync();
                        
                        if (monitoringResult.NewlyInactiveSessions.Any())
                        {
                            _logger.LogInformation("Marked {Count} sessions as inactive due to missed polls", 
                                monitoringResult.NewlyInactiveSessions.Count);
                        }

                        // 3. Process the queue (only active sessions will be assigned)
                        await chatAssignmentService.ProcessQueueAsync();
                        
                        _logger.LogDebug("Queue processing cycle completed. Active: {Active}, Inactive: {Inactive}", 
                            monitoringResult.ActiveSessions, monitoringResult.InactiveSessions);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing chat queue");
                }

                await Task.Delay(_processingInterval, stoppingToken);
            }

            _logger.LogInformation("Chat Queue Background Service stopped");
        }
    }
}
