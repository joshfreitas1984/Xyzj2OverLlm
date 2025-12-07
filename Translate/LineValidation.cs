using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Translate.Utility;

namespace Translate;

public static partial class LineValidation
{
    public const string ChineseCharPattern = @".*\p{IsCJKUnifiedIdeographs}.*";
    public const string ChinesePlaceholderPattern = @"\{[a-zA-Z]*\s*\p{IsCJKUnifiedIdeographs}+\}";
    public const string PlaceholderMatchPattern = @"(\{[^{}]+\})";

    // If you use Regexes in hot paths, compile them for performance
    public static readonly Regex ChineseCharPatternCompiled = new Regex(ChineseCharPattern, RegexOptions.Compiled);

    public static string PrepareRaw(string raw, StringTokenReplacer? tokenReplacer)
    {
        // Clean up the Raw string before using

        //StripColorTags(raw)
        raw = raw
            //.Replace("。", ".") //Hold off on this one for now
            .Replace("…", "...")
            .Replace("：", ":")
            .Replace("：", ":")
            .Replace("「", "'")
            .Replace("」", "'")
            .Replace("《", "'")
            .Replace("》", "'")
            .Replace("（", "(")
            .Replace("）", ")")
            .Replace("？", "?")
            .Replace("、", ",")
            .Replace("，", ",")
            .Replace("！", "!");

        //if (raw.Contains("<"))
        //    raw = HtmlTagValidator.TrimHtmlTagsInContent(raw);

        //For testing
        if (tokenReplacer != null)
            raw = tokenReplacer.Replace(raw);

        return raw;
    }

    public static string PrepareResult(string raw, string llmResult)
    {
        // Fix up anything we know the LLM has messed up but can autocorrect before validation

        // Easy way to fix ...
        if (raw.EndsWith("...") && !llmResult.EndsWith("...") && llmResult.EndsWith("."))
            llmResult = $"{llmResult}..";

        return llmResult;
    }

    public static string CleanupLineBeforeSaving(string input, string raw, TextFileToSplit textFile, StringTokenReplacer tokenReplacer)
    {
        //Finalise line before saving out
        var result = input.Trim();

        if (!string.IsNullOrEmpty(result))
        {
            if (result.Contains('\"') && !raw.Contains('\"'))
                result = result.Replace("\"", "");

            if (!StringTokenReplacer.EmojiItems.Any(phrase => result.IndexOf(phrase, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                if (result.Contains('[') && !raw.Contains('['))
                    result = result.Replace("[", "");

                if (result.Contains(']') && !raw.Contains(']'))
                    result = result.Replace("]", "");
            }

            if (result.Contains('`') && !raw.Contains('`'))
                result = result.Replace("`", "'");

            // Take out wide quotes
            if (result.Contains('“') && !raw.Contains('“'))
                result = result.Replace("“", "");

            if (result.Contains('”') && !raw.Contains('”'))
                result = result.Replace("”", "");

            // Take out wierd ** being added
            if (result.Contains("**") && !raw.Contains("**"))
                result = result.Replace("**", "");

            result = result
                .Replace("…", "...")
                .Replace("？", "?")
                .Replace(".:", ":")
                .Replace(". -", " -")
                .Replace("！", "!");

            //Take out wide quotes and line split items
            result = result
                .Replace("’", "'")
                .Replace("‘", "'")
                .Replace("—", "-")
                .Replace("-", "\u2011") //Change Hyphens to non breaking hyphens
                .Replace("{‑1}", "{-1}"); // Change special {-1} non breaking hyphen back to normal hyphen

            //Strip .'s
            //if (result.EndsWith('.') && !raw.EndsWith(".") && !result.EndsWith(".."))
            //    result = result[..^1];

            if (textFile.RemoveNumbers)
                result = RemoveNumbers(result);

            if (textFile.NameCleanupRoutines || textFile.NameCleanupRoutines2)
            {
                if (textFile.NameCleanupRoutines)
                    result = result.Replace(" ", "");
                else if (textFile.NameCleanupRoutines2)
                {
                    if (!Regex.IsMatch(input, ChineseCharPattern))
                    {
                        var splits = result.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                        switch (splits.Length)
                        {
                            case 1:
                                result = "";
                                break;
                            case 2:
                                break;
                            case 3:
                                result = $"{splits[0]} {splits[1]}{splits[2]}";
                                break;
                            case 4:
                                result = $"{splits[0]}{splits[1]} {splits[2]}{splits[3]}";
                                break;
                            case 5:
                                result = $"{splits[0]}{splits[1]} {splits[2]}{splits[3]}{splits[4]}";
                                break;
                            default:
                                break;
                        }
                    }
                }

                result = result.Replace(".", "");
                result = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(result);
            }

            if (textFile.RemoveExtraFullStop)
                result = RemoveFullStop(raw, result);

            if (textFile.RemoveExtraThe)
                result = RemoveExtraThe(raw, result);

            result = RemoveDiacritics(result);
            result = ReplaceIncorrectLowercaseWords(result);
            result = EncaseColorsForWholeLines(raw, result);
            result = EncaseSquareBracketsForWholeLines(raw, result);

            if (string.IsNullOrEmpty(result))
            {
                Console.WriteLine($"Something Bad happened somewhere: {raw}\n{result}");
                return result;
            }

            if (result.StartsWith('\'') && result.EndsWith('\''))
                if (result.Length > 3)
                    result = result[1..^1];

            if (Char.IsLower(result[0]) && raw != result)
                result = Char.ToUpper(result[0]) + result[1..];
        }

        result = tokenReplacer.Restore(result);

        return result;
    }

    public static ValidationResult CheckTransalationSuccessful(LlmConfig config, string raw, string result, TextFileToSplit textFile)
    {
        var response = true;
        var correctionPrompts = new StringBuilder();

        if (string.IsNullOrEmpty(raw))
            response = false;

        var invalidPhrases = new[]
        {
            "provide the text",
            "Certainly! Please provide the Chinese",
            "Certainly! Please provide the specific Chinese",
            "It seems like your input might be incomplete or missing some context",
            "Please provide the Chinese string you would like to be translated into English",
            "please provide the Chinese string",
            "please provide the specific Chinese strings",
            "Chinese text",
            "Chinese sentence",
            "translates to",
            //"also known as" //Causes issues
            "'''",
            "<p", "</p", "<em", "</em", "<|", "<strong", "</strong",
            "\\U",
        };

        if (invalidPhrases.Any(phrase => result.IndexOf(phrase, StringComparison.OrdinalIgnoreCase) >= 0))
            response = false;

        // 99% chance its gone crazy with hallucinations
        if (result.Length > 50 && raw.Length <= 4)
            response = false;

        if (result.Length > raw.Length * 15)
            response = false;

        // Small source with 'or' is usually an alternative
        if ((result.Contains(" or") || result.Contains("(or"))
            && raw.Length <= 4
            && !result.Contains("ore", StringComparison.OrdinalIgnoreCase)) //Handle edge case
        {
            response = false;
            correctionPrompts.AddPromptWithValues(config, "CorrectAlternativesPrompt", "or");
        }

        // Small source with 'and' is ususually an alternative
        //if (result.Contains(" and") && raw.Length < 3 && !result.Contains("Spear and Staff", StringComparison.OrdinalIgnoreCase))
        //{
        //    response = false;
        //    correctionPrompts.AddPromptWithValues(config, "CorrectAlternativesPrompt", "and");
        //}

        // Small source with ';' is ususually an alternative
        if (result.Contains(';') && !raw.Contains(';') && raw.Length < 4)
        {
            response = false;
            correctionPrompts.AddPromptWithValues(config, "CorrectAlternativesPrompt", ";");
        }

        if (result.Contains(',') && !raw.Contains(',') && !raw.Contains("，") && !raw.Contains("、") && raw.Length < 4)
        {
            response = false;
            correctionPrompts.AddPromptWithValues(config, "CorrectAlternativesPrompt", ",");
        }

        // Added literal
        if (result.Contains("(lit."))
        {
            response = false;
            correctionPrompts.AddPromptWithValues(config, "CorrectExplainationPrompt");
        }

        // Removed :
        if (raw.Contains(':') && !result.Contains(':'))
        {
            response = false;
            correctionPrompts.AddPromptWithValues(config, "CorrectColonSegementPrompt");
        }

        //Place holders - incase the model ditched them
        var matches = PlaceholderPatternRegex().Matches(raw);
        foreach (Match match in matches)
        {
            if (!result.Contains(match.Value))
            {
                response = false;
                correctionPrompts.AddPromptWithValues(config, "CorrectRemovalPrompt", match.Value);
            }
        }

        // Removed characters
        (string raw, string trans)[] checkForRemoval = { 
            ("·", "·"), 
            ("(", "("),
            ("（", "("), 
            (")", ")"),
            ("）", ")"), 
            //("...", "..."), //Problematic still
            //("…", "..."),
            ("：", ":"),
            ("：", ":"),
            (":", ":"),
        };

        foreach (var check in checkForRemoval)
        {
            if (raw.Contains(check.raw) && !result.Contains(check.trans))
            {
                response = false;
                correctionPrompts.AddPromptWithValues(config, "CorrectRemovalPrompt", check.raw);
            }
        }

        if (raw.Contains("\\n") && !result.Contains("\\n"))
        {
            response = false;
            correctionPrompts.AddPromptWithValues(config, "CorrectRemovalPrompt", "\\n");
        }

        if (raw.Contains('-') && !result.Contains('-') && !result.Contains("\u2011"))
        {
            response = false;
            correctionPrompts.AddPromptWithValues(config, "CorrectRemovalPrompt", "-");
        }

        // This can cause bad hallucinations if not being explicit on retries
        if (raw.Contains("<br>") && !result.Contains("<br>"))
        {
            response = false;
            correctionPrompts.AddPromptWithValues(config, "CorrectRemovalPrompt", "<br>");
            //correctionPrompts.AddPromptWithValues(config, "CorrectTagPrompt");
        }
        // Color tags are evil
        //else if (raw.Contains("<color") && !result.Contains("<color"))
        //{
        //    response = false;
        //    correctionPrompts.AddPromptWithValues(config, "CorrectRemovalPrompt", "<color>");
        //    correctionPrompts.AddPromptWithValues(config, "CorrectTagPrompt");
        //}                

        // Some raws dont have both because they are dynamic strings
        // Color invalidation - if it has a start tag but no end tag
        if (result.Contains("<color") && raw.Contains("</color>") && !result.Contains("</color>"))
        {
            response = false;
        }
        // Color invalidation - if it has a end tag but no start tag
        if (result.Contains("</color") && raw.Contains("<color") && !result.Contains("<color"))
        {
            response = false;
        }

        // Random additions
        if (result.Contains("<br>") && !raw.Contains("<br>"))
        {
            response = false;
            correctionPrompts.AddPromptWithValues(config, "CorrectAdditionalPrompt", "<br>");
        }

        if (result.Contains('\n') && !raw.Contains('\n'))
        {
            response = false;
            correctionPrompts.AddPromptWithValues(config, "CorrectAdditionalPrompt", "\\n");
        }

        if (Regex.IsMatch(result, ChineseCharPattern) && !Regex.IsMatch(result, ChinesePlaceholderPattern))
        {
            response = false;
            correctionPrompts.AddPromptWithValues(config, "CorrectChinesePrompt");
        }

        // Dialog specific
        // Added Brackets (Literation) where no brackets or widebrackets in raw
        if (result.Contains('(') && !raw.Contains('(') && !raw.Contains('（'))
        {
            response = false;
            correctionPrompts.AddPromptWithValues(config, "CorrectExplainationPrompt");
        }

        ////Alternatives
        if (result.Contains('/') && !raw.Contains('/'))
        {
            response = false;
            correctionPrompts.AddPromptWithValues(config, "CorrectAlternativesPrompt", "/");
        }

        if (result.Contains('\\') && !raw.Contains('\\'))
        {
            response = false;
            correctionPrompts.AddPromptWithValues(config, "CorrectAlternativesPrompt", "\\");
        }

        if (raw.Contains('<') && raw != "<商贩>")
            if (!HtmlTagHelpers.ValidateTags(raw, result, textFile.AllowMissingColorTags))
                response = false;

        if (textFile.NameCleanupRoutines)
        {
            if ((raw.Length == 1 && result.Length > 6)
                || (raw.Length == 2 && result.Length > 12)
                || (raw.Length == 3 && result.Length > 17))
                response = false;
        }

        return new ValidationResult
        {
            Valid = response,
            Result = result,
            CorrectionPrompt = correctionPrompts.ToString(),
        };
    }

    public static string CalulateCorrectionPrompt(LlmConfig config, ValidationResult validationResult, string raw, string result)
    {
        return string.Format(config.Prompts["BaseCorrectionPrompt"], raw, result, validationResult.CorrectionPrompt);
    }

    public static List<string> FindMarkup(string input)
    {
        var markupTags = new List<string>();

        if (input == null)
            return markupTags;

        // Regular expression to match markup tags in the format <tag>
        string pattern = "<[^>]+>";
        MatchCollection matches = Regex.Matches(input, pattern);

        // Add each match to the list of markup tags
        foreach (Match match in matches)
            markupTags.Add(match.Value);

        return markupTags;
    }

    public static string EncaseColorsForWholeLines(string raw, string translated)
    {
        var pattern = @"(<[^>]+>).*(</[^>]+>)";

        if (raw.StartsWith("<color") && raw.EndsWith("</color>")
            && raw.LastIndexOf("<color") == 0 && !translated.StartsWith("<color"))
        {
            var matches = Regex.Matches(raw, pattern);
            string start = matches[0].Groups[1].Value;
            string end = matches[0].Groups[2].Value;
            translated = $"{start}{translated}{end}";
        }

        return translated;
    }

    public static string EncaseSquareBracketsForWholeLines(string raw, string translated)
    {
        if (raw.StartsWith('【')
            && raw.EndsWith('】')
            && !translated.Contains('【')
            && !translated.Contains('】'))
        {
            translated = $"【{translated}】";
        }

        return translated;
    }

    public static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    public static string ReplaceIncorrectLowercaseWords(string input)
    {
        var words = new Dictionary<string, string>
        {
            { "jianghu", "Jianghu" },
            { "wulin", "Wulin"  }
        };

        foreach (var word in words)
        {
            string pattern = $"\\b{word.Key}\\b"; // \b ensures 'jianghu' is a whole word
            input = Regex.Replace(input, pattern, word.Value);
        }

        return input;
    }

    public static string RemoveNumbers(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Remove all digits from the string
        return Regex.Replace(input, @"\d", "");
    }

    public static string RemoveExtraThe(string raw, string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;
        if (raw.Contains(' '))
            return input;
        if (input.StartsWith("The ") && input.Count(c => c == '.') == 0)
        {
            var words = input.
                Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (words.Length <= 5)
                return input[4..];
        }
        return input;
    }

    public static string RemoveFullStop(string raw, string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        if (raw.Contains(' '))
            return input;

        var fullStop = '.';

        // Check if there's only one sentence (one full stop at the end)
        if (input.Count(c => c == fullStop) == 1
            && !input.Contains("!")
            && !input.Contains("?")
            && input.TrimEnd().EndsWith(fullStop))
        {
            // Count words
            var words = input.TrimEnd(fullStop).
                Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (words.Length <= 7)
            {
                return input.Replace(fullStop.ToString(), string.Empty); // Remove full stop leaving spaces
            }
        }

        return input;
    }

    [GeneratedRegex(PlaceholderMatchPattern)]
    private static partial Regex PlaceholderPatternRegex();
}
