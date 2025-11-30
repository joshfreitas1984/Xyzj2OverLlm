using BepInEx;
using BepInEx.Logging;
using EnglishPatch.Support;
using HarmonyLib;
using SweetPotato;
using System.Collections.Generic;
using System.Reflection;
using LitJson;
using TMPro;
using UnityEngine;

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
        SweetPotato.LocatableController.Init

        // Toggle UI with F2 key
        //if (Input.GetKeyDown(KeyCode.F4))
        //{
        //    foreach (var l in TMP_Settings.linebreakingRules.leadingCharacters)
        //        Logger.LogError($"leading rules: {l.Key} {l.Value}");

        //    foreach (var l in TMP_Settings.linebreakingRules.followingCharacters)
        //        Logger.LogError($"following rules: {l.Key} {l.Value}");

        //    Logger.LogError($"leading: {TMP_Settings.leadingCharacters}");
        //    Logger.LogError($"following: {TMP_Settings.followingCharacters}");
        //    Logger.LogError($"useModernHangulLineBreakingRules: {TMP_Settings.useModernHangulLineBreakingRules}");
        //}
    }

    //// Opening Screen
    //[HarmonyPrefix, HarmonyPatch(typeof(SweetPotato.LoginViewNew), "OnButtonClick")]
    //public static void Postfix_OnButtonClick()
    //{
    //    Logger.LogWarning($"Hooked POSTFIX OnButtonClick!");
    //}

    ////[HarmonyPostfix, HarmonyPatch(typeof(SweetPotato.LoginViewNew), "OnButtonClick")]
    ////public static void Postfix_OnButtonClick(IEnumerable<CodeInstruction> __instructions)
    ////{
    ////    InstructionLogger.LogInstructions(__instructions);
    ////}

    //[HarmonyPostfix, HarmonyPatch(typeof(SweetPotato.LoginViewNew), "OnButtonNewGame")]
    //public static void Postfix_LoginViewNew_OnButtonNewGame()
    //{
    //    Logger.LogWarning("Hooked OnButtonNewGame!");
    //}
}

