using Translate.Utility;

namespace Translate;

public class FileIteration
{
    public static async Task IterateTranslatedFilesAsync(string workingDirectory, Func<string, TextFileToSplit, List<TranslationLine>, Task> performActionAsync)
    {
        var deserializer = Yaml.CreateDeserializer();
        string outputPath = $"{workingDirectory}/Converted";

        foreach (var textFileToTranslate in GameTextFiles.TextFilesToSplit)
        {
            var outputFile = $"{outputPath}/{textFileToTranslate.Path}";

            if (!File.Exists(outputFile))
                continue;

            var content = await File.ReadAllTextAsync(outputFile);

            var fileLines = deserializer.Deserialize<List<TranslationLine>>(content);

            if (performActionAsync != null)
                await performActionAsync(outputFile, textFileToTranslate, fileLines);
        }
    }

    public static async Task IterateTranslatedFilesInParallelAsync(string workingDirectory, Func<string, TextFileToSplit, List<TranslationLine>, Task> performActionAsync)
    {
        var deserializer = Yaml.CreateDeserializer();
        string outputPath = $"{workingDirectory}/Converted";

        var tasks = GameTextFiles.TextFilesToSplit
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
}
