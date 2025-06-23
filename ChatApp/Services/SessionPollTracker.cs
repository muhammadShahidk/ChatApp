using ChatApp.Interfaces;
using ChatApp.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace ChatApp.Services
{
    /// <summary>
    /// Tracks polling activity for chat sessions
    /// Independent of session queue management for clean separation of concerns
    /// </summary>
    public class SessionPollTrackerService : ISessionPollTracker
    {
        private readonly ILogger<SessionPollTrackerService> _logger;
        private readonly MonitoringConfiguration _config;
        private readonly ConcurrentDictionary<int, Models.SessionPollTracker> _pollTrackers;

        public event EventHandler<SessionActivityEventArgs>? SessionShouldBeMarkedInactive;
        public event EventHandler<SessionActivityEventArgs>? PollReceived;

        public SessionPollTrackerService(ILogger<SessionPollTrackerService> logger, MonitoringConfiguration? config = null)
        {
            _logger = logger;
            _config = config ?? new MonitoringConfiguration();
            _pollTrackers = new ConcurrentDictionary<int, Models.SessionPollTracker>();
        }

        public async Task RecordPollAsync(int chatId, string customerId, string? clientInfo = null)
        {
            var tracker = _pollTrackers.GetOrAdd(chatId, id => new Models.SessionPollTracker
            {
                ChatId = id,
                CustomerId = customerId,
                CreatedAt = DateTime.Now
            });

            // Update poll information
            var now = DateTime.Now;
            tracker.LastPollTime = now;
            tracker.TotalPollCount++;
            tracker.ConsecutiveMissedPolls = 0; // Reset missed polls counter

            // Set first poll time if this is the first poll
            if (!tracker.FirstPollTime.HasValue)
            {
                tracker.FirstPollTime = now;
            }

            // Add to poll history (keep limited history)
            tracker.PollHistory.Add(now);
            if (tracker.PollHistory.Count > _config.MaxPollHistorySize)
            {
                tracker.PollHistory.RemoveAt(0); // Remove oldest
            }

            _logger.LogDebug("Poll recorded for chat {ChatId} from customer {CustomerId} at {PollTime}", 
                chatId, customerId, now);

            // Fire event
            PollReceived?.Invoke(this, new SessionActivityEventArgs
            {
                Session = null!, // We don't have session reference here - will be populated by caller if needed
                PollTracker = tracker,
                EventType = "poll_received"
            });

            await Task.CompletedTask;
        }

        public async Task<Models.SessionPollTracker?> GetPollTrackerAsync(int chatId)
        {
            _pollTrackers.TryGetValue(chatId, out var tracker);
            return await Task.FromResult(tracker);
        }

        public async Task<List<Models.SessionPollTracker>> GetAllPollTrackersAsync()
        {
            return await Task.FromResult(_pollTrackers.Values.ToList());
        }

        public async Task<List<int>> GetSessionsToMarkInactiveAsync()
        {
            var now = DateTime.Now;
            var sessionsToMarkInactive = new List<int>();

            foreach (var tracker in _pollTrackers.Values)
            {
                var timeSinceLastPoll = now - tracker.LastPollTime;
                
                // Check if we should increment missed polls
                if (timeSinceLastPoll > _config.PollInterval)
                {
                    var missedPolls = (int)(timeSinceLastPoll.TotalSeconds / _config.PollInterval.TotalSeconds);
                    tracker.ConsecutiveMissedPolls = Math.Max(tracker.ConsecutiveMissedPolls, missedPolls);
                }

                // Check if session should be marked inactive
                if (tracker.ConsecutiveMissedPolls >= _config.MaxMissedPolls)
                {
                    sessionsToMarkInactive.Add(tracker.ChatId);
                    
                    _logger.LogInformation("Session {ChatId} should be marked inactive. Missed {MissedPolls} consecutive polls",
                        tracker.ChatId, tracker.ConsecutiveMissedPolls);

                    // Fire event
                    SessionShouldBeMarkedInactive?.Invoke(this, new SessionActivityEventArgs
                    {
                        Session = null!, // Will be populated by caller
                        PollTracker = tracker,
                        EventType = "marked_inactive"
                    });
                }
            }

            return await Task.FromResult(sessionsToMarkInactive);
        }

        public async Task RemovePollTrackerAsync(int chatId)
        {
            if (_pollTrackers.TryRemove(chatId, out var removedTracker))
            {
                _logger.LogDebug("Removed poll tracker for chat {ChatId}", chatId);
            }
            await Task.CompletedTask;
        }

        public async Task<List<SessionActivityStats>> GetActivityStatsAsync()
        {
            var stats = new List<SessionActivityStats>();
            var now = DateTime.Now;

            foreach (var tracker in _pollTrackers.Values)
            {
                var timeSinceLastPoll = now - tracker.LastPollTime;
                var shouldBeInactive = tracker.ConsecutiveMissedPolls >= _config.MaxMissedPolls;

                stats.Add(new SessionActivityStats
                {
                    ChatId = tracker.ChatId,
                    CustomerId = tracker.CustomerId,
                    TotalPolls = tracker.TotalPollCount,
                    MissedPolls = tracker.ConsecutiveMissedPolls,
                    LastPoll = tracker.LastPollTime,
                    PollFrequency = tracker.PollFrequency,
                    TimeSinceLastPoll = timeSinceLastPoll,
                    ShouldBeMarkedInactive = shouldBeInactive,
                    TimeInQueue = tracker.TimeSinceCreation
                });
            }

            return await Task.FromResult(stats);
        }

        public async Task CleanupOldDataAsync()
        {
            var cutoffTime = DateTime.Now - _config.AbandonedSessionRetentionTime;
            var trackersToRemove = _pollTrackers.Values
                .Where(t => t.CreatedAt < cutoffTime)
                .Select(t => t.ChatId)
                .ToList();

            foreach (var chatId in trackersToRemove)
            {
                await RemovePollTrackerAsync(chatId);
            }

            _logger.LogInformation("Cleaned up {Count} old poll trackers", trackersToRemove.Count);
        }

        /// <summary>
        /// Get monitoring summary for dashboard
        /// </summary>
        public async Task<SessionMonitoringResult> GetMonitoringSummaryAsync()
        {
            var allTrackers = await GetAllPollTrackersAsync();
            var inactiveSessions = await GetSessionsToMarkInactiveAsync();
            var activityStats = await GetActivityStatsAsync();

            return new SessionMonitoringResult
            {
                TotalSessions = allTrackers.Count,
                ActiveSessions = allTrackers.Count - inactiveSessions.Count,
                InactiveSessions = inactiveSessions.Count,
                ActivityStats = activityStats,
                MonitoringTime = DateTime.Now
            };
        }
    }
}
