using HarmonyLib;
using SweetPotato;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EnglishPatch.Patches
{
    [HarmonyPatch(typeof(NanDuView), "RefreshAttriItems")]
    public class NanDuViewPatch
    {
        [HarmonyPrefix]
        public static bool RefreshAttriItemsPrefix(NanDuView __instance)
        {
            var m_shuxingitems = __instance.m_shuxingitems;

            if (m_shuxingitems == null)
                return false;

            for (int index = 0; index < m_shuxingitems.Count; index++)
            {
                AttriType attriType = InstantiateViewNewNew_mobile.GetAttriType(index);
                string attriDesc = InstantiateViewNewNew_mobile.GetAttriDesc(attriType);

                m_shuxingitems[index].text = Tools.m_AttrName[(int)attriType] + "<color=#616161><size=20>(" + attriDesc + ")";
            }

            return false; // Skip original method
        }
    }
}
