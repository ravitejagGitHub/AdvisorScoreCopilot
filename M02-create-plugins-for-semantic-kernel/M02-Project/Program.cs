using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;


string filePath = Path.GetFullPath("appsettings.json");

if (!File.Exists(filePath))
{
    Console.WriteLine($"Configuration file not found: {filePath}");
    return;
}

var config = new ConfigurationBuilder()
    .AddJsonFile(filePath)
    .Build();

// Set your values in appsettings.json
string modelId = config["modelId"]!;
string endpoint = config["endpoint"]!;
string apiKey = config["apiKey"]!;

// Create a kernel with Azure OpenAI chat completion
var builder = Kernel.CreateBuilder();
builder.AddAzureOpenAIChatCompletion(modelId, endpoint, apiKey);

var kernel = builder.Build();

var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
kernel.Plugins.AddFromType<AdvisorScorePlugin>("AdvisorScorePlugin");
kernel.Plugins.AddFromType<AdvisorScoreDocumentationPlugin>("AdvisorScoreDocumentationPlugin");

OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new() 
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};


string prompt = """
    You are a helpful advisor score support engineer agent. Your role is to analyze score data and explain the score logic to users.
    
    Prompt Decoding:
    Decode the prompt to extract subscription IDs, scores, and other relevant information.
    Identify the subscription ID and score from the prompt and provide a detailed explanation of the score logic.
   
    Unique Identifiers:
    Recognize UUID or GUID type unique identifiers provided by the user, such as b4a7f3e1-9d5e-4a9c-8b5e-2b0a7c8f6d3d or those found in the prefix of an Azure resource ID, e.g., /subscriptions/{subscriptionId}.
    
    Score Categories:
    Determine the category of the score or topic the user needs help with by decoding the score context using the name property.
    Possible values include: 'Advisor', 'Overall', 'Cost', 'Security', 'OperationalExcellence', 'Performance', 'HighAvailability', or 'Reliability'.
    Map any synonyms to one of these categories based on the name property in the advisor score context.
    
    Documentation Utilization:
    Use the advisor score documentation to provide answers to queries.
    Utilize this documentation for calculating scores for single and multiple subscriptions or any other specific sub-category score calculations.
    """;

// Create the kernel function from the prompt
var activitiesFunction = kernel.CreateFunctionFromPrompt(prompt);


// InvokeAsync on the kernel object
//var result = await kernel.InvokeAsync(activitiesFunction);
// Console.WriteLine(result);

var history = new ChatHistory();

Console.WriteLine("Type 'exit' to quit the program.");
do
{
    Console.Write("User: ");
    string input = Console.ReadLine()!;
    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }
    history.AddUserMessage(input);
    await GetReply();
} while (true);



async Task GetReply() {
    ChatMessageContent reply = await chatCompletionService.GetChatMessageContentAsync(
        history,
        executionSettings: openAIPromptExecutionSettings,
        kernel: kernel
    );
    Console.WriteLine("Assistant: " + reply.ToString());
    history.AddAssistantMessage(reply.ToString());
}