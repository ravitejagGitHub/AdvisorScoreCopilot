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

string prompt = """
    You are a helpful travel guide. 
    I'm visiting {{$city}}. {{$background}}. What are some activities I should do today?
    """;
string city = "Barcelona";
string background = "I really enjoy art and dance.";

// Create the kernel function from the prompt
var activitiesFunction = kernel.CreateFunctionFromPrompt(prompt);

// Create the kernel arguments
var arguments = new KernelArguments { ["city"] = city, ["background"] = background };

// InvokeAsync on the kernel object
var result = await kernel.InvokeAsync(activitiesFunction, arguments);
Console.WriteLine(result);