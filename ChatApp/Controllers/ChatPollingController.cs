using ChatApp.Interfaces;
using ChatApp.Models;
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

        public ChatPollingController(
            ISessionMonitorService sessionMonitorService,
            ISessionQueueService sessionQueueService)
        {
            _sessionMonitorService = sessionMonitorService;
            _sessionQueueService = sessionQueueService;
        }
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
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

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
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

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
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}


