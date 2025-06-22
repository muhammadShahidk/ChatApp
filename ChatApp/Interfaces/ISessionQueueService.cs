using ChatApp.Models;

namespace ChatApp.Interfaces
{
    public interface ISessionQueueService
    {
        Task<ChatSession> CreateChatSessionAsync(string customerId, string customerName);
        Task<ChatQueueStatus> GetQueueStatusAsync();
        Task<List<ChatSession>> GetQueuedChatsAsync();
        Task<ChatAcceptanceResult> CanAcceptNewChatAsync();
    }
}