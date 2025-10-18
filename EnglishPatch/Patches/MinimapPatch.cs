using HarmonyLib;
using System;
using System.Collections.Generic;

namespace EnglishPatch.Patches;

[HarmonyPatch(typeof(MainView))]
[HarmonyPatch("RefreshDateTimeText")]
public static class MinimapPatch
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var from = AccessTools.Method(typeof(SweetPotato.Tools), nameof(SweetPotato.Tools.GetGameTimeDate), [typeof(double)]);
        var to = AccessTools.Method(typeof(MinimapPatch), "MyGetGameTimeDate");
        return Transpilers.MethodReplacer(instructions, from, to);
    }

    public static string MyGetGameTimeDate(double mainQuestTimeSecond)
    {
        double totalSeconds = ToolsPatch.ConvertToSeconds(mainQuestTimeSecond);
        ToolsPatch.CalculateTimeIncrements(totalSeconds, out var year, out var month, out var day, out var hour);

        if (hour == 24)
            hour = 0;

        var dateTime = new DateTime(year, month, day);
        var shortMonth = dateTime.ToString("MMM");

        //MainView.RefreshDateTimeText uses 年 and put the first half into year display and second half below
        return $"JY{year}年{day}\n{shortMonth}\n{SweetPotato.Tools.SHI_CHENG_STR[hour / 2]}";
    }   
}