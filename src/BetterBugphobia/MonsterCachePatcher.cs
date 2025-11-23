using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace BetterBugphobia;

[HarmonyPatch]
public static class MonsterCreationPatcher
{
    [HarmonyTargetMethods]
    static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (var type in Plugin.MonsterTypesHashSet) 
        {
            MethodInfo creationMethod = AccessTools.Method(type, "Awake") ?? AccessTools.Method(type, "Start");
            if (creationMethod != null)
            {
                yield return creationMethod;
            }
            else
            {
                Plugin.Log.LogWarning($"[BetterBugphobia] Couldn't find start / awake method for {type.Name}.");
            }
        }
    }

    [HarmonyPostfix]
    static void Postfix(Component __instance)
    {
        MonsterCache.Add(__instance);
    }
}


[HarmonyPatch(typeof(Object), "Destroy", new[] { typeof(Object) })]
public static class MonsterDestructionPatcher
{
    [HarmonyPrefix]
    static void Prefix(Object __0)
    {
        if (__0 is not GameObject gameObject) return;
        foreach (var monsterType in Plugin.MonsterTypesHashSet)
        {
            Component monsterComponent = gameObject.GetComponent(monsterType);
            if (monsterComponent != null)
            {
                MonsterCache.Remove(monsterComponent);
                Plugin.Log.LogInfo($"[BetterBugphobia] {monsterComponent.GetType().Name} removed from cache.");
                break; 
            }
        }
    }
}