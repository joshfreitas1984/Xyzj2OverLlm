using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Translate;

public enum TextFileType
{
    RegularDb,
    PrefabText,
    DynamicStrings,
    LocalTextString
}

public class TextFileToSplit
{
    [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
    public string Path { get; set; } = string.Empty;

    public bool PackageOutput { get; set; } = true;

    public TextFileType TextFileType { get; set; } = TextFileType.RegularDb;

    public bool IsMainDialogueAsset { get; set; } = false;

    public bool EnableGlossary { get; set; } = true;

    public string AdditionalPromptName { get; set; } = string.Empty;

    public bool EnableBasePrompts { get; set; } = true;

    public bool RemoveNumbers { get; set; } = false;

    public bool RemoveExtraFullStop { get; set; } = true;

    public bool RemoveExtraThe { get; set; } = true;

    public bool IgnoreGameObjects { get; set; } = false;

    public bool NameCleanupRoutines { get; set; } = false;

    public bool NameCleanupRoutines2 { get; set; } = false;

    public bool AllowMissingColorTags { get; set; } = true;
}
