using ChatApp.Interfaces;
using ChatApp.Models;
using ChatApp.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChatApp.TestConsole
{
    internal class RunTestScenarios
    {
        private readonly ITeamService teamService;
        private readonly ILogger logger;
        private readonly ISessionQueueService sessionQueueService;

        public RunTestScenarios(ITeamService teamService, ILogger<RunTestScenarios> logger, ISessionQueueService sessionQueueService)
        {
            this.teamService = teamService;
            this.logger = logger;
            this.sessionQueueService = sessionQueueService;
        }

        public async Task Run()
        {
            //await RunTestScenariost(sessionQueueService, logger);
            await TestSessionMonitoring();
            //await testTeamCapicityCalqulations();
            //await TestSessionMonitoring();
        }


        async Task RunTestScenariost(ISessionQueueService sessionQueueService, ILogger logger)
        {
            Console.WriteLine("\n📋 Test Scenario 1: Basic Queue Operations");
            await TestBasicQueueOperations(sessionQueueService, logger);

            //Console.WriteLine("\n📋 Test Scenario 2: Queue Capacity and Limits");
            //await TestQueueCapacityLimits(sessionQueueService, logger);

            //Console.WriteLine("\n📋 Test Scenario 3: Chat Refusal Logic");
            //await TestChatRefusalLogic(sessionQueueService, logger);

            //Console.WriteLine("\n📋 Test Scenario 4: FIFO Queue Ordering");
            //await TestFIFOOrdering(sessionQueueService, logger);

            //Console.WriteLine("\n📋 Test Scenario 5: Queue Status Monitoring");
            //await TestQueueStatusMonitoring(sessionQueueService, logger);
        }
        async Task testTeamCapicityCalqulations()
        {
            Console.WriteLine("✅ Testing Team Capacity Calculations...");
            Console.WriteLine(new string('=', 60));

            // Initialize teams
            teamService.InitializeTeams();

            // Get all teams
            var allTeams = teamService.GetAllTeams();
            var activeTeams = teamService.GetActiveTeams();
            var overflowTeam = teamService.GetOverflowTeam();

            Console.WriteLine($"\n📊 Total Teams: {allTeams.Count}");
            Console.WriteLine($"📊 Active Teams: {activeTeams.Count}");
            Console.WriteLine($"📊 Overflow Team: {overflowTeam.Name}");

            // Test each team's capacity calculations
            Console.WriteLine("\n🏢 TEAM CAPACITY ANALYSIS:");
            Console.WriteLine(new string('=', 60));

            foreach (var team in allTeams)
            {
                await AnalyzeTeamCapacity(team);
            }            // Test overflow team
            Console.WriteLine("\n🚨 OVERFLOW TEAM ANALYSIS:");
            Console.WriteLine(new string('=', 60));
            await AnalyzeTeamCapacity(overflowTeam);

            // Test system-wide capacity
            await AnalyzeSystemCapacity(activeTeams, overflowTeam);
        }
 
        async Task AnalyzeTeamCapacity(Team team)
        {
            Console.WriteLine($"\n🏢 Team: {team.Name} (ID: {team.Id})");
            Console.WriteLine($"   📅 Shift: {team.Shift?.ToString() ?? "24/7"}");
            Console.WriteLine($"   🔄 Is Overflow: {team.IsOverflowTeam}");
            Console.WriteLine($"   👥 Total Agents: {team.Agents.Count}");

            // Agent breakdown by seniority
            var seniorityGroups = team.Agents.GroupBy(a => a.Seniority);
            Console.WriteLine($"   📋 Agent Breakdown:");
            foreach (var group in seniorityGroups)
            {
                Console.WriteLine($"      {group.Key}: {group.Count()} agents");
            }

            // Capacity calculations
            Console.WriteLine($"   📊 CAPACITY CALCULATIONS:");
            Console.WriteLine($"      Total Capacity: {team.TotalCapacity} concurrent chats");
            Console.WriteLine($"      Max Queue Length: {team.MaxQueueLength} (1.5x capacity)");
            Console.WriteLine($"      Available Agents: {team.AvailableAgents.Count}");

            // Individual agent analysis
            Console.WriteLine($"   👤 INDIVIDUAL AGENT ANALYSIS:");
            foreach (var agent in team.Agents)
            {
                Console.WriteLine($"      {agent.Name} ({agent.Seniority}):");
                Console.WriteLine($"         Status: {agent.Status}");
                Console.WriteLine($"         Efficiency Multiplier: {agent.EfficiencyMultiplier:F1}");
                Console.WriteLine($"         Max Concurrent Chats: {agent.MaxConcurrentChats}");
                Console.WriteLine($"         Current Chats: {agent.CurrentChatCount}");
                Console.WriteLine($"         Can Take New Chat: {agent.CanTakeNewChat}");
                Console.WriteLine($"         Shift: {agent.ShiftStartTime:HH:mm} - {agent.ShiftEndTime:HH:mm}");
            }
        }
        async Task AnalyzeSystemCapacity(List<Team> activeTeams, Team overflowTeam)
        {
            Console.WriteLine("\n🌐 SYSTEM-WIDE CAPACITY ANALYSIS:");
            Console.WriteLine(new string('=', 60));

            var totalSystemCapacity = activeTeams.Sum(t => t.TotalCapacity);
            var totalMaxQueue = activeTeams.Sum(t => t.MaxQueueLength);
            var totalAvailableAgents = activeTeams.Sum(t => t.AvailableAgents.Count);
            var totalAgents = activeTeams.Sum(t => t.Agents.Count);

            Console.WriteLine($"📊 Active Teams Summary:");
            Console.WriteLine($"   Total System Capacity: {totalSystemCapacity} concurrent chats");
            Console.WriteLine($"   Total Max Queue Length: {totalMaxQueue}");
            Console.WriteLine($"   Total Available Agents: {totalAvailableAgents}/{totalAgents}");

            // Office hours check
            var isOfficeHours = teamService.IsOfficeHours();
            Console.WriteLine($"\n⏰ Current Time Analysis:");
            Console.WriteLine($"   Is Office Hours: {isOfficeHours}");
            Console.WriteLine($"   Current Time: {DateTime.Now:HH:mm}");

            // Overflow capacity (if applicable)
            var isOverflowActive = overflowTeam.Agents.Any(a => a.Status != AgentWorkStatus.Offline);
            Console.WriteLine($"\n🚨 Overflow Team Status:");
            Console.WriteLine($"   Is Overflow Active: {isOverflowActive}");
            Console.WriteLine($"   Overflow Capacity: {overflowTeam.TotalCapacity}");
            Console.WriteLine($"   Overflow Max Queue: {overflowTeam.MaxQueueLength}");

            if (isOfficeHours && isOverflowActive)
            {
                var totalWithOverflow = totalSystemCapacity + overflowTeam.TotalCapacity;
                var maxQueueWithOverflow = totalMaxQueue + overflowTeam.MaxQueueLength;
                Console.WriteLine($"   TOTAL WITH OVERFLOW:");
                Console.WriteLine($"      Combined Capacity: {totalWithOverflow} concurrent chats");
                Console.WriteLine($"      Combined Max Queue: {maxQueueWithOverflow}");
            }

            // Efficiency analysis
            Console.WriteLine($"\n📈 EFFICIENCY ANALYSIS:");
            var juniorCount = activeTeams.Sum(t => t.Agents.Count(a => a.Seniority == Seniority.Junior));
            var midLevelCount = activeTeams.Sum(t => t.Agents.Count(a => a.Seniority == Seniority.MidLevel));
            var seniorCount = activeTeams.Sum(t => t.Agents.Count(a => a.Seniority == Seniority.Senior));
            var teamLeadCount = activeTeams.Sum(t => t.Agents.Count(a => a.Seniority == Seniority.TeamLead));

            Console.WriteLine($"   Junior Agents: {juniorCount} (4 chats each = {juniorCount * 4} total)");
            Console.WriteLine($"   Mid-Level Agents: {midLevelCount} (6 chats each = {midLevelCount * 6} total)");
            Console.WriteLine($"   Senior Agents: {seniorCount} (8 chats each = {seniorCount * 8} total)");
            Console.WriteLine($"   Team Lead Agents: {teamLeadCount} (5 chats each = {teamLeadCount * 5} total)");

            var calculatedCapacity = (juniorCount * 4) + (midLevelCount * 6) + (seniorCount * 8) + (teamLeadCount * 5);
            Console.WriteLine($"   Calculated Total: {calculatedCapacity} (should match: {totalSystemCapacity})");
            Console.WriteLine($"   Calculation Match: {calculatedCapacity == totalSystemCapacity}");
        }

        async Task TestBasicQueueOperations(ISessionQueueService sessionQueueService, ILogger logger)
        {
            Console.WriteLine("✅ Testing basic queue operations...");

            try
            {
                // Test 1: Check if we can accept new chats
                var canAccept = await sessionQueueService.CanAcceptNewChatAsync();
                Console.WriteLine($"   Can accept new chat: {canAccept.CanAccept}");
                Console.WriteLine($"   Reason: {canAccept.Reason}");
                Console.WriteLine($"   Current queue: {canAccept.CurrentQueueLength}/{canAccept.MaxQueueLength}");

                // Test 2: Create a few chat sessions
                for (int i = 1; i <= 100; i++)
                {
                    var chat = await sessionQueueService.CreateChatSessionAsync(
                        $"customer_{i:D3}",
                        $"Test Customer {i}"
                    );
                    Console.WriteLine($"   ✅ Created chat {chat.Id}: {chat.CustomerName}");
                }

                // Test 3: Check queue status
                var queuedChats = await sessionQueueService.GetQueuedChatsAsync();
                Console.WriteLine($"   📊 Total queued chats: {queuedChats.Count}");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Error: {ex.Message}");
                logger.LogError(ex, "Error in basic queue operations test");
            }
        }

        async Task TestQueueCapacityLimits(ISessionQueueService sessionQueueService, ILogger logger)
        {
            Console.WriteLine("✅ Testing queue capacity and limits...");

            try
            {
                var status = await sessionQueueService.GetQueueStatusAsync();
                Console.WriteLine($"   📊 System Capacity: {status.TotalCapacity} concurrent chats");
                Console.WriteLine($"   📈 Max Queue Length: {status.MaxQueueLength}");
                Console.WriteLine($"   ⚡ Overflow Active: {status.IsOverflowActive}");

                Console.WriteLine($"   👥 Team Details:");
                foreach (var team in status.TeamStatuses)
                {
                    Console.WriteLine($"      🏢 {team.TeamName}: {team.AvailableAgents}/{team.TotalAgents} agents, {team.TeamCapacity} capacity");
                    //print team memebers with there capacity

                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Error: {ex.Message}");
                logger.LogError(ex, "Error in capacity limits test");
            }
        }

        async Task TestChatRefusalLogic(ISessionQueueService sessionQueueService, ILogger logger)
        {
            Console.WriteLine("✅ Testing chat refusal logic...");

            try
            {
                // Get current capacity
                var status = await sessionQueueService.GetQueueStatusAsync();
                var maxQueue = status.MaxQueueLength;
                var currentQueue = status.TotalQueuedChats;
                var chatsToAdd = maxQueue - currentQueue + 5; // Try to exceed capacity

                Console.WriteLine($"   🎯 Will try to add {chatsToAdd} chats to test refusal logic");
                Console.WriteLine($"   📊 Current: {currentQueue}, Max: {maxQueue}");

                int successCount = 0;
                int refusalCount = 0;

                for (int i = 1; i <= chatsToAdd; i++)
                {
                    try
                    {
                        var chat = await sessionQueueService.CreateChatSessionAsync(
                            $"test_customer_{i:D3}",
                            $"Test Customer {i}"
                        );
                        successCount++;
                        Console.WriteLine($"   ✅ Chat {i}: Accepted (ID: {chat.Id})");
                    }
                    catch (InvalidOperationException ex)
                    {
                        refusalCount++;
                        Console.WriteLine($"   ❌ Chat {i}: Refused - {ex.Message}");
                        if (refusalCount >= 3) break; // Don't spam refusals
                    }
                }

                Console.WriteLine($"   📊 Results: {successCount} accepted, {refusalCount} refused");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Error: {ex.Message}");
                logger.LogError(ex, "Error in chat refusal logic test");
            }
        }

        async Task TestFIFOOrdering(ISessionQueueService sessionQueueService, ILogger logger)
        {
            Console.WriteLine("✅ Testing FIFO queue ordering...");

            try
            {
                // Create a few more chats with timestamps
                var testChats = new List<ChatSession>();

                for (int i = 1; i <= 5; i++)
                {
                    try
                    {
                        var chat = await sessionQueueService.CreateChatSessionAsync(
                            $"fifo_customer_{i:D3}",
                            $"FIFO Test Customer {i}"
                        );
                        testChats.Add(chat);
                        Console.WriteLine($"   ➕ Added chat {chat.Id} at {chat.CreatedAt:HH:mm:ss.fff}");

                        // Small delay to ensure different timestamps
                        await Task.Delay(10);
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.WriteLine($"   ❌ Chat {i} refused: {ex.Message}");
                        break;
                    }
                }

                // Check queue order
                var queuedChats = await sessionQueueService.GetQueuedChatsAsync();
                Console.WriteLine($"   📋 Queue order (FIFO verification):");

                for (int i = 0; i < Math.Min(10, queuedChats.Count); i++)
                {
                    var chat = queuedChats[i];
                    Console.WriteLine($"      {i + 1}. Chat {chat.Id} - {chat.CustomerName} ({chat.CreatedAt:HH:mm:ss.fff})");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Error: {ex.Message}");
                logger.LogError(ex, "Error in FIFO ordering test");
            }
        }

        async Task TestQueueStatusMonitoring(ISessionQueueService sessionQueueService, ILogger logger)
        {
            Console.WriteLine("✅ Testing queue status monitoring...");

            try
            {
                var status = await sessionQueueService.GetQueueStatusAsync();

                Console.WriteLine($"   📊 Final Queue Status:");
                Console.WriteLine($"      Total Queued: {status.TotalQueuedChats}");
                Console.WriteLine($"      Total Capacity: {status.TotalCapacity}");
                Console.WriteLine($"      Max Queue Length: {status.MaxQueueLength}");
                Console.WriteLine($"      Overflow Active: {status.IsOverflowActive}");
                Console.WriteLine($"      Last Updated: {status.LastUpdated:HH:mm:ss}");

                Console.WriteLine($"   👥 Team Status Summary:");
                foreach (var team in status.TeamStatuses)
                {
                    Console.WriteLine($"      🏢 {team.TeamName}:");
                    Console.WriteLine($"         Agents: {team.AvailableAgents}/{team.TotalAgents} available");
                    Console.WriteLine($"         Capacity: {team.TeamCapacity}");
                    Console.WriteLine($"         Active Chats: {team.ActiveChats}");
                    Console.WriteLine($"         Active: {team.IsActive}");
                }

                // Show utilization
                var utilization = status.TotalCapacity > 0
                    ? (double)status.TotalQueuedChats / status.TotalCapacity * 100
                    : 0;
                Console.WriteLine($"   📈 System Utilization: {utilization:F1}%");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Error: {ex.Message}");
                logger.LogError(ex, "Error in queue status monitoring test");
            }
        }

        async Task TestSessionMonitoring()
        {
            Console.WriteLine("✅ Testing Session Monitoring and Poll Tracking...");
            Console.WriteLine(new string('=', 60));

            try
            {
                // Create some test sessions
                var session1 = await sessionQueueService.CreateChatSessionAsync("customer_001", "Test Customer 1");
                var session2 = await sessionQueueService.CreateChatSessionAsync("customer_002", "Test Customer 2");
                var session3 = await sessionQueueService.CreateChatSessionAsync("customer_003", "Test Customer 3");

                Console.WriteLine($"\n📋 Created {3} test sessions for monitoring");
                Console.WriteLine($"   Session 1: {session1.Id} - {session1.CustomerName}");
                Console.WriteLine($"   Session 2: {session2.Id} - {session2.CustomerName}");
                Console.WriteLine($"   Session 3: {session3.Id} - {session3.CustomerName}");

                // TODO: Simulate polling behavior
                // Note: This would require the monitoring service to be properly configured
                // For now, just show the structure
                
                Console.WriteLine($"\n🔍 Monitoring Structure Demonstration:");
                Console.WriteLine($"   - Sessions are created in queue");
                Console.WriteLine($"   - Poll tracker monitors customer polling");
                Console.WriteLine($"   - Sessions marked inactive after 3 missed polls");
                Console.WriteLine($"   - Clean separation between queue and monitoring");

                Console.WriteLine($"\n📊 Session Status:");
                var queuedChats = await sessionQueueService.GetQueuedChatsAsync();
                foreach (var chat in queuedChats.Take(5))
                {
                    Console.WriteLine($"   Chat {chat.Id}: {chat.CustomerName} - Active: {chat.IsActive} - Status: {chat.Status}");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Error in monitoring test: {ex.Message}");
                logger.LogError(ex, "Error in session monitoring test");
            }
        }
    }
}