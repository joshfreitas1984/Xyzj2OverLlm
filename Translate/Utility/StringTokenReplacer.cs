using System.Text;
using System.Text.RegularExpressions;
using YamlDotNet.Core.Tokens;

namespace Translate.Utility;

/// <summary>
/// Replace things we know cause issues with the LLM with straight tokens which it seems to handle ok. 
/// </summary>
public class StringTokenReplacer
{
    private static readonly Regex PlaceholderRegex = new(@"(\{[^{}]+\})", RegexOptions.Compiled);
    private static readonly Regex CoordinateRegex = new(@"\(-?\d+,-?\d+\)", RegexOptions.Compiled);
    private static readonly Regex NumericValueRegex = new(@"(?<![{<]|color=|<[^>]*)(?:[+-]?(?:\d+\.\d*|\.\d+|\d+))(?![}>])", RegexOptions.Compiled);   
    private static readonly Regex ColorStartRegex = new(@"<color=[^>]+>", RegexOptions.Compiled);
    private static readonly Regex KeyPressRegex = new(@"<\w+\s+>", RegexOptions.Compiled);
    private static readonly Regex TokenRegex;
    private static readonly Regex EmojiRegex;

    public static readonly Regex SizeRegex = new(@"<size=[^>]+>", RegexOptions.Compiled);
    public static readonly Regex SizeValueRegex = new(@"(?<=<size=)\d+", RegexOptions.Compiled);
    public static readonly Regex SizeValue2Regex = new(@"(?<=<size=#)\d+", RegexOptions.Compiled);

    public static string[] otherTokens = ["{}"];
    public static string[] EmojiItems = [
        "[发现宝箱]",
        "[石化]",
        "[开心]",
        "[不知所措]",
        "[疑问]",
        "[担忧]",
        "[生气]",
        "[哭泣]",
        "[惊讶]",
        "[发怒]",
        "[抓狂]",
        "[委屈]",
    ];

    private Dictionary<int, string> placeholderMap = new();
    private Dictionary<string, string> colorMap = new();
    private Dictionary<string, string> sizeMap = new();

    // Use Static constructor to make sure the regexes are only compiled once (otherwise very slow)
    static StringTokenReplacer()
    {
        var tokenPattern = string.Join("|", otherTokens.Select(Regex.Escape));
        TokenRegex = new Regex(tokenPattern, RegexOptions.Compiled);

        var emojiPattern = string.Join("|", EmojiItems.Select(Regex.Escape));
        EmojiRegex = new Regex(emojiPattern, RegexOptions.Compiled);
    }

    public static int CalculateNewSize(string sizeTag)
    {
        var sizeString = SizeValueRegex.Match(sizeTag).Value;
        if (string.IsNullOrEmpty(sizeString))
            sizeString = SizeValue2Regex.Match(sizeTag).Value;

        return (int)Math.Round(Convert.ToInt32(sizeString) * 0.7);
    }

    public string Replace(string input)
    {
        int index = 0;
        int colorIndex = 0;
        int sizeIndex = 0;
        placeholderMap.Clear();
        colorMap.Clear();
        var result = new StringBuilder(input);

        result.Replace(PlaceholderRegex, match =>
        {
            placeholderMap.Add(index, match.Value);
            return $"{{{index++}}}";
        });

        result.Replace(CoordinateRegex, match =>
        {
            placeholderMap.Add(index, match.Value);
            return $"{{{index++}}}";
        });

        result.Replace(ColorStartRegex, match =>
        {
            string replacement = $"<color={colorIndex++}>";
            colorMap.Add(replacement, match.Value);
            return replacement;
        });

        result.Replace(KeyPressRegex, match =>
        {
            placeholderMap.Add(index, match.Value.Replace(" ", ""));
            return $"{{{index++}}}";
        });

        // Check for size tags and replace the numeric value inside
        result.Replace(SizeRegex, match =>
        {
            var sizeTag = match.Value;
            var sizeValue = CalculateNewSize(sizeTag);
            var hasHash = sizeTag.Contains("#");
            var key = hasHash ? $"<size=#{sizeIndex++}>" : $"<size={sizeIndex++}>";
            var replacement = hasHash ? $"<size=#{sizeValue}>" : $"<size={sizeValue}>";

            sizeMap.Add(key, replacement);
            return key;
        });

        result.Replace(NumericValueRegex, match =>
        {
            placeholderMap.Add(index, match.Value);
            return $"{{{index++}}}";
        });      

        result.Replace(TokenRegex, match =>
        {
            placeholderMap.Add(index, match.Value);
            return $"{{{index++}}}";
        });

        result.Replace(EmojiRegex, match =>
        {
            placeholderMap.Add(index, match.Value);
            return $"{{{index++}}}";
        });

        return result.ToString();
    }

    public string Restore(string input)
    {
        var result = new StringBuilder(input);

        result.Replace(PlaceholderRegex, match =>
        {
            if (int.TryParse(match.Value.Trim('{', '}'), out int index)
                && placeholderMap.TryGetValue(index, out string? original))
            {
                return original;
            }
            return match.Value;
        });

        foreach (var size in sizeMap)
            result.Replace(size.Key, size.Value);

        foreach (var color in colorMap)
            result.Replace(color.Key, color.Value);

        return result.ToString();
    }

    public static string CleanTranslatedForApplyRules(string input)
    {
        return EmojiRegex.Replace(input, "");
    }
}