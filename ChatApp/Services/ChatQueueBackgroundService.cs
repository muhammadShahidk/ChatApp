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
            _logger.LogInformation("Chat Queue Background Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var chatAssignmentService = scope.ServiceProvider.GetRequiredService<IChatAssignmentService>();
                        var teamService = scope.ServiceProvider.GetRequiredService<ITeamService>();

                        // Update agent shift statuses
                        teamService.UpdateAgentShiftStatus();

                        // Process the queue
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
