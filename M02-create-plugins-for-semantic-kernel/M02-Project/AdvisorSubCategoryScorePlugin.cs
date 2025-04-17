using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.SemanticKernel;

public class AdvisorSubCategoryScorePlugin
{
    private const string ScoreFilePath = "subcategory_score.json";
    private const string MetadataFilePath = "subcategory_metadata.json";
    private List<SubCategoryScoreModel> subCategoryScores;

    public AdvisorSubCategoryScorePlugin()
    {
        subCategoryScores = LoadSubCategoryScoresFromFile();
    }

    [KernelFunction("get_subcategory_scores")]
    [Description("Fetches subcategory scores optionally filtered by subscription ID and/or category type")]
    [return: Description("Subcategory scores filtered by the specified criteria")]
    public string GetSubCategoryScores(string? subscriptionId = null, string? categoryType = null)
    {
        var scores = subCategoryScores.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(subscriptionId))
        {
            scores = scores.Where(score => score.id.Contains(subscriptionId, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(categoryType))
        {
            scores = scores.Where(score => score.id.Contains(categoryType, StringComparison.OrdinalIgnoreCase));
        }

        var scoresList = scores.ToList();
        if (!scoresList.Any())
        {
            if (subscriptionId != null && categoryType != null)
                return "No subcategory scores found for the provided subscription ID and category type.";
            else if (subscriptionId != null)
                return "No subcategory scores found for the provided subscription ID.";
            else if (categoryType != null)
                return "No subcategory scores found for the provided category type.";
            else
                return "No subcategory scores found.";
        }

        return JsonSerializer.Serialize(scoresList, new JsonSerializerOptions { WriteIndented = false });
    }

    [KernelFunction("get_subcategory_metadata")]
    [Description("Fetches sub categories metadata for all available categorues. use this to indentify the category and sub categories types. Use to calculate overall max score (OverallMaxscoreByCategory) for each category by summing max score of each sub category (MaxScoreBySubCategory).")]
    [return: Description("A list of sub categories and max score for each sub category.")]
    public List<SubCategoreisMetadata> GetSubscriptionsMetadata()
    {
        if (!File.Exists(MetadataFilePath))
        {
            throw new FileNotFoundException($"The file '{MetadataFilePath}' was not found. Please ensure the file exists.");
        }

        var metadataJson = File.ReadAllText(MetadataFilePath);
        return JsonSerializer.Deserialize<List<SubCategoreisMetadata>>(metadataJson)!;
    }

    private List<SubCategoryScoreModel> LoadSubCategoryScoresFromFile()
    {
        if (File.Exists(ScoreFilePath))
        {
            var json = File.ReadAllText(ScoreFilePath);
            return JsonSerializer.Deserialize<List<SubCategoryScoreModel>>(json)!;
        }

        throw new FileNotFoundException($"The file '{ScoreFilePath}' was not found. Please provide a valid subcategory_score.json file.");
    }
}

// Subcategory score models
public class SubCategoryScoreModel
{
    public string id { get; set; } = string.Empty;
    public string type { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
    public SubCategoryScoreProperties properties { get; set; } = new();
}

public class SubCategoryScoreProperties
{
    public SubCategoryScoreData lastRefreshedScore { get; set; } = new();
}

public class SubCategoryScoreData
{
    public DateTime date { get; set; }
    public float score { get; set; }
    public float MaxScoreBySubCategory { get; set; }
    public float OverallMaxscoreByCategory { get; set; }
    public float consumptionUnits { get; set; }
    public int impactedResourceCount { get; set; }
    public float potentialScoreIncrease { get; set; }
    public int categoryCount { get; set; }
}

// Metadata model
public class SubCategoreisMetadata
{
    public string Category { get; set; } = string.Empty;
    public string SubCategory { get; set; } = string.Empty;

    public int MaxScoreBySubCategory { get; set; } = 1;
}