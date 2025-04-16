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
OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new() 
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};


string prompt = """
    You are a helpful advisor score support engineer agent, where you can analise score data and help use exaplin the score logic. 
    You can able to decode prompt and get subscription ids and score and other information from the prompt.
    Idenity the subscription ID and score from the prompt and provide a detailed explanation of the score logic.
    A UUID or GUID type unique identifier provided by the user e.g. b4a7f3e1-9d5e-4a9c-8b5e-2b0a7c8f6d3d or found in the prefix of an Azure resource id, e.g. /subscriptions/{subscriptionId}.
    The category of the score or topic user wants help by decoding score context with name property. Possible values include: 'Advisor' or 'Overall', 'Cost', 'Security', 'OperationalExcellence', 'Performance', 'HighAvailability' or 'Reliability'. Map any synonyms to one of the categories based name propery in advisor score context.
    """;

// Create the kernel function from the prompt
var activitiesFunction = kernel.CreateFunctionFromPrompt(prompt);


// InvokeAsync on the kernel object
var result = await kernel.InvokeAsync(activitiesFunction);
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




// void GetInput() {
//     Console.Write("User: ");
//     string input = Console.ReadLine()!;
//     history.AddUserMessage(input);
// }

async Task GetReply() {
    ChatMessageContent reply = await chatCompletionService.GetChatMessageContentAsync(
        history,
        executionSettings: openAIPromptExecutionSettings,
        kernel: kernel
    );
    Console.WriteLine("Assistant: " + reply.ToString());
    history.AddAssistantMessage(reply.ToString());
}