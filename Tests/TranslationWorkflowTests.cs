using System.Collections.Concurrent;
using System.IO.Compression;
using System.Text.RegularExpressions;
using Translate.Utility;

namespace Translate.Tests;

public class TranslationWorkflowTests
{
    public const string WorkingDirectory = "../../../../Files";
    public const string GameFolder = "G:\\SteamLibrary\\steamapps\\common\\下一站江湖Ⅱ\\下一站江湖Ⅱ\\";

    [Fact(DisplayName = "3. ApplyRulesToCurrentTranslation")]
    public async Task ApplyRulesToCurrentTranslation()
    {
        await UpdateCurrentTranslationLines(true);
    }

    [Fact(DisplayName = "4. TranslateLines")]
    public async Task TranslateLines()
    {
        await PerformTranslateLines(false);
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
                await TranslationService.TranslateViaLlmAsync(WorkingDirectory, false);
                remaining = await UpdateCurrentTranslationLines(false);
                iterations++;
            }

            await FileOutputWorkflowTests.PackageFinalTranslation();
        }
        else
            await TranslationService.TranslateViaLlmAsync(WorkingDirectory, false);
    }

    [Fact(DisplayName = "0. Reset All Flags")]
    public async Task ResetAllFlags()
    {
        var config = Configuration.GetConfiguration(WorkingDirectory);
        var serializer = Yaml.CreateSerializer();

        await FileIteration.IterateTranslatedFilesInParallelAsync(WorkingDirectory, async (outputFile, textFileToTranslate, fileLines) =>
        {
            foreach (var line in fileLines)
                foreach (var split in line.Splits)
                    // Reset all the retrans flags
                    split.ResetFlags(false);

            await File.WriteAllTextAsync(outputFile, serializer.Serialize(fileLines));
        });
    }

    public static async Task<int> UpdateCurrentTranslationLines(bool resetFlag)
    {
        var config = Configuration.GetConfiguration(WorkingDirectory);
        var totalRecordsModded = 0;
        var logLines = new ConcurrentBag<string>();

        string[] fullFileRetrans = [
        ];

        var newGlossaryStrings = new List<string>
        {
        };

        var badRegexes = new List<string>
        {
        };

        // Compile regexes once for reuse
        var compiledBadRegexes = badRegexes.Select(r => new Regex(r, RegexOptions.Compiled)).ToList();
        var chineseCharRegex = new Regex(LineValidation.ChineseCharPattern, RegexOptions.Compiled);

        // Use parallelization for file iteration
        await FileIteration.IterateTranslatedFilesInParallelAsync(WorkingDirectory, async (outputFile, textFile, fileLines) =>
        {
            var serializer = Yaml.CreateSerializer();
            int recordsModded = 0;

            // Use Parallel.For for splits if thread-safe
            Parallel.ForEach(fileLines, line =>
            {
                // Only one StringTokenReplacer per line
                var tokenReplacer = new StringTokenReplacer();
                foreach (var split in line.Splits)
                {
                    // Reset all the retrans flags
                    if (resetFlag)
                        split.ResetFlags(false);

                    if (fullFileRetrans.Contains(textFile.Path))
                    {
                        split.FlaggedForRetranslation = true;
                        Interlocked.Increment(ref recordsModded);
                        continue;
                    }

                    if (UpdateSplitOptimized(logLines, newGlossaryStrings, compiledBadRegexes, split, textFile, config, chineseCharRegex, tokenReplacer))
                        Interlocked.Increment(ref recordsModded);
                }
            });

            Interlocked.Add(ref totalRecordsModded, recordsModded);
            if (recordsModded > 0 || resetFlag)
            {
                Console.WriteLine($"Writing {recordsModded} records to {outputFile}");
                await File.WriteAllTextAsync(outputFile, serializer.Serialize(fileLines));
            }
        });

        Console.WriteLine($"Total Lines: {totalRecordsModded} records");
        File.WriteAllLines($"{WorkingDirectory}/TestResults/LineValidationLog.txt", logLines);

        return totalRecordsModded;
    }

    public static bool UpdateSplitOptimized(
        ConcurrentBag<string> logLines,
        List<string> newGlossaryStrings,
        List<Regex> compiledBadRegexes,
        TranslationSplit split,
        TextFileToSplit textFile,
        LlmConfig config,
        Regex chineseCharRegex,
        StringTokenReplacer tokenReplacer)
    {
        bool modified = false;
        bool cleanWithGlossary = true;

        if (!split.SafeToTranslate)
            return false;

        // If it is already translated or just special characters return it
        var preparedRaw = LineValidation.PrepareRaw(split.Text, tokenReplacer);
        var cleanedRaw = LineValidation.CleanupLineBeforeSaving(split.Text, split.Text, textFile, tokenReplacer);
        var preparedResultRaw = LineValidation.CleanupLineBeforeSaving(preparedRaw, preparedRaw, textFile, tokenReplacer);

        if (!chineseCharRegex.IsMatch(preparedRaw) && split.Translated != cleanedRaw && split.Translated != preparedResultRaw)
        {
            logLines.Add($"Already Translated {textFile.Path} \n{split.Translated}");
            split.Translated = preparedResultRaw;
            split.ResetFlags();
            return true;
        }

        foreach (var glossary in newGlossaryStrings)
        {
            if (preparedRaw.Contains(glossary))
            {
                logLines.Add($"New Glossary {textFile.Path} Replaces: \n{split.Translated}");
                split.FlaggedForRetranslation = true;
                return true;
            }
        }

        foreach (var badRegex in compiledBadRegexes)
        {
            if (badRegex.IsMatch(split.Text))
            {
                logLines.Add($"Bad Regex {textFile.Path} Replaces: \n{split.Translated}");
                split.FlaggedForRetranslation = true;
                return true;
            }
        }

        // Do not manipulate anything that is to do with the UI
        if (textFile.TextFileType == TextFileType.DynamicStrings)
        {
            if (split.Text.Contains("Sprite")
                || split.Text.Contains("UI")
                || split.Text.Contains("Prefab")
                || split.Text.StartsWith("INVALIDCHAR:"))
            {
                split.SafeToTranslate = false;
                return true;
            }
        }

        // Add Manual Translations in that are missing
        if (textFile.EnableGlossary)
        {
            foreach (var manual in config.ManualTranslations)
            {
                if (split.Text == manual.Raw)
                {
                    if (split.Translated != manual.Result)
                    {
                        logLines.Add($"Manually Translated {textFile.Path} \n{split.Text}\n{split.Translated}");
                        split.Translated = LineValidation.CleanupLineBeforeSaving(LineValidation.PrepareResult(preparedRaw, manual.Result), split.Text, textFile, new StringTokenReplacer());
                        split.ResetFlags();
                        return true;
                    }

                    return false;
                }
            }
        }

        // Skip Empty but flag so we can find them easily
        if (string.IsNullOrEmpty(split.Translated) && !string.IsNullOrEmpty(preparedRaw))
        {
            split.FlaggedForRetranslation = true;
            split.FlaggedMistranslation = "Failed"; //Easy search
            return true;
        }

        if (MatchesBadWords(split.Translated))
        {
            split.FlaggedForRetranslation = true;
            modified = true;
        }

        //////// Manipulate split from here
        if (cleanWithGlossary)
        {
            // Glossary Clean up - this won't check our manual jobs
            modified = CheckMistranslationGlossary(config, split, modified, textFile);
            modified = CheckHallucinationGlossary(config, split, modified, textFile);
        }

        // Characters  
        if (preparedRaw.EndsWith("...")
            && preparedRaw.Length < 15
            && !split.Translated.EndsWith("...")
            && !split.Translated.EndsWith("...?")
            && !split.Translated.EndsWith("...!")
            && !split.Translated.EndsWith("...!!")
            && !split.Translated.EndsWith("...?!"))
        {
            logLines.Add($"Missing ... {textFile.Path} Replaces: \n{split.Translated}");
            split.FlaggedForRetranslation = true;
            modified = true;
        }

        if (preparedRaw.StartsWith("...") && !split.Translated.StartsWith("..."))
        {
            logLines.Add($"Missing ... {textFile.Path} Replaces: \n{split.Translated}");
            split.Translated = $"...{split.Translated}";
            modified = true;
        }

        // Trim line
        if (split.Translated.Trim().Length != split.Translated.Length)
        {
            logLines.Add($"Needed Trimming:{textFile.Path} \n{split.Translated}");
            split.Translated = split.Translated.Trim();
            modified = true;
        }

        // Clean up Diacritics -- Use a new tokenizer because the translated isnt generated off the prep raw
        var cleanedUp = LineValidation.CleanupLineBeforeSaving(split.Translated, preparedRaw, textFile, new StringTokenReplacer());
        if (cleanedUp != split.Translated)
        {
            logLines.Add($"Cleaned up {textFile.Path} \n{split.Translated}\n{cleanedUp}");
            split.Translated = cleanedUp;
            modified = true;
        }

        // Remove Invalid ones -- Have to use pure raw because translated is untokenised
        var translated2 = StringTokenReplacer.CleanTranslatedForApplyRules(split.Translated);
        var result = LineValidation.CheckTransalationSuccessful(config, split.Text, translated2, textFile);
        if (!result.Valid)
        {
            logLines.Add($"Invalid {textFile.Path} Failures:{result.CorrectionPrompt}\n{split.Translated}");
            split.FlaggedForRetranslation = true;
            modified = true;
        }

        return modified;
    }

    private static bool CheckMistranslationGlossary(LlmConfig config, TranslationSplit split, bool modified, TextFileToSplit textFile)
    {
        if (!textFile.EnableGlossary)
            return modified;

        var tokenReplacer = new StringTokenReplacer();
        var preparedRaw = LineValidation.PrepareRaw(split.Text, tokenReplacer);

        if (split.Translated == null)
            return modified;

        foreach (var item in config.GlossaryLines)
        {
            if (!item.CheckForBadTranslation)
                continue;

            //Exclusions and Targetted Glossary
            if (item.OnlyOutputFiles.Count > 0 && !item.OnlyOutputFiles.Contains(textFile.Path))
                continue;
            else if (item.ExcludeOutputFiles.Count > 0 && item.ExcludeOutputFiles.Contains(textFile.Path))
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

    private static bool CheckHallucinationGlossary(LlmConfig config, TranslationSplit split, bool modified, TextFileToSplit textFile)
    {
        if (!textFile.EnableGlossary)
            return modified;

        var tokenReplacer = new StringTokenReplacer();
        var preparedRaw = LineValidation.PrepareRaw(split.Text, tokenReplacer);

        if (split.Translated == null)
            return modified;

        foreach (var item in config.GlossaryLines)
        {
            var wordPattern = $"\\b{item.Result}\\b";

            if (!preparedRaw.Contains(item.Raw) && split.Translated.Contains(item.Result))
            {
                if (!item.CheckForMisusedTranslation)
                    continue;

                //Exclusions and Targetted Glossary
                if (item.OnlyOutputFiles.Count > 0 && !item.OnlyOutputFiles.Contains(textFile.Path))
                    continue;
                else if (item.ExcludeOutputFiles.Count > 0 && item.ExcludeOutputFiles.Contains(textFile.Path))
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
        HashSet<string> words =
        [
            "hiu", "tut", "thut", "oi", "avo", "porqe", "obrigado",
            "knight", "knights", "knight-at-arms", "knights-errant",
            "nom", "esto", "tem", "mais", "com", "ver", "nos", "sobre", "vermos",
            "dar", "nam", "J'ai", "je", "veux", "pas", "ele", "una", "keqi", "shiwu",
            "fuck", "ich", "ein", "der", "ganzes", "Leben", "dort", //"de", NAmes can have de
            "knight", "thay", "tien", "div", "html", "tiantu", "ngoc", "truong", "Phong"
        ];

        string pattern = $@"\b({string.Join("|", words)})\b";

        return Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase);
    }
}
