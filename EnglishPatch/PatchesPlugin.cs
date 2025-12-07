using BepInEx;
using BepInEx.Logging;
using EnglishPatch.Patches;
using HarmonyLib;
using SweetPotato;

namespace EnglishPatch;

/// <summary>
/// Extra patches that are risky that might change as the game changes
/// </summary>
[BepInPlugin($"{MyPluginInfo.PLUGIN_GUID}.PatchesPlugin", "PatchesPlugin", MyPluginInfo.PLUGIN_VERSION)]
public class PatchesPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    private void Awake()
    {
        Logger = base.Logger;

        // Plugin startup logic
        Logger.LogWarning($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        Harmony.CreateAndPatchAll(typeof(NameRestrictionPatch));
        Harmony.CreateAndPatchAll(typeof(MinimapPatch));
        Harmony.CreateAndPatchAll(typeof(ItemsPatch));
        Harmony.CreateAndPatchAll(typeof(ToolsPatch));
        Harmony.CreateAndPatchAll(typeof(RandomNamePatch));
        Harmony.CreateAndPatchAll(typeof(QuestIconPatch));
        Harmony.CreateAndPatchAll(typeof(NanDuViewPatch));
        Harmony.CreateAndPatchAll(typeof(NewCharacterScreenPatch));
        Logger.LogWarning($"Plugin {MyPluginInfo.PLUGIN_GUID} should be patched!");
    }
}