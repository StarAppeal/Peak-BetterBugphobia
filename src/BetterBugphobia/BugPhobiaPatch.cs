using HarmonyLib;

namespace BetterBugphobia;

[HarmonyPatch(typeof(BugPhobia), "Start")]
public class BugPhobiaPatch
{
    [HarmonyPrefix]
    static bool Prefix(BugPhobia __instance)
    {
        foreach (var monsterObject in __instance.defaultGameObjects)
        {
            var monsterName = monsterObject.GetType().Name;
            if (!Plugin.bugPhobiaMap.TryGetValue(monsterName, out var configEntry)) continue;
            
            monsterObject.SetActive(!configEntry.Value);
        }

        foreach (var monsterObject in __instance.bugPhobiaGameObjects)
        {
            var monsterName = monsterObject.GetType().Name;
            if (!Plugin.bugPhobiaMap.TryGetValue(monsterName, out var configEntry)) continue;
            
            monsterObject.SetActive(configEntry.Value);
        }
        
        Plugin.Log.LogInfo($"[BetterBugphobia] Patch finished");
        
        return false; 
    }
}