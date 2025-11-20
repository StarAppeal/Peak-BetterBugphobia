using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BetterBugphobia;

[BepInAutoPlugin]
public partial class Plugin : BaseUnityPlugin
{
    private static List<Setting> mobList =
    [
        new() { MobType = typeof(Antlion), SettingName = "Antlion", OnSettingChanged = [OnSettingChanged] },
        new()
        {
            MobType = typeof(BeeSwarm), SettingName = "Bees",
            OnSettingChanged = [OnSettingChanged, OnBeesSettingChanged]
        },
        new() { MobType = typeof(Beetle), SettingName = "Beetle", OnSettingChanged = [OnSettingChanged] },
        new() { MobType = typeof(Scorpion), SettingName = "Scorpion", OnSettingChanged = [OnSettingChanged] },
        new() { MobType = typeof(Spider), SettingName = "Spider", OnSettingChanged = [OnSettingChanged] },
        new() { MobType = typeof(Bugfix), SettingName = "Tick", OnSettingChanged = [OnSettingChanged] }
    ];

    internal static ManualLogSource Log { get; private set; } = null!;

    public static Dictionary<string, ConfigEntry<bool>> bugPhobiaMap = new();

    public static Material? OriginalBeeSwarmMaterial;

    private void Awake()
    {
        Log = Logger;

        foreach (var mob in mobList)
        {
            var configEntry = Config.Bind("General", mob.SettingName, false,
                $"Toggle the {mob.SettingName}s appearance. false = normal appearance, true = Bing Bong");
            foreach (var onSettingChanged in mob.OnSettingChanged)
            {
                configEntry.SettingChanged += onSettingChanged;
            }

            bugPhobiaMap.Add(mob.MobType.Name, configEntry);
        }

        Harmony.CreateAndPatchAll(typeof(Plugin).Assembly);

        Log.LogInfo($"Plugin {Name} is loaded!");
    }

    private static void OnSettingChanged(object? sender, EventArgs e)
    {
        Log.LogInfo("Setting Changed!");
        ApplyAllBugphobiaSettings();
    }

    private static void ApplyAllBugphobiaSettings()
    {
        foreach (var mobType in mobList)
        {
            foreach (var mob in FindObjectsOfType(mobType.MobType))
            {
                var monsterComponent = mob as MonoBehaviour;
                if (monsterComponent == null) continue;
                if (monsterComponent.TryGetComponent<BugPhobia>(out var bugPhobiaComponent))
                {
                    ApplyBugphobia(bugPhobiaComponent, mobType.MobType.Name);
                }
            }
        }
    }

    public static void ApplyBugphobia(BugPhobia bugPhobiaComponent, string monsterName)
    {
        if (!bugPhobiaMap.TryGetValue(monsterName, out var configEntry)) return;
        foreach (var go in bugPhobiaComponent.defaultGameObjects) go.SetActive(!configEntry.Value);
        foreach (var go in bugPhobiaComponent.bugPhobiaGameObjects) go.SetActive(configEntry.Value);
    }

    private static void OnBeesSettingChanged(object? sender, EventArgs e)
    {
        foreach (var beeSwarm in FindObjectsOfType<BeeSwarm>()) ApplyBeeSwarmSettings(beeSwarm);
    }

    public static void ApplyBeeSwarmSettings(BeeSwarm beeSwarm)
    {
        if (!bugPhobiaMap.TryGetValue("BeeSwarm", out var configEntry)) return;
        {
            if (configEntry.Value)
            {
                beeSwarm.beeParticles.GetComponent<ParticleSystemRenderer>().material = beeSwarm.bingBongMaterial;
            }
            else if (OriginalBeeSwarmMaterial != null)
            {
                beeSwarm.beeParticles.GetComponent<ParticleSystemRenderer>().material = OriginalBeeSwarmMaterial;
            }
            else
            {
                Log.LogWarning("Original BeeSwarm Material not found, ignoring setting for now");
            }
        }
    }
}

public struct Setting
{
    public Type MobType;
    public string SettingName;
    public List<EventHandler> OnSettingChanged;
}