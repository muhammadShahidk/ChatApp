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
        }        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var chatAssignmentService = scope.ServiceProvider.GetRequiredService<IChatAssignmentService>();
                        var teamService = scope.ServiceProvider.GetRequiredService<ITeamService>();
                        var sessionMonitorService = scope.ServiceProvider.GetRequiredService<ISessionMonitorService>();

                        teamService.UpdateAgentShiftStatus();

                        var monitoringResult = await sessionMonitorService.CheckSessionActivityAsync();
                        
                        if (monitoringResult.NewlyInactiveSessions.Any())
                        {
                            _logger.LogInformation("Marked {Count} sessions as inactive due to missed polls", 
                                monitoringResult.NewlyInactiveSessions.Count);
                        }

                        await chatAssignmentService.ProcessQueueAsync();
                        
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
