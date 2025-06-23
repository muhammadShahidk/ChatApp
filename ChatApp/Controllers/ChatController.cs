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

        public ChatController(IChatAssignmentService chatAssignmentService, ISessionQueueService sessionQueueService)
        {
            _chatAssignmentService = chatAssignmentService;
            _sessionQueueService = sessionQueueService;
        }
        [HttpPost("create")]
        public async Task<ActionResult<ChatSession>> CreateChatSession([FromBody] CreateChatRequest request)
        {
            try
            {
                var acceptanceResult = await _sessionQueueService.CanAcceptNewChatAsync();
                
                if (!acceptanceResult.CanAccept)
                {
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

                
                return Ok(new
                {
                    success = true,
                    chat = chatSession,
                    acceptanceInfo = acceptanceResult
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error");
            }
        }

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
                return StatusCode(500, "Internal server error");
            }
        }

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
                return StatusCode(500, "Internal server error");
            }
        }

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
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("queue/process")]
        public async Task<ActionResult> ProcessQueue()
        {
            try
            {
                await _chatAssignmentService.ProcessQueueAsync();
                return Ok(new { message = "Queue processed successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error");
            }
        }

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
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
