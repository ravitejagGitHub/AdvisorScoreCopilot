using System.ComponentModel;
using System.Text.Json;
using UglyToad.PdfPig;
using Microsoft.SemanticKernel;
using Azure;
using Azure.AI.FormRecognizer;
using Azure.AI.FormRecognizer.DocumentAnalysis;

public class AdvisorScoreDocumentationPlugin
{
    private const string KnowledgeFilePath = "advisor_knowledge.json";
    private const string AdvisorScoreDocumentation = "AdvisorScoreDocumentation.pdf";
    private List<string> knowledgeChunks;
    private readonly DocumentAnalysisClient _documentAnalysisClient;

    public AdvisorScoreDocumentationPlugin()
    {
        // Initialize Azure AI Document Intelligence client
        string endpoint = "https://rg-hackathon-document-intelligence.cognitiveservices.azure.com/"; // Replace with your endpoint
        string apiKey = "<your-form-recognizer-api-key>"; // Replace with your API key
        _documentAnalysisClient = new DocumentAnalysisClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        AnalyzeDocumentAsync(AdvisorScoreDocumentation).Wait();

        // Load knowledge chunks from the file
        ExtractKnowledgeFromPdf();
        knowledgeChunks = LoadKnowledgeFromFile();
    }

    [KernelFunction("query_advisor_documentation")]
    [Description("")]
    [return: Description("Relevant information from the documentation and clarification of the score logic.")]
    public string QueryDocumentation(string query)
    {
        // Simple keyword matching for demonstration
        var relevantChunks = knowledgeChunks
            .Where(chunk => chunk.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(3);

        return string.Join("\n\n", relevantChunks);
    }

    [KernelFunction("analyze_document")]
    [Description("Analyzes a document using Azure AI Document Intelligence and extracts structured data. Queries the knowledge extracted from Advisor Score documentation.Use this documentation to answer questions related advisor score and potential score caliculation and calculate the score using fromula.")]
    [return: Description("Extracted structured data from the document.")]
    public async Task<string> AnalyzeDocumentAsync(string documentPath)
    {
        if (!File.Exists(documentPath))
        {
            return $"The file '{documentPath}' was not found. Please provide a valid document path.";
        }

        try
        {
            using var stream = File.OpenRead(documentPath);
            AnalyzeDocumentOperation operation = await _documentAnalysisClient.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-document", stream);

            var result = operation.Value;

            var extractedData = new
            {
                KeyValuePairs = result.KeyValuePairs.Select(kvp => new { Key = kvp.Key.Content, Value = kvp.Value.Content }),
                Tables = result.Tables.Select(table => new
                {
                    Rows = table.Cells.GroupBy(cell => cell.RowIndex).Select(row => row.Select(cell => cell.Content))
                })
            };

            return JsonSerializer.Serialize(extractedData, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return $"Error analyzing document: {ex.Message}";
        }
    }

    public void ExtractKnowledgeFromPdf()
    {
        if (!File.Exists(AdvisorScoreDocumentation))
        {
            throw new FileNotFoundException($"The file '{AdvisorScoreDocumentation}' was not found. Please ensure the PDF file is in the correct location.");
        }

        var text = ExtractTextFromPdf(AdvisorScoreDocumentation);
        knowledgeChunks = ChunkText(text);
        SaveKnowledgeToFile();
    }

    private string ExtractTextFromPdf(string pdfPath)
    {
        using var pdf = PdfDocument.Open(pdfPath);
        return string.Join("\n", pdf.GetPages().Select(page => page.Text));
    }

    private List<string> ChunkText(string text, int chunkSize = 500)
    {
        var words = text.Split(' ');
        var chunks = new List<string>();
        var currentChunk = new List<string>();

        foreach (var word in words)
        {
            if (currentChunk.Sum(w => w.Length) + word.Length > chunkSize)
            {
                chunks.Add(string.Join(" ", currentChunk));
                currentChunk.Clear();
            }
            currentChunk.Add(word);
        }

        if (currentChunk.Any())
        {
            chunks.Add(string.Join(" ", currentChunk));
        }

        return chunks;
    }

    private void SaveKnowledgeToFile()
    {
        var json = JsonSerializer.Serialize(knowledgeChunks, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(KnowledgeFilePath, json);
    }

    private List<string> LoadKnowledgeFromFile()
    {
        if (File.Exists(KnowledgeFilePath))
        {
            var json = File.ReadAllText(KnowledgeFilePath);
            return JsonSerializer.Deserialize<List<string>>(json)!;
        }

        return new List<string>(); // Return an empty list if the file does not exist
    }
}