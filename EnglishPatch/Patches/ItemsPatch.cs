using HarmonyLib;
using SweetPotato;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine.UI;

namespace EnglishPatch.Patches;

public static class ItemsPatch
{
    // This is most items description - warning if the underlying code changes this breaks badly
    // TODO: It majorly changed and broke like predicted. Need to re-implement later.
    //public static string Replacement_GetSubType456Des(string itemModifiersRaw, string itemRestrictionsRaw, string itemBonusesRaw)
    //{
    //    StringBuilder builder = new StringBuilder();

    //    var itemModifiers = itemModifiersRaw.Split('|', StringSplitOptions.None);
    //    List<int> recoveryItemIds = new List<int>() { 2, 3, 33, 83 };

    //    for (int index = 0; index < itemModifiers.Length; ++index)
    //    {
    //        var itemProperties = itemModifiers[index].Split('&', StringSplitOptions.None);

    //        var itemIdString = itemProperties[0];
    //        int.TryParse(itemIdString, out var itemId);

    //        var prefix = index == 0 ? "After use " : "， ";
    //        var modifiedDesc = !recoveryItemIds.Contains(itemId) ? "Increase" : "Recover";
    //        var statModified = Tools.bagTypeName.ContainsKey(itemIdString) ? Tools.bagTypeName[itemIdString] : Tools.m_AttrName[itemId];
    //        var amount = itemProperties[1];
    //        var amountSuffix = itemProperties[2] == "1" ? "%" : "";

    //        builder.Append($"{prefix} {modifiedDesc} {statModified} by {amount}{amountSuffix}");
    //    }

    //    if (Tools.IsStrAvailable(itemRestrictionsRaw))
    //    {
    //        var itemRestrictions = itemRestrictionsRaw.Split("&", StringSplitOptions.None)[1].Split("|", StringSplitOptions.None);

    //        if (itemRestrictions[2] == "<")
    //            builder.AppendFormat("，this item can only be used {0} times.", itemRestrictions[3]); //This item can only be used {0} times
    //    }

    //    if (Tools.IsStrAvailable(itemBonusesRaw))
    //    {
    //        string[] itemBonuses = itemBonusesRaw.Split("|", StringSplitOptions.None);
    //        if (itemBonuses[0] == "0")
    //        {
    //            var itemProperties = itemBonuses[1].Split("&", StringSplitOptions.None);

    //            var itemIdString = itemProperties[0];
    //            int.TryParse(itemIdString, out var itemId);

    //            var prefix = "，On first use get an extra: ";
    //            var modifiedDesc = !recoveryItemIds.Contains(itemId) ? "Increase" : "Recover";
    //            var statModified = Tools.bagTypeName.ContainsKey(itemIdString) ? Tools.bagTypeName[itemIdString] : Tools.m_AttrName[itemId];
    //            var amount = itemProperties[1];
    //            string amountSuffix = itemProperties[2] == "0" ? "" : "%";

    //            builder.Append($"{prefix} {modifiedDesc} {statModified} by {amount}{amountSuffix}");
    //        }
    //    }

    //    return builder.ToString();
    //}

    //[HarmonyPrefix, HarmonyPatch(typeof(Item), nameof(Item.GetSubType456Des))]
    //public static bool Prefix_GetSubType456Des(Item __instance, ref string __result, string misc2, string misc1, string misc3)
    //{
    //    __result = Replacement_GetSubType456Des(misc2, misc1, misc3);
    //    return false;
    //}
}
