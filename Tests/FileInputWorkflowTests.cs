using Translate.Utility;

namespace Translate.Tests;

public class FileInputWorkflowTests
{
    public const string WorkingDirectory = TranslationWorkflowTests.WorkingDirectory;
    public const string GameFolder = TranslationWorkflowTests.GameFolder;

    [Fact(DisplayName = "1. SplitDbAssets")]
    public void SplitDbAssets()
    {
        InputFileHandling.SplitDbAssets(WorkingDirectory);
    }

    [Fact(DisplayName = "2. ExportAssetsIntoTranslated")]
    public void ExportAssetsIntoTranslated()
    {
        InputFileHandling.ExportTextAssetsToCustomFormat(WorkingDirectory);
    }

    [Fact(DisplayName = "2. ExportDumpedIntoTranslated")]
    public void ExportDumpedIntoTranslated()
    {
        InputFileHandling.ExportDumpedPrefabToCustomFormat(WorkingDirectory);
    }

    [Fact(DisplayName = "2. ExportDumpedDyanmicIntoTranslated")]
    public void ExportDumpedDyanmicIntoTranslated()
    {
        InputFileHandling.ExportDynamicStringsToCustomFormat(WorkingDirectory);
    }

    [Fact(DisplayName = "3. MergeFilesIntoTranslated")]
    public async Task MergeFilesIntoTranslated()
    {
        await InputFileHandling.MergeFilesIntoTranslatedAsync(WorkingDirectory);
    }

    [Fact(DisplayName = "99. Check File Lines Match")]
    public void CheckFileLinesMatch()
    {
        var config = Configuration.GetConfiguration(WorkingDirectory);
        var badFiles = new List<string>();

        foreach (var textFile in GameTextFiles.TextFilesToSplit)
        {
            var file = $"{TranslationWorkflowTests.WorkingDirectory}/Raw/Export/{textFile.Path}";
            var convertedFile = $"{TranslationWorkflowTests.WorkingDirectory}/Converted/{textFile.Path}";

            var deserializer = Yaml.CreateDeserializer();

            var lines = deserializer.Deserialize<List<TranslationLine>>(File.ReadAllText(file));
            var convertedLines = deserializer.Deserialize<List<TranslationLine>>(File.ReadAllText(convertedFile)); ;

            if (lines.Count != convertedLines.Count)
                badFiles.Add($"Bad File: {Path.GetFileName(file)} Export: {lines.Count} Converted: {convertedLines.Count} ");

            Assert.Empty(badFiles);
        }
    }
}
