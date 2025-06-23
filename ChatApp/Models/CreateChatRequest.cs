namespace ChatApp.Models
{
    public class CreateChatRequest
    {
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
    }
}
