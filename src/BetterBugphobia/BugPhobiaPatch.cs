using System.Linq;
using HarmonyLib;

namespace BetterBugphobia;

[HarmonyPatch(typeof(BugPhobia), "Start")]
public class BugPhobiaPatch
{
    [HarmonyPrefix]
    static bool Prefix(BugPhobia __instance)
    {
        Plugin.Log.LogInfo("BugPhobia Prefix started!");
        Plugin.Log.LogDebug($"{__instance.gameObject.name}");

        var type = Plugin.MonsterTypesHashSet
            .FirstOrDefault(type => __instance.gameObject.GetComponentInChildren(type, true) != null);
        // extra check for item
        if (type == null)
        {
            var item = __instance.gameObject.GetComponentInChildren<Item>();
            if (item == null)
            {
                Plugin.Log.LogInfo("No item found!");
                return true;
            }

            if (item.UIData.itemName == "Tick")
            {
                type = typeof(Bugfix);
            }
        }

        Plugin.Log.LogInfo($"Found type: {type}");
        
        // extra check
        if (type == null) return true;

        Plugin.bugPhobiaMap.TryGetValue(type, out var configEntry);

        if (configEntry == null) return true;

        foreach (var go in __instance.defaultGameObjects) go.SetActive(!configEntry.Value);
        foreach (var go in __instance.bugPhobiaGameObjects) go.SetActive(configEntry.Value);
        
        if (__instance.bbas) __instance.bbas.Init();
        
        return false;
    }
}