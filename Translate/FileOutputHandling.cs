using SharedAssembly.DynamicStrings;
using System.Text.RegularExpressions;
using Translate;
using Translate.Utility;

public class FileOutputHandling
{
    public static async Task PackageFinalTranslationAsync(string workingDirectory)
    {
        string inputPath = $"{workingDirectory}/Converted";
        string outputPath = $"{workingDirectory}/Mod/{ModHelper.ContentFolder}";
        string outputDbPath = $"{workingDirectory}/Mod/";

        if (Directory.Exists(outputPath))
            Directory.Delete(outputPath, true);

        Directory.CreateDirectory(outputPath);

        var finalDb = new List<string>();
        var passedCount = 0;
        var failedCount = 0;

        await FileIteration.IterateTranslatedFilesAsync(workingDirectory, async (outputFile, textFileToTranslate, fileLines) =>
        {
            var failedLines = new List<string>();
            var outputLines = new List<string>();

            if (textFileToTranslate.TextFileType == TextFileType.PrefabText)
            {
                foreach (var line in fileLines)
                {
                    foreach (var split in line.Splits)
                        if (!split.FlaggedForRetranslation && !(string.IsNullOrEmpty(split.Translated)))
                            outputLines.Add($"- raw: {split.Text}\n  result: {split.Translated}");
                        else if (!split.SafeToTranslate)
                            continue; // Do not count failure
                        else
                            failedCount++;
                }
            }
            else if (textFileToTranslate.TextFileType == TextFileType.DynamicStrings)
            {
                var serializer = Yaml.CreateSerializer();
                var contracts = new List<DynamicStringContract>();

                foreach (var line in fileLines)
                {
                    if (line.Splits.Count != 1)
                    {
                        failedCount++;
                        continue;
                    }

                    // Do not package but dont count as failure
                    if (!line.Splits[0].SafeToTranslate)
                        continue;

                    var lineRaw = line.Raw;
                    var splits = lineRaw.Split(",");

                    var lineTrans = line.Splits[0].Translated
                        .Replace("，", ","); // Replace Wide quotes back

                    if (splits.Length != 5
                        || string.IsNullOrEmpty(lineTrans)
                        || line.Splits[0].FlaggedForRetranslation)
                    {
                        failedCount++;
                        continue;
                    }

                    string[] parameters = DynamicStringSupport.PrepareMethodParameters(splits[4]);

                    var contract = new DynamicStringContract()
                    {
                        Type = splits[0],
                        Method = splits[1],
                        ILOffset = long.Parse(splits[2]),
                        Raw = splits[3],
                        Translation = lineTrans,
                        Parameters = parameters
                    };

                    if (DynamicStringSupport.IsSafeContract(contract, false))
                        contracts.Add(contract);
                }

                File.WriteAllText($"{outputDbPath}/Formatted/{textFileToTranslate.Path}", serializer.Serialize(contracts));
                passedCount += contracts.Count;

                await Task.CompletedTask;
                return;
            }
            else // TextFileType.RegularDb
            {

                foreach (var line in fileLines)
                {
                    // Regular DB handling
                    var splits = line.Raw.Split('#');
                    var failed = false;

                    foreach (var split in line.Splits)
                    {
                        if (!textFileToTranslate.PackageOutput
                            || split.FlaggedForRetranslation
                            || !split.SafeToTranslate) //Count Failure
                        {
                            failed = true;
                            break;
                        }

                        //Check line to be extra safe
                        if (split.Translated.Contains('#') || Regex.IsMatch(split.Translated, @"(?<!\\)\n"))
                            failed = true;
                        else if (!string.IsNullOrEmpty(split.Translated))
                            splits[split.Split] = split.Translated;
                        //If it was already blank its all good
                        else if (!string.IsNullOrEmpty(split.Text))
                            failed = true;
                    }

                    line.Translated = string.Join('#', splits);

                    if (!failed)
                        outputLines.Add(line.Translated);
                    else
                    {
                        outputLines.Add(line.Raw);
                        failedLines.Add(line.Raw);
                    }
                }
            }

            // Do not want to package prefabs into main db
            if (textFileToTranslate.TextFileType == TextFileType.RegularDb 
                || textFileToTranslate.TextFileType == TextFileType.LocalTextString)
            {
                finalDb.Add($"{Path.GetFileNameWithoutExtension(outputFile)}|{fileLines.Count}");
                finalDb.AddRange(outputLines);
            }

            File.WriteAllLines($"{outputDbPath}/Formatted/{textFileToTranslate.Path}", outputLines);

            passedCount += outputLines.Count;
            failedCount += failedLines.Count;

            await Task.CompletedTask;
        });

        var dir = new DirectoryInfo($"{workingDirectory}/Raw/Remaining");
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            var fileLines = File.ReadAllLines(file.FullName);

            finalDb.Add($"{Path.GetFileNameWithoutExtension(file.Name)}|{fileLines.Length}");
            finalDb.AddRange(fileLines);
        }

        Console.WriteLine($"Passed: {passedCount}");
        Console.WriteLine($"Failed: {failedCount}");

        ModHelper.GenerateModConfig(workingDirectory);
        File.WriteAllLines($"{outputDbPath}/db1.txt", finalDb);
    }

    public static void CopyDirectory(string sourceDir, string destDir)
    {
        // Get the subdirectories for the specified directory.
        var dir = new DirectoryInfo(sourceDir);

        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory does not exist or could not be found: {sourceDir}");

        // If the destination directory doesn't exist, create it.
        if (!Directory.Exists(destDir))
            Directory.CreateDirectory(destDir);

        // Get the files in the directory and copy them to the new location.
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            var tempPath = Path.Combine(destDir, file.Name);
            file.CopyTo(tempPath, false);
        }

        // Copy each subdirectory using recursion
        DirectoryInfo[] dirs = dir.GetDirectories();
        foreach (DirectoryInfo subdir in dirs)
        {
            if (subdir.Name == ".git" || subdir.Name == ".vs")
                continue;

            var tempPath = Path.Combine(destDir, subdir.Name);
            CopyDirectory(subdir.FullName, tempPath);
        }
    }
}