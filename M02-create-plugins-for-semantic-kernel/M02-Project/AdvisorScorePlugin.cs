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
    private const string MetadataFilePath = "subscriptions_metadata.json";
    private List<AdvisorScoreModel> advisorScores;

    public AdvisorScorePlugin()
    {
        // Load advisor scores from the file
        advisorScores = LoadAdvisorScoresFromFile();
        GetSubscriptionsMetadata();
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

    [KernelFunction("get_subscriptions_metadata")]
    [Description("Fetches metadata for all available subscriptions. use this to indentify the subscription ID, name and type.")]
    [return: Description("A list of subscription metadata including ID, name, and type.")]
    public List<SubscriptionMetadata> GetSubscriptionsMetadata()
    {
        if (!File.Exists(MetadataFilePath))
        {
            throw new FileNotFoundException($"The file '{MetadataFilePath}' was not found. Please ensure the file exists.");
        }

        var metadataJson = File.ReadAllText(MetadataFilePath);
        return JsonSerializer.Deserialize<List<SubscriptionMetadata>>(metadataJson)!;
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

// Subscription metadata model
public class SubscriptionMetadata
{
    public string Id { get; set; } = string.Empty;
    public required string Name { get; set; }
    public required string Type { get; set; }
}