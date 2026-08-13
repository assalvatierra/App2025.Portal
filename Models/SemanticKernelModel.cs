namespace Portal.SemanticKernelModel
{
    public class ChatRequest
    {
        public string Message { get; set; }
        public List<ChatMessage> History { get; set; } = new();
    }

    public class ChatMessage
    {
        public string Role { get; set; } // "user" or "assistant"
        public string Content { get; set; }
    }
}
