using System.IO.Compression;
using Translate.Utility;
using static SweetPotato.FileView;

namespace Translate.Tests;

public class FileOutputWorkflowTests
{
    public const string WorkingDirectory = TranslationWorkflowTests.WorkingDirectory;
    public const string GameFolder = TranslationWorkflowTests.GameFolder;


    [Fact(DisplayName = "6. Package to Game Files")]
    public static async Task PackageFinalTranslation()
    {
        await FileOutputHandling.PackageFinalTranslationAsync(WorkingDirectory);

        var sourceDirectory = $"{WorkingDirectory}/Mod/{ModHelper.ContentFolder}";
        var modDirectory = $"{GameFolder}/下一站江湖Ⅱ_Data/StreamingAssets/Mod/{ModHelper.ContentFolder}";
        var resourceDirectory = $"{GameFolder}/BepInEx/resources";

        if (Directory.Exists(modDirectory))
            Directory.Delete(modDirectory, true);

        FileOutputHandling.CopyDirectory(sourceDirectory, modDirectory);

        File.Copy($"{TranslationWorkflowTests.WorkingDirectory}/Mod/db1.txt", $"{resourceDirectory}/db1.txt", true);
        foreach (var file in GameTextFiles.TextFilesToSplit.Where(t => t.TextFileType != TextFileType.RegularDb))
            File.Copy($"{WorkingDirectory}/Mod/Formatted/{file.Path}", $"{resourceDirectory}/{file.Path}", true);

        //await PackageRelease();
    }

    [Fact(DisplayName = "5. Copy Sprites")]
    public static async Task CopySprites()
    {
        await FileOutputHandling.PackageFinalTranslationAsync(WorkingDirectory);

        var sourceDirectory = $@"G:\xzyj2-sprites/completed";
        var spritesDirectory = $"{GameFolder}/BepInEx/sprites";

        if (Directory.Exists(spritesDirectory))
            Directory.Delete(spritesDirectory, true);

        FileOutputHandling.CopyDirectory(sourceDirectory, spritesDirectory);
    }

    [Fact(DisplayName = "7. Zip Release")]
    public static async Task ZipRelease()
    {
        var version = ModHelper.CalculateVersionNumber();

        string releaseFolder = $"{GameFolder}/ReleaseFolder/Files";

        File.Copy($"{WorkingDirectory}/Mod/db1.txt", $"{releaseFolder}/BepInEx/resources/db1.txt", true);
        File.Copy($"{WorkingDirectory}/Mod/Formatted/dumpedPrefabText.txt", $"{releaseFolder}/BepInEx/resources/dumpedPrefabText.txt", true);
        File.Copy($"{GameFolder}/BepInEx/Plugins/FanslationStudio.EnglishPatch.dll", $"{releaseFolder}/BepInEx/Plugins/FanslationStudio.EnglishPatch.dll", true);
        File.Copy($"{GameFolder}/BepInEx/Plugins/FanslationStudio.SharedAssembly.dll", $"{releaseFolder}/BepInEx/Plugins/FanslationStudio.SharedAssembly.dll", true);
        //File.Copy($"{gameFolder}/BepInEx/Translation/en/Text/resizer.txt", $"{releaseFolder}/BepInEx/Translation/en/Text/resizer.txt", true);

        foreach (var file in GameTextFiles.TextFilesToSplit.Where(t => t.TextFileType != TextFileType.RegularDb))
            File.Copy($"{WorkingDirectory}/Mod/Formatted/{file.Path}", $"{releaseFolder}/BepInEx/resources/{file.Path}", true);

        List<string> copyDirs = ["sprites", "resizers"];
        foreach (var copyDir in copyDirs)
        {
            var newDirectory = $"{releaseFolder}/BepInEx/{copyDir}";
            if (Directory.Exists(newDirectory))
                Directory.Delete(newDirectory, true);

            FileOutputHandling.CopyDirectory($"{GameFolder}/BepInEx/{copyDir}", newDirectory);
        }

        ZipFile.CreateFromDirectory($"{releaseFolder}", $"{releaseFolder}/../EnglishPatch-{version}.zip");

        await Task.CompletedTask;
    }
}
