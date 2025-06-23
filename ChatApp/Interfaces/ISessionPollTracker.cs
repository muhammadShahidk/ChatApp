using ChatApp.Models;

namespace ChatApp.Interfaces
{
    public interface ISessionPollTracker
    {
        Task RecordPollAsync(int chatId, string customerId, string? clientInfo = null);
        Task<SessionPollTracker?> GetPollTrackerAsync(int chatId);
        Task<List<SessionPollTracker>> GetAllPollTrackersAsync();
        Task<List<int>> GetSessionsToMarkInactiveAsync();
        Task RemovePollTrackerAsync(int chatId);
        Task<List<SessionActivityStats>> GetActivityStatsAsync();
        Task CleanupOldDataAsync();
        event EventHandler<SessionActivityEventArgs>? SessionShouldBeMarkedInactive;

        event EventHandler<SessionActivityEventArgs>? PollReceived;
    }
}
