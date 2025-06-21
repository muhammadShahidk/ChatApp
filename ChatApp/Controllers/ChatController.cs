using Microsoft.AspNetCore.Mvc;
using ChatApp.Models;
using ChatApp.Services;

namespace ChatApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatAssignmentService _chatAssignmentService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(IChatAssignmentService chatAssignmentService, ILogger<ChatController> logger)
        {
            _chatAssignmentService = chatAssignmentService;
            _logger = logger;
        }

        /// <summary>
        /// Create a new chat session and add it to the queue
        /// </summary>
        [HttpPost("create")]
        public async Task<ActionResult<ChatSession>> CreateChatSession([FromBody] CreateChatRequest request)
        {
            try
            {
                _logger.LogInformation("Creating new chat session for customer: {CustomerId}", request.CustomerId);
                
                var chatSession = await _chatAssignmentService.CreateChatSessionAsync(
                    request.CustomerId, 
                    request.CustomerName
                );

                return Ok(chatSession);
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
        [HttpPost("{chatId}/complete")]
        public async Task<ActionResult> CompleteChatSession(int chatId)
        {
            try
            {
                _logger.LogInformation("Completing chat session: {ChatId}", chatId);
                
                var result = await _chatAssignmentService.CompleteChatAsync(chatId);
                
                if (result)
                {
                    return Ok(new { message = "Chat completed successfully" });
                }
                
                return NotFound(new { message = "Chat not found or not in progress" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing chat session {ChatId}", chatId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get current queue status and team information
        /// </summary>
        [HttpGet("queue/status")]
        public async Task<ActionResult<ChatQueueStatus>> GetQueueStatus()
        {
            try
            {
                var status = await _chatAssignmentService.GetQueueStatusAsync();
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
                var queuedChats = await _chatAssignmentService.GetQueuedChatsAsync();
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
    }

    public class CreateChatRequest
    {
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
    }
}
