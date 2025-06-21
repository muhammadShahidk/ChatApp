namespace ChatApp.Models
{
    public class Team
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<Agent> Agents { get; set; } = new();
        public Shift? Shift { get; set; } // null for 24/7 teams
        public bool IsOverflowTeam { get; set; }
        public int TotalCapacity => Agents
            .Where(a => a.Status != AgentWorkStatus.Offline)
            .Sum(a => a.MaxConcurrentChats);

        public int MaxQueueLength => (int)(TotalCapacity * 1.5);

        public List<Agent> AvailableAgents => Agents
            .Where(a => a.CanTakeNewChat)
            .OrderBy(a => a.Seniority) // Junior first for round-robin
            .ThenBy(a => a.CurrentChatCount)
            .ToList();
    }
}
