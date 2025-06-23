namespace ChatApp.Models
{
    public enum ChatStatus
    {
        Queued,
        InProgress,
        Completed,
        Abandoned
    }    public class ChatSession
    {
        public int Id { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public ChatStatus Status { get; set; }
        public int? AssignedAgentId { get; set; }
        public Agent? AssignedAgent { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? AssignedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string TeamId { get; set; } = string.Empty;
        public List<ChatMessage> Messages { get; set; } = new();

        public bool IsActive { get; set; } = true;
        
        public int? QueuePosition { get; set; }
        public DateTime? EstimatedAssignmentTime { get; set; }
    }

    public class ChatMessage
    {
        public int Id { get; set; }
        public int ChatSessionId { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool IsFromAgent { get; set; }
    }

    public class ChatAcceptanceResult
    {
        public bool CanAccept { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int CurrentQueueLength { get; set; }
        public int MaxQueueLength { get; set; }
        public bool IsOverflowActive { get; set; }
    }
}
