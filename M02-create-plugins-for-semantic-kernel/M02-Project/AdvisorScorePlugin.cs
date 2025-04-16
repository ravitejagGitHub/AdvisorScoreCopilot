using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.SemanticKernel;

public class AdvisorScorePlugin
{
    private const string FilePath = "advisor_scores.json";
    private List<AdvisorScoreModel> advisorScores;

    public AdvisorScorePlugin()
    {
        // Load advisor scores from the file
        advisorScores = LoadAdvisorScoresFromFile();
    }

 
    [KernelFunction("get_advisor_score_context")]
    [Description("Fetches the context of advisor scores based on the provided subscription in format UUID,  GUID or subscription name.")]
    [return: Description("Contextual data for the advisor score.")]
    public string GetAdvisorScoreContext(string subscriptionId)
    {
        var scores = advisorScores.Where(score => score.id.Contains(subscriptionId, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!scores.Any())
        {
            return "No advisor scores found for the provided subscription ID.";
        }

        return JsonSerializer.Serialize(scores, new JsonSerializerOptions { WriteIndented = true });
    }

    private List<AdvisorScoreModel> LoadAdvisorScoresFromFile()
    {
        if (File.Exists(FilePath))
        {
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<List<AdvisorScoreModel>>(json)!;
        }

        throw new FileNotFoundException($"The file '{FilePath}' was not found. Please provide a valid advisor_scores.json file.");
    }
}

// Advisor score model
public class AdvisorScoreModel
{
    public string id { get; set; } = string.Empty; // Used as identity subscription ID path
    public string type { get; set; } = string.Empty; // Added property for Type
    public string name { get; set; } = string.Empty; // Added property for Name
    public AdvisorScoreProperties properties { get; set; } = new(); // Moved LastRefreshedScore and TimeSeries under Properties
}

public class AdvisorScoreProperties
{
    public ScoreData lastRefreshedScore { get; set; } = new();
    public List<TimeSeries> timeSeries { get; set; } = new();
}


public class TimeSeries
{
    public string aggregationLevel { get; set; } = string.Empty;
    public List<ScoreData> scoreHistory { get; set; } = new();
}

public class ScoreData
{
    public DateTime date { get; set; }
    public float score { get; set; }
    public float consumptionUnits { get; set; }
    public int impactedResourceCount { get; set; }
    public float potentialScoreIncrease { get; set; }
    public int categoryCount { get; set; }
}