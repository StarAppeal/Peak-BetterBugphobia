using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace BetterBugphobia;

[HarmonyPatch(typeof(BugPhobia), "Start")]
public class BugPhobiaPatch
{
    private static readonly Dictionary<string, string> MonsterTypeCache = new();

    [HarmonyPostfix]
    static void Postfix(BugPhobia __instance)
    {
        string gameObjectName = __instance.gameObject.name;

        if (MonsterTypeCache.TryGetValue(gameObjectName, out var monsterName))
        {
            Plugin.Log.LogInfo($"[BetterBugphobia] Cache Hit: {monsterName}");
        }
        else
        {
            var allComponents = __instance.gameObject.GetComponents<MonoBehaviour>();
            foreach (var component in allComponents)
            {
                var componentName = component.GetType().Name;
                if (!Plugin.bugPhobiaMap.ContainsKey(componentName)) continue;
                monsterName = componentName;
                break;
            }

            if (monsterName != null)
            {
                MonsterTypeCache[gameObjectName] = monsterName;
            }
        }

        Plugin.Log.LogInfo($"[BetterBugphobia] Patching {monsterName}");
        
        if (monsterName == null) return;
        
        Plugin.ApplyBugphobia(__instance, monsterName);
        
        Plugin.Log.LogInfo($"[BetterBugphobia] Patch finished for {monsterName}");
    }
}