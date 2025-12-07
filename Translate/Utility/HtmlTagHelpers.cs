using System.Text.RegularExpressions;

namespace Translate.Utility;

public static class HtmlTagHelpers
{
    public static bool ValidateTags(string raw, string translated, bool allowMissingColors)
    {
        HashSet<string> rawTags = ExtractTagsWithAttributes(raw, true);
        HashSet<string> translatedTags = ExtractTagsWithAttributes(translated, false);     
    
        var response = rawTags.SetEquals(translatedTags);

        if (!response 
            && allowMissingColors
            && rawTags.Count != translatedTags.Count) //So if its got them get it right
        {
            rawTags.RemoveWhere(tag => tag.Contains("color"));
            translatedTags.RemoveWhere(tag => tag.Contains("color"));
            response = rawTags.SetEquals(translatedTags);
        }

        // Test for Trimmed tags (when we're using the raw in the validation test)
        if (!response)
        {
            var trimmedTags = new HashSet<string>();
            foreach (var tag in rawTags)
                trimmedTags.Add(tag.Trim());

            response = trimmedTags.SetEquals(translatedTags);
        }

        return response;
    }

    private static HashSet<string> ExtractTagsWithAttributes(string input, bool updateSizes)
    {
        var tags = new HashSet<string>();
        var regex = new Regex(@"<(/?\w+\s*[^>]*)>");
        foreach (Match match in regex.Matches(input))
        {
            var tag = match.Groups[1].Value;

            // Update size tags if needed
            if (updateSizes && tag.StartsWith("size"))
            {
                var newSize = StringTokenReplacer.CalculateNewSize($"<{tag}>");
                tag = tag.Contains("#") ? $"size=#{newSize}" : $"size={newSize}";
            }

            tags.Add(tag);
        }
        return tags;
    }

    public static List<string> ExtractTagsListWithAttributes(string input, string ignore)
    {
        var tags = new List<string>();
        var regex = new Regex(@"<(\w+\s*[^/>]*)>");
        foreach (Match match in regex.Matches(input))
        {
            if (!match.Groups[1].Value.StartsWith(ignore))
                tags.Add($"<{match.Groups[1].Value}>");
        }
        return tags;
    }

    public static string TrimHtmlTagsInContent(string input)
    {
        // Regular expression to match HTML tags and remove extra spaces, including self-closing tags
        var tagPattern = new Regex(@"<\s*(\w+)(.*?)\s*/?>");

        // Replace each tag by trimming unnecessary spaces inside the tag
        return tagPattern.Replace(input, match =>
        {
            var tagName = match.Groups[1].Value;
            var attributes = match.Groups[2].Value.Trim();

            // Determine if the tag is self-closing
            bool isSelfClosing = match.Value.EndsWith("/>");

            // Rebuild the tag with no extra spaces and ensure self-closing tag has the slash without spaces before it
            return isSelfClosing
                ? $"<{tagName}{(string.IsNullOrEmpty(attributes) ? "" : " " + attributes)}/>"
                : $"<{tagName}{(string.IsNullOrEmpty(attributes) ? "" : " " + attributes)}>";
        });
    }
}

