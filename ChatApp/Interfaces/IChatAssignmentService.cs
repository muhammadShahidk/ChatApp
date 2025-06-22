using ChatApp.Models;

namespace ChatApp.Interfaces
{
    public interface IChatAssignmentService
    {
        Task<ChatSession> CreateChatSessionAsync(string customerId, string customerName);
        Task<bool> AssignChatToAgentAsync(int chatId);
        Task<bool> CompleteChatAsync(int chatId);
        Task<ChatQueueStatus> GetQueueStatusAsync();
        Task<List<ChatSession>> GetQueuedChatsAsync();
        Task<List<ChatSession>> GetAgentChatsAsync(int agentId);
        Task ProcessQueueAsync();
        Task<ChatAcceptanceResult> CanAcceptNewChatAsync();
    }
}
