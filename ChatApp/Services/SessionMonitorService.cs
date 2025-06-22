using ChatApp.Interfaces;
using ChatApp.Models;
using Microsoft.Extensions.Logging;

namespace ChatApp.Services
{
    public interface ISessionMonitorService
    {
        // Core monitoring operations
        Task<bool> RecordPollAsync(int chatId, string customerId, string? clientInfo = null);
        Task<SessionMonitoringResult> CheckSessionActivityAsync();
        
        // Session lifecycle management
        Task RegisterSessionAsync(ChatSession session);
        Task UnregisterSessionAsync(int chatId);
        Task<ChatSession?> GetSessionAsync(int chatId);
        
        // Session queries
        List<ChatSession> GetActiveSessions();
        List<ChatSession> GetInactiveSessions();
        Task<List<SessionActivityStats>> GetSessionActivityStatsAsync();
        
        // Configuration and monitoring
        void UpdateConfiguration(MonitoringConfiguration config);
        Task<SessionMonitoringResult> GetMonitoringStatusAsync();
        
        // Events for other services to subscribe to
        event EventHandler<ChatSession>? SessionMarkedInactive;
        event EventHandler<SessionMonitoringResult>? MonitoringCompleted;
    }

    /// <summary>
    /// Coordinates session monitoring using the poll tracker
    /// Manages the connection between sessions and polling activity
    /// </summary>
    public class SessionMonitorService : ISessionMonitorService
    {
        private readonly ILogger<SessionMonitorService> _logger;
        private readonly ISessionPollTracker _pollTracker;
        private readonly Dictionary<int, ChatSession> _sessions;
        private readonly object _lockObject = new object();
        
        // Events
        public event EventHandler<ChatSession>? SessionMarkedInactive;
        public event EventHandler<SessionMonitoringResult>? MonitoringCompleted;

        public SessionMonitorService(
            ILogger<SessionMonitorService> logger, 
            ISessionPollTracker pollTracker)
        {
            _logger = logger;
            _pollTracker = pollTracker;
            _sessions = new Dictionary<int, ChatSession>();
            
            // Subscribe to poll tracker events
            _pollTracker.SessionShouldBeMarkedInactive += OnSessionShouldBeMarkedInactive;
            _pollTracker.PollReceived += OnPollReceived;
        }

        /// <summary>
        /// Record a polling request from a customer
        /// </summary>
        public async Task<bool> RecordPollAsync(int chatId, string customerId, string? clientInfo = null)
        {
            try
            {
                await _pollTracker.RecordPollAsync(chatId, customerId, clientInfo);
                
                _logger.LogDebug("Poll recorded for chat {ChatId} from customer {CustomerId}", 
                    chatId, customerId);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording poll for chat {ChatId}", chatId);
                return false;
            }
        }

        /// <summary>
        /// Check which sessions need to be marked inactive due to missed polls
        /// </summary>
        public async Task<SessionMonitoringResult> CheckSessionActivityAsync()
        {
            var startTime = DateTime.Now;
            
            try
            {
                // Get sessions that should be marked inactive
                var sessionsToMarkInactive = await _pollTracker.GetSessionsToMarkInactiveAsync();
                var newlyInactiveSessions = new List<ChatSession>();

                lock (_lockObject)
                {
                    foreach (var chatId in sessionsToMarkInactive)
                    {
                        if (_sessions.TryGetValue(chatId, out var session) && session.IsActive)
                        {
                            session.IsActive = false;
                            newlyInactiveSessions.Add(session);
                            
                            _logger.LogInformation("Marked session {ChatId} as inactive due to missed polls", 
                                chatId);
                        }
                    }
                }

                // Fire events for newly inactive sessions
                foreach (var session in newlyInactiveSessions)
                {
                    SessionMarkedInactive?.Invoke(this, session);
                }

                // Get monitoring summary
                var monitoringResult = new SessionMonitoringResult
                {
                    TotalSessions = _sessions.Count,
                    ActiveSessions = _sessions.Values.Count(s => s.IsActive),
                    InactiveSessions = _sessions.Values.Count(s => !s.IsActive),
                    NewlyInactiveSessions = newlyInactiveSessions,
                    ActivityStats = await GetSessionActivityStatsAsync(),
                    MonitoringTime = DateTime.Now,
                    MonitoringDuration = DateTime.Now - startTime
                };

                MonitoringCompleted?.Invoke(this, monitoringResult);
                
                return monitoringResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during session activity check");
                throw;
            }
        }

        public async Task RegisterSessionAsync(ChatSession session)
        {
            lock (_lockObject)
            {
                _sessions[session.Id] = session;
            }
            
            _logger.LogDebug("Registered session {ChatId} for monitoring", session.Id);
            await Task.CompletedTask;
        }

        public async Task UnregisterSessionAsync(int chatId)
        {
            lock (_lockObject)
            {
                _sessions.Remove(chatId);
            }
            
            // Also remove from poll tracker
            await _pollTracker.RemovePollTrackerAsync(chatId);
            
            _logger.LogDebug("Unregistered session {ChatId} from monitoring", chatId);
        }

        public async Task<ChatSession?> GetSessionAsync(int chatId)
        {
            lock (_lockObject)
            {
                _sessions.TryGetValue(chatId, out var session);
                return session;
            }
        }

        public List<ChatSession> GetActiveSessions()
        {
            lock (_lockObject)
            {
                return _sessions.Values.Where(s => s.IsActive).ToList();
            }
        }

        public List<ChatSession> GetInactiveSessions()
        {
            lock (_lockObject)
            {
                return _sessions.Values.Where(s => !s.IsActive).ToList();
            }
        }

        public async Task<List<SessionActivityStats>> GetSessionActivityStatsAsync()
        {
            var pollStats = await _pollTracker.GetActivityStatsAsync();
            var combinedStats = new List<SessionActivityStats>();

            lock (_lockObject)
            {
                foreach (var session in _sessions.Values)
                {
                    var pollStat = pollStats.FirstOrDefault(p => p.ChatId == session.Id);
                    
                    var stat = new SessionActivityStats
                    {
                        ChatId = session.Id,
                        CustomerName = session.CustomerName,
                        CustomerId = session.CustomerId,
                        Status = session.Status,
                        IsActive = session.IsActive,
                        QueuePosition = session.QueuePosition,
                        TimeInQueue = DateTime.Now - session.CreatedAt,
                        // Merge poll tracking data if available
                        TotalPolls = pollStat?.TotalPolls ?? 0,
                        MissedPolls = pollStat?.MissedPolls ?? 0,
                        LastPoll = pollStat?.LastPoll ?? session.CreatedAt,
                        PollFrequency = pollStat?.PollFrequency ?? 0,
                        TimeSinceLastPoll = pollStat?.TimeSinceLastPoll ?? TimeSpan.Zero,
                        ShouldBeMarkedInactive = pollStat?.ShouldBeMarkedInactive ?? false
                    };
                    
                    combinedStats.Add(stat);
                }
            }

            return combinedStats;
        }

        public void UpdateConfiguration(MonitoringConfiguration config)
        {
            // Configuration is handled by the poll tracker
            _logger.LogInformation("Monitoring configuration updated");
        }

        public async Task<SessionMonitoringResult> GetMonitoringStatusAsync()
        {
            return await CheckSessionActivityAsync();
        }

        private void OnSessionShouldBeMarkedInactive(object? sender, SessionActivityEventArgs e)
        {
            // This event is already handled in CheckSessionActivityAsync
            // This is just for logging/debugging
            _logger.LogDebug("Poll tracker indicates session {ChatId} should be marked inactive", 
                e.PollTracker.ChatId);
        }

        private void OnPollReceived(object? sender, SessionActivityEventArgs e)
        {
            _logger.LogTrace("Poll received for session {ChatId}", e.PollTracker.ChatId);
        }
    }
}
