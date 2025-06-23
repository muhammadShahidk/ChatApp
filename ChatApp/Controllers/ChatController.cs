using Microsoft.AspNetCore.Mvc;
using ChatApp.Models;
using ChatApp.Interfaces;

namespace ChatApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatAssignmentService _chatAssignmentService;
        private readonly ISessionQueueService _sessionQueueService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(IChatAssignmentService chatAssignmentService, ILogger<ChatController> logger, ISessionQueueService sessionQueueService)
        {
            _chatAssignmentService = chatAssignmentService;
            _logger = logger;
            _sessionQueueService = sessionQueueService;
        }        /// <summary>
                 /// Create a new chat session and add it to the queue
                 /// Implements chat refusal logic according to documentation requirements
                 /// </summary>
        [HttpPost("create")]
        public async Task<ActionResult<ChatSession>> CreateChatSession([FromBody] CreateChatRequest request)
        {
            try
            {
                _logger.LogInformation("Creating new chat session for customer: {CustomerId}", request.CustomerId);
                
                // First check if we can accept the chat
                var acceptanceResult = await _sessionQueueService.CanAcceptNewChatAsync();
                
                if (!acceptanceResult.CanAccept)
                {
                    _logger.LogWarning("Chat refused for customer {CustomerId}: {Reason}", 
                        request.CustomerId, acceptanceResult.Reason);
                    
                    return BadRequest(new
                    {
                        success = false,
                        message = "Chat request refused",
                        reason = acceptanceResult.Reason,
                        queueStatus = new
                        {
                            currentQueueLength = acceptanceResult.CurrentQueueLength,
                            maxQueueLength = acceptanceResult.MaxQueueLength,
                            isOverflowActive = acceptanceResult.IsOverflowActive
                        }
                    });
                }
                
                var chatSession = await _sessionQueueService.CreateChatSessionAsync(
                    request.CustomerId, 
                    request.CustomerName
                );

                _logger.LogInformation("Chat session created successfully: {ChatId}", chatSession.Id);
                
                return Ok(new
                {
                    success = true,
                    chat = chatSession,
                    acceptanceInfo = acceptanceResult
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Chat refused: {Message}", ex.Message);
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating chat session");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Complete a chat session
        /// </summary>
       

        /// <summary>
        /// Get current queue status and team information
        /// </summary>
        [HttpGet("queue/status")]
        public async Task<ActionResult<ChatQueueStatus>> GetQueueStatus()
        {
            try
            {
                var status = await _sessionQueueService.GetQueueStatusAsync();
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue status");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get all queued chats
        /// </summary>
        [HttpGet("queue")]
        public async Task<ActionResult<List<ChatSession>>> GetQueuedChats()
        {
            try
            {
                var queuedChats = await _sessionQueueService.GetQueuedChatsAsync();
                return Ok(queuedChats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queued chats");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get chats assigned to a specific agent
        /// </summary>
        [HttpGet("agent/{agentId}/chats")]
        public async Task<ActionResult<List<ChatSession>>> GetAgentChats(int agentId)
        {
            try
            {
                var agentChats = await _chatAssignmentService.GetAgentChatsAsync(agentId);
                return Ok(agentChats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting agent chats for agent {AgentId}", agentId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Manually process the queue (force assignment attempt)
        /// </summary>
        [HttpPost("queue/process")]
        public async Task<ActionResult> ProcessQueue()
        {
            try
            {
                _logger.LogInformation("Manually processing chat queue");
                await _chatAssignmentService.ProcessQueueAsync();
                return Ok(new { message = "Queue processed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing queue");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Check if a new chat can be accepted without actually creating it
        /// Useful for frontend to show queue status before chat creation
        /// </summary>
        [HttpGet("can-accept")]
        public async Task<ActionResult<ChatAcceptanceResult>> CanAcceptNewChat()
        {
            try
            {
                var result = await _sessionQueueService.CanAcceptNewChatAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking chat acceptance");
                return StatusCode(500, "Internal server error");
            }
        }
    }

    public class CreateChatRequest
    {
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
    }
}
