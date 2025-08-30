using SharedAssembly.DynamicStrings;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Net.Http.Headers;
using System.Runtime;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Translate.Support;
using Translate.Utility;

namespace Translate;

public static class TranslationService
{
    public const int BatchlessLog = 25;
    public const int BatchlessBuffer = 25;

    // "。" doesnt work like u think it would   
    public static string[] SplitCharactersList() => [":", "<br>", "\\n", "-", "|"];

    public static TextFileToSplit[] GetTextFilesToSplit()
        => [
            // Invalid for translation
            //new() {Path = "ai_dialog.txt"},
            //new() {Path = "keywordfilter.txt"},
            //new() {Path = "custom_data.txt", Output = true, OutputRawResource = true},
            //new() {Path = "born_points.txt", Output = true},
            //new() {Path = "emoji.txt", PackageOutput = true},
            new() {Path = "dlc_prototype.txt", PackageOutput = true },
            new() {Path = "dlcinfo.txt", PackageOutput = true },
            new() {Path = "dlc_data.txt", PackageOutput = true },

            new() {Path = "achievement.txt", PackageOutput = true},
            new() {Path = "achievement_xianejianghu.txt", PackageOutput = true},
            new() {Path = "achievement_zhenshijianghu.txt", PackageOutput = true},
            new() {Path = "buildprototype.txt", PackageOutput = true},
            new() {Path = "cardinfo.txt", PackageOutput = true},
            new() {Path = "chuanwenprototype.txt", PackageOutput = true},
            new() {Path = "condition_group.txt", PackageOutput = true},
            new() {Path = "condition_show_anim.txt", PackageOutput = true},
            new() {Path = "dumpedPrefabText.txt", TextFileType = TextFileType.PrefabText, AllowMissingColorTags = false},
            
            new() {Path = "dynamicStrings.txt", TextFileType = TextFileType.DynamicStrings, AllowMissingColorTags = false},
            new() {Path = "entrust_event_prototype.txt", PackageOutput = true},
            new() {Path = "fuben_prototype.txt", PackageOutput = true},
            new() {Path = "game_manual.txt", PackageOutput = true},
            new() {Path = "game_manual_clue.txt", PackageOutput = true},
            new() {Path = "guanqiaenemy.txt", PackageOutput = true},
            new() {Path = "guanqiainfo.txt", PackageOutput = true},
            new() {Path = "horoscope.txt", PackageOutput = true, AdditionalPromptName = "FileHoroscopePrompt"},
            new() {Path = "identity.txt", PackageOutput = true},
            new() {Path = "item_base.txt", PackageOutput = true},
            new() {Path = "item_base_xianejianghu.txt", PackageOutput = true},
            new() {Path = "item_base_zhenshijianghu.txt", PackageOutput = true},
            new() {Path = "item_ma_prototype.txt", PackageOutput = true},
            new() {Path = "jingmai_node_pos.txt", PackageOutput = true},
            new() {Path = "jueyinglou.txt", PackageOutput = true},
            new() {Path = "keylist.txt", PackageOutput = true},
            new() {Path = "loadingpicture.txt", PackageOutput = true},
            new() {Path = "loadingtips.txt", PackageOutput = true},
            new() {Path = "living_assemblyskill.txt", PackageOutput = true },
            new() {Path = "living_assemblyskill_zhenshijianghu.txt", PackageOutput = true },            
            new() {Path = "makerplayer_prototype.txt", PackageOutput = true},
            new() {Path = "mapinfo.txt", PackageOutput = true},
            new() {Path = "map_area.txt", PackageOutput = true},
            new() {Path = "map_area_shili.txt", PackageOutput = true},
            new() {Path = "map_area_title.txt", PackageOutput = true},
            new() {Path = "menpai.txt", PackageOutput = true},
            new() {Path = "menpaibuild.txt", PackageOutput = true},
            new() {Path = "menpaipaibie.txt", PackageOutput = true},
            new() {Path = "menpaipeifang.txt", PackageOutput = true},
            new() {Path = "menpaiquest.txt", PackageOutput = true},
            new() {Path = "menpairandom.txt", PackageOutput = true},
            new() {Path = "menpaisoldier.txt", PackageOutput = true},
            new() {Path = "menpaitalent.txt", PackageOutput = true},
            new() {Path = "mystique.txt", PackageOutput = true},
            new() {Path = "nandu.txt", PackageOutput = true},
            new() {Path = "npc_interact.txt", PackageOutput = true},
            new() {Path = "npc_prototype.txt", PackageOutput = true},
            new() {Path = "npc_spell_container.txt", PackageOutput = true},
            new() {Path = "npc_spell_dynamic_name.txt", PackageOutput = true},
            new() {Path = "npc_team_info.txt", PackageOutput = true},
            new() {Path = "pve_data.txt", PackageOutput = true},
            new() {Path = "qinggong_node.txt", PackageOutput = true},
            new() {Path = "questjiemi.txt", PackageOutput = true},
            new() {Path = "questprototype.txt", PackageOutput = true },
            new() {Path = "randomname.txt", PackageOutput = true, AdditionalPromptName = "FileRandomNamePrompt",
                EnableGlossary = false, EnableBasePrompts = false, RemoveNumbers = true, NameCleanupRoutines = true},
            new() {Path = "randomnamenew.txt", PackageOutput = true, AdditionalPromptName = "FileRandomNameNewPrompt",
                EnableGlossary = false, EnableBasePrompts = false, RemoveNumbers = true, NameCleanupRoutines2 = true},
            new() {Path = "randomquestion.txt", PackageOutput = true},
            new() {Path = "shangcheng_prototype.txt", PackageOutput = true},
            new() {Path = "spelleffect.txt", PackageOutput = true},
            new() {Path = "spelleffect_xianejianghu.txt", PackageOutput = true},
            new() {Path = "spelleffect_zhenshijianghu.txt", PackageOutput = true},
            new() {Path = "spellprotype.txt", PackageOutput = true},
            new() {Path = "spellprotype_xianejianghu.txt", PackageOutput = true},
            new() {Path = "spellprotype_zhenshijianghu.txt", PackageOutput = true},
            new() {Path = "stunt_proto.txt", PackageOutput = true},
            new() {Path = "system_introduce.txt", PackageOutput = true},
            new() {Path = "talent_proto.txt", PackageOutput = true},
            new() {Path = "teleport_trans.txt", PackageOutput = true},
            new() {Path = "triggertip.txt", PackageOutput = true},
            new() {Path = "tujian.txt", PackageOutput = true},
            new() {Path = "wordentryrandomtype.txt", PackageOutput = true},
            new() {Path = "wordentrytitle.txt", PackageOutput = true},
            new() {Path = "wordentrytype.txt", PackageOutput = true},
            new() {Path = "xunwen_prototype.txt", PackageOutput = true},
            new() {Path = "yingdao_prototype.txt", PackageOutput = true},

            //Biggest one
            new() {Path = "stringlang.txt", PackageOutput = true, IsMainDialogueAsset = true, }, //RemoveExtraFullStop = false
        ];

    public static void WriteSplitDbFile(string outputDirectory, string fileName, int shouldHave, bool hasChinese, List<string> lines)
    {
        if (string.IsNullOrEmpty(fileName))
            return;

        Console.WriteLine($"Writing Split {fileName}.. Should have..{shouldHave} Have..{lines.Count}");

        // Files we split but not actually changing
        if (fileName == "ai_dialog"
            || fileName == "keywordfilter"
            //|| fileName == "living_assemblyskill"
            //|| fileName == "living_assemblyskill_zhenshijianghu"
            //|| fileName == "questprototype"
            || fileName == "born_points"
            || fileName == "custom_data"
            || fileName == "emoji")
            hasChinese = false;

        if (hasChinese)
            File.WriteAllLines($"{outputDirectory}/{fileName}.txt", lines);
        else
            File.WriteAllLines($"{outputDirectory}/../Remaining/{fileName}.txt", lines);
    }

    public static void SplitDbAssets(string workingDirectory)
    {
        string inputPath = $"{workingDirectory}/Raw/DB";
        string outputPath = $"{workingDirectory}/Raw/SplitDb";

        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        var lines = File.ReadAllLines($"{inputPath}/db1.txt");
        var splitDbName = string.Empty;
        var splitDbCount = 0;
        var hasChinese = false;
        var pattern = LineValidation.ChineseCharPattern;
        var currentSplitLines = new List<string>();

        foreach (var line in lines)
        {
            // New File Split
            if (line.Contains('|') && !line.Contains('#'))
            {
                var splits = line.Split('|');
                if (splits.Length == 2)
                {
                    // Primary Write
                    WriteSplitDbFile(outputPath, splitDbName, splitDbCount, hasChinese, currentSplitLines);
                    splitDbName = splits[0];
                    splitDbCount = int.Parse(splits[1]);
                    hasChinese = false;
                    currentSplitLines = [];
                    Console.WriteLine($"Starting New Split: {splitDbName}...");
                    continue;
                }
            }

            // We only care about DB entries with CN text in it
            if (!hasChinese)
                if (Regex.IsMatch(line, pattern))
                    hasChinese = true;

            currentSplitLines.Add(line);
        }

        //Trailing Write
        WriteSplitDbFile(outputPath, splitDbName, splitDbCount, hasChinese, currentSplitLines);
    }

    public static void ExportTextAssetsToCustomFormat(string workingDirectory)
    {
        string outputPath = $"{workingDirectory}/Raw/Export";

        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        var serializer = Yaml.CreateSerializer();
        var pattern = LineValidation.ChineseCharPattern;

        var dir = new DirectoryInfo($"{workingDirectory}/Raw/SplitDb");
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            var foundLines = new List<TranslationLine>();
            var lines = File.ReadAllLines(file.FullName);
            var lineIncrement = 0;

            foreach (var line in lines)
            {
                lineIncrement++;
                var splits = line.Split("#");
                var foundSplits = new List<TranslationSplit>();

                // Default to line number when it doesnt have line number in split
                if (!long.TryParse(splits[0], out long lineNum))
                    lineNum = lineIncrement;

                // Find Chinese
                for (int i = 0; i < splits.Length; i++)
                {
                    if (Regex.IsMatch(splits[i], pattern))
                    {
                        foundSplits.Add(new TranslationSplit()
                        {
                            Split = i,
                            Text = splits[i],
                        });
                    }
                }

                //The translation line
                foundLines.Add(new TranslationLine()
                {
                    //LineNum = lineNum,
                    Raw = line,
                    Splits = foundSplits,
                });
            }

            // Write the found lines
            var yaml = serializer.Serialize(foundLines);
            File.WriteAllText($"{outputPath}/{file.Name}", yaml);
        }
    }

    public static void ExportDumpedPrefabToCustomFormat(string workingDirectory)
    {
        string inputPath = $"{workingDirectory}/Raw/ExportedText";
        string outputPath = $"{workingDirectory}/Raw/Export";

        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        var serializer = Yaml.CreateSerializer();
        var pattern = LineValidation.ChineseCharPattern;

        var dir = new DirectoryInfo(inputPath);
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            var foundLines = new List<TranslationLine>();
            var lines = File.ReadAllLines(file.FullName);
            var lineIncrement = 0;

            foreach (var line in lines)
            {
                lineIncrement++;
                var splits = new string[] { line };
                var foundSplits = new List<TranslationSplit>();

                // Default to line number when it doesnt have line number in split
                if (!long.TryParse(splits[0], out long lineNum))
                    lineNum = lineIncrement;

                // Find Chinese
                for (int i = 0; i < splits.Length; i++)
                {
                    if (Regex.IsMatch(splits[i], pattern))
                    {
                        foundSplits.Add(new TranslationSplit()
                        {
                            Split = i,
                            Text = splits[i],
                        });
                    }
                }

                //The translation line
                foundLines.Add(new TranslationLine()
                {
                    //LineNum = lineNum,
                    Raw = line,
                    Splits = foundSplits,
                });
            }

            // Write the found lines
            var yaml = serializer.Serialize(foundLines);
            File.WriteAllText($"{outputPath}/{file.Name}", yaml);
        }
    }

    public static void ExportDynamicStringsToCustomFormat(string workingDirectory)
    {
        string inputPath = $"{workingDirectory}/Raw/DynamicStrings";
        string outputPath = $"{workingDirectory}/Raw/Export";

        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        var serializer = Yaml.CreateSerializer();
        var pattern = LineValidation.ChineseCharPattern;

        var dir = new DirectoryInfo(inputPath);
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            var foundLines = new List<TranslationLine>();
            var lines = File.ReadAllLines(file.FullName);
            var lineIncrement = 0;

            foreach (var line in lines)
            {
                lineIncrement++;
                var splits = line.Split(",");
                var foundSplits = new List<TranslationSplit>();

                // Default to line number when it doesnt have line number in split
                if (!long.TryParse(splits[0], out long lineNum))
                    lineNum = lineIncrement;

                // Find Chinese
                for (int i = 0; i < splits.Length; i++)
                {
                    if (Regex.IsMatch(splits[i], pattern))
                    {
                        var cleaned = splits[i];
                        if (cleaned.StartsWith('\"'))
                            cleaned = cleaned[1..];
                        if (cleaned.EndsWith('\"'))
                            cleaned = cleaned[..^1];

                        foundSplits.Add(new TranslationSplit()
                        {
                            Split = i,
                            Text = cleaned,
                        });
                    }
                }

                //The translation line
                foundLines.Add(new TranslationLine()
                {
                    //LineNum = lineNum,
                    Raw = line,
                    Splits = foundSplits,
                });
            }

            // Write the found lines
            var yaml = serializer.Serialize(foundLines);
            File.WriteAllText($"{outputPath}/{file.Name}", yaml);
        }
    }

    public static async Task MergeFilesIntoTranslatedAsync(string workingDirectory)
    {
        await TranslationService.IterateTranslatedFilesAsync(workingDirectory, async (outputFile, textFileToTranslate, fileLines) =>
        {
            var newCount = 0;

            ////Disable for now since they should be same
            //if (textFileToTranslate.TextFileType == TextFileType.RegularDb)
            //    return;

            var deserializer = Yaml.CreateDeserializer();
            var exportFile = outputFile.Replace("Converted", "Raw/Export");
            var exportLines = deserializer.Deserialize<List<TranslationLine>>(File.ReadAllText(exportFile));

            foreach (var line in exportLines)
            {
                var found = fileLines.FirstOrDefault(x => x.Raw == line.Raw);
                if (found != null)
                {
                    foreach (var split in line.Splits)
                    {
                        var found2 = found.Splits.FirstOrDefault(x => x.Text == split.Text);
                        if (found2 != null)
                            split.Translated = found2.Translated;
                    }
                }
                else
                {
                    // Try matching on split instead of line incase they changed line format
                    foreach (var split in line.Splits)
                    {
                        var found2 = fileLines
                            .Select(x => x.Splits.FirstOrDefault(s => s.Text == split.Text))
                            .FirstOrDefault(s => s != null);

                        if (found2 != null)
                            split.Translated = found2.Translated;
                        else
                            newCount++;
                    }
                }
            }

            Console.WriteLine($"New Lines {textFileToTranslate.Path}: {newCount}");

            //if (newCount > 0 || exportLines.Count != fileLines.Count) //Always Write because they might have changed format
            {
                var serializer = Yaml.CreateSerializer();
                File.WriteAllText(outputFile, serializer.Serialize(exportLines));
            }

            await Task.CompletedTask;
        });
    }

    public static async Task FillTranslationCacheAsync(string workingDirectory, int charsToCache, Dictionary<string, string> cache, LlmConfig config)
    {
        // Add Manual adjustments 
        foreach (var k in config.ManualTranslations)
            cache.Add(k.Raw, k.Result);

        // Add Glossary Lines to Cache
        foreach (var line in config.GlossaryLines)
        {
            if (!cache.ContainsKey(line.Raw))
                cache.Add(line.Raw, line.Result);
        }

        // File with old files
        var oldFolder = $"{workingDirectory}/TestResults/OldFiles";

        var deserializer = Yaml.CreateDeserializer();

        foreach (var file in Directory.EnumerateFiles(oldFolder))
        {
            var content = File.ReadAllText(file);
            var lines = deserializer.Deserialize<List<TranslationLine>>(content);

            foreach (var line in lines)
            {
                foreach (var split in line.Splits)
                {
                    if (!cache.ContainsKey(split.Text))
                        cache.Add(split.Text, split.Translated);
                }
            }
        }

        await TranslationService.IterateTranslatedFilesAsync(workingDirectory, async (outputFile, textFileToTranslate, fileLines) =>
        {
            foreach (var line in fileLines)
            {
                foreach (var split in line.Splits)
                {
                    if (string.IsNullOrEmpty(split.Translated) || split.FlaggedForRetranslation)
                        continue;

                    if (split.Text.Length <= charsToCache && !cache.ContainsKey(split.Text))
                        cache.Add(split.Text, split.Translated);

                    //// EXPERIMENTAL: Add in splits to cache
                    //var splitsTranslated = CalculateSubSplits(split.Translated);
                    //var splitsRaw = CalculateSubSplits(split.Text);
                    //if (splitsTranslated.foundSplit
                    //    && splitsRaw.foundSplit
                    //    && splitsRaw.splits.Count == splitsTranslated.splits.Count)
                    //{
                    //    for (int i = 0; i < splitsTranslated.splits.Count; i++)
                    //        cache.Add(splitsRaw.splits[i], splitsTranslated.splits[i]);
                    //}
                }
            }

            await Task.CompletedTask;
        });

        //Add it to config to make it easier to use
        config.TranslationCache = cache;
    }

    public static async Task TranslateViaLlmAsync(string workingDirectory, bool forceRetranslation)
    {
        string inputPath = $"{workingDirectory}/Raw/Export";
        string outputPath = $"{workingDirectory}/Converted";

        // Create output folder
        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        var config = Configuration.GetConfiguration(workingDirectory);

        // Translation Cache - for smaller translations that tend to hallucinate
        var translationCache = new Dictionary<string, string>();
        var charsToCache = 10;
        await FillTranslationCacheAsync(workingDirectory, charsToCache, translationCache, config);

        // Create an HttpClient instance
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);

        if (config.ApiKeyRequired)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);

        int incorrectLineCount = 0;
        int totalRecordsProcessed = 0;

        foreach (var textFileToTranslate in GetTextFilesToSplit())
        {
            var inputFile = $"{inputPath}/{textFileToTranslate.Path}";
            var outputFile = $"{outputPath}/{textFileToTranslate.Path}";

            if (!File.Exists(outputFile))
                File.Copy(inputFile, outputFile);

            var content = File.ReadAllText(outputFile);

            Console.WriteLine($"Processing File: {outputFile}");

            var serializer = Yaml.CreateSerializer();
            var deserializer = Yaml.CreateDeserializer();
            var fileLines = deserializer.Deserialize<List<TranslationLine>>(content);

            var batchSize = config.BatchSize ?? 20;
            var totalLines = fileLines.Count;
            var stopWatch = Stopwatch.StartNew();
            int recordsProcessed = 0;
            int bufferedRecords = 0;

            int logProcessed = 0;

            for (int i = 0; i < totalLines; i += batchSize)
            {
                int batchRange = Math.Min(batchSize, totalLines - i);

                // Use a slice of the list directly
                var batch = fileLines.GetRange(i, batchRange);

                // Get Unique splits incase the batch has the same entry multiple times (eg. NPC Names)
                var uniqueSplits = batch.SelectMany(line => line.Splits)
                    .GroupBy(split => split.Text)
                    .Select(group => group.First())
                    .ToList(); // Materialize to prevent multiple enumerations;

                // Process the unique in parallel
                await Task.WhenAll(uniqueSplits.Select(async split =>
                {
                    if (string.IsNullOrEmpty(split.Text) || !split.SafeToTranslate)
                        return;

                    var cacheHit = translationCache.ContainsKey(split.Text)
                        // We use this for name files etc which will be in cache
                        && textFileToTranslate.EnableGlossary;

                    if (string.IsNullOrEmpty(split.Translated)
                        || forceRetranslation
                        || (config.TranslateFlagged && split.FlaggedForRetranslation))
                    {
                        var original = split.Translated;

                        if (cacheHit)
                            split.Translated = translationCache[split.Text];
                        else
                        {
                            var result = await TranslateSplitAsync(config, split.Text, client, textFileToTranslate);
                            split.Translated = result.Valid ? result.Result : string.Empty;
                        }

                        split.ResetFlags(split.Translated != original);
                        recordsProcessed++;
                        totalRecordsProcessed++;
                        bufferedRecords++;
                    }

                    if (string.IsNullOrEmpty(split.Translated))
                        incorrectLineCount++;
                    //Two translations could be doing this at the same time
                    else if (!cacheHit && split.Text.Length <= charsToCache)
                        translationCache.TryAdd(split.Text, split.Translated);
                }));

                // Duplicates
                var duplicates = batch.SelectMany(line => line.Splits)
                    .GroupBy(split => split.Text)
                    .Where(group => group.Count() > 1);

                foreach (var splitDupes in duplicates)
                {
                    var firstSplit = splitDupes.First();

                    // Skip first one - it should be ok
                    foreach (var split in splitDupes.Skip(1))
                    {
                        if (split.Translated != firstSplit.Translated
                            || string.IsNullOrEmpty(split.Translated)
                            || forceRetranslation
                            || (config.TranslateFlagged && split.FlaggedForRetranslation))
                        {
                            split.Translated = firstSplit.Translated;
                            split.ResetFlags();
                            recordsProcessed++;
                            totalRecordsProcessed++;
                            bufferedRecords++;
                        }
                    }
                }

                logProcessed++;

                if (batchSize != 1 || (logProcessed % BatchlessLog == 0))
                    Console.WriteLine($"Line: {i + batchRange} of {totalLines} File: {outputFile} Unprocessable: {incorrectLineCount} Processed: {totalRecordsProcessed}");

                if (bufferedRecords > BatchlessBuffer)
                {
                    Console.WriteLine($"Writing Buffer....");
                    File.WriteAllText(outputFile, serializer.Serialize(fileLines));
                    bufferedRecords = 0;
                }
            }

            var elapsed = stopWatch.ElapsedMilliseconds;
            var speed = recordsProcessed == 0 ? 0 : elapsed / recordsProcessed;
            Console.WriteLine($"Done: {totalLines} ({elapsed} ms ~ {speed}/line)");
            File.WriteAllText(outputFile, serializer.Serialize(fileLines));
        }
    }

    public static async Task PackageFinalTranslationAsync(string workingDirectory)
    {
        string inputPath = $"{workingDirectory}/Converted";
        string outputPath = $"{workingDirectory}/Mod/{ModHelper.ContentFolder}";
        string outputDbPath = $"{workingDirectory}/Mod/";

        if (Directory.Exists(outputPath))
            Directory.Delete(outputPath, true);

        Directory.CreateDirectory(outputPath);

        var finalDb = new List<string>();
        var passedCount = 0;
        var failedCount = 0;

        await TranslationService.IterateTranslatedFilesAsync(workingDirectory, async (outputFile, textFileToTranslate, fileLines) =>
        {
            var failedLines = new List<string>();
            var outputLines = new List<string>();

            if (textFileToTranslate.TextFileType == TextFileType.PrefabText)
            {
                foreach (var line in fileLines)
                {
                    foreach (var split in line.Splits)
                        if (!split.FlaggedForRetranslation && !(string.IsNullOrEmpty(split.Translated)))
                            outputLines.Add($"- raw: {split.Text}\n  result: {split.Translated}");
                        else if (!split.SafeToTranslate)
                            continue; // Do not count failure
                        else
                            failedCount++;
                }
            }
            else if (textFileToTranslate.TextFileType == TextFileType.DynamicStrings)
            {
                var serializer = Yaml.CreateSerializer();
                var contracts = new List<DynamicStringContract>();

                foreach (var line in fileLines)
                {
                    if (line.Splits.Count != 1)
                    {
                        failedCount++;
                        continue;
                    }

                    // Do not package but dont count as failure
                    if (!line.Splits[0].SafeToTranslate)
                        continue;

                    var lineRaw = line.Raw;
                    var splits = lineRaw.Split(",");

                    var lineTrans = line.Splits[0].Translated
                        .Replace("，", ","); // Replace Wide quotes back

                    if (splits.Length != 5
                        || string.IsNullOrEmpty(lineTrans)
                        || line.Splits[0].FlaggedForRetranslation)
                    {
                        failedCount++;
                        continue;
                    }

                    string[] parameters = DynamicStringSupport.PrepareMethodParameters(splits[4]);

                    var contract = new DynamicStringContract()
                    {
                        Type = splits[0],
                        Method = splits[1],
                        ILOffset = long.Parse(splits[2]),
                        Raw = splits[3],
                        Translation = lineTrans,
                        Parameters = parameters
                    };

                    if (DynamicStringSupport.IsSafeContract(contract, false))
                        contracts.Add(contract);
                }

                File.WriteAllText($"{outputDbPath}/Formatted/{textFileToTranslate.Path}", serializer.Serialize(contracts));
                passedCount += contracts.Count;

                await Task.CompletedTask;
                return;
            }
            else // TextFileType.RegularDb
            {

                foreach (var line in fileLines)
                {
                    // Regular DB handling
                    var splits = line.Raw.Split('#');
                    var failed = false;

                    foreach (var split in line.Splits)
                    {
                        if (!textFileToTranslate.PackageOutput
                            || split.FlaggedForRetranslation
                            || !split.SafeToTranslate) //Count Failure
                        {
                            failed = true;
                            break;
                        }

                        //Check line to be extra safe
                        if (split.Translated.Contains('#') || Regex.IsMatch(split.Translated, @"(?<!\\)\n"))
                            failed = true;
                        else if (!string.IsNullOrEmpty(split.Translated))
                            splits[split.Split] = split.Translated;
                        //If it was already blank its all good
                        else if (!string.IsNullOrEmpty(split.Text))
                            failed = true;
                    }

                    line.Translated = string.Join('#', splits);

                    if (!failed)
                        outputLines.Add(line.Translated);
                    else
                    {
                        outputLines.Add(line.Raw);
                        failedLines.Add(line.Raw);
                    }
                }
            }

            // Do not want to package prefabs into main db
            if (textFileToTranslate.TextFileType == TextFileType.RegularDb)
            {
                finalDb.Add($"{Path.GetFileNameWithoutExtension(outputFile)}|{fileLines.Count}");
                finalDb.AddRange(outputLines);
            }

            File.WriteAllLines($"{outputDbPath}/Formatted/{textFileToTranslate.Path}", outputLines);

            passedCount += outputLines.Count;
            failedCount += failedLines.Count;

            await Task.CompletedTask;
        });

        var dir = new DirectoryInfo($"{workingDirectory}/Raw/Remaining");
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            var fileLines = File.ReadAllLines(file.FullName);

            finalDb.Add($"{Path.GetFileNameWithoutExtension(file.Name)}|{fileLines.Length}");
            finalDb.AddRange(fileLines);
        }

        Console.WriteLine($"Passed: {passedCount}");
        Console.WriteLine($"Failed: {failedCount}");

        ModHelper.GenerateModConfig(workingDirectory);
        File.WriteAllLines($"{outputDbPath}/db1.txt", finalDb);
    }

    public static async Task IterateTranslatedFilesAsync(string workingDirectory, Func<string, TextFileToSplit, List<TranslationLine>, Task> performActionAsync)
    {
        var deserializer = Yaml.CreateDeserializer();
        string outputPath = $"{workingDirectory}/Converted";

        foreach (var textFileToTranslate in GetTextFilesToSplit())
        {
            var outputFile = $"{outputPath}/{textFileToTranslate.Path}";

            if (!File.Exists(outputFile))
                continue;

            var content = File.ReadAllText(outputFile);

            var fileLines = deserializer.Deserialize<List<TranslationLine>>(content);

            if (performActionAsync != null)
                await performActionAsync(outputFile, textFileToTranslate, fileLines);
        }
    }

    public static async Task IterateTranslatedFilesInParallelAsync(string workingDirectory, Func<string, TextFileToSplit, List<TranslationLine>, Task> performActionAsync)
    {
        var deserializer = Yaml.CreateDeserializer();
        string outputPath = $"{workingDirectory}/Converted";

        var tasks = GetTextFilesToSplit()
            .Select(async textFileToTranslate =>
            {
                var outputFile = $"{outputPath}/{textFileToTranslate.Path}";

                if (!File.Exists(outputFile))
                    return;

                var content = await File.ReadAllTextAsync(outputFile);
                var fileLines = deserializer.Deserialize<List<TranslationLine>>(content);

                if (performActionAsync != null)
                    await performActionAsync(outputFile, textFileToTranslate, fileLines);
            });

        await Task.WhenAll(tasks);
    }

    public static (bool foundSplit, List<string> splits) CalculateSubSplits(string origSplit)
    {
        var response = new List<string>();
        bool foundSplit = false;

        foreach (var splitCharacters in SplitCharactersList())
        {
            if (origSplit.Contains(splitCharacters))
            {
                foundSplit = true;
                var newSplits = origSplit.Split(splitCharacters);

                foreach (var newSplit in newSplits)
                {
                    if (!string.IsNullOrEmpty(newSplit))
                    {
                        var subSplits = CalculateSubSplits(newSplit);
                        if (subSplits.foundSplit)
                            response.AddRange(subSplits.splits);
                        else
                            response.Add(newSplit);
                    }
                }

                // Break after processing one split character type
                // Because recursion would have got the rest
                return (foundSplit, response);
            }
        }

        return (foundSplit, response);
    }

    public static async Task<(bool split, string result)> SplitIfNeededAsync(string splitCharacters, LlmConfig config, string raw, HttpClient client, TextFileToSplit textFile)
    {
        if (raw.Contains(splitCharacters))
        {
            var splits = raw.Split(splitCharacters);
            var builder = new StringBuilder();

            string suffix;

            if (splitCharacters == "-")
                suffix = " - ";
            else if (splitCharacters == ":")
                suffix = ": ";
            else
                suffix = splitCharacters;

            foreach (var split in splits)
            {
                var trans = await TranslateSplitAsync(config, split, client, textFile);

                // If one fails we have to kill the lot
                if (!trans.Valid && !config.SkipLineValidation)
                    return (true, string.Empty);

                builder.Append($"{trans.Result}{suffix}");
            }

            var result = builder.ToString();

            // Remove the very last suffix that was added
            if (splits.Length > 1)
                return (true, result[..^suffix.Length]);
            else
                return (true, result);
        }

        return (false, string.Empty);
    }

    public static async Task<(bool split, string result)> SplitBracketsIfNeededAsync(LlmConfig config, string raw, HttpClient client, TextFileToSplit textFile)
    {
        if (raw.Contains('('))
        {
            string output = string.Empty;
            string pattern = @"([^\(]*|(?:.*?))\(([^\)]*)\)|([^\(\)]*)$"; // Matches text outside and inside brackets

            MatchCollection matches = Regex.Matches(raw, pattern);
            foreach (Match match in matches)
            {
                var outsideStart = match.Groups[1].Value.Trim();
                var outsideEnd = match.Groups[3].Value.Trim();
                var inside = match.Groups[2].Value.Trim();

                if (!string.IsNullOrEmpty(outsideStart))
                {
                    var trans = await TranslateSplitAsync(config, outsideStart, client, textFile);
                    output += trans.Result;

                    // If one fails we have to kill the lot
                    if (!trans.Valid && !config.SkipLineValidation)
                        return (true, string.Empty);
                }

                if (!string.IsNullOrEmpty(inside))
                {
                    var trans = await TranslateSplitAsync(config, inside, client, textFile);
                    output += $" ({trans.Result}) ";

                    // If one fails we have to kill the lot
                    if (!trans.Valid && !config.SkipLineValidation)
                        return (true, string.Empty);
                }

                if (!string.IsNullOrEmpty(outsideEnd))
                {
                    var trans = await TranslateSplitAsync(config, outsideEnd, client, textFile);
                    output += trans.Result;

                    // If one fails we have to kill the lot
                    if (!trans.Valid && !config.SkipLineValidation)
                        return (true, string.Empty);
                }
            }

            return (true, output.Trim());
        }

        return (false, string.Empty);
    }

    public static async Task<ValidationResult> TranslateSplitAsync(LlmConfig config, string? raw, HttpClient client, TextFileToSplit textFile, string additionalPrompts = "")
    {
        if (string.IsNullOrEmpty(raw))
            return new ValidationResult(true, string.Empty); //Is ok because raw was empty

        var pattern = LineValidation.ChineseCharPattern;

        // If it is already translated or just special characters return it
        if (!Regex.IsMatch(raw, pattern))
            return new ValidationResult(true, raw);

        // Prepare the raw by stripping out anything the LLM can't support
        var tokenReplacer = new StringTokenReplacer();
        var preparedRaw = LineValidation.PrepareRaw(raw, tokenReplacer);

        // If it is already translated or just special characters return it
        if (!Regex.IsMatch(preparedRaw, pattern))
            return new ValidationResult(true, LineValidation.CleanupLineBeforeSaving(preparedRaw, preparedRaw, textFile, tokenReplacer));

        // Brackets Split first - so it doesnt split stuff inside the brackets
        //var (split2, result2) = await SplitBracketsIfNeededAsync(config, preparedRaw, client, fileName);
        //if (split2)
        //    return LineValidation.CleanupLineBeforeSaving(result2, preparedRaw, fileName, tokenReplacer);

        // TODO: We really should move this segementation to the object model itself and split it at export time
        // We do segementation here since saves context window by splitting // "。" doesnt work like u think it would        
        foreach (var splitCharacters in SplitCharactersList())
        {
            var (split, result) = await SplitIfNeededAsync(splitCharacters, config, preparedRaw, client, textFile);

            // Because its recursive we want to bail out on the first successful one
            if (split)
                return new ValidationResult(LineValidation.CleanupLineBeforeSaving(result, preparedRaw, textFile, tokenReplacer));
        }

        if (ColorTagHelpers.StartsWithHalfColorTag(preparedRaw, out string start, out string end))
        {
            var startResult = await TranslateSplitAsync(config, start, client, textFile);
            var endResult = await TranslateSplitAsync(config, end, client, textFile);
            var combinedResult = $"{startResult.Result}{endResult.Result}";

            if (!config.SkipLineValidation && (!startResult.Valid || !endResult.Valid))
                return new ValidationResult(false, string.Empty);
            else
                return new ValidationResult(LineValidation.CleanupLineBeforeSaving($"{combinedResult}", preparedRaw, textFile, tokenReplacer));
        }

        var cacheHit = config.TranslationCache.ContainsKey(preparedRaw);
        if (cacheHit)
            return new ValidationResult(LineValidation.CleanupLineBeforeSaving(config.TranslationCache[preparedRaw], preparedRaw, textFile, tokenReplacer));

        // Define the request payload
        List<object> messages = GenerateBaseMessages(config, preparedRaw, textFile, additionalPrompts);

        try
        {
            var retryCount = 0;
            var preparedResult = string.Empty;
            var validationResult = new ValidationResult();

            while (!validationResult.Valid && retryCount < (config.RetryCount ?? 1))
            {
                var llmResult = await TranslateMessagesAsync(client, config, messages);
                preparedResult = LineValidation.PrepareResult(preparedRaw, llmResult);
                validationResult = LineValidation.CheckTransalationSuccessful(config, preparedRaw, preparedResult, textFile);
                validationResult.Result = LineValidation.CleanupLineBeforeSaving(validationResult.Result, preparedRaw, textFile, tokenReplacer);

                // Append history of failures
                if (!validationResult.Valid && config.CorrectionPromptsEnabled)
                {
                    var correctionPrompt = LineValidation.CalulateCorrectionPrompt(config, validationResult, preparedRaw, llmResult);

                    // Regenerate base messages so we dont hit token limit by constantly appending retry history
                    messages = GenerateBaseMessages(config, preparedRaw, textFile);
                    AddCorrectionMessages(messages, llmResult, correctionPrompt);
                }

                retryCount++;
            }

            return validationResult;
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Request error: {e.Message}");
            return new ValidationResult(string.Empty);
        }
    }

    public static void AddCorrectionMessages(List<object> messages, string result, string correctionPrompt)
    {
        messages.Add(LlmHelpers.GenerateAssistantPrompt(result));
        messages.Add(LlmHelpers.GenerateUserPrompt(correctionPrompt));
    }

    public static List<object> GenerateBaseMessages(LlmConfig config, string raw, TextFileToSplit splitFile, string additionalSystemPrompt = "")
    {
        //Dynamically build prompt using whats in the raws
        var basePrompt = new StringBuilder();

        if (splitFile.EnableBasePrompts)
        {
            basePrompt.AppendLine(config.Prompts["BaseSystemPrompt"]);

            if (raw.Contains("<color"))
                basePrompt.AppendLine(config.Prompts["DynamicColorPrompt"]);
            else if (raw.Contains("</color>"))
                basePrompt.AppendLine(config.Prompts["DynamicCloseColorPrompt"]);

            if (raw.Contains("·"))
                basePrompt.AppendLine(config.Prompts["DynamicSegement1Prompt"]);

            if (raw.Contains("<"))
            {
                var rawTags = HtmlTagHelpers.ExtractTagsListWithAttributes(raw, "color");
                if (rawTags.Count > 0)
                {
                    var prompt = string.Format(config.Prompts["DynamicTagPrompt"], string.Join("\n", rawTags));
                    //Console.WriteLine(raw);
                    //Console.WriteLine(prompt);
                    basePrompt.AppendLine(prompt);
                }
            }

            if (raw.Contains('{'))
                basePrompt.AppendLine(config.Prompts["DynamicPlaceholderPrompt"]);
        }

        if (!string.IsNullOrEmpty(splitFile.AdditionalPromptName))
            basePrompt.AppendLine(config.Prompts[splitFile.AdditionalPromptName]);

        basePrompt.AppendLine(additionalSystemPrompt);

        if (splitFile.EnableGlossary)
        {
            basePrompt.AppendLine(config.Prompts["BaseGlossaryPrompt"]);
            basePrompt.AppendLine(GlossaryLine.AppendPromptsFor(raw, config.GlossaryLines, splitFile.Path));
        }

        return
        [
            LlmHelpers.GenerateSystemPrompt(basePrompt.ToString()),
            LlmHelpers.GenerateUserPrompt(raw)
        ];
    }

    public static void AddPromptWithValues(this StringBuilder builder, LlmConfig config, string promptName, params string[] values)
    {
        var prompt = string.Format(config.Prompts[promptName], values);
        builder.Append(' ');
        builder.Append(prompt);
    }

    public static void CopyDirectory(string sourceDir, string destDir)
    {
        // Get the subdirectories for the specified directory.
        var dir = new DirectoryInfo(sourceDir);

        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory does not exist or could not be found: {sourceDir}");

        // If the destination directory doesn't exist, create it.
        if (!Directory.Exists(destDir))
            Directory.CreateDirectory(destDir);

        // Get the files in the directory and copy them to the new location.
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            var tempPath = Path.Combine(destDir, file.Name);
            file.CopyTo(tempPath, false);
        }

        // Copy each subdirectory using recursion
        DirectoryInfo[] dirs = dir.GetDirectories();
        foreach (DirectoryInfo subdir in dirs)
        {
            if (subdir.Name == ".git" || subdir.Name == ".vs")
                continue;

            var tempPath = Path.Combine(destDir, subdir.Name);
            CopyDirectory(subdir.FullName, tempPath);
        }
    }

    public static async Task<string> TranslateInputAsync(HttpClient client, LlmConfig config, string input, TextFileToSplit textFile, string additionalPrompt = "")
    {
        List<object> messages = TranslationService.GenerateBaseMessages(config, input, textFile, additionalPrompt);
        return await TranslateMessagesAsync(client, config, messages);
    }

    public static async Task<string> TranslateMessagesAsync(HttpClient client, LlmConfig config, List<object> messages)
    {
        // Generate based on what would have been created
        var requestData = LlmHelpers.GenerateLlmRequestData(config, messages);

        // Send correction & Get result
        HttpContent content = new StringContent(requestData, Encoding.UTF8, "application/json");
        HttpResponseMessage response = await client.PostAsync(config.Url, content);
        response.EnsureSuccessStatusCode();
        string responseBody = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(responseBody);
        var result = jsonDoc.RootElement
            .GetProperty("message")!
            .GetProperty("content")!
            .GetString()
            ?.Trim() ?? string.Empty;

        return result;
    }
}
