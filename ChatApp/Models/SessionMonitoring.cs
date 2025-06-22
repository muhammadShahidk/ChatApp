namespace ChatApp.Models
{
    /// <summary>
    /// Tracks polling activity for individual chat sessions
    /// </summary>
    public class SessionPollTracker
    {
        public int ChatId { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastPollTime { get; set; } = DateTime.Now;
        public DateTime? FirstPollTime { get; set; }
        public int TotalPollCount { get; set; } = 0;
        public int ConsecutiveMissedPolls { get; set; } = 0;
        public List<DateTime> PollHistory { get; set; } = new();
        
        // Computed properties
        public TimeSpan TimeSinceLastPoll => DateTime.Now - LastPollTime;
        public TimeSpan TimeSinceCreation => DateTime.Now - CreatedAt;
        public bool HasMissedRecentPoll => TimeSinceLastPoll > TimeSpan.FromSeconds(3);
        public double PollFrequency => TotalPollCount > 0 && FirstPollTime.HasValue 
            ? TotalPollCount / TimeSinceCreation.TotalMinutes 
            : 0;
    }

    /// <summary>
    /// Configuration for session monitoring behavior
    /// </summary>
    public class MonitoringConfiguration
    {
        public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(1);
        public int MaxMissedPolls { get; set; } = 3;
        public TimeSpan MonitoringInterval { get; set; } = TimeSpan.FromSeconds(1);
        public TimeSpan SessionTimeout { get; set; } = TimeSpan.FromMinutes(10);
        public bool EnableDetailedLogging { get; set; } = true;
        public bool AutoRemoveAbandonedSessions { get; set; } = false;
        public TimeSpan AbandonedSessionRetentionTime { get; set; } = TimeSpan.FromHours(1);
        public int MaxPollHistorySize { get; set; } = 100; // Keep last 100 polls
    }

    /// <summary>
    /// Result of polling update operation
    /// </summary>
    public class SessionPollUpdate
    {
        public int ChatId { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public DateTime PollTime { get; set; } = DateTime.Now;
        public string ClientInfo { get; set; } = string.Empty; // Browser, IP, etc.
        public bool IsSuccessful { get; set; } = true;
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Result of monitoring check operation
    /// </summary>
    public class SessionMonitoringResult
    {
        public int TotalSessions { get; set; }
        public int ActiveSessions { get; set; }
        public int InactiveSessions { get; set; }
        public int AbandonedSessions { get; set; }
        public List<ChatSession> NewlyInactiveSessions { get; set; } = new();
        public List<SessionActivityStats> ActivityStats { get; set; } = new();
        public DateTime MonitoringTime { get; set; } = DateTime.Now;
        public TimeSpan MonitoringDuration { get; set; }
    }

    /// <summary>
    /// Detailed activity statistics for a session
    /// </summary>
    public class SessionActivityStats
    {
        public int ChatId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public ChatStatus Status { get; set; }
        public TimeSpan TimeInQueue { get; set; }
        public int TotalPolls { get; set; }
        public int MissedPolls { get; set; }
        public DateTime LastPoll { get; set; }
        public bool IsActive { get; set; }
        public double PollFrequency { get; set; } // polls per minute
        public int? QueuePosition { get; set; }
        public TimeSpan TimeSinceLastPoll { get; set; }
        public bool ShouldBeMarkedInactive { get; set; }
    }

    /// <summary>
    /// Event arguments for session activity events
    /// </summary>
    public class SessionActivityEventArgs : EventArgs
    {
        public ChatSession Session { get; set; } = null!;
        public SessionPollTracker PollTracker { get; set; } = null!;
        public string EventType { get; set; } = string.Empty; // "poll_received", "marked_inactive", "abandoned"
        public DateTime EventTime { get; set; } = DateTime.Now;
    }
}
