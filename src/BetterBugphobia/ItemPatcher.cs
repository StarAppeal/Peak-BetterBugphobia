using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BetterBugphobia;

[HarmonyPatch(typeof(Item.ItemUIData))]
public class ItemPatcher
{
    private static readonly Dictionary<string, Type> ItemNameToMobTypeMap = new()
    {
        { "Tick", typeof(Bugfix) },
        { "Scorpion", typeof(Scorpion) }
    };

    [HarmonyPatch("GetIcon")]
    [HarmonyPostfix]
    static void Postfix(Item.ItemUIData __instance, ref Texture2D __result)
    {
        if (ItemNameToMobTypeMap.TryGetValue(__instance.itemName, out var mobType))
        {
            if (Plugin.bugPhobiaMap.TryGetValue(mobType, out var configEntry))
            {
                __result = configEntry.Value ? __instance.altIcon : __instance.icon;
            }
        }
    }
}