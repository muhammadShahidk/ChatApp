using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ChatApp.Services;
using ChatApp.Interfaces;
using ChatApp.Models;

namespace ChatApp.TestConsole;

class Program
{
    static async Task Main(string[] args)
    {        Console.WriteLine("🚀 ChatApp Session Queue Service Test Console");
        Console.WriteLine(new string('=', 50));

        // Create host with dependency injection
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Configure logging
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });
                
                // Register configuration
                services.AddSingleton<MonitoringConfiguration>();
                  // Register services
                services.AddSingleton<ITeamService, TeamService>();
                services.AddSingleton<ISessionQueueService, SessionQueueService>();
                services.AddSingleton<ISessionPollTracker, SessionPollTrackerService>();
                services.AddSingleton<ISessionMonitorService, SessionMonitorService>();
                services.AddSingleton<RunTestScenarios>();
            })
            .Build();

        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        var RunTestScenarios = host.Services.GetRequiredService<RunTestScenarios>();



        try
        {
            await RunTestScenarios.Run();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running test scenarios");
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

}
