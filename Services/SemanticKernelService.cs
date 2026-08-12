using Azure;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Portal.Services.SemanticKernelPlugins;
using Portal.DBServices;

namespace Portal.Services
{
    public interface ISemanticKernelService
    {
        Task<string> ProcessUserMessageAsync(string userMessage);
    }

    public class SemanticKernelService : ISemanticKernelService
    {
        private readonly Kernel _kernel;
        private readonly IChatCompletionService _chatCompletionService;
        private readonly ILogger<SemanticKernelService> _logger;
        private readonly IPortalItemService _portalItemService;
        private readonly IPortalCategoryServices _portalCategoryService;
        private readonly IPortalContentService _portalContentService;

        public SemanticKernelService(IConfiguration configuration, 
            ILogger<SemanticKernelService> logger, 
            IPortalItemService portalItemService,
            IPortalCategoryServices portalCategoryService,
            IPortalContentService portalContentService  
            )
        {
            _logger = logger;
            _portalItemService = portalItemService;
            _portalCategoryService = portalCategoryService;
            _portalContentService = portalContentService;   

            // Get Ollama configuration from appsettings
            var ollamaEndpoint = configuration["Ollama:Endpoint"] ?? "http://localhost:11434";
            var ollamaModel = configuration["Ollama:Model"] ?? "llama2";

            // Initialize Semantic Kernel with Ollama chat completion
            IKernelBuilder builder = Kernel.CreateBuilder();
            builder.AddOllamaChatCompletion(
                modelId: ollamaModel,
                endpoint: new Uri(ollamaEndpoint)
                );

            _kernel = builder.Build();
            _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();

            _logger.LogInformation($"Semantic Kernel initialized with Ollama endpoint: {ollamaEndpoint}, model: {ollamaModel}");
        }

        public async Task<string> ProcessUserMessageAsync(string userMessage)
        {
            try
            {
                // Prepare the agent tools/plugins
                EnvInfoPlugin envInfo = new EnvInfoPlugin();
                ProductsPlugin productsPlugin = new ProductsPlugin(_portalItemService, _portalCategoryService);
                ContentsPlugin contentPlugin = new ContentsPlugin(_portalContentService);

                //string temp = await productsPlugin.GetProducts();
                //return temp;


                _kernel.Plugins.AddFromObject(envInfo);
                _kernel.Plugins.AddFromObject(productsPlugin);
                _kernel.Plugins.AddFromObject(contentPlugin);
                OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                };

                // Create a chat history with system context about the car rental company
                var chatHistory = new ChatHistory();
                chatHistory.AddSystemMessage(@"You are an AI assistant for a car rental company. 
You provide helpful, accurate, and friendly responses about car rental services, pricing, fleet information, policies, and bookings.
Keep responses concise and professional.");

                chatHistory.AddUserMessage(userMessage);

                // Get response from Ollama
                var response = await _chatCompletionService.GetChatMessageContentAsync(
                    chatHistory,
                    executionSettings: openAIPromptExecutionSettings,
                    kernel: _kernel);

                return response.Content ?? "I apologize, but I could not generate a response. Please try again.";
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"Connection error with Ollama: {ex.Message}");
                return "I'm unable to connect to the AI service right now. Please try again later or contact our support team.";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing message with Semantic Kernel: {ex.Message}");
                return "An error occurred while processing your message. Please try again or contact our support team for assistance.";
            }
        }
    }
}
