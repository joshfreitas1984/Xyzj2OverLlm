﻿using System.Linq;
using System.Text;

namespace Translate;

public class Glossary
{
    public static string ConstructGlossaryPrompt(string raw, LlmConfig config)
    {
        var prompt = new StringBuilder();

        var names = AppendPromptsFor(raw, config.GameData.Names.Entries);
        var locations = AppendPromptsFor(raw, config.GameData.Locations.Entries);
        var specialTermsSafe = AppendPromptsFor(raw, config.GameData.SpecialTermsSafe.Entries);
        var specialTermsUnsafe = AppendPromptsFor(raw, config.GameData.SpecialTermsUnsafe.Entries);
        var factions = AppendPromptsFor(raw, config.GameData.Factions.Entries);
        var titles = AppendPromptsFor(raw, config.GameData.Titles.Entries);

        var p1titles = AppendPromptsFor(raw, config.GameData.Placeholder1WithTitles.Entries);
        var p2titles = AppendPromptsFor(raw, config.GameData.Placeholder2WithTitles.Entries);
        var p1and2titles = AppendPromptsFor(raw, config.GameData.Placeholder1and2WithTitles.Entries);

        if (names.Length > 0)
        {
            prompt.AppendLine("#### Character Names");
            prompt.Append(names);         
            
            if (p1titles.Length > 0)
                prompt.Append(p1titles);

            if (p1and2titles.Length > 0)
                prompt.Append(p1and2titles);
            //1 and 2 conflicts
            else if (p2titles.Length > 0)
                prompt.Append(p2titles);
        }

        if (locations.Length > 0)
        {
            prompt.AppendLine("#### Locations and Places");
            prompt.Append(locations);
        }

        if (titles.Length > 0)
        {
            prompt.AppendLine("#### Titles");
            prompt.Append(titles);
        }

        if (specialTermsSafe.Length > 0 || specialTermsUnsafe.Length > 0 || factions.Length > 0)
        {
            prompt.AppendLine("#### Special Terms");

            if (specialTermsSafe.Length > 0)
                prompt.Append(specialTermsSafe);

            if (specialTermsUnsafe.Length > 0)
                prompt.Append(specialTermsUnsafe);

            if (factions.Length > 0)
                prompt.Append(factions);
        }

        if (prompt.Length > 0)
        {           
            var resultPrompt = $"{config.Prompts["BaseGlossaryPrompt"]}\n{prompt}";
            return resultPrompt;
        }
        else
            return string.Empty;
    }

    public static void AddGlossaryToCache(LlmConfig config, Dictionary<string, string> cache)
    {
        AddGlossaryLinesToCache(cache, config.GameData.Names.Entries);
        AddGlossaryLinesToCache(cache, config.GameData.Locations.Entries);
        AddGlossaryLinesToCache(cache, config.GameData.SpecialTermsSafe.Entries);
        AddGlossaryLinesToCache(cache, config.GameData.SpecialTermsUnsafe.Entries);
        AddGlossaryLinesToCache(cache, config.GameData.Factions.Entries);
    }

    public static void AddGlossaryLinesToCache(Dictionary<string, string> cache, List<DataLine> dataLines)
    {
        foreach(var line in dataLines)
            if (!cache.ContainsKey(line.Raw))
                cache.Add(line.Raw, line.Result);
    }

    private static string AppendPromptsFor(string raw, List<DataLine> dataLines)
    {
        var prompt = new StringBuilder();

        foreach (var line in dataLines)
        {
            if (raw.Contains(line.Raw))
                prompt.Append($"- {line.Raw}: {line.Result}\n");
        }

        return prompt.ToString();
    }
}
