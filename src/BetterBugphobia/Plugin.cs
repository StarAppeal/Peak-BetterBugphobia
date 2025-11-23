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
    public static List<Setting> mobList =
    [
        new(typeof(Antlion), "Antlion"),
        new(typeof(BeeSwarm), "Bee")
        {
            AdditionalOnSettingChanged = [OnBeesSettingChanged]
        },
        new(typeof(Beetle), "Beetle"),
        new(typeof(Scorpion), "Scorpion"),
        new(typeof(Spider), "Spider"),
        new(typeof(Bugfix), "Tick")
    ];

    internal static ManualLogSource Log { get; private set; } = null!;

    public static Dictionary<string, ConfigEntry<bool>> bugPhobiaMap = new();

    public static Material? OriginalBeeSwarmMaterial;
    
    public static HashSet<Type> MonsterTypesHashSet { get; private set; } = [..mobList.ConvertAll(mob => mob.MobType)];


    private void Awake()
    {
        Log = Logger;

        foreach (var mob in mobList)
        {
            var configEntry = Config.Bind("General", mob.SettingName, false,
                $"Toggle the {mob.SettingName}s appearance. false = normal appearance, true = Bing Bong");
            foreach (var onSettingChanged in mob.AdditionalOnSettingChanged)
            {
                configEntry.SettingChanged -= onSettingChanged;
            }

            // add default onSettingChanged event handler
            configEntry.SettingChanged += (_, _) =>
            {
                var mobType = mob.MobType;

                foreach (var mobObject in MonsterCache.GetActiveMobs(mobType))
                {
                    var monsterComponent = mobObject as MonoBehaviour;
                    if (monsterComponent == null) continue;
                    if (monsterComponent.TryGetComponent<BugPhobia>(out var bugPhobiaComponent))
                    {
                        if (!bugPhobiaMap.TryGetValue(mobType.Name, out var config)) return;
                        foreach (var go in bugPhobiaComponent.defaultGameObjects) go.SetActive(!config.Value);
                        foreach (var go in bugPhobiaComponent.bugPhobiaGameObjects) go.SetActive(config.Value);
                    }
                }
            };

            bugPhobiaMap.Add(mob.MobType.Name, configEntry);
        }

        Harmony.CreateAndPatchAll(typeof(Plugin).Assembly);

        Log.LogInfo($"Plugin {Name} is loaded!");
    }

    private static void OnBeesSettingChanged(object? sender, EventArgs e)
    {
        foreach (var beeSwarm in MonsterCache.GetActiveMobs(typeof(BeeSwarm)))
            ApplyBeeSwarmSettings((BeeSwarm)beeSwarm);
    }

    public static void ApplyBeeSwarmSettings(BeeSwarm beeSwarm)
    {
        if (!bugPhobiaMap.TryGetValue(nameof(BeeSwarm), out var configEntry)) return;
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

    public class Setting
    {
        public Type MobType { get; }
        public string SettingName { get; }
        public List<EventHandler> AdditionalOnSettingChanged { get; set; } = [];

        public Setting(Type mobType, string settingName)
        {
            MobType = mobType;
            SettingName = settingName;
        }
    }
}