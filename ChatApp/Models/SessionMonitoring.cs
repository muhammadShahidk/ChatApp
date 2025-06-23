namespace ChatApp.Models
{
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
        
        public TimeSpan TimeSinceCreation => DateTime.Now - CreatedAt;
        public double PollFrequency => TotalPollCount > 0 && FirstPollTime.HasValue 
            ? TotalPollCount / TimeSinceCreation.TotalMinutes 
            : 0;
    }

    public class MonitoringConfiguration
    {
        public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(1);
        public int MaxMissedPolls { get; set; } = 3;
        public TimeSpan AbandonedSessionRetentionTime { get; set; } = TimeSpan.FromHours(1);
        public int MaxPollHistorySize { get; set; } = 100; // Keep last 100 polls
    }

    public class SessionMonitoringResult
    {
        public int TotalSessions { get; set; }
        public int ActiveSessions { get; set; }
        public int InactiveSessions { get; set; }
        public List<ChatSession> NewlyInactiveSessions { get; set; } = new();
        public List<SessionActivityStats> ActivityStats { get; set; } = new();
        public DateTime MonitoringTime { get; set; } = DateTime.Now;
        public TimeSpan MonitoringDuration { get; set; }
    }

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

    public class SessionActivityEventArgs : EventArgs
    {
        public ChatSession Session { get; set; } = null!;
        public SessionPollTracker PollTracker { get; set; } = null!;
        public string EventType { get; set; } = string.Empty;
    }
}
