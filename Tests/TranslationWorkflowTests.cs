﻿using System.Text.RegularExpressions;

namespace Translate.Tests;

public class TranslationWorkflowTests
{
    const string workingDirectory = "../../../../Files";


    [Fact(DisplayName = "1. SplitDbAssets")]
    public void SplitDbAssets()
    {
        TranslationService.SplitDbAssets(workingDirectory);
    }

    [Fact(DisplayName = "2. ExportAssetsIntoTranslated")]
    public void ExportAssetsIntoTranslated()
    {
        TranslationService.ExportTextAssetsToCustomFormat(workingDirectory);
    }

    [Fact(DisplayName = "5. TranslateLinesBruteForce")]
    public async Task TranslateLinesBruteForce()
    {
        await PerformTranslateLines(true);
    }

    [Fact(DisplayName = "4. TranslateLines")]
    public async Task TranslateLines()
    {
        await PerformTranslateLines(false);
        await PackageFinalTranslation();
    }

    private async Task PerformTranslateLines(bool keepCleaning)
    {
        if (keepCleaning)
        {
            int remaining = await UpdateCurrentTranslationLines();
            int lastRemaining = remaining;
            int iterations = 0;
            while (remaining > 0 && iterations < 10)
            {
                await TranslationService.TranslateViaLlmAsync(workingDirectory, false);
                remaining = await UpdateCurrentTranslationLines();
                iterations++;

                // We've hit our brute force limit
                if (lastRemaining == remaining)
                    break;
            }
        }
        else
            await TranslationService.TranslateViaLlmAsync(workingDirectory, false);


        await PackageFinalTranslation();
    }

    [Fact(DisplayName = "6. PackageFinalTranslation")]
    public async Task PackageFinalTranslation()
    {
        await TranslationService.PackageFinalTranslationAsync(workingDirectory);

        var sourceDirectory = $"{workingDirectory}/Mod/{ModHelper.ContentFolder}";
        var gameDirectory = $"G:\\SteamLibrary\\steamapps\\common\\下一站江湖Ⅱ\\下一站江湖Ⅱ\\下一站江湖Ⅱ_Data\\StreamingAssets\\Mod\\{ModHelper.ContentFolder}";
        if (Directory.Exists(gameDirectory))
            Directory.Delete(gameDirectory, true);

        TranslationService.CopyDirectory(sourceDirectory, gameDirectory);
    }

    public static Dictionary<string, string> GetManualCorrections()
    {
        return new Dictionary<string, string>()
        {
            // Manual
            //{  "奖励：", "Reward:" },
        };
    }


    [Fact(DisplayName = "3. ApplyRulesToCurrentTranslation")]
    public async Task ApplyRulesToCurrentTranslation()
    {
        await UpdateCurrentTranslationLines();
    }

    public static async Task<int> UpdateCurrentTranslationLines()
    {
        var config = Configuration.GetConfiguration(workingDirectory);
        var totalRecordsModded = 0;
        var manual = GetManualCorrections();
        bool resetFlag = true;

        //Use this when we've changed a glossary value that doesnt check hallucination
        var newGlossaryStrings = new List<string>
        {
            //"狂",
            //"邪",
            //"正",
            //"阴",
            //"阳",
        };

        var mistranslationCheckGlossary = new Dictionary<string, string>();

        var hallucinationCheckGlossary = new Dictionary<string, string>();

        //var dupeNames = new Dictionary<string, (string key1, string key2)>();
        var dupeNames = mistranslationCheckGlossary
            .GroupBy(pair => pair.Value)
            .Where(group => group.Count() > 1)
            .ToDictionary(
                group => group.Key,
                group => group.Select(pair => pair.Key).ToList()
            );

        await TranslationService.IterateThroughTranslatedFilesAsync(workingDirectory, async (outputFile, textFileToTranslate, fileLines) =>
        {
            int recordsModded = 0;

            foreach (var line in fileLines)
                foreach (var split in line.Splits)
                {
                    // Reset all the retrans flags
                    if (resetFlag)
                        split.ResetFlags();

                    // Manual Retrans trigger
                    //if (line.LineNum > 0 && line.LineNum < 1000 && outputFile.Contains("NpcTalkItem.txt"))
                    //    split.FlaggedForRetranslation = true


                    if (CheckSplit(newGlossaryStrings, manual, split, outputFile, hallucinationCheckGlossary, mistranslationCheckGlossary, dupeNames, config))
                        recordsModded++;
                }

            totalRecordsModded += recordsModded;
            var serializer = Yaml.CreateSerializer();
            if (recordsModded > 0 || resetFlag)
            {
                Console.WriteLine($"Writing {recordsModded} records to {outputFile}");
                File.WriteAllText(outputFile, serializer.Serialize(fileLines));
            }

            await Task.CompletedTask;
        });

        Console.WriteLine($"Total Lines: {totalRecordsModded} records");

        return totalRecordsModded;
    }

    public static bool CheckSplit(List<string> newGlossaryStrings, Dictionary<string, string> manual, TranslationSplit split, string outputFile,
        Dictionary<string, string> hallucinationCheckGlossary, Dictionary<string, string> mistranslationCheckGlossary, Dictionary<string, List<string>> dupeNames, LlmConfig config)
    {
        var pattern = LineValidation.ChineseCharPattern;
        bool modified = false;

        // Flags        
        bool cleanWithGlossary = true;

        //////// Quick Validation here

        // If it is already translated or just special characters return it
        var preparedRaw = LineValidation.PrepareRaw(split.Text);
        var cleanedRaw = LineValidation.CleanupLineBeforeSaving(split.Text, split.Text, outputFile);
        if (!Regex.IsMatch(preparedRaw, pattern) && split.Translated != cleanedRaw)
        {
            Console.WriteLine($"Already Translated {outputFile} \n{split.Translated}");
            split.Translated = cleanedRaw;
            split.ResetFlags();
            return true;
        }

        //if (split.Text.Contains("Target") || split.Text.Contains("Location") || split.Text.Contains("Inventory"))
        //{
        //    Console.WriteLine($"New Glossary {outputFile} Replaces: \n{split.Translated}");
        //    split.FlaggedForRetranslation = true;
        //    return true;
        //}

        foreach (var glossary in newGlossaryStrings)
        {
            if (split.Text.Contains(glossary))
            {
                Console.WriteLine($"New Glossary {outputFile} Replaces: \n{split.Translated}");
                split.FlaggedForRetranslation = true;
                return true;
            }
        }

        // Add Manual Translations in that are missing        
        if (manual.TryGetValue(preparedRaw, out string? value))
        {
            if (split.Translated != value)
            {
                Console.WriteLine($"Manually Translated {outputFile} \n{split.Text}\n{split.Translated}");
                split.Translated = LineValidation.CleanupLineBeforeSaving(LineValidation.PrepareResult(value), split.Text, outputFile);
                split.ResetFlags();
                return true;
            }

            return false;
        }

        // Skip Empty
        if (string.IsNullOrEmpty(split.Translated))
            return false;

        // Context retrans too fricken big
        //if (outputFile.Contains("NpcTalkItem.txt") && MatchesContextRetrans(split.Translated))
        //{
        //    split.FlaggedForRetranslation = true;
        //    modified = true;
        //}

        if (MatchesPinyin(split.Translated))
        {
            split.FlaggedForRetranslation = true;
            modified = true;
        }

        //////// Manipulate split from here
        if (cleanWithGlossary)
        {
            // Glossary Clean up - this won't check our manual jobs
            modified = CheckMistranslationGlossary(split, mistranslationCheckGlossary, modified);
            modified = CheckHallucinationGlossary(split, hallucinationCheckGlossary, dupeNames, modified);
        }

        // Characters
        //if (preparedRaw.Contains("?")
        //    && !split.Translated.Contains("?"))
        //{
        //    Console.WriteLine($"Missing ? {outputFile} Replaces: \n{split.Translated}");
        //    split.FlaggedForRetranslation = true;
        //    modified = true;
        //}

        //if (preparedRaw.Contains("!")
        //    && !split.Translated.Contains("!"))
        //{
        //    Console.WriteLine($"Missing ! {outputFile} Replaces: \n{split.Translated}");
        //    split.FlaggedForRetranslation = true;
        //    modified = true;
        //}

        if (preparedRaw.EndsWith("...")
            && split.Text.Length < 15
            && !split.Translated.EndsWith("...")
            && !split.Translated.EndsWith("...?")
            && !split.Translated.EndsWith("...!")
            && !split.Translated.EndsWith("...!!")
            && !split.Translated.EndsWith("...?!"))
        {
            Console.WriteLine($"Missing ... {outputFile} Replaces: \n{split.Translated}");
            split.FlaggedForRetranslation = true;
            modified = true;
        }

        if (preparedRaw.StartsWith("...") && !split.Translated.StartsWith("..."))
        {
            Console.WriteLine($"Missing ... {outputFile} Replaces: \n{split.Translated}");
            split.Translated = $"...{split.Translated}";
            modified = true;
        }

        //// Try and flag crazy shit
        //if (!split.FlaggedForRetranslation
        //    //&& ContainsGender(split.Translated))
        //    && ContainsAnimalSounds(split.Translated))
        //{
        //    Console.WriteLine($"Contains whack {outputFile} \n{split.Translated}");
        //    recordsModded++;
        //    split.FlaggedForRetranslation = true;
        //}        

        // Long NPC Names - this really should be 30
        if (outputFile.Contains("NpcItem.txt") && split.Translated.Length > 50)
        {
            split.FlaggedForRetranslation = true;
            modified = true;
        }

        // Trim line
        if (split.Translated.Trim().Length != split.Translated.Length)
        {
            Console.WriteLine($"Needed Trimming:{outputFile} \n{split.Translated}");
            split.Translated = split.Translated.Trim();
            modified = true;
            //Don't continue we still want other stuff to happen
        }

        // Add . into Dialogue
        if (outputFile.EndsWith("NpcTalkItem.txt") && char.IsLetter(split.Translated[^1]) && preparedRaw != split.Translated)
        {
            Console.WriteLine($"Needed full stop:{outputFile} \n{split.Translated}");
            split.Translated += '.';
            modified = true;
        }

        // Clean up Diacritics
        var cleanedUp = LineValidation.CleanupLineBeforeSaving(split.Translated, split.Text, outputFile);
        if (cleanedUp != split.Translated)
        {
            Console.WriteLine($"Cleaned up {outputFile} \n{split.Translated}\n{cleanedUp}");
            split.Translated = cleanedUp;
            modified = true;
        }

        // Remove Invalid ones
        var result = LineValidation.CheckTransalationSuccessful(config, split.Text, split.Translated ?? string.Empty, outputFile);
        if (!result.Valid)
        {
            Console.WriteLine($"Invalid {outputFile} Failures:{result.CorrectionPrompt}\n{split.Translated}");
            split.FlaggedForRetranslation = true;
            modified = true;
        }

        return modified;
    }

    private static bool CheckMistranslationGlossary(TranslationSplit split, Dictionary<string, string> glossary, bool modified)
    {
        var preparedRaw = LineValidation.PrepareRaw(split.Text);

        if (split.Translated == null)
            return modified;

        foreach (var item in glossary)
        {
            if (preparedRaw.Contains(item.Key) && !split.Translated.Contains(item.Value, StringComparison.OrdinalIgnoreCase))
            {
                // Handle placeholders being annoying basically if it caught a {name_2} only when the text has {1} and {2}
                if (split.Text.Contains("{name_1}{name_2}") && !item.Value.Contains("{name_1}"))
                    continue;

                if (item.Key == "天外来客" && split.Translated.Contains("guests from beyond the skies", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (item.Key == "村长家" && split.Translated.Contains("Village Chief Mei's house", StringComparison.OrdinalIgnoreCase))
                    continue;

                //Console.WriteLine($"Mistranslated:{outputFile}\n{item.Value}\n{split.Translated}");
                split.FlaggedForRetranslation = true;
                split.FlaggedGlossaryIn += $"{item.Value},{item.Key},";
                modified = true;
            }
        }

        return modified; // Will be previous value - even if it didnt find anything
    }

    private static bool CheckHallucinationGlossary(TranslationSplit split, Dictionary<string, string> glossary, Dictionary<string, List<string>> dupeNames, bool modified)
    {
        var preparedRaw = LineValidation.PrepareRaw(split.Text);

        if (split.Translated == null)
            return modified;

        foreach (var item in glossary)
        {
            var wordPattern = $"\\b{item.Value}\\b";

            if (!preparedRaw.Contains(item.Key) && split.Translated.Contains(item.Value, StringComparison.OrdinalIgnoreCase))
            {
                //If we dont word match - ie matched He Family in the family
                if (!Regex.IsMatch(split.Translated, wordPattern, RegexOptions.IgnoreCase))
                    continue;

                // Handle Quanpai (entire sect)
                if (item.Value == "Qingcheng Sect" && split.Text.Contains("青城全派"))
                    continue;

                // If one of the dupes are in the raw
                bool found = false;
                if (dupeNames.TryGetValue(item.Value, out List<string>? dupes))
                {
                    foreach (var dupe in dupes)
                    {
                        found = split.Text.Contains(dupe);
                        if (found)
                            break;
                    }
                }

                if (!found)
                {
                    //Console.WriteLine($"Hallucinated:{outputFile}\n{item.Value}\n{split.Translated}");
                    split.FlaggedForRetranslation = true;
                    split.FlaggedGlossaryOut += $"{item.Value},{item.Key},";
                    modified = true;
                }
            }
        }

        return modified; // Will be previous value - even if it didnt find anything
    }

    public static bool MatchesPinyin(string input)
    {
        HashSet<string> words = new HashSet<string>
        {
            "hiu", "guniang", "tut", "thut", "oi", "avo", "porqe", "obrigado",
            "nom", "esto", "tem", "mais", "com", "ver", "nos", "sobre", "vermos",
            "dar", "nam", "J'ai", "je", "veux", "pas", "ele", "una", "keqi", "shiwu",
            "niang", "fuck", "ich", "daren", "furen", "ein", "der", "ganzes", "Leben", "dort", "xiansheng",
            "knight", "thay", "tien",
            //"-in-law"
        };

        string pattern = $@"\b({string.Join("|", words)})\b";

        return Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase);
    }
}
