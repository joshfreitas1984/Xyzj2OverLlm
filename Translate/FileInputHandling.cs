using System.Text;
using System.Text.RegularExpressions;
using Translate.Utility;

namespace Translate;

public class InputFileHandling
{
    public static void ExportTextAssetsToCustomFormat(string workingDirectory)
    {
        string outputPath = $"{workingDirectory}/Raw/Export";
        string convertedPath = $"{workingDirectory}/Converted";

        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        var serializer = Yaml.CreateSerializer();
        var pattern = LineValidation.ChineseCharPattern;

        var dir = new DirectoryInfo($"{workingDirectory}/Raw/SplitDb");
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            var foundLines = new List<TranslationLine>();
            var lines = File.ReadAllLines(file.FullName);
            var lineIncrement = 0;

            foreach (var line in lines)
            {
                lineIncrement++;
                var splits = line.Split("#");
                var foundSplits = new List<TranslationSplit>();

                // Default to line number when it doesnt have line number in split
                if (!long.TryParse(splits[0], out long lineNum))
                    lineNum = lineIncrement;

                // Find Chinese
                for (int i = 0; i < splits.Length; i++)
                {
                    if (Regex.IsMatch(splits[i], pattern))
                    {
                        foundSplits.Add(new TranslationSplit()
                        {
                            Split = i,
                            Text = splits[i],
                        });
                    }
                }

                //The translation line
                foundLines.Add(new TranslationLine()
                {
                    //LineNum = lineNum,
                    Raw = line,
                    Splits = foundSplits,
                });
            }

            // Write the found lines
            var yaml = serializer.Serialize(foundLines);
            File.WriteAllText($"{outputPath}/{file.Name}", yaml);

            // Add missing converted file if it doesnt exist yet
            if (!File.Exists($"{convertedPath}/{file.Name}"))
                File.WriteAllText($"{convertedPath}/{file.Name}", yaml);
        }
    }
    
    public static void SplitDbAssets(string workingDirectory)
    {
        string inputPath = $"{workingDirectory}/Raw/DB";
        string outputPath = $"{workingDirectory}/Raw/SplitDb";

        if (Directory.Exists(outputPath))
            Directory.Delete(outputPath, true);

        Directory.CreateDirectory(outputPath);

        //var lines = File.ReadAllLines($"{inputPath}/db1.txt");
        using FileStream fileStream = new FileStream($"{inputPath}/db1.txt", FileMode.Open, FileAccess.Read);
        using BinaryReader binaryReader = new BinaryReader(fileStream);
        var bytes =  binaryReader.ReadBytes((int)fileStream.Length);
        var lines = Encoding.UTF8.GetString(bytes).Split('\n');

        var splitDbName = string.Empty;
        var splitDbCount = 0;
        var hasChinese = false;
        var pattern = LineValidation.ChineseCharPattern;
        var currentSplitLines = new List<string>();

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // New File Split
            if (line.Contains('|') && !line.Contains('#'))
            {
                var splits = line.Split('|');
                if (splits.Length == 2)
                {
                    // Primary Write
                    WriteSplitDbFile(outputPath, splitDbName, splitDbCount, hasChinese, currentSplitLines);
                    splitDbName = splits[0];
                    splitDbCount = int.Parse(splits[1]);
                    hasChinese = false;
                    currentSplitLines = [];
                    Console.WriteLine($"Starting New Split: {splitDbName}...");
                    continue;
                }
            }

            // We only care about DB entries with CN text in it
            if (!hasChinese)
                if (Regex.IsMatch(line, pattern))
                    hasChinese = true;

            // We have a bad breakpoint (usually local_text_string)
            if (line.Contains("\r"))
                currentSplitLines.Add(line.Replace("\r", string.Empty));
            else
                currentSplitLines.Add(line);
        }

        //Trailing Write
        WriteSplitDbFile(outputPath, splitDbName, splitDbCount, hasChinese, currentSplitLines);
    }
    
    public static void ExportDumpedPrefabToCustomFormat(string workingDirectory)
    {
        string inputPath = $"{workingDirectory}/Raw/ExportedText";
        string outputPath = $"{workingDirectory}/Raw/Export";

        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        var serializer = Yaml.CreateSerializer();
        var pattern = LineValidation.ChineseCharPattern;

        var dir = new DirectoryInfo(inputPath);
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            var foundLines = new List<TranslationLine>();
            var lines = File.ReadAllLines(file.FullName);
            var lineIncrement = 0;

            foreach (var line in lines)
            {
                lineIncrement++;
                var splits = new string[] { line };
                var foundSplits = new List<TranslationSplit>();

                // Default to line number when it doesnt have line number in split
                if (!long.TryParse(splits[0], out long lineNum))
                    lineNum = lineIncrement;

                // Find Chinese
                for (int i = 0; i < splits.Length; i++)
                {
                    if (Regex.IsMatch(splits[i], pattern))
                    {
                        foundSplits.Add(new TranslationSplit()
                        {
                            Split = i,
                            Text = splits[i],
                        });
                    }
                }

                //The translation line
                foundLines.Add(new TranslationLine()
                {
                    //LineNum = lineNum,
                    Raw = line,
                    Splits = foundSplits,
                });
            }

            // Write the found lines
            var yaml = serializer.Serialize(foundLines);
            File.WriteAllText($"{outputPath}/{file.Name}", yaml);
        }
    }
    
    public static void ExportDynamicStringsToCustomFormat(string workingDirectory)
    {
        string inputPath = $"{workingDirectory}/Raw/DynamicStrings";
        string outputPath = $"{workingDirectory}/Raw/Export";

        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        var serializer = Yaml.CreateSerializer();
        var pattern = LineValidation.ChineseCharPattern;

        var dir = new DirectoryInfo(inputPath);
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            var foundLines = new List<TranslationLine>();
            var lines = File.ReadAllLines(file.FullName);
            var lineIncrement = 0;

            foreach (var line in lines)
            {
                lineIncrement++;
                var splits = line.Split(",");
                var foundSplits = new List<TranslationSplit>();

                // Default to line number when it doesnt have line number in split
                if (!long.TryParse(splits[0], out long lineNum))
                    lineNum = lineIncrement;

                // Find Chinese
                for (int i = 0; i < splits.Length; i++)
                {
                    if (Regex.IsMatch(splits[i], pattern))
                    {
                        var cleaned = splits[i];
                        if (cleaned.StartsWith('\"'))
                            cleaned = cleaned[1..];
                        if (cleaned.EndsWith('\"'))
                            cleaned = cleaned[..^1];

                        foundSplits.Add(new TranslationSplit()
                        {
                            Split = i,
                            Text = cleaned,
                        });
                    }
                }

                //The translation line
                foundLines.Add(new TranslationLine()
                {
                    //LineNum = lineNum,
                    Raw = line,
                    Splits = foundSplits,
                });
            }

            // Write the found lines
            var yaml = serializer.Serialize(foundLines);
            File.WriteAllText($"{outputPath}/{file.Name}", yaml);
        }
    }
    
    public static async Task MergeFilesIntoTranslatedAsync(string workingDirectory)
    {
        await FileIteration.IterateTranslatedFilesAsync(workingDirectory, async (outputFile, textFileToTranslate, fileLines) =>
        {
            var newCount = 0;

            ////Disable for now since they should be same
            //if (textFileToTranslate.TextFileType == TextFileType.RegularDb)
            //    return;

            var deserializer = Yaml.CreateDeserializer();
            var exportFile = outputFile.Replace("Converted", "Raw/Export");
            var exportLines = deserializer.Deserialize<List<TranslationLine>>(File.ReadAllText(exportFile));

            foreach (var line in exportLines)
            {
                var found = fileLines.FirstOrDefault(x => x.Raw == line.Raw);
                if (found != null)
                {
                    foreach (var split in line.Splits)
                    {
                        var found2 = found.Splits.FirstOrDefault(x => x.Text == split.Text);
                        if (found2 != null)
                            split.Translated = found2.Translated;
                    }
                }
                else
                {
                    // Try matching on split instead of line incase they changed line format
                    foreach (var split in line.Splits)
                    {
                        var found2 = fileLines
                            .Select(x => x.Splits.FirstOrDefault(s => s.Text == split.Text))
                            .FirstOrDefault(s => s != null);

                        if (found2 != null)
                            split.Translated = found2.Translated;
                        else
                            newCount++;
                    }
                }
            }

            Console.WriteLine($"New Lines {textFileToTranslate.Path}: {newCount}");

            //if (newCount > 0 || exportLines.Count != fileLines.Count) //Always Write because they might have changed format
            {
                var serializer = Yaml.CreateSerializer();
                File.WriteAllText(outputFile, serializer.Serialize(exportLines));
            }

            await Task.CompletedTask;
        });
    }

    public static void WriteSplitDbFile(string outputDirectory, string fileName, int shouldHave, bool hasChinese, List<string> lines)
    {
        if (string.IsNullOrEmpty(fileName))
            return;

        Console.WriteLine($"Writing Split {fileName}.. Should have..{shouldHave} Have..{lines.Count}");

        // Files we split but not actually changing
        if (GameTextFiles.FilesNotHandled.Contains($"{fileName}.txt"))
            hasChinese = false;

        if (hasChinese)
            File.WriteAllLines($"{outputDirectory}/{fileName}.txt", lines);
        else
            File.WriteAllLines($"{outputDirectory}/../Remaining/{fileName}.txt", lines);
    }

}
