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
kernel.Plugins.AddFromType<FlightBookingPlugin>("FlightBooking");
OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new() 
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};


// string prompt = """
//     You are a helpful travel guide. 
//     I'm visiting {{$city}}. {{$background}}. What are some activities I should do today?
//     """;
// string city = "India";
// string background = "I really enjoy adventurous and new places.";

// // Create the kernel function from the prompt
// var activitiesFunction = kernel.CreateFunctionFromPrompt(prompt);

// // Create the kernel arguments
// var arguments = new KernelArguments { ["city"] = city, ["background"] = background };

// // InvokeAsync on the kernel object
// var result = await kernel.InvokeAsync(activitiesFunction, arguments);
// Console.WriteLine(result);

var history = new ChatHistory();
history.AddSystemMessage("The year is 2025 and the current month is January");

GetInput();
await GetReply();
GetInput();
await GetReply();


void GetInput() {
    Console.Write("User: ");
    string input = Console.ReadLine()!;
    history.AddUserMessage(input);
}

async Task GetReply() {
    ChatMessageContent reply = await chatCompletionService.GetChatMessageContentAsync(
        history,
        executionSettings: openAIPromptExecutionSettings,
        kernel: kernel
    );
    Console.WriteLine("Assistant: " + reply.ToString());
    history.AddAssistantMessage(reply.ToString());
}

void AddUserMessage(string msg) {
    Console.WriteLine("User: " + msg);
    history.AddUserMessage(msg);
}