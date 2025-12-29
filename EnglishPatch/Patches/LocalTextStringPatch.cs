using HarmonyLib;
using SweetPotato;
using System.Linq;

namespace EnglishPatch.Patches;

[HarmonyPatch(typeof(LocalTextString))]
public class LocalTextStringPatch
{
    //[HarmonyPatch(nameof(LocalTextString.CreateFromCsvRow))]
    //[HarmonyPrefix]
    //public static bool CreateFromCsvRowPrefix(string[] array)
    //{
    //    if (array != null && array.Length > 0)
    //        PatchesPlugin.Logger.LogWarning($"LocalTextString.CreateFromCsvRow called with {array.Length} rows. id: {array[0]}");
    //    else
    //        PatchesPlugin.Logger.LogWarning("LocalTextString.CreateFromCsvRow called with null or empty row");

    //    return true; // Continue to original method
    //}

    //[HarmonyPatch(nameof(LocalTextString.TryGetInfoByKey))]
    //[HarmonyPrefix]
    //public static bool TryGetInfoByKey(int id)
    //{
    //    if (LocalTextString.mTemplateList.TryGetValue(id, out var value))
    //    {
    //        //PatchesPlugin.Logger.LogWarning($"Found: {id} Content: {value}");

    //        var content = value.content;
    //        if (!string.IsNullOrEmpty(content))
    //        {
    //            content = content.Replace("<color=", "<color=#").Replace("<size=", "<size=#");
    //            //PatchesPlugin.Logger.LogWarning($"Found Color: {id} Content: {content}");
    //        }

    //    }
    //    else
    //        PatchesPlugin.Logger.LogError($"Not Found: {id} Templates: {LocalTextString.mTemplateList.Count}");

    //    return true;
    //}

    [HarmonyPatch(nameof(LocalTextString.ChangeLanguageFuc))]
    [HarmonyPrefix]
    public static bool ChangeLanguageFuc(string[] array)
    {
        if (array.Length != 0)
        {
            int num = 0;
            // Fix array[num++] bug - hopefully they fix soon
            if (int.TryParse(array[num], out var result))
                PatchesPlugin.Logger.LogError($"Not an Int: {array.Length} array0: {array[0]} array1: {array[1]} LocalTextString.mTemplateList: {LocalTextString.mTemplateList[0]}");

            if (LocalTextString.mTemplateList.TryGetValue(result, out var value))
                value.content = array[num++];
            else
                PatchesPlugin.Logger.LogError($"WTF is this: {array.Length} array0: {array[0]}");
        }

        return false;
    }
}