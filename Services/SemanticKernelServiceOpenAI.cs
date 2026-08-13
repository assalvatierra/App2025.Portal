using Azure;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Portal.Services.SemanticKernelPlugins;
using Portal.DBServices;
using Portal.Controllers;
using Portal.SemanticKernelModel;

namespace Portal.Services
{
    public class SemanticKernelServiceOpenAI : ISemanticKernelService
    {
        private readonly Kernel _kernel;
        private readonly IChatCompletionService _chatCompletionService;
        private readonly ILogger<SemanticKernelService> _logger;
        private readonly IPortalItemService _portalItemService;
        private readonly IPortalCategoryServices _portalCategoryService;
        private readonly IPortalContentService _portalContentService;

        public SemanticKernelServiceOpenAI(IConfiguration configuration, 
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

            // Get OpenAI configuration from appsettings
            var openAIApiKey = configuration["OpenAI:ApiKey"];
            var openAIModel = configuration["OpenAI:Model"] ?? "gpt-4o-mini";
            var openAIEndpoint = configuration["OpenAI:Endpoint"]; // Optional: for vLLM or other OpenAI-compatible endpoints

            if (string.IsNullOrEmpty(openAIApiKey))
            {
                throw new InvalidOperationException("OpenAI API Key is not configured. Please set OpenAI:ApiKey in appsettings.");
            }

            // Initialize Semantic Kernel with OpenAI chat completion
            IKernelBuilder builder = Kernel.CreateBuilder();

            // If endpoint is provided, use custom OpenAI-compatible API (e.g., vLLM on RunPod)
            if (!string.IsNullOrEmpty(openAIEndpoint))
            {
                builder.AddOpenAIChatCompletion(
                    modelId: openAIModel,
                    apiKey: openAIApiKey,
                    endpoint: new Uri(openAIEndpoint)
                    );
                _logger.LogInformation($"Semantic Kernel initialized with custom OpenAI endpoint: {openAIEndpoint}, model: {openAIModel}");
            }
            else
            {
                // Use official OpenAI API
                builder.AddOpenAIChatCompletion(
                    modelId: openAIModel,
                    apiKey: openAIApiKey
                    );
                _logger.LogInformation($"Semantic Kernel initialized with OpenAI API, model: {openAIModel}");
            }

            _kernel = builder.Build();
            _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
        }

        public async Task<string> ProcessUserMessageAsync(string userMessage, List<ChatMessage> chatHistory = null)
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
                var chatHistoryObj = new ChatHistory();
                chatHistoryObj.AddSystemMessage(@"You are an AI assistant for a car rental company. 
You provide helpful, accurate, and friendly responses about car rental services, pricing, fleet information, policies, and bookings.
Keep responses concise and professional.Maintain a chat friendly format in the responses and avoid markdown formatting.");

                // Add previous chat history if provided
                if (chatHistory != null && chatHistory.Count > 0)
                {
                    foreach (var msg in chatHistory)
                    {
                        if (msg.Role?.ToLower() == "user")
                        {
                            chatHistoryObj.AddUserMessage(msg.Content);
                        }
                        else if (msg.Role?.ToLower() == "assistant")
                        {
                            chatHistoryObj.AddAssistantMessage(msg.Content);
                        }
                    }
                }

                chatHistoryObj.AddUserMessage(userMessage);

                // Get response from OpenAI
                var response = await _chatCompletionService.GetChatMessageContentAsync(
                    chatHistoryObj,
                    executionSettings: openAIPromptExecutionSettings,
                    kernel: _kernel);

                return response.Content ?? "I apologize, but I could not generate a response. Please try again.";
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"Connection error with OpenAI: {ex.Message}");
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
