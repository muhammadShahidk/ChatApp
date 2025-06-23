using ChatApp.Interfaces;
using ChatApp.Models;
using Microsoft.Extensions.Logging;

namespace ChatApp.Services
{
    public interface ISessionMonitorService
    {
        Task<bool> RecordPollAsync(int chatId, string customerId, string? clientInfo = null);
        Task<SessionMonitoringResult> CheckSessionActivityAsync();
        
        Task RegisterSessionAsync(ChatSession session);
        Task UnregisterSessionAsync(int chatId);
        Task<ChatSession?> GetSessionAsync(int chatId);
        
        Task<List<SessionActivityStats>> GetSessionActivityStatsAsync();
        
        Task<SessionMonitoringResult> GetMonitoringStatusAsync();
        
        event EventHandler<ChatSession>? SessionMarkedInactive;
        event EventHandler<SessionMonitoringResult>? MonitoringCompleted;
    }

    public class SessionMonitorService : ISessionMonitorService
    {
        private readonly ILogger<SessionMonitorService> _logger;
        private readonly ISessionPollTracker _pollTracker;
        private readonly Dictionary<int, ChatSession> _sessions;
        private readonly object _lockObject = new object();
        
        public event EventHandler<ChatSession>? SessionMarkedInactive;
        public event EventHandler<SessionMonitoringResult>? MonitoringCompleted;

        public SessionMonitorService(
            ILogger<SessionMonitorService> logger, 
            ISessionPollTracker pollTracker)
        {
            _logger = logger;
            _pollTracker = pollTracker;
            _sessions = new Dictionary<int, ChatSession>();
            
            _pollTracker.SessionShouldBeMarkedInactive += OnSessionShouldBeMarkedInactive;
            _pollTracker.PollReceived += OnPollReceived;
        }

        public async Task<bool> RecordPollAsync(int chatId, string customerId, string? clientInfo = null)
        {
            try
            {
                await _pollTracker.RecordPollAsync(chatId, customerId, clientInfo);
                
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<SessionMonitoringResult> CheckSessionActivityAsync()
        {
            var startTime = DateTime.Now;
            
            try
            {
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
                        }
                    }
                }

                foreach (var session in newlyInactiveSessions)
                {
                    SessionMarkedInactive?.Invoke(this, session);
                }

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
                throw;
            }
        }

        public async Task RegisterSessionAsync(ChatSession session)
        {
            lock (_lockObject)
            {
                _sessions[session.Id] = session;
            }
            
            await Task.CompletedTask;
        }

        public async Task UnregisterSessionAsync(int chatId)
        {
            lock (_lockObject)
            {
                _sessions.Remove(chatId);
            }
            
            await _pollTracker.RemovePollTrackerAsync(chatId);
            
        }

        public async Task<ChatSession?> GetSessionAsync(int chatId)
        {
            lock (_lockObject)
            {
                _sessions.TryGetValue(chatId, out var session);
                return session;
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

        public async Task<SessionMonitoringResult> GetMonitoringStatusAsync()
        {
            return await CheckSessionActivityAsync();
        }

        private void OnSessionShouldBeMarkedInactive(object? sender, SessionActivityEventArgs e)
        {
            _logger.LogDebug("Poll tracker indicates session {ChatId} should be marked inactive", 
                e.PollTracker.ChatId);
        }

        private void OnPollReceived(object? sender, SessionActivityEventArgs e)
        {
            _logger.LogTrace("Poll received for session {ChatId}", e.PollTracker.ChatId);
        }
    }
}
