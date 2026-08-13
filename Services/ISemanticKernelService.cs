using Portal.SemanticKernelModel;

namespace Portal.Services
{
    public interface ISemanticKernelService
    {
        Task<string> ProcessUserMessageAsync(string userMessage, List<ChatMessage> chatHistory = null);
    }

}
