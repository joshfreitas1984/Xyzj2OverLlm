using SharedAssembly.DynamicStrings;
using SweetPotato;
using System.Text.RegularExpressions;
using Translate.Utility;

namespace Translate.Tests;

public class UtilityTests
{
    const string workingDirectory = "../../../../Files";

    [Theory]
    [InlineData("A.Hello name, welcome to place!",
           "A.Hello name, welcome to place!")]
    [InlineData("B.Hello {name}, welcome to {place}!",
           "B.Hello {0}, welcome to {1}!")]
    [InlineData("C.与{E}as（{IsCanFinish:0:1}/1)",
           "C.与{0}as({1}/{2})")]
    [InlineData("D.击败目标点{GetZhiYingTargetPos}的{E}（{NeedKillNpcItemsCount}/{N})",
           "D.击败目标点{0}的{1}({2}/{3})")]
    [InlineData("E.击败目标点{GetZhiYingTargetPos}的{E} \\n {NeedKillNpcItemsCount}/{N})",
       "E.击败目标点{0}的{1} \\n {2}/{3})")]
    [InlineData("F.各家学说，各抒己见，两两之间，总有克制。\\n强克制：对目标伤害提升0.5倍。被强克制：对目标伤害降低0.5倍。\\n强克制关系：道学→佛学→儒学→魔学→墨学→农学→道学。\\n弱克制：对目标伤害提升0.25倍。被弱克制：对目标伤害降低0.25倍。\\n弱克制关系：道学→儒学→墨学；佛学→魔学→农学。",
       "F.各家学说,各抒己见,两两之间,总有克制。\\n强克制:对目标伤害提升{0}倍。被强克制:对目标伤害降低{1}倍。\\n强克制关系:道学→佛学→儒学→魔学→墨学→农学→道学。\\n弱克制:对目标伤害提升{2}倍。被弱克制:对目标伤害降低{3}倍。\\n弱克制关系:道学→儒学→墨学；佛学→魔学→农学。")]
    [InlineData("G.正有事找你，前些日子{}特意送来好礼，如今也该是回礼的日子了，你拿上此物交给{}事务总管，事成之后门中会奖励一枚不夜京承渝令。",
           "G.正有事找你,前些日子{0}特意送来好礼,如今也该是回礼的日子了,你拿上此物交给{1}事务总管,事成之后门中会奖励一枚不夜京承渝令。")]
    [InlineData("H.天随人愿，历经千辛万苦，终于在{1}发现了{0}，可谓福气满满。",
           "H.天随人愿,历经千辛万苦,终于在{0}发现了{1},可谓福气满满。")]
    [InlineData("I.覆灭穆特前线的所有穆特族（{IsCanFinish:0:1}/1）",
           "I.覆灭穆特前线的所有穆特族({0}/{1})")]
    [InlineData("J.到达目标点{GetZhiYingTargetPos}({IsCanFinish:0:1}/3)",
       "J.到达目标点{0}({1}/{2})")]
    [InlineData("K.在淮陵游456玩之际，<color=&&00ff00ff>遇到{0}一123位自</color>，我观其似乎武艺高强。",
       "K.在淮陵游{1}玩之际,<color=0>遇到{0}一{2}位自</color>,我观其似乎武艺高强。")]
    [InlineData("L.王铁(1000，1000)",
       "L.王铁{0}")]
    [InlineData("O.<fontsize=24.12>abc</fontsize>",
       "O.<fontsize=24.12>abc</fontsize>")]
    [InlineData("{{限}}{0}", "{{0}}{1}")]
    [InlineData("{-1}|阁下的目标并非等闲之辈，眼下派出的杀手并非他的对手。绝影楼不做折兵的生意，阁下还是多花些钱财重新指派下人选吧。",
        "{0}|阁下的目标并非等闲之辈,眼下派出的杀手并非他的对手。绝影楼不做折兵的生意,阁下还是多花些钱财重新指派下人选吧。")]
    public static void StringTokenReplacerTests(string original, string expectedToken)
    {
        var replacer = new StringTokenReplacer();

        //Want string cleaned up
        original = LineValidation.PrepareRaw(original, null);
        string replaced = replacer.Replace(original);
        string restored = replacer.Restore(replaced);

        Console.WriteLine("Original: " + original);
        Console.WriteLine("Replaced: " + replaced);
        Console.WriteLine("Restored: " + restored);

        Assert.Equal(original, restored);
        Assert.Equal(expectedToken, replaced);
    }

    [Theory]
    [InlineData("M.<size=36>5</size><size=32>人</size>",
       "M.<size=0>{0}</size><size=1>人</size>",
       "M.<size=25>5</size><size=22>人</size>")]
    [InlineData("N.<color=36>10</color><size=24>人</fontsize>22.11 +14 -11",
       "N.<color=0>{0}</color><size=0>人</fontsize>{1} {2} {3}",
       "N.<color=36>10</color><size=17>人</fontsize>22.11 +14 -11")]
  
    [InlineData("正有事找你,前些日子{1}特意送来好礼,<size=24>是回礼的日子了. [发现宝箱]",
        "正有事找你,前些日子{0}特意送来好礼,<size=0>是回礼的日子了. {1}",
        "正有事找你,前些日子{1}特意送来好礼,<size=17>是回礼的日子了. [发现宝箱]")]
    [InlineData("[开心]正有事找你,前些日子{1}特意送来好礼,<size=24>是回礼的日子了.", 
        "{1}正有事找你,前些日子{0}特意送来好礼,<size=0>是回礼的日子了.",
        "[开心]正有事找你,前些日子{1}特意送来好礼,<size=17>是回礼的日子了.")]
    public static void StringTokenReplacerSizeTests(string original, string expectedToken, string expectedRestored)
    {
        var replacer = new StringTokenReplacer();

        //Want string cleaned up
        original = LineValidation.PrepareRaw(original, null);
        string replaced = replacer.Replace(original);
        string restored = replacer.Restore(replaced);

        Console.WriteLine("Original: " + original);
        Console.WriteLine("Replaced: " + replaced);
        Console.WriteLine("Restored: " + restored);

        Assert.Equal(expectedRestored, restored);
        Assert.Equal(expectedToken, replaced);
    }

    [Theory]
    [InlineData("[SweetPotato.Gift/GIFT_TYPE，System.Collections.Generic.Dictionary`2<System.Int64，System.Collections.Generic.Dictionary`2<System.Int64，System.Int32>>]", 1)]
    [InlineData("[System.Collections.Generic.Dictionary`2<System.Int64>，SweetPotato.Gift/GIFT_TYPE，System.Collections.Generic.Dictionary`2<System.Int64，System.Int32>>]", 2)]
    public void TestParameterSplitRegex(string rawParameters, int index)
    {
        var serializer = Yaml.CreateSerializer();
        var parameters = DynamicStringSupport.PrepareMethodParameters(rawParameters);
        var output = serializer.Serialize(parameters);

        string outputFile = $"{workingDirectory}/TestResults/TestParameterSplitRegex{index}.yaml";
        File.WriteAllText(outputFile, output);
        File.AppendAllLines(outputFile, [rawParameters]);
    }

    [Theory]
    [InlineData("Hello.", "Hello")] // Single word, should remove full stop
    [InlineData("This is a test.", "This is a test")] // Three words, should remove full stop
    [InlineData("This is an even longer test ok ok.", "This is an even longer test ok ok.")] // seven words, should keep full stop
    [InlineData("No full stop here", "No full stop here")] // No full stop, should remain unchanged
    [InlineData("Multiple. Sentences here.", "Multiple. Sentences here.")] // Multiple sentences, should remain unchanged
    [InlineData("  Spaces before and after.  ", "  Spaces before and after  ")] // Leading/trailing spaces, should remove full stop
    [InlineData("Spaces before and after.  ", "Spaces before and after  ")] // trailing spaces, should remove full stop
    public void RemoveFullStop_Tests(string input, string expected)
    {
        string result = LineValidation.RemoveFullStop("", input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("<size=30>可分配点数<color=#ff0000>不足</color></size>", "<size=3>0 Allocatable Points <color=#ff0000>Insufficient</color></size>", false)]
    [InlineData("<size=30>可分配点数<color=#ff0000>不足</color></size>", "<size=21> Allocatable Points <color=#ff0000>Insufficient</color></size>", true)]
    [InlineData("<size=30>可分配点数<color=#ff0000>不足</color></size>", "<size=21> Allocatable Points Insufficient</size>", true, true)]
    [InlineData("<size=30>可分配点数<color=#ff0000>不足</color></size>", "<size=30> Allocatable Points Insufficient</size>", false, false)]
    [InlineData("<b>Hello</b>", "<b>Bonjour</b>", true)]
    [InlineData("<i>Test</i>", "<b>Test</b>", false)]
    [InlineData("<p class='text'>Paragraph</p>", "<p class='text'>Paragraphe</p>", true)]
    [InlineData("<div id='main'><span>Text</span></div>", "<div id='main'><span>Texte</span></div>", true)]
    [InlineData("<div class='container'><span>Text</span></div>", "<div class='content'><span>Texte</span></div>", false)]
    [InlineData("<span style='color:red;'>Text</span>", "<span style='color:blue;'>Text</span>", false)]
    [InlineData("<button disabled>Click</button>", "<button disabled>Click</button>", true)]
    public void HtmlTagValidator_ValidateTags_Tests(string raw, string translated, bool expected, bool allowMissingColors = false)
    {
        bool result = HtmlTagHelpers.ValidateTags(raw, translated, allowMissingColors);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("<w   >", "<w>")]
    [InlineData("<div   class=\"example\">", "<div class=\"example\">")]
    [InlineData("<input    type=\"text\" value=\"hello\">", "<input type=\"text\" value=\"hello\">")]
    [InlineData("<p     >", "<p>")]
    [InlineData("<   h1>", "<h1>")]
    [InlineData("<   span   >", "<span>")]
    [InlineData("asdaf   <  br  />   sadasd", "asdaf   <br/>   sadasd")]
    [InlineData("<  img src=\"image.jpg\"   />", "<img src=\"image.jpg\"/>")]
    public void TrimHtmlTag_ShouldTrimExtraSpaces(string input, string expected)
    {
        // Act
        string result = HtmlTagHelpers.TrimHtmlTagsInContent(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("<div class='test'>Content</div><p>Text</p>", "p", "<div class='test'>")]
    [InlineData("<div><span>Text</span><p>Text</p></div>", "p", "<div>", "<span>")]
    [InlineData("<img src='image.jpg' alt='image'><a href='url.com'>Link</a>", "a", "<img src='image.jpg' alt='image'>")]
    [InlineData("No tags here!", "div")]
    [InlineData("", "div")]
    [InlineData("??<color=0>+{0}</color> <tag>assd</tag>", "color", "<tag>")]
    public void ExtractTagsListWithAttributes_ShouldExtractCorrectTags(string input, string ignore, params string[] expectedTags)
    {
        // Act
        var result = HtmlTagHelpers.ExtractTagsListWithAttributes(input, ignore);

        // Assert
        Assert.Equal(expectedTags, result);
    }

    [Theory]
    [InlineData("<color=#b8b8b8>性别:", true, "<color=#b8b8b8>", "性别:")] // Should match
    [InlineData("<color=123456>", true, "<color=123456>", "")] // Should match (even if the color value is just numbers)
    [InlineData("<color=#ff5733>SomeText", true, "<color=#ff5733>", "SomeText")] // Should match
    [InlineData("<color=#b8b8b8>Test</color>", false)] // Should not match (contains </color>)
    [InlineData("<color=#123456>Valid Color</color>", false)] // Should not match (contains </color>)
    [InlineData("<color=#aaa>SomeText</color> and more", false)] // Should not match (contains </color>)
    [InlineData("<color=#abc>Test content</color>", false)] // Should not match (contains </color>)
    [InlineData("Not a color tag", false)] // Should not match (not a color tag)    
    [InlineData("性别<color=123456>", false)] // Doesnt start with color
    public void MatchColorTagTests(string input, bool expected, string expectedStart = "", string expectedEnd = "")
    {
        // Assert if the result matches the expected value
        bool result = ColorTagHelpers.StartsWithHalfColorTag(input, out string start, out string end);
        Assert.Equal(expected, result);
        Assert.Equal(expectedStart, start);
        Assert.Equal(expectedEnd, end);
    }

    [Theory]
    [InlineData("{Hello你好}", true)]   // Test with both English and CJK characters
    [InlineData("{Hello 你好}", true)]   // Test with both English and CJK characters
    [InlineData("{你好}", true)]         // Test with only CJK characters
    [InlineData("{Hello}", false)]      // Test with only English characters (should fail)
    [InlineData("{Hello123}", false)]   // Test with English characters and numbers (should fail)
    [InlineData("{ }", false)]          // Test with empty curly braces (should fail)
    [InlineData("{}", false)]          // Test with empty curly braces (should fail)
    [InlineData("{0}", false)]          // Test with number
    [InlineData("{0 }", false)]
    [InlineData("{A }", false)]
    [InlineData("{你好 }", false)]
    public void TestChinesePlaceholderPattern(string input, bool expectedResult)
    {
        // Compile the regex
        var regex = new Regex(LineValidation.ChinesePlaceholderPattern);

        // Check if the input matches the pattern and assert the result
        var result = regex.IsMatch(input);
        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData("前往乘风渡劫杀{E}（{IsCanFinish:0:1}/1)", "asffsdf（{IsCanFinish:0:1}/1)", false)]
    [InlineData("前往乘风渡劫杀{E}（{IsCanFinish:0:1}/1)", "asffsdf {E} ({IsCanFinish:0:1}/1)", true)]
    [InlineData("前往乘风渡劫杀{E}（{IsCanFinish:0:1}/1)", "asffsdf {E}（{IsCan Finish:0:1}/1)", false)]
    [InlineData("前往乘风渡劫杀{E}（{IsCanFinish:0:1}/1)", "asffsdf {E}（0/1)", false)]
    public void CheckTransalationSuccessfulTest(string raw, string result, bool valid)
    {
        var config = Configuration.GetConfiguration(workingDirectory);

        // Act
        var validationResult = LineValidation.CheckTransalationSuccessful(config, raw, result, new TextFileToSplit());

        // Assert
        Assert.Equal(valid, validationResult.Valid);
    }

    [Fact]
    public void LocalStringTest()
    {
        string[] lines = File.ReadAllLines($"{workingDirectory}/Mod/Formatted/local_text_string.txt");

        LocalTextString.CreateFromCsvRow(lines);
    }
}
