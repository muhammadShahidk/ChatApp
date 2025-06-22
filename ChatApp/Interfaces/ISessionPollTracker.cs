using ChatApp.Models;

namespace ChatApp.Interfaces
{
    /// <summary>
    /// Service for tracking polling activity of chat sessions
    /// Separate from session queue management for clean separation of concerns
    /// </summary>
    public interface ISessionPollTracker
    {
        /// <summary>
        /// Record a polling request from a customer
        /// </summary>
        Task RecordPollAsync(int chatId, string customerId, string? clientInfo = null);

        /// <summary>
        /// Get polling statistics for a specific session
        /// </summary>
        Task<SessionPollTracker?> GetPollTrackerAsync(int chatId);

        /// <summary>
        /// Get all active poll trackers
        /// </summary>
        Task<List<SessionPollTracker>> GetAllPollTrackersAsync();

        /// <summary>
        /// Check which sessions have missed too many polls and should be marked inactive
        /// </summary>
        Task<List<int>> GetSessionsToMarkInactiveAsync();

        /// <summary>
        /// Remove poll tracker when session is completed/abandoned
        /// </summary>
        Task RemovePollTrackerAsync(int chatId);

        /// <summary>
        /// Get activity statistics for monitoring dashboard
        /// </summary>
        Task<List<SessionActivityStats>> GetActivityStatsAsync();

        /// <summary>
        /// Clean up old poll history data
        /// </summary>
        Task CleanupOldDataAsync();

        /// <summary>
        /// Event fired when a session should be marked inactive
        /// </summary>
        event EventHandler<SessionActivityEventArgs>? SessionShouldBeMarkedInactive;

        /// <summary>
        /// Event fired when a poll is received
        /// </summary>
        event EventHandler<SessionActivityEventArgs>? PollReceived;
    }
}
