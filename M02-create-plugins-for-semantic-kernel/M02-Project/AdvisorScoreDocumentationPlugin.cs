using System.ComponentModel;
using System.Text.Json;
using UglyToad.PdfPig;
using Microsoft.SemanticKernel;

public class AdvisorScoreDocumentationPlugin
{
    private const string KnowledgeFilePath = "advisor_knowledge.json";
    private const string AdvisorScoreDocumentation = "AdvisorScoreDocumentation.pdf";

    private List<string> knowledgeChunks;

    public AdvisorScoreDocumentationPlugin()
    {
        // Load knowledge chunks from the file
        ExtractKnowledgeFromPdf();
        knowledgeChunks = LoadKnowledgeFromFile();
    }

    [KernelFunction("query_advisor_documentation")]
    [Description("Queries the knowledge extracted from Advisor Score documentation. Use this documentation to answer questions related advisor score and potential score caliculation and calculate the score using fromula.")]
    [return: Description("Relevant information from the documentation and clarification of the score logic.")]
    public string QueryDocumentation(string query)
    {
        // Simple keyword matching for demonstration
        var relevantChunks = knowledgeChunks
            .Where(chunk => chunk.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(3);

        return string.Join("\n\n", relevantChunks);
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
        if (!File.Exists(KnowledgeFilePath)) {
            throw new FileNotFoundException($"The file '{KnowledgeFilePath}' was not found. Please ensure the knowledge file is in the correct location.");
        }
        var json = File.ReadAllText(KnowledgeFilePath);
        return JsonSerializer.Deserialize<List<string>>(json)!;
    }
}