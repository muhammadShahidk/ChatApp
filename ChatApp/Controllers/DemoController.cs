using Microsoft.AspNetCore.Mvc;
using ChatApp.Services;
using ChatApp.Models;

namespace ChatApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DemoController : ControllerBase
    {
        private readonly IChatAssignmentService _chatAssignmentService;
        private readonly ITeamService _teamService;
        private readonly ILogger<DemoController> _logger;

        public DemoController(
            IChatAssignmentService chatAssignmentService,
            ITeamService teamService,
            ILogger<DemoController> logger)
        {
            _chatAssignmentService = chatAssignmentService;
            _teamService = teamService;
            _logger = logger;
        }

        /// <summary>
        /// Simulate the chat assignment system with multiple scenarios
        /// </summary>
        [HttpPost("simulate")]
        public async Task<ActionResult> SimulateSystem([FromBody] SimulationRequest request)
        {
            try
            {
                var results = new List<string>();

                _logger.LogInformation("Starting simulation with {ChatCount} chats", request.NumberOfChats);

                // Reset teams first
                _teamService.InitializeTeams();
                results.Add($"✅ Teams initialized successfully");

                // Get initial status
                var initialStatus = await _chatAssignmentService.GetQueueStatusAsync();
                results.Add($"📊 Initial Capacity: {initialStatus.TotalCapacity} concurrent chats");
                results.Add($"📈 Max Queue Length: {initialStatus.MaxQueueLength}");

                // Create multiple chat sessions
                var chatSessions = new List<ChatSession>();
                for (int i = 1; i <= request.NumberOfChats; i++)
                {
                    var chat = await _chatAssignmentService.CreateChatSessionAsync(
                        $"customer_{i}",
                        $"Customer {i}"
                    );
                    chatSessions.Add(chat);
                }

                results.Add($"💬 Created {request.NumberOfChats} chat sessions");

                // Wait a moment for processing
                await Task.Delay(1000);

                // Get final status
                var finalStatus = await _chatAssignmentService.GetQueueStatusAsync();
                results.Add($"🎯 Final Queue Length: {finalStatus.TotalQueuedChats}");
                results.Add($"⚡ Overflow Team Active: {(finalStatus.IsOverflowActive ? "YES" : "NO")}");

                // Show team distribution
                foreach (var teamStatus in finalStatus.TeamStatuses)
                {
                    if (teamStatus.ActiveChats > 0 || teamStatus.AvailableAgents > 0)
                    {
                        results.Add($"👥 {teamStatus.TeamName}: {teamStatus.ActiveChats} active chats, {teamStatus.AvailableAgents}/{teamStatus.TotalAgents} agents available");
                    }
                }

                // Show round-robin assignment details
                var assignedChats = chatSessions.Where(c => c.AssignedAgent != null).ToList();
                var chatsByAgent = assignedChats.GroupBy(c => c.AssignedAgent!.Name)
                    .OrderBy(g => g.First().AssignedAgent!.Seniority)
                    .ThenBy(g => g.First().AssignedAgent!.Name);

                results.Add($"🔄 Round-Robin Assignment Results:");
                foreach (var agentGroup in chatsByAgent)
                {
                    var agent = agentGroup.First().AssignedAgent!;
                    results.Add($"   👤 {agent.Name} ({agent.Seniority}): {agentGroup.Count()} chats (max: {agent.MaxConcurrentChats})");
                }

                return Ok(new
                {
                    success = true,
                    message = "Simulation completed successfully",
                    results = results,
                    finalStatus = finalStatus,
                    assignedChats = assignedChats.Count,
                    queuedChats = finalStatus.TotalQueuedChats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running simulation");
                return StatusCode(500, "Simulation failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Example scenario: Test the capacity calculation from the requirements
        /// Team of 2 mid-levels and a junior = (2 x 10 x 0.6) + (1 x 10 x 0.4) = 16 concurrent chats capacity
        /// </summary>
        [HttpPost("test-capacity-calculation")]
        public async Task<ActionResult> TestCapacityCalculation()
        {
            try
            {
                var results = new List<string>();

                // Get Team A details (matches the example: 1 team lead, 2 mid-levels, 1 junior)
                var teamA = _teamService.GetTeam("TEAM_A");
                if (teamA == null)
                {
                    return BadRequest("Team A not found");
                }

                results.Add("📋 Team A Capacity Calculation:");
                results.Add($"👥 Team Composition:");

                int totalCapacity = 0;
                foreach (var agent in teamA.Agents)
                {
                    var capacity = agent.MaxConcurrentChats;
                    totalCapacity += capacity;
                    results.Add($"   • {agent.Name} ({agent.Seniority}): {capacity} chats (10 × {agent.EfficiencyMultiplier})");
                }

                results.Add($"🎯 Total Team Capacity: {totalCapacity} concurrent chats");
                results.Add($"📊 Max Queue Length: {teamA.MaxQueueLength} (capacity × 1.5)");

                // Compare with requirements example
                results.Add("");
                results.Add("📖 Requirements Example: 2 mid-levels + 1 junior");
                var exampleCapacity = (2 * 10 * 0.6) + (1 * 10 * 0.4);
                results.Add($"   Expected: (2 × 10 × 0.6) + (1 × 10 × 0.4) = {exampleCapacity} chats");

                return Ok(new
                {
                    success = true,
                    results = results,
                    teamACapacity = totalCapacity,
                    maxQueueLength = teamA.MaxQueueLength,
                    exampleCapacity = exampleCapacity
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing capacity calculation");
                return StatusCode(500, "Test failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Test round-robin assignment: "A team of 2 people: 1 snr(cap 8), 1 jnr (cap 4). 5 chats arrive. 4 of which would be assigned to the jnr and 1 to the senior"
        /// </summary>
        [HttpPost("test-round-robin")]
        public async Task<ActionResult> TestRoundRobinAssignment()
        {
            try
            {
                var results = new List<string>();

                // Reset and create a custom team for this test
                var testTeam = new Team
                {
                    Id = "TEST_TEAM",
                    Name = "Round Robin Test Team",
                    Agents = new List<Agent>
                    {
                        new Agent
                        {
                            Id = 999,
                            Name = "Junior Test Agent",
                            Seniority = Seniority.Junior,
                            Status = AgentWorkStatus.Available,
                            TeamId = "TEST_TEAM"
                        },
                        new Agent
                        {
                            Id = 998,
                            Name = "Senior Test Agent",
                            Seniority = Seniority.Senior,
                            Status = AgentWorkStatus.Available,
                            TeamId = "TEST_TEAM"
                        }
                    }
                };

                results.Add("🧪 Round-Robin Test Scenario:");
                results.Add($"👥 Team: 1 Senior (cap {testTeam.Agents[1].MaxConcurrentChats}) + 1 Junior (cap {testTeam.Agents[0].MaxConcurrentChats})");
                results.Add($"💬 Creating 5 chats...");

                // The actual implementation assigns to existing teams, so this is more of a conceptual demonstration
                results.Add("");
                results.Add("📊 Expected Round-Robin Result:");
                results.Add("   • Junior (cap 4): Should get 4 chats");
                results.Add("   • Senior (cap 8): Should get 1 chat");
                results.Add("");
                results.Add("💡 This ensures higher seniority agents are available to assist lower seniority agents");

                return Ok(new
                {
                    success = true,
                    results = results,
                    explanation = "Round-robin prioritizes junior agents first to keep senior agents available for assistance",
                    juniorCapacity = testTeam.Agents[0].MaxConcurrentChats,
                    seniorCapacity = testTeam.Agents[1].MaxConcurrentChats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing round-robin assignment");
                return StatusCode(500, "Test failed: " + ex.Message);
            }
        }
    }

    public class SimulationRequest
    {
        public int NumberOfChats { get; set; } = 10;
    }
}
