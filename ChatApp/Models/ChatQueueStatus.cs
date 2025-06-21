namespace ChatApp.Models
{
    public class ChatQueueStatus
    {
        public int TotalQueuedChats { get; set; }
        public int TotalCapacity { get; set; }
        public int MaxQueueLength { get; set; }
        public bool IsOverflowActive { get; set; }
        public List<TeamStatus> TeamStatuses { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }

    public class TeamStatus
    {
        public string TeamId { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public int AvailableAgents { get; set; }
        public int TotalAgents { get; set; }
        public int TeamCapacity { get; set; }
        public int ActiveChats { get; set; }
        public List<AgentStatus> AgentStatuses { get; set; } = new();
        public bool IsActive { get; set; }
    }    public class AgentStatus
    {
        public int AgentId { get; set; }
        public string AgentName { get; set; } = string.Empty;
        public Seniority Seniority { get; set; }
        public AgentWorkStatus Status { get; set; }
        public int CurrentChats { get; set; }
        public int MaxChats { get; set; }
        public DateTime? ShiftEndTime { get; set; }
    }
}
