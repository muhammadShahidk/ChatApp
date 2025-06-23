using ChatApp.Models;

namespace ChatApp.Interfaces
{
    public interface ISessionQueueService
    {
        Task<ChatSession> CreateChatSessionAsync(string customerId, string customerName);
        Task<ChatQueueStatus> GetQueueStatusAsync();
        Task<List<ChatSession>> GetQueuedChatsAsync();
        Task<List<ChatSession>> GetAllChatsAsync();
        Task<bool> AssignChatToAgentAsync(int chatId, int agentId);
        Task<bool> CompleteChatAsync(int chatId);
        Task<ChatAcceptanceResult> CanAcceptNewChatAsync();
    }
}