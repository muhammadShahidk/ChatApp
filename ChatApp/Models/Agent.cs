namespace ChatApp.Models
{
    public enum Seniority
    {
        Junior,
        MidLevel,
        Senior,
        TeamLead
    }

    public enum Shift
    {
        Morning,   // 8 AM - 4 PM
        Evening,   // 4 PM - 12 AM
        Night      // 12 AM - 8 AM
    }
    public enum AgentWorkStatus
    {
        Available,
        Busy,
        ShiftEnding,
        Offline
    }

    public class Agent
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Seniority Seniority { get; set; }
        public Shift CurrentShift { get; set; }
        public AgentWorkStatus Status { get; set; }
        public int CurrentChatCount { get; set; }
        public List<int> AssignedChatIds { get; set; } = new();
        public DateTime ShiftStartTime { get; set; }
        public DateTime ShiftEndTime { get; set; }
        public string TeamId { get; set; } = string.Empty;
        public bool IsOverflowTeam { get; set; }

        public double EfficiencyMultiplier => Seniority switch
        {
            Seniority.Junior => 0.4,
            Seniority.MidLevel => 0.6,
            Seniority.Senior => 0.8,
            Seniority.TeamLead => 0.5,
            _ => 0.4
        };

        public int MaxConcurrentChats => (int)(10 * EfficiencyMultiplier);
        public bool CanTakeNewChat => Status == AgentWorkStatus.Available &&
                                  CurrentChatCount < MaxConcurrentChats &&
                                  Status != AgentWorkStatus.ShiftEnding;
    }
}
