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

    [Fact(DisplayName = "3. ApplyRulesToCurrentTranslation")]
    public async Task ApplyRulesToCurrentTranslation()
    {
        await UpdateCurrentTranslationLines(true);
    }

    [Fact(DisplayName = "4. TranslateLines")]
    public async Task TranslateLines()
    {
        await PerformTranslateLines(false);
        await PackageFinalTranslation();
    }

    [Fact(DisplayName = "0. TranslateLinesBruteForce")]
    public async Task TranslateLinesBruteForce()
    {
        await PerformTranslateLines(true);
    }

    private async Task PerformTranslateLines(bool keepCleaning)
    {
        if (keepCleaning)
        {
            int remaining = await UpdateCurrentTranslationLines(false);
            int iterations = 0;
            while (remaining > 0 && iterations < 10)
            {
                await TranslationService.TranslateViaLlmAsync(workingDirectory, false);
                remaining = await UpdateCurrentTranslationLines(false);
                iterations++;
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
        var resourceDirectory = "G:\\SteamLibrary\\steamapps\\common\\下一站江湖Ⅱ\\下一站江湖Ⅱ\\BepInEx\\resources";

        if (Directory.Exists(gameDirectory))
            Directory.Delete(gameDirectory, true);

        TranslationService.CopyDirectory(sourceDirectory, gameDirectory);

        File.Copy($"{workingDirectory}/Mod/db1.txt", $"{resourceDirectory}/db1.txt", true);
    }

    public static Dictionary<string, string> GetManualCorrections()
    {
        return new Dictionary<string, string>()
        {
            // Manual
            //{  "奖励：", "Reward:" },
        };
    }

    [Fact(DisplayName = "0. Reset All Flags")]
    public async Task ResetAllFlags()
    {
        var config = Configuration.GetConfiguration(workingDirectory);
        await TranslationService.IterateThroughTranslatedFilesAsync(workingDirectory, async (outputFile, textFileToTranslate, fileLines) =>
        {
            foreach (var line in fileLines)
                foreach (var split in line.Splits)
                    // Reset all the retrans flags
                    split.ResetFlags(false);

            var serializer = Yaml.CreateSerializer();
            File.WriteAllText(outputFile, serializer.Serialize(fileLines));

            await Task.CompletedTask;
        });
    }

    public static async Task<int> UpdateCurrentTranslationLines(bool resetFlag)
    {
        var config = Configuration.GetConfiguration(workingDirectory);
        var totalRecordsModded = 0;
        var manual = GetManualCorrections();
        var logLines = new List<string>();

        //Use this when we've changed a glossary value that doesnt check hallucination
        var newGlossaryStrings = new List<string>
        {
            //"狂",
            //"邪",
            //"正",
            //"阴",
            //"阳",
        };

        await TranslationService.IterateThroughTranslatedFilesAsync(workingDirectory, async (outputFile, textFileToTranslate, fileLines) =>
        {
            int recordsModded = 0;

            foreach (var line in fileLines)
                foreach (var split in line.Splits)
                {
                    // Reset all the retrans flags
                    if (resetFlag)
                        split.ResetFlags(false);

                    if (UpdateSplit(logLines, newGlossaryStrings, manual, split, outputFile, config))
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
        File.WriteAllLines($"{workingDirectory}/TestResults/LineValidationLog.txt", logLines);

        return totalRecordsModded;
    }

    public static bool UpdateSplit(List<string> logLines, List<string> newGlossaryStrings, Dictionary<string, string> manual, TranslationSplit split, string outputFile,
        LlmConfig config)
    {
        var pattern = LineValidation.ChineseCharPattern;
        bool modified = false;
        bool cleanWithGlossary = true;

        //////// Quick Validation here

        // If it is already translated or just special characters return it
        var tokenReplacer = new StringTokenReplacer(); 
        var preparedRaw = LineValidation.PrepareRaw(split.Text, tokenReplacer);
        var cleanedRaw = LineValidation.CleanupLineBeforeSaving(split.Text, split.Text, outputFile, tokenReplacer);
        if (!Regex.IsMatch(preparedRaw, pattern) && split.Translated != cleanedRaw)
        {
            logLines.Add($"Already Translated {outputFile} \n{split.Translated}");
            split.Translated = cleanedRaw;
            split.ResetFlags();
            return true;
        }      

        foreach (var glossary in newGlossaryStrings)
        {
            if (preparedRaw.Contains(glossary))
            {
                logLines.Add($"New Glossary {outputFile} Replaces: \n{split.Translated}");
                split.FlaggedForRetranslation = true;
                return true;
            }
        }

        // Add Manual Translations in that are missing        
        if (manual.TryGetValue(preparedRaw, out string? value))
        {
            if (split.Translated != value)
            {
                logLines.Add($"Manually Translated {outputFile} \n{split.Text}\n{split.Translated}");
                split.Translated = LineValidation.CleanupLineBeforeSaving(LineValidation.PrepareResult(value), split.Text, outputFile, tokenReplacer);
                split.ResetFlags();
                return true;
            }

            return false;
        }

        // Skip Empty but flag so we can find them easily
        if (string.IsNullOrEmpty(split.Translated) && !string.IsNullOrEmpty(preparedRaw))
        {
            split.FlaggedForRetranslation = true;
            split.FlaggedMistranslation = "Failed"; //Easy search
            return true;
        }

        // Temp force retrans of splits because of changes in calcs
        //foreach (var splitCharacters in TranslationService.SplitCharactersList)
        //    if (preparedRaw.Contains(splitCharacters))
        //    {
        //        split.FlaggedForRetranslation = true;
        //        return true;
        //    }

        if (MatchesBadWords(split.Translated))
        {
            split.FlaggedForRetranslation = true;
            modified = true;
        }

        //////// Manipulate split from here
        if (cleanWithGlossary)
        {
            // Glossary Clean up - this won't check our manual jobs
            modified = CheckMistranslationGlossary(config, split, modified);
            modified = CheckHallucinationGlossary(config,split, modified);
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
            && preparedRaw.Length < 15
            && !split.Translated.EndsWith("...")
            && !split.Translated.EndsWith("...?")
            && !split.Translated.EndsWith("...!")
            && !split.Translated.EndsWith("...!!")
            && !split.Translated.EndsWith("...?!"))
        {
            logLines.Add($"Missing ... {outputFile} Replaces: \n{split.Translated}");
            split.FlaggedForRetranslation = true;
            modified = true;
        }

        if (preparedRaw.StartsWith("...") && !split.Translated.StartsWith("..."))
        {
            logLines.Add($"Missing ... {outputFile} Replaces: \n{split.Translated}");
            split.Translated = $"...{split.Translated}";
            modified = true;
        }
    

        // Trim line
        if (split.Translated.Trim().Length != split.Translated.Length)
        {
            logLines.Add($"Needed Trimming:{outputFile} \n{split.Translated}");
            split.Translated = split.Translated.Trim();
            modified = true;
        }

        // Add . into Dialogue
        //if (outputFile.EndsWith("stringlang.txt") && char.IsLetter(split.Translated[^1]) && preparedRaw != split.Translated)
        //{
        //    logLines.Add($"Needed full stop:{outputFile} \n{split.Translated}");
        //    split.Translated += '.';
        //    modified = true;
        //}

        // Clean up Diacritics
        var cleanedUp = LineValidation.CleanupLineBeforeSaving(split.Translated, preparedRaw, outputFile, tokenReplacer);
        if (cleanedUp != split.Translated)
        {
            logLines.Add($"Cleaned up {outputFile} \n{split.Translated}\n{cleanedUp}");
            split.Translated = cleanedUp;
            modified = true;
        }

        // Remove Invalid ones -- Have to use final raw because translated is untokenised
        var result = LineValidation.CheckTransalationSuccessful(config, split.Text, split.Translated ?? string.Empty, outputFile);
        if (!result.Valid)
        {
            logLines.Add($"Invalid {outputFile} Failures:{result.CorrectionPrompt}\n{split.Translated}");
            split.FlaggedForRetranslation = true;
            modified = true;
        }

        return modified;
    }

    private static bool CheckMistranslationGlossary(LlmConfig config, TranslationSplit split, bool modified)
    {
        var tokenReplacer = new StringTokenReplacer();
        var preparedRaw = LineValidation.PrepareRaw(split.Text, tokenReplacer);

        if (split.Translated == null)
            return modified;

        foreach (var item in config.GlossaryLines)
        {
            if (!item.CheckForMistranslation)
                continue;

            if (preparedRaw.Contains(item.Raw) && !split.Translated.Contains(item.Result, StringComparison.OrdinalIgnoreCase))
            {
                var found = false;
                foreach (var alternative in item.AllowedAlternatives)
                {
                    found = split.Translated.Contains(alternative, StringComparison.OrdinalIgnoreCase);
                    if (found)
                        break;
                }

                if (!found)
                {                    
                    split.FlaggedForRetranslation = true;
                    split.FlaggedMistranslation += $"{item.Result},{item.Raw},";
                    modified = true;
                }
            }
        }

        return modified; // Will be previous value - even if it didnt find anything
    }

    private static bool CheckHallucinationGlossary(LlmConfig config, TranslationSplit split, bool modified)
    {
        var tokenReplacer = new StringTokenReplacer();
        var preparedRaw = LineValidation.PrepareRaw(split.Text, tokenReplacer);

        if (split.Translated == null)
            return modified;

        foreach (var item in config.GlossaryLines)
        {
            var wordPattern = $"\\b{item.Result}\\b";

            if (!preparedRaw.Contains(item.Raw) && split.Translated.Contains(item.Result))
            {
                if (!item.CheckForHallucination)
                    continue;

                // Regex matches on terms with ... match incorrectly
                if (!Regex.IsMatch(split.Translated, wordPattern, RegexOptions.IgnoreCase))
                    continue;

                // Check for Alternatives
                var dupes = config.GlossaryLines.Where(s => s.Result == item.Result && s.Raw != item.Raw);
                bool found = false;
               
                foreach (var dupe in dupes)
                {
                    found = preparedRaw.Contains(dupe.Raw);
                    if (found)
                        break;
                }

                if (!found)
                {
                    split.FlaggedForRetranslation = true;
                    split.FlaggedHallucination += $"{item.Result},{item.Raw},";
                    modified = true;
                }
            }
        }

        return modified; // Will be previous value - even if it didnt find anything
    }

    public static bool MatchesBadWords(string input)
    {
        HashSet<string> words = new HashSet<string>
        {
            "hiu", "guniang", "tut", "thut", "oi", "avo", "porqe", "obrigado",
            "nom", "esto", "tem", "mais", "com", "ver", "nos", "sobre", "vermos",
            "dar", "nam", "J'ai", "je", "veux", "pas", "ele", "una", "keqi", "shiwu",
            "niang", "fuck", "ich", "daren", "furen", "ein", "der", "ganzes", "Leben", "dort", "xiansheng",
            "knight", "thay", "tien", "div", "html",
            //"-in-law"
        };

        string pattern = $@"\b({string.Join("|", words)})\b";

        return Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase);
    }
}
