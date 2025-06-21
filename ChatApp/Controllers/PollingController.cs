using Microsoft.AspNetCore.Mvc;
using ChatApp.Models;
using ChatApp.Services;
using System.Diagnostics;

namespace ChatApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PollingController : ControllerBase
    {
        private readonly IChatAssignmentService _chatAssignmentService;
        private readonly ITeamService _teamService;
        private readonly ILogger<PollingController> _logger;

        public PollingController(
            IChatAssignmentService chatAssignmentService,
            ITeamService teamService,
            ILogger<PollingController> logger)
        {
            _chatAssignmentService = chatAssignmentService;
            _teamService = teamService;
            _logger = logger;
        }

        /// <summary>
        /// Customer polling endpoint - Check chat status and queue position
        /// Recommended polling: Every 1-2 seconds
        /// </summary>
        [HttpGet("chat/{chatId}/status")]
        public async Task<ActionResult<ChatStatusResponse>> GetChatStatus(int chatId)
        {
            try
            {
                var queuedChats = await _chatAssignmentService.GetQueuedChatsAsync();
                var queuePosition = queuedChats.FindIndex(c => c.Id == chatId) + 1;
                
                // Find the chat in all sessions
                var allChats = new List<ChatSession>();
                // Note: In real implementation, you'd have a method to get all chat sessions
                
                var status = await _chatAssignmentService.GetQueueStatusAsync();
                var estimatedWaitTime = CalculateEstimatedWaitTime(queuePosition, status.TotalCapacity);

                return Ok(new ChatStatusResponse
                {
                    ChatId = chatId,
                    Status = queuePosition > 0 ? "Queued" : "Processing",
                    QueuePosition = queuePosition > 0 ? queuePosition : null,
                    EstimatedWaitTimeMinutes = estimatedWaitTime,
                    TotalQueueLength = status.TotalQueuedChats,
                    IsOverflowActive = status.IsOverflowActive,
                    LastUpdated = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chat status for chat {ChatId}", chatId);
                return StatusCode(500, "Error retrieving chat status");
            }
        }

        /// <summary>
        /// Agent polling endpoint - Get current workload and new assignments
        /// Recommended polling: Every 2-5 seconds
        /// </summary>
        [HttpGet("agent/{agentId}/dashboard")]
        public async Task<ActionResult<AgentDashboardResponse>> GetAgentDashboard(int agentId)
        {
            try
            {
                var agentChats = await _chatAssignmentService.GetAgentChatsAsync(agentId);
                var queueStatus = await _chatAssignmentService.GetQueueStatusAsync();
                
                // Find agent details
                var allTeams = _teamService.GetAllTeams();
                var agent = allTeams.SelectMany(t => t.Agents).FirstOrDefault(a => a.Id == agentId);
                
                if (agent == null)
                {
                    return NotFound($"Agent {agentId} not found");
                }

                return Ok(new AgentDashboardResponse
                {
                    AgentId = agentId,
                    AgentName = agent.Name,
                    Status = agent.Status.ToString(),
                    CurrentChatCount = agent.CurrentChatCount,
                    MaxChatCapacity = agent.MaxConcurrentChats,
                    ActiveChats = agentChats.Select(c => new ActiveChatSummary
                    {
                        ChatId = c.Id,
                        CustomerName = c.CustomerName,
                        Duration = DateTime.Now - c.AssignedAt ?? TimeSpan.Zero,
                        LastActivity = c.AssignedAt ?? c.CreatedAt
                    }).ToList(),
                    ShiftEndTime = agent.ShiftEndTime,
                    QueueLength = queueStatus.TotalQueuedChats,
                    LastUpdated = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting agent dashboard for agent {AgentId}", agentId);
                return StatusCode(500, "Error retrieving agent dashboard");
            }
        }

        /// <summary>
        /// Supervisor polling endpoint - System overview and team performance
        /// Recommended polling: Every 5-10 seconds
        /// </summary>
        [HttpGet("supervisor/overview")]
        public async Task<ActionResult<SupervisorOverviewResponse>> GetSupervisorOverview()
        {
            try
            {
                var queueStatus = await _chatAssignmentService.GetQueueStatusAsync();
                var activeTeams = _teamService.GetActiveTeams();
                
                var teamPerformance = queueStatus.TeamStatuses.Select(ts => new TeamPerformance
                {
                    TeamId = ts.TeamId,
                    TeamName = ts.TeamName,
                    ActiveAgents = ts.AvailableAgents,
                    TotalAgents = ts.TotalAgents,
                    CurrentWorkload = ts.ActiveChats,
                    Capacity = ts.TeamCapacity,
                    UtilizationPercent = ts.TeamCapacity > 0 ? (double)ts.ActiveChats / ts.TeamCapacity * 100 : 0
                }).ToList();

                return Ok(new SupervisorOverviewResponse
                {
                    SystemStatus = queueStatus.TotalQueuedChats > queueStatus.MaxQueueLength ? "Overloaded" : "Normal",
                    TotalQueuedChats = queueStatus.TotalQueuedChats,
                    TotalSystemCapacity = queueStatus.TotalCapacity,
                    MaxQueueLength = queueStatus.MaxQueueLength,
                    IsOverflowActive = queueStatus.IsOverflowActive,
                    TeamPerformance = teamPerformance,
                    AverageWaitTime = CalculateAverageWaitTime(queueStatus),
                    LastUpdated = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting supervisor overview");
                return StatusCode(500, "Error retrieving supervisor overview");
            }
        }

        /// <summary>
        /// Health check endpoint for monitoring systems
        /// Recommended polling: Every 30-60 seconds
        /// </summary>
        [HttpGet("health")]
        public async Task<ActionResult<SystemHealthResponse>> GetSystemHealth()
        {
            try
            {
                var queueStatus = await _chatAssignmentService.GetQueueStatusAsync();
                var activeTeams = _teamService.GetActiveTeams();
                
                var health = new SystemHealthResponse
                {
                    Status = "Healthy",
                    BackgroundServiceRunning = true, // In real app, you'd check this
                    TotalActiveAgents = activeTeams.SelectMany(t => t.Agents).Count(a => a.Status == AgentWorkStatus.Available || a.Status == AgentWorkStatus.Busy),
                    QueueHealthy = queueStatus.TotalQueuedChats <= queueStatus.MaxQueueLength,
                    LastProcessedAt = DateTime.Now, // In real app, track this
                    SystemUptime = DateTime.Now - Process.GetCurrentProcess().StartTime,
                    Timestamp = DateTime.Now
                };

                return Ok(health);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system health");
                return StatusCode(500, new SystemHealthResponse 
                { 
                    Status = "Unhealthy", 
                    Timestamp = DateTime.Now 
                });
            }
        }

        private int CalculateEstimatedWaitTime(int queuePosition, int totalCapacity)
        {
            if (queuePosition <= 0) return 0;
            
            // Rough estimate: average chat time of 5 minutes
            // Capacity determines how many chats can be processed simultaneously
            var averageChatTimeMinutes = 5;
            var estimatedMinutes = (queuePosition * averageChatTimeMinutes) / Math.Max(totalCapacity, 1);
            
            return Math.Max(1, estimatedMinutes);
        }

        private string CalculateAverageWaitTime(ChatQueueStatus status)
        {
            // In real implementation, you'd track actual wait times
            var estimatedMinutes = CalculateEstimatedWaitTime(status.TotalQueuedChats, status.TotalCapacity);
            return $"{estimatedMinutes} minutes";
        }
    }

    // Response Models for Polling
    public class ChatStatusResponse
    {
        public int ChatId { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? QueuePosition { get; set; }
        public int EstimatedWaitTimeMinutes { get; set; }
        public int TotalQueueLength { get; set; }
        public bool IsOverflowActive { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class AgentDashboardResponse
    {
        public int AgentId { get; set; }
        public string AgentName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int CurrentChatCount { get; set; }
        public int MaxChatCapacity { get; set; }
        public List<ActiveChatSummary> ActiveChats { get; set; } = new();
        public DateTime ShiftEndTime { get; set; }
        public int QueueLength { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class ActiveChatSummary
    {
        public int ChatId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public DateTime LastActivity { get; set; }
    }

    public class SupervisorOverviewResponse
    {
        public string SystemStatus { get; set; } = string.Empty;
        public int TotalQueuedChats { get; set; }
        public int TotalSystemCapacity { get; set; }
        public int MaxQueueLength { get; set; }
        public bool IsOverflowActive { get; set; }
        public List<TeamPerformance> TeamPerformance { get; set; } = new();
        public string AverageWaitTime { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
    }

    public class TeamPerformance
    {
        public string TeamId { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public int ActiveAgents { get; set; }
        public int TotalAgents { get; set; }
        public int CurrentWorkload { get; set; }
        public int Capacity { get; set; }
        public double UtilizationPercent { get; set; }
    }

    public class SystemHealthResponse
    {
        public string Status { get; set; } = string.Empty;
        public bool BackgroundServiceRunning { get; set; }
        public int TotalActiveAgents { get; set; }
        public bool QueueHealthy { get; set; }
        public DateTime LastProcessedAt { get; set; }
        public TimeSpan SystemUptime { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
