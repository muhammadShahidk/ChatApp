using ChatApp.Interfaces;
using ChatApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatPollingController : ControllerBase
    {
        private readonly ISessionMonitorService _sessionMonitorService;
        private readonly ISessionQueueService _sessionQueueService;
        private readonly ILogger<ChatPollingController> _logger;

        public ChatPollingController(
            ISessionMonitorService sessionMonitorService,
            ISessionQueueService sessionQueueService,
            ILogger<ChatPollingController> logger)
        {
            _sessionMonitorService = sessionMonitorService;
            _sessionQueueService = sessionQueueService;
            _logger = logger;
        }        /// <summary>
                 /// Customer polling endpoint - called every 1 second after chat is created
                 /// Implements: "Once the chat window receives OK as a response it will start polling every 1s"
                 /// </summary>
        [HttpPost("poll/{chatId}")]
        public async Task<IActionResult> PollChatStatus(int chatId, [FromBody] PollRequest? request = null)
        {

            try
            {
                var customerId = request?.CustomerId ?? $"customer_{chatId}";
                var clientInfo = request?.ClientInfo ?? Request.Headers.UserAgent.ToString();

                // Record the poll in monitoring service
                var success = await _sessionMonitorService.RecordPollAsync(chatId, customerId, clientInfo);

                if (!success)
                {
                    return NotFound(new { message = "Chat session not found or not active" });
                }

                // Get current session status
                var session = await _sessionMonitorService.GetSessionAsync(chatId);
                if (session == null)
                {
                    return NotFound(new { message = "Chat session not found" });
                }

                // Check if session is still active
                if (!session.IsActive)
                {
                    return Ok(new
                    {
                        status = "inactive",
                        message = "Chat session has been marked inactive due to missed polls",
                        chatId = chatId
                    });
                }

                // Return current status based on session state
                var response = session.Status switch
                {
                    Models.ChatStatus.Queued => new PoolResponse
                    {
                        Status = "queued",
                        Message = "Your chat is in queue, please wait",
                        Position = session.QueuePosition,
                        EstimatedWaitTime = session.EstimatedAssignmentTime,
                        ChatId = chatId
                    },
                    Models.ChatStatus.InProgress => new PoolResponse
                    {
                        Status = "assigned",
                        Message = "You have been assigned to an agent",
                        AgentId = session.AssignedAgentId,
                        AgentName = session.AssignedAgent?.Name,
                        ChatId = chatId
                    },
                    Models.ChatStatus.Completed => new PoolResponse
                    {
                        Status = "completed",
                        Message = "Chat session has been completed",
                        ChatId = chatId
                    },
                    Models.ChatStatus.Abandoned => new PoolResponse
                    {
                        Status = "abandoned",
                        Message = "Chat session was abandoned",
                        ChatId = chatId
                    },
                    _ => new PoolResponse
                    {
                        Status = "unknown",
                        Message = "Unknown chat status",
                        ChatId = chatId
                    }
                };


                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling poll for chat {ChatId}", chatId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get session activity statistics for monitoring dashboard
        /// </summary>
        [HttpGet("monitoring/stats")]
        public async Task<IActionResult> GetMonitoringStats()
        {
            try
            {
                var stats = await _sessionMonitorService.GetSessionActivityStatsAsync();
                var monitoringStatus = await _sessionMonitorService.GetMonitoringStatusAsync();

                return Ok(new
                {
                    sessionStats = stats,
                    summary = new
                    {
                        totalSessions = monitoringStatus.TotalSessions,
                        activeSessions = monitoringStatus.ActiveSessions,
                        inactiveSessions = monitoringStatus.InactiveSessions,
                        lastUpdated = monitoringStatus.MonitoringTime
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting monitoring stats");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Manually trigger session activity check (for testing)
        /// </summary>
        [HttpPost("monitoring/check")]
        public async Task<IActionResult> TriggerActivityCheck()
        {
            try
            {
                var result = await _sessionMonitorService.CheckSessionActivityAsync();

                return Ok(new
                {
                    totalSessions = result.TotalSessions,
                    activeSessions = result.ActiveSessions,
                    inactiveSessions = result.InactiveSessions,
                    newlyInactiveSessions = result.NewlyInactiveSessions.Count,
                    monitoringTime = result.MonitoringTime,
                    duration = result.MonitoringDuration.TotalMilliseconds
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering activity check");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }

    public class PollRequest
    {
        public string CustomerId { get; set; } = string.Empty;
        public string ClientInfo { get; set; } = string.Empty;
    }
}



public class PoolResponse
{
    public string Status { get; set; }
    public string Message { get; set; }
    public int? Position { get; set; }
    public DateTime? EstimatedWaitTime { get; set; }
    public int? AgentId { get; set; }
    public string? AgentName { get; set; }
    public int ChatId { get; set; }
}