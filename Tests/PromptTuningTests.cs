using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using Translate.Utility;

namespace Translate.Tests;
public class PromptTuningTests
{
    const string workingDirectory = "../../../../Files";

    public class TranslatedRaw(string raw)
    {
        public string Raw { get; set; } = raw;
        public ValidationResult ValidationResult { get; set; } = new ValidationResult();
    }

    public static TextFileToSplit DefaultTestTextFile() => new TextFileToSplit()
    {
        Path = "",
    };

    [Fact(DisplayName = "1. Test Current Prompts")]
    public async Task TestPrompt()
    {
        var config = Configuration.GetConfiguration(workingDirectory);
        config.SkipLineValidation = true;
        config.RetryCount = 1;

        // Create an HttpClient instance
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);

        config.RetryCount = 1;
        var batchSize = config.BatchSize ?? 50;

        var testLines = new List<TranslatedRaw> {
            //new("而且我已经不再迷惘，已经在你的帮助下……更加坚定了。"),
            new("\" 幻化说明\\\\n\\\\n1.只要获取过武器即可解锁对应的外观（即使被卖出或者丢弃，也不会再次锁上）\\\\n2.幻化时，请保持主角当前所穿戴的武器类型一致\\\\n3.武器简介可以下滑，在尾部有获取提示\\\\n4.幻化只变化外观，不会影响当前的装备属性与词条\\\\n5.幻化时选择“替换所有”，背包内所有的同类型武器会一起幻化\"")
            //new("{{限}}{0}"),
            //new("当前任职位：<color=#f1c36b>头牌</color>，收益加成：<color=#da914b>2倍</color><color=#a8a8a8>预计收益</color>")
            //new("<color=#b8b8b8>性别：</color>{0}"),
            //new("经脉点位由外而内分层级打通。五脏开窍不区分层级，但需打通所有经脉点。打通经脉点和五脏开窍会消耗一定的修为值。|经脉点属性分为固灵属性，附灵属性，暗灵属性。其中固灵属性激活点位时可获得；附灵属性，除大经脉点位激活拥有外，其余点位必须使用密卷激活才可附加上；暗灵属性必须使用带有效果的心法攻体激活时才会附加上。|五脏属性分为经脉属性和经脉技能。其中属性激活时可获得，也可通过密卷激活附加上；技能只能使用带有效果的心法攻体激活时才会附加上。"),
            //new("通关后天赋值"),
            //new("操作:地面上双击并长按<w >施放 "),
            //new("操作 靠近竹子然后双击<space     > 施放 "),
            //new("<color=#b7a57d>打造说明：</color>指针滑动时，在不同颜色区域按下锤炼按钮可增加对应的灵感值，灵感值影响打造完成时领悟到的属性点数。"),
            //new("完成奇遇任务《江海图志·枰栌》"),
            //new("万宗"),
            //new("劫数：                 <color=#2EDFA3>[大限将至]</color>"),
            //new("外功："),
            //new("实力："),
            //new("轻功："),
            //new("研发"),
            //new("御心："),
            //new("王铁(1000，1000)"),
            //new("品质:  人阶-超一流武学"),
            //new("淮陵-酒楼"),
            //new("前往乘风渡劫杀{E}（{IsCanFinish:0:1}/1)"),
            //new("男：忙忙碌碌把财求，何时云开见日头。\\n难得祖基家可立，中年衣食渐无忧。\\n女：早年行运在忙头，奔走四方事多忧。\\n费心劳神把家立，老年安生不发愁。"),
            //new("押镖问询·二"),
            //new("九环刀"),
            //new("桌凳"),
            //new("{0&0}%几率对我方随机角色添加持续一轮次的随即论点。"),
            //new("听闻枰栌附近的河域，有不少的{I}，这或许是个不错的财路。特颁此令以征三条{I}。"),
            //new("在<color=&&00ff00ff>武馆教头（枰栌）</color>处有概率获得"),
            //new("在对抗有优势火力的敌人时，兵家的常用战术是："),
            //new("阴、阳、刚、柔、毒各+{0:G}"),
            //new("{0&0}%几率对我方随机角色添加持续一轮次的随即论点。"),
            //new("达成机杼墨师身份，在龙蛇寨附近完成机杼传承"),
            //new("前往(882,-117)请教兵狂"),
            //new("击败目标点{GetZhiYingTargetPos}的{E} \\n {NeedKillNpcItemsCount}/{N})"),
            //new("各家学说，各抒己见，两两之间，总有克制。\\n强克制：对目标伤害提升0.5倍。被强克制：对目标伤害降低0.5倍。\\n强克制关系：道学→佛学→儒学→魔学→墨学→农学→道学。\\n弱克制：对目标伤害提升0.25倍。被弱克制：对目标伤害降低0.25倍。\\n弱克制关系：道学→儒学→墨学；佛学→魔学→农学。"),
            //new("为人心性最聪明，经营求财命不穷。善于经商，同时因为过多钻研商道导致心眼狭小，商道+50，豪爽-50。"),
            //new("到达目标点{GetZhiYingTargetPos}({IsCanFinish:0:1}/3)"),
            ////new("在淮陵游玩之际，<color=&&00ff00ff>遇到一位自称烈火刀阎巧的侠客正在挑战淮陵豪侠</color>，我观其似乎武艺高强。"),
            ////new("在淮陵游玩之际，<color=&&00ff00ff>遇到一位自称烈火刀阎巧的侠客正在挑战淮陵豪侠</color>，我观其似乎武艺高强。"),
            //new("在淮陵游玩之际，<color=&&00ff00ff>遇到一位自称烈火刀阎巧的侠客正在挑战淮陵豪侠</color>，我观其似乎武艺高强。"),
        };

        var cache = new Dictionary<string, string>();
        await TranslationService.FillTranslationCacheAsync(workingDirectory, 10, cache, config);       

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
                line.ValidationResult = await TranslationService.TranslateSplitAsync(config, line.Raw, client, DefaultTestTextFile());
                recordsProcessed++;
            }));

            var elapsed = stopWatch.ElapsedMilliseconds;
            var speed = recordsProcessed == 0 ? 0 : elapsed / recordsProcessed;
            Console.WriteLine($"Line: {i + batchRange} of {totalLines} ({elapsed} ms ~ {speed}/line)");
        }

        foreach (var line in testLines)
            results.Add($"From: {line.Raw}\nTo: {line.ValidationResult.Result}\nValid={line.ValidationResult.Valid}\n{line.ValidationResult.CorrectionPrompt}\n");

        File.WriteAllLines($"{workingDirectory}/TestResults/0.PromptTest.txt", results);
    }


    [Fact(DisplayName = "2. Optimise Provided Prompt")]
    public async Task OptimiseProvidedPrompt()
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);
        var config = Configuration.GetConfiguration(workingDirectory);

        // Prime the Request

        var basePrompt = config.Prompts["0PromptToOptimise"];
        var optimisePrompt = config.Prompts["0OptimisePrompt"];

        List<object> messages =
            [
                LlmHelpers.GenerateSystemPrompt(optimisePrompt),
                LlmHelpers.GenerateUserPrompt(basePrompt.ToString())
            ];

        // Generate based on what would have been created
        var result = await TranslationService.TranslateMessagesAsync(client, config, messages);

        File.WriteAllText($"{workingDirectory}/TestResults/1.MinimisePrompt.txt", result);
    }

    [Fact]
    public async Task NamePromptTest()
    {
        var textFile = DefaultTestTextFile();

        textFile.EnableBasePrompts = false;
        textFile.EnableGlossary = false;
        textFile.AdditionalPromptName = "FileRandomNamePrompt";

        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);
        var config = Configuration.GetConfiguration(workingDirectory);
        var input = "白亦";
        var result = await TranslationService.TranslateSplitAsync(config, input, client, textFile);

        File.WriteAllText($"{workingDirectory}/TestResults/2.NamePromptTest.txt", result.Result);
    }

    [Theory]
    [InlineData(1, "完成奇遇任务《轻功高手》")]
    [InlineData(2, "雄霸武林")]
    [InlineData(3, "德高望重重，才广武林称。兼备风云志，胸怀揽星辰。")]
    [InlineData(4, "于狂刀门贡献堂主处累积购买二十次")]
    [InlineData(5, "路过城西村时，遇到沈大娘正在收拾沈蛋，似乎是因为他将表姐家的衣服撕毁之事，沈蛋为了避免挨打，将竹条扔到了风车上面。")]
    [InlineData(6, "实力")]
    public async Task ExplainGlossaryPrompt(int index, string input)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);
        var config = Configuration.GetConfiguration(workingDirectory);
        var result = await TranslationService.TranslateSplitAsync(config, input, client, DefaultTestTextFile(),
            "Explain in a <think> why the glossary was or was not used. " +
            "How do I update the system prompt to make sure it uses the glossary in this case.");

        File.WriteAllText($"{workingDirectory}/TestResults/2.ExplainGlossaryPrompt{index}.txt", result.Result);
    }

    [Fact]
    public async Task ExplainPrompt2()
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);
        var config = Configuration.GetConfiguration(workingDirectory);
        var input = "豆花嫂希望你能为她丈夫带来虎鞭，至于用途应该不难猜？";
        var result = await TranslationService.TranslateSplitAsync(config, input, client, DefaultTestTextFile(),
            "Explain your reasoning in a <think> tag at the end of the response. Also explain why the ? was removed. Also explain how to adjust the system prompt to correct it to make sure the '?' was not removed and context is retained.");

        File.WriteAllText($"{workingDirectory}/TestResults/2.ExplainPrompt2.txt", result.Result);
    }

    [Fact]
    public async Task ExplainPrompt3()
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);
        var config = Configuration.GetConfiguration(workingDirectory);
        var input = "若果有此意，叫八戒伐几棵树来，沙僧寻些草来，我做木匠，就在这里搭个窝铺，你与她圆房成事，我们大家散了，却不是件事业？何必又跋涉，取什经去！";
        var result = await TranslationService.TranslateSplitAsync(config, input, client, DefaultTestTextFile(),
            "Explain your reasoning in a <think> tag at the end of the response. Also explain why the ! was removed. Also explain how to adjust the system prompt to correct it to make sure the '!' was not removed and context is retained. Show an example prompt.");

        File.WriteAllText($"{workingDirectory}/TestResults/2.ExplainPrompt3.txt", result.Result);
    }

    [Fact]
    public async Task ExplainAlternativesPrompt()
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);
        var config = Configuration.GetConfiguration(workingDirectory);
        //var input = "好嘞，客官您慢走！";
        //var input = "完成菩提";
        //var input = "人阶";
        var input = "实力";
        var result = await TranslationService.TranslateSplitAsync(config, input, client, DefaultTestTextFile(),
            "Explain your reasoning in a <think> tag at the end of the response. " +
            "Explain if/why you provided an alternative." +           
            "Show where to update my current system prompt to stop the alternative and just give me one answer.");

        File.WriteAllText($"{workingDirectory}/TestResults/2.ExplainAltPrompt.txt", result.Result);
    }

    [Fact]
    public async Task ExplainExplanationPrompt()
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);

        var config = Configuration.GetConfiguration(workingDirectory);
        //var input = "好嘞，客官您慢走！";
        var input = "幽影-剑意纵横";

        var result = await TranslationService.TranslateSplitAsync(config, input, client, DefaultTestTextFile(),
            "Explain your reasoning in a <think> tag at the end of the response. " +
            "Explain if/why you provided an explanation." +
            "Also explain how to adjust the system prompt to correct it to make sure you do not provide this explanation." +
            "Show an example prompt.");

        File.WriteAllText($"{workingDirectory}/TestResults/2.ExplainExplanationPrompt.txt", result.Result);
    }

    [Fact]
    public async Task ExplainNotEnglishPrompt()
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);

        var config = Configuration.GetConfiguration(workingDirectory);
        var input = "通关后天赋值";

        var result = await TranslationService.TranslateSplitAsync(config, input, client, DefaultTestTextFile(),
            "Explain your reasinging in a <explain> tag, why is the result is not in english." +
            "Show in a <prompt> tag, An updated system prompt that would have translated this to english.");

        File.WriteAllText($"{workingDirectory}/TestResults/2.ExplainNotEnglishPrompt.txt", result.Result);
    }

    [Fact]
    public async Task ExplainRemovedCharsPrompt()
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);

        var config = Configuration.GetConfiguration(workingDirectory);
        config.SkipLineValidation = true;
        config.RetryCount = 1;

        var input = "而且我已经不再迷惘，已经在你的帮助下……更加坚定了。";

        var result = await TranslationService.TranslateSplitAsync(config, input, client, DefaultTestTextFile(),
            "Explain your reasoning in a <explain> tag, why '...' characters were removed." +
            "Show in a <prompt> tag, the full updated current system prompt that would have not removed the '...' characters. " +
            "Highlight system prompt changes in a <changes> tag.");

        File.WriteAllText($"{workingDirectory}/TestResults/2.ExplainRemovedCharsPrompt.txt", result.Result);
    }

    [Fact]
    public async Task ExplainMissingMarkupPrompt()
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);

        var config = Configuration.GetConfiguration(workingDirectory);
        var input = "操作 地面上双击并长按<w >施放 ";

        var prompts = TranslationService.GenerateBaseMessages(config, input, DefaultTestTextFile());

        var result = await TranslationService.TranslateSplitAsync(config, input, client, DefaultTestTextFile(),
            "Explain your reasinging in a <explain> tag, why the <w  > tag is missing." +
            "Show in a <prompt> tag, An updated system prompt that would have translated this to english.");

        File.WriteAllText($"{workingDirectory}/TestResults/2.ExplainMissingMarkupPrompt.txt", result.Result);
    }

    [Fact]
    public async Task ExplainColorPrompt()
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);

        var config = Configuration.GetConfiguration(workingDirectory);
        var input = "在淮陵游玩之际，<color=&&00ff00ff>遇到一位自称烈火刀阎巧的侠客正在挑战淮陵豪侠</color>，我观其似乎武艺高强。";

        var result = await TranslationService.TranslateSplitAsync(config, input, client, DefaultTestTextFile(),            
            "Explain your reasinging in a <explain> tag, why is there no <color> tag in the final result." +
            "Show in a <prompt> tag, An updated system prompt to ensure the <color> tag is included in the final result.");

        File.WriteAllText($"{workingDirectory}/TestResults/2.ExplainColorPrompt.txt", result.Result);
    }

    [Fact]
    public async Task OptimiseCorrectTagPrompt()
    {
        var textFile = DefaultTestTextFile();

        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(300);
        var config = Configuration.GetConfiguration(workingDirectory);

        // Prime the Request
        var raw = "<color=#FF0000>炼狱</color>";
        var origResult = "Hellforge";
        var origValidationResult = LineValidation.CheckTransalationSuccessful(config, raw, origResult, textFile);
        List<object> messages = TranslationService.GenerateBaseMessages(config, raw, textFile);

        // Tweak Correction Prompt here
        var correctionPrompt = LineValidation.CalulateCorrectionPrompt(config, origValidationResult, raw, origResult);
        //var correctionPrompt = "Try again. The markup rules were not followed.";

        // Add what the correction prompt would have been
        TranslationService.AddCorrectionMessages(messages, origResult, correctionPrompt);

        var result = await TranslationService.TranslateMessagesAsync(client, config, messages);

        // Calculate output of test
        var validationResult = LineValidation.CheckTransalationSuccessful(config, raw, result, textFile);
        var lines = $"Valid:{validationResult.Valid}\nRaw:{raw}\nResult:{result}";
        File.WriteAllText($"{workingDirectory}/TestResults/OptimiseCorrectTag.txt", lines);
    }   
}
