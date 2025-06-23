namespace ChatApp.Models
{
    public class PoolResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public int? Position { get; set; }
        public DateTime? EstimatedWaitTime { get; set; }
        public int? AgentId { get; set; }
        public string? AgentName { get; set; }
        public int ChatId { get; set; }
    }

    public class PollRequest
    {
        public string CustomerId { get; set; } = string.Empty;
        public string ClientInfo { get; set; } = string.Empty;
    }
}
