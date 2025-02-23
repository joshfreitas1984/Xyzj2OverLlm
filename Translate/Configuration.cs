﻿using System.Text;
using YamlDotNet.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Translate;

public class LlmConfig
{
    public string? ApiKey { get; set; }
    public bool ApiKeyRequired { get; set; }
    public string? Url { get; set; }
    public string? Model { get; set; }
    public int? RetryCount { get; set; }
    public int? BatchSize { get; set; }
    public bool SkipLineValidation { get; set; }
    public bool CorrectionPromptsEnabled { get; set; }
    public bool TranslateFlagged { get; set; }
    public Dictionary<string, object>? ModelParams { get; set; }

    // Not serialised in Yaml
    public Dictionary<string, string> Prompts { get; set; } = [];
    public GameData GameData { get; set; } = new GameData();
    public string? WorkingDirectory { get; set; }
}

public class GameData
{
    public DataFormat Names { get; set; } = new DataFormat();
    public DataFormat Factions { get; set; } = new DataFormat();
    public DataFormat Locations { get; set; } = new DataFormat();
    public DataFormat SpecialTermsSafe { get; set; } = new DataFormat();
    public DataFormat SpecialTermsUnsafe { get; set; } = new DataFormat();
    public DataFormat Titles { get; set; } = new DataFormat();

    // Separate for handling later
    public DataFormat Placeholder1WithTitles { get; set; } = new DataFormat();
    public DataFormat Placeholder2WithTitles { get; set; } = new DataFormat();
    public DataFormat Placeholder1and2WithTitles { get; set; } = new DataFormat();
}

public static class Configuration
{
    public static LlmConfig GetConfiguration(string workingDirectory)
    {       
        var deserializer = Yaml.CreateDeserializer();
        var response = deserializer.Deserialize<LlmConfig>(File.ReadAllText($"{workingDirectory}/Config.yaml", Encoding.UTF8));

        response.WorkingDirectory = workingDirectory;
        response.Prompts = CachePrompts(workingDirectory);
        response.GameData = LoadGameData(workingDirectory);

        return response;
    }

    public static Dictionary<string, string> CachePrompts(string workingDirectory)
    {
        var prompts = new Dictionary<string, string>();
        var path = $"{workingDirectory}/Prompts";

        foreach (var file in Directory.EnumerateFiles(path))
            prompts.Add(Path.GetFileNameWithoutExtension(file), File.ReadAllText(file));

        return prompts;
    }

    public static GameData LoadGameData(string workingDirectory)
    {
        var yaml = Yaml.CreateDeserializer();
        var result = new GameData()
        {
            Names = GetGameData($"{workingDirectory}/GameData/Names.yaml", yaml),
            Factions = GetGameData($"{workingDirectory}/GameData/Factions.yaml", yaml),
            Locations = GetGameData($"{workingDirectory}/GameData/Locations.yaml", yaml),
            SpecialTermsSafe = GetGameData($"{workingDirectory}/GameData/SpecialTermsSafe.yaml", yaml),
            SpecialTermsUnsafe = GetGameData($"{workingDirectory}/GameData/SpecialTermsUnsafe.yaml", yaml),
            Titles = GetGameData($"{workingDirectory}/GameData/Titles.yaml", yaml),
        };

        //Add Placeholders with titles
        foreach (var title in result.Titles.Entries)
        {
            result.Placeholder1WithTitles.Entries.Add(new DataLine()
            {
                Raw = $"{{name_1}}{title.Raw}",
                Result = $"{title.Result} {{name_1}}"
            });

            result.Placeholder2WithTitles.Entries.Add(new DataLine()
            {
                Raw = $"{{name_2}}{title.Raw}",
                Result = $"{title.Result} {{name_2}}"
            });

            result.Placeholder1and2WithTitles.Entries.Add(new DataLine()
            {
                Raw = $"{{name_1}}{{name_2}}{title.Raw}",
                Result = $"{title.Result} {{name_1}} {{name_2}}"
            });           
        }

        return result;
    }

    public static void AddToDictionaryGlossary(Dictionary<string, string> globalGlossary, List<DataLine> entries)
    {
        foreach (var line in entries)
            globalGlossary.Add(line.Raw, line.Result);
    }

    private static DataFormat GetGameData(string file, IDeserializer yaml)
    {
        return yaml.Deserialize<DataFormat>(File.ReadAllText(file, Encoding.UTF8));
    }
}
