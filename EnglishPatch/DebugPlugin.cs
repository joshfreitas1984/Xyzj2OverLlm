using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace EnglishPatch;

/// <summary>
/// Put dicey stuff in here that might crash the plugin - so it doesnt crash the existing plugins
/// </summary>
[BepInPlugin($"{MyPluginInfo.PLUGIN_GUID}.Debug", "DebugGame", MyPluginInfo.PLUGIN_VERSION)]
internal class DebugPlugin: BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    private void Awake()
    {
        Logger = base.Logger;
        Harmony.CreateAndPatchAll(typeof(DebugPlugin));
        Logger.LogWarning($"Debug Game Plugin should be patched!");
    }
    
    private void Update()
    {
    }
}

