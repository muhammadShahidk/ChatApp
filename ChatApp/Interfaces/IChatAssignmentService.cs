using ChatApp.Models;

namespace ChatApp.Interfaces
{
    public interface IChatAssignmentService
    {
        
        Task<bool> AssignChatToAgentAsync(int chatId);
        Task<List<ChatSession>> GetAgentChatsAsync(int agentId);
        Task ProcessQueueAsync();
 
    }
}
