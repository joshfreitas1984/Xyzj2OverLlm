﻿using System.Linq;
using System.Text;

namespace Translate;

public class Glossary
{
    //public static void AddGlossaryToCache(LlmConfig config, Dictionary<string, string> cache)
    //{
    //    AddGlossaryLinesToCache(cache, config.GameData.Names.Entries);
    //    AddGlossaryLinesToCache(cache, config.GameData.Locations.Entries);
    //    AddGlossaryLinesToCache(cache, config.GameData.SpecialTermsSafe.Entries);
    //    AddGlossaryLinesToCache(cache, config.GameData.SpecialTermsUnsafe.Entries);
    //    AddGlossaryLinesToCache(cache, config.GameData.Factions.Entries);
    //}

    //public static void AddGlossaryLinesToCache(Dictionary<string, string> cache, List<DataLine> dataLines)
    //{
    //    foreach(var line in dataLines)
    //        if (!cache.ContainsKey(line.Raw))
    //            cache.Add(line.Raw, line.Result);
    //}

    //private static string AppendPromptsFor(string raw, List<DataLine> dataLines)
    //{
    //    var prompt = new StringBuilder();

    //    foreach (var line in dataLines)
    //    {
    //        if (raw.Contains(line.Raw))
    //            prompt.Append($"- {line.Raw}: {line.Result}\n");
    //    }

    //    return prompt.ToString();
    //}
}
