using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SweetPotato;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

namespace EnglishPatch;

[BepInPlugin($"{MyPluginInfo.PLUGIN_GUID}.PropertyChangerPlugin", "PropertyChangerPlugin", MyPluginInfo.PLUGIN_VERSION)]
public class PropertyChangerPlugin : BaseUnityPlugin
{
    private bool showUI = false;
    private string playerName = "";

    private static bool renderMiddleNpc = true;
    private static double middleNpcDistance = 1000.0; //Default is 500.0
    private static double nearNpcDistance = 100.0;

    private bool localRenderMiddleNpc = renderMiddleNpc;
    private double localMiddleNpcDistance = middleNpcDistance;
    private double localNearNpcDistance = nearNpcDistance;

    private bool initialized = false;  // Flag to check if input was initialized

    internal static new ManualLogSource Logger;


    private void Awake()
    {
        Logger = base.Logger;

        Harmony.CreateAndPatchAll(typeof(PropertyChangerPlugin));

        // Plugin startup logic
        Logger.LogWarning($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    private void Update()
    {
        // Toggle UI with F2 key
        if (Input.GetKeyDown(KeyCode.KeypadPeriod))
        {
            showUI = !showUI;

            if (showUI && !initialized)
            {
                if (WorldManager.Instance != null && WorldManager.Instance.m_PlayerEntity != null)
                {
                    playerName = WorldManager.Instance.m_PlayerEntity.m_name;
                    SetName(); //Autofix name display issues

                    //SetNpcCulling(); //Default to desired culling settings
                    initialized = true; // Mark as initialized
                }
            }
        }
    }

    private void OnGUI()
    {
        if (!showUI) return;

        // Define starting x and y positions as variables for easier adjustment
        const int startX = 500;
        const int startY = 100;

        // Modal window positioned using the defined starting x and y positions
        GUI.Box(new Rect(startX, startY, 400, 300), "Property Changer");

        GUI.Label(new Rect(startX + 10, startY + 30, 80, 20), "Enter Name:");
        playerName = GUI.TextField(new Rect(startX + 90, startY + 30, 180, 20), playerName);

        if (GUI.Button(new Rect(startX + 280, startY + 30, 100, 20), "Change Name"))
            SetName();

        // NPC Rendering Options
        localRenderMiddleNpc = GUI.Toggle(new Rect(startX + 10, startY + 70, 200, 20), localRenderMiddleNpc, "Render Middle NPC");

        GUI.Label(new Rect(startX + 10, startY + 100, 150, 20), "Middle NPC Distance:");
        localMiddleNpcDistance = double.TryParse(GUI.TextField(new Rect(startX + 160, startY + 100, 100, 20), localMiddleNpcDistance.ToString()), out var middleDist) ? middleDist : localMiddleNpcDistance;

        GUI.Label(new Rect(startX + 10, startY + 130, 150, 20), "Near NPC Distance:");
        localNearNpcDistance = double.TryParse(GUI.TextField(new Rect(startX + 160, startY + 130, 100, 20), localNearNpcDistance.ToString()), out var nearDist) ? nearDist : localNearNpcDistance;

        // Button to apply NPC distance settings
        if (GUI.Button(new Rect(startX + 10, startY + 160, 150, 20), "Apply NPC Settings"))
        {
            nearNpcDistance = localNearNpcDistance;
            middleNpcDistance = localMiddleNpcDistance;
            renderMiddleNpc = localRenderMiddleNpc;
        }
    }

    private void SetName()
    {
        //Replace spaces with non breaking space
        playerName = Regex.Replace(playerName, @"\s", "\u00A0");
        WorldManager.Instance.m_PlayerEntity.m_name = playerName;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(LocatableController), "UpdateMeshShow")]
    private static bool UpdateMeshShow_Prefix(LocatableController __instance, ref bool __result)
    {
        bool shouldShow = __instance.m_CullMeshShow || __instance.m_ForceShow;

        if (renderMiddleNpc)
        {
            // New rule: only hide if far and not forced to show (Middle and near always show)
            if (shouldShow && !__instance.m_ForceShow && __instance.m_DisType == NPC_DIS_TYPE.NDT_FAR)
                shouldShow = false;
        }
        else
        {
            //Original rule: Hide if in combat and far, or not in combat and not near (Middle and far hide)
            if (shouldShow && !__instance.m_ForceShow 
                && (PlayerController.Instance.m_IsInCombat && __instance.m_DisType == NPC_DIS_TYPE.NDT_FAR 
                    || !PlayerController.Instance.m_IsInCombat && __instance.m_DisType != NPC_DIS_TYPE.NDT_NEAR))
                shouldShow = false;
        }

        if (shouldShow == __instance.m_MeshComponent.m_MeshObject.gameObject.activeSelf)
        {
            __result = false;
        }
        else
        {
            __instance.m_MeshComponent.m_MeshObject.gameObject.SetActive(shouldShow);
            __instance.m_UnitHead?.parent.gameObject.SetActive(shouldShow);
            __result = shouldShow;
        }

        return false; // We are replacing
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(LocatableController), "UpdateInternalCount")]
    private static bool UpdateInternalCount_Prefix(LocatableController __instance, ref bool __result)
    {
        // This is mostly the same with the new distance variables for messing with
        // Might need to adjust as the distance increases
        if (AppGame.Instance.optimizeNpcUpdateInternal)
        {
            if (__instance.m_UpdateInternalCount > 0)
                --__instance.m_UpdateInternalCount;
            else
                __instance.m_UpdateInternalCount = (double)__instance.m_DistanceSqr2Player >= nearNpcDistance ? (int)__instance.m_DistanceSqr2Player / 10 : 0;
        }
        else
            __instance.m_UpdateInternalCount = 0;

        if (AppGame.Instance.optimizeNpcFarHide && (double)UnityEngine.Time.time > (double)__instance.m_NextCheckTime)
        {
            int disType1 = (int)__instance.m_DisType;
            __instance.m_DisType = (double)__instance.m_DistanceSqr2Player <= middleNpcDistance ? 
                ((double)__instance.m_DistanceSqr2Player >= nearNpcDistance ? NPC_DIS_TYPE.NDT_MIDDLE : NPC_DIS_TYPE.NDT_NEAR) 
                    : NPC_DIS_TYPE.NDT_FAR;
            int disType2 = (int)__instance.m_DisType;

            if (disType1 != disType2 || __instance.m_ForceShow)
                __instance.UpdateMeshShow();

            __instance.m_NextCheckTime = UnityEngine.Time.time + 1f;
        }

        __result = true;

        return false; // We are replacing
    }
}
