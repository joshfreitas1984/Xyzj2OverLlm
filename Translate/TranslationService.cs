using SharedAssembly.DynamicStrings;
using System.Diagnostics;
using System.Net.Http.Headers;
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

        await FileIteration.IterateTranslatedFilesAsync(workingDirectory, async (outputFile, textFileToTranslate, fileLines) =>
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

        foreach (var textFileToTranslate in GameTextFiles.TextFilesToSplit)
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

    public static (bool foundSplit, List<string> splits) CalculateSubSplits(string origSplit)
    {
        var response = new List<string>();
        bool foundSplit = false;

        foreach (var splitCharacters in GameTextFiles.SplitCharactersList)
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
        foreach (var splitCharacters in GameTextFiles.SplitCharactersList)
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
