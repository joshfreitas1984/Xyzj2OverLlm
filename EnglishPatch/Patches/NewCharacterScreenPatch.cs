using BepInEx.Logging;
using HarmonyLib;
using SweetPotato;
using System.Collections.Generic;
using System.Reflection;
using TMPro;

namespace EnglishPatch.Patches
{
    [HarmonyPatch(typeof(InstantiateViewNewNew_mobile), "RefreshAttriUI")]
    public class NewCharacterScreenPatch
    {
        [HarmonyPostfix]
        public static void RefreshAttriUI(InstantiateViewNewNew_mobile __instance)
        {
            PatchesPlugin.Logger.LogError("RefreshAttriUI");

            // Use reflection to access the private field m_shuxingitems
            var type = __instance.GetType();
            var field = type.GetField("playerAttriManager", BindingFlags.NonPublic | BindingFlags.Instance);
            var playerAttriManager = field?.GetValue(__instance) as AttriManager;
            PatchesPlugin.Logger.LogError($"Field: {field} playerAttriManager: {playerAttriManager}");

            var field2 = type.GetField("zhandoushuxingPre", BindingFlags.NonPublic | BindingFlags.Instance);
            var zhandoushuxingPre = field2?.GetValue(__instance) as Dictionary<int, int>;
            PatchesPlugin.Logger.LogError($"Field: {field2} zhandoushuxingPre: {zhandoushuxingPre}");

            string GetColorString(AttriType type)
            {
                int attriResult = playerAttriManager.GetAttriResult(type);
                int num;
                return zhandoushuxingPre.TryGetValue((int)type, out num) && attriResult != num ? "#e6b868" : "#dcdcdc";
            }

            var m_shuxingitems = __instance.m_shuxingitems;
            if (m_shuxingitems == null)
                return;

            for (int index = 0; index < m_shuxingitems.Count; index++)
            { 
                AttriType attrType = InstantiateViewNewNew_mobile.GetAttriType(index);
                string attriDesc = InstantiateViewNewNew_mobile.GetAttriDesc(attrType);

                string str = $"<color={GetColorString(attrType)}>{Tools.m_AttrName[(int)attrType]}</color> <size=20>{attriDesc}</size>";
                m_shuxingitems[index].FindChildCustom<TextMeshProUGUI>("name").text = str;
            }
        }
    }
}
