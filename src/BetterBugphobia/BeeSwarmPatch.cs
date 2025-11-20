using HarmonyLib;
using UnityEngine;

namespace BetterBugphobia;

[HarmonyPatch(typeof(BeeSwarm), "Start")]
public class BeeSwarmPatch
{
    [HarmonyPrefix]
    private static bool Prefix(BeeSwarm __instance)
    {
        Plugin.Log.LogInfo("BeeSwarm Prefix started!");
        if (Plugin.OriginalBeeSwarmMaterial == null)
        {
            Plugin.Log.LogInfo("Saving original particles");
            Plugin.OriginalBeeSwarmMaterial = __instance.beeParticles.GetComponent<ParticleSystemRenderer>().material;
        }
        
        Plugin.ApplyBeeSwarmSettings(__instance);
        Plugin.Log.LogInfo("BeeSwarm Prefix finished, preventing calling original method");
        return false;
    }
}