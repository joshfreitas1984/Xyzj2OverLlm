﻿using Microsoft.VisualStudio.TestPlatform.Common;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Threading.Tasks;
using YamlDotNet.Core;

namespace Translate.Tests;
public class PromptTuningTests
{
    const string workingDirectory = "../../../../Files";

    [Fact]
    public async Task OptimiseCorrectTagPrompt()
    {
        var config = Configuration.GetConfiguration(workingDirectory);

        // Create an HttpClient instance
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);

        // Prime the Request
        var raw = "<color=#FF0000>炼狱</color>";
        var origResult = "Hellforge";
        var origValidationResult = LineValidation.CheckTransalationSuccessful(config, raw, origResult, string.Empty);
        List<object> messages = TranslationService.GenerateBaseMessages(config, raw, string.Empty);

        // Tweak Correction Prompt here
        var correctionPrompt = LineValidation.CalulateCorrectionPrompt(config, origValidationResult, raw, origResult);
        //var correctionPrompt = "Try again. The markup rules were not followed.";

        // Add what the correction prompt would have been
        TranslationService.AddCorrectionMessages(messages, origResult, correctionPrompt);

        // Generate based on what would have been created
        var requestData = LlmHelpers.GenerateLlmRequestData(config, messages);

        //var requestData = File.ReadAllText($"{workingDirectory}/Optimise/OptimiseCorrectTag.json")
        //  .Replace("%ReplaceMe%", correctionPrompt);

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

        // Calculate output of test
        var validationResult = LineValidation.CheckTransalationSuccessful(config, raw, result, string.Empty);
        var lines = $"Valid:{validationResult.Valid}\nRaw:{raw}\nResult:{result}";
        File.WriteAllText($"{workingDirectory}/TestResults/OptimiseCorrectTag.txt", lines);
    }

    [Fact]
    public async Task OptimiseProvidedPrompt()
    {
        var config = Configuration.GetConfiguration(workingDirectory);

        // Create an HttpClient instance
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);

        // Prime the Request

        var basePrompt = config.Prompts["0PromptToOptimise"];
        var optimisePrompt = config.Prompts["0OptimisePrompt"];

        List<object> messages =
            [
                LlmHelpers.GenerateSystemPrompt(optimisePrompt),
                LlmHelpers.GenerateUserPrompt(basePrompt.ToString())
            ]; 
        
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

        File.WriteAllText($"{workingDirectory}/TestResults/1.MinimisePrompt.txt", result);
    }

    [Fact]
    public async Task ExplainPrompt()
    {
        var config = Configuration.GetConfiguration(workingDirectory);

        // Create an HttpClient instance
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);

        // Prime the Request

        List<object> messages = TranslationService.GenerateBaseMessages(config, "飞虹剑阵？那是甚么？很厉害吗？", 
            string.Empty,
            "Explain your reasoning in a <think> tag at the end of the response. Also explain how and if the glossary was used. Also explain how to adjust the system prompt to correct the fact the resulting output is not in english?");

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

        File.WriteAllText($"{workingDirectory}/TestResults/2.ExplainPrompt.txt", result);
    }

    [Fact]
    public async Task ExplainPrompt2()
    {
        var config = Configuration.GetConfiguration(workingDirectory);

        // Create an HttpClient instance
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);

        // Prime the Request

        List<object> messages = TranslationService.GenerateBaseMessages(config, "豆花嫂希望你能为她丈夫带来虎鞭，至于用途应该不难猜？",
            string.Empty,
            "Explain your reasoning in a <think> tag at the end of the response. Also explain why the ? was removed. Also explain how to adjust the system prompt to correct it to make sure the '?' was not removed and context is retained.");

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

        File.WriteAllText($"{workingDirectory}/TestResults/2.ExplainPrompt.txt", result);
    }

    [Fact]
    public async Task ExplainPrompt3()
    {
        var config = Configuration.GetConfiguration(workingDirectory);

        // Create an HttpClient instance
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);

        // Prime the Request

        List<object> messages = TranslationService.GenerateBaseMessages(config, "若果有此意，叫八戒伐几棵树来，沙僧寻些草来，我做木匠，就在这里搭个窝铺，你与她圆房成事，我们大家散了，却不是件事业？何必又跋涉，取什经去！",
            string.Empty,
            "Explain your reasoning in a <think> tag at the end of the response. Also explain why the ! was removed. Also explain how to adjust the system prompt to correct it to make sure the '!' was not removed and context is retained. Show an example prompt.");

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

        File.WriteAllText($"{workingDirectory}/TestResults/2.ExplainPrompt.txt", result);
    }

    [Fact]
    public async Task TestPrompt()
    {
        var config = Configuration.GetConfiguration(workingDirectory);

        // Create an HttpClient instance
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);

        config.RetryCount = 1;
        var batchSize = config.BatchSize ?? 10;

        var testLines = new List<TranslatedRaw> {
            new("飞虹剑阵？那是甚么？很厉害吗？"),
            new("哼，看来不教训教训你，你不会明白胡乱耍嘴皮子是要付出代价的。"),
            new("原来少侠只需要看看书就够了，那也不需要阁里姑娘的青睐吧？"),
            new("小时"),
            new("嗷呜"),
            new("吱吱！！"),
            new ("亢龍有悔"),
            new ("初入江湖"),
            new ("经验值80000点，江湖声望，玄铁"),
            new("{0} 加入队伍"),
            new("{0} 离开队伍"),
            new("{0}{1} 经验"),
            new ("唔…也许<color=#FF0000>李叹兄弟</color>识货无数，对于藏宝诗词肯定也是懂的。或许可以找个适当借口，向李叹兄弟问问这事。"),
            new("资质+{0}"),
            new("心念不起，自性不动。<br>着相即乱，离相不乱。"),
            new("<color=#FF0000>炼狱</color>"),
            new("颜玉书在场上时，所有队友的攻击力提升50%，减伤10%"),
            new("剧情"),
            new("{name_1}兄，你没事吧？"),
            new("{name_2}兄，你没事吧？"),
            new("柔：卸勁"),
            new("蟒蛇"),
            new("黄连"),
            new("迷惑"),
            new("孩子，若是你<color=#FF0000>搜索天书的过程里有了些进展，便回来这儿看看</color>，说不准我们也会有什么重大的突破。"),
            new("<color=#FFCC22>我手上有一封信，是洪义交给我的。</color>"),
            new("难道会是梨花姑娘挣扎之时，从<color=#FF0000>凶手</color>身上扯将下来的<color=#FF0000>证据</color>吗？"),
            new("（收殓第<color=#FF0000>四</color>具骸骨。）"),
            new("佛教七宝就是，佛教僧人修行所用的七项宝物，有「<color=#FF0000>金</color>、<color=#FF0000>银</color>、<color=#FF0000>珍珠</color>、<color=#FF0000>珊瑚</color>、<color=#FF0000>蜜蜡</color>、<color=#FF0000>砗磲</color>、<color=#FF0000>红玉髓</color>」等七种，各自都有不同的修行作用与宗教意义。"),
            new("这是第<color=#FF0000>六</color>次的份量。大哥哥，记得，只剩最后一次的份量了，千万记得在一天之内将足量的药草带过来，否则就会前功尽弃，一切得要<color=#FF0000>重新来过</color>了呀！"),
            new("在孔金舍命相救下，总算是惊险逃出了圣堂，但如今势单力孤，敌暗我明，只能按照孔金之前的计划，前往拱石村寻找同为河洛一族的南闲。然而南闲似乎因为拖欠了酒钱，被押到村长那里却又早已自行逃脱。在协助解决了蛇窟的问题后，梅村长派出的乡勇邀请你一同搜索徐暇客的下落。<br>跟着乡勇前往集合处，却想不到乡勇之中竟混入了豹王寨的流寇，根据他们供出的情报，梅小青被他们绑到了后山山洞，村中乡勇不知还藏着多少内应，为免打草惊蛇，此刻当先前往后山山洞营救梅小青。"),
        };

        var results = new List<string>();
        var totalLines = testLines.Count;
        var stopWatch = Stopwatch.StartNew();

        for (int i = 0; i < totalLines; i += batchSize)
        {
            stopWatch.Restart();

            int batchRange = Math.Min(batchSize, totalLines - i);

            // Use a slice of the list directly
            var batch = testLines.GetRange(i, batchRange);

            int recordsProcessed = 0;

            // Process the batch in parallel
            await Task.WhenAll(batch.Select(async line =>
            {
                line.Trans = await TranslationService.TranslateSplitAsync(config, line.Raw, client, string.Empty);
                recordsProcessed++;
            }));

            var elapsed = stopWatch.ElapsedMilliseconds;
            var speed = recordsProcessed == 0 ? 0 : elapsed / recordsProcessed;
            Console.WriteLine($"Line: {i + batchRange} of {totalLines} ({elapsed} ms ~ {speed}/line)");
        }

        foreach (var line in testLines)
            results.Add($"From: {line.Raw}\nTo: {line.Trans}\n");

        File.WriteAllLines($"{workingDirectory}/TestResults/0.PromptTest.txt", results);
    }

    private class StructuredTranslation
    {
        public string Translated { get; set; }
        public string Transliterated { get; set; }
    }

    [Fact]
    public async Task StructuredPromptTests()
    {
        var config = Configuration.GetConfiguration(workingDirectory);

        // Create an HttpClient instance
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);

        // Prime the Request
        var basePrompt = "Direct Translate and Transliterate whatever I give you. Respond in VALID Json only.";

        List<object> messages =
            [
                LlmHelpers.GenerateSystemPrompt(basePrompt),
                LlmHelpers.GenerateUserPrompt("经验值80000点，江湖声望，玄铁")
            ];

        //Generate Schema
        JsonSerializerOptions options = JsonSerializerOptions.Default;
        JsonNode schema = options.GetJsonSchemaAsNode(typeof(StructuredTranslation));
        config.ModelParams.Add("format", schema);

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

        File.WriteAllText($"{workingDirectory}/TestResults/4.FormatedResponse.json", result);
    }

    [Fact]
    public async Task ExtractGlossaryItemTest()
    {
        var config = Configuration.GetConfiguration(workingDirectory);

        // Create an HttpClient instance
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);
        var result = await TranslationService.ExtractGlossaryItemAsync(config, client, "经验值80000点，江湖声望，玄铁");
        File.WriteAllText($"{workingDirectory}/TestResults/3.GlossaryResponse.json", result);
    }
}
