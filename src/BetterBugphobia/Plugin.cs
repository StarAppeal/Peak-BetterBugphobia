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
    internal static ManualLogSource Log { get; private set; } = null!;

    private List<Type> mobList = [typeof(Spider), typeof(Scorpion), typeof(Antlion), typeof(Beetle)];
    
    public static Dictionary<string, ConfigEntry<bool>> bugPhobiaMap = new();
    
    private void Awake()
    {
        Log = Logger;

        foreach (var mob in mobList)
        {
            var configEntry = Config.Bind("General", mob.Name, false, "BingBong?");
            configEntry.SettingChanged += OnSettingChanged;
            bugPhobiaMap.Add(mob.Name, configEntry);
        }

        Harmony.CreateAndPatchAll(typeof(Plugin).Assembly);
        
        Log.LogInfo($"Plugin {Name} is loaded!");
    }

    private void OnSettingChanged(object? sender, EventArgs e)
    {
        Log.LogInfo("Setting Changed!");
        ApplyAllBugphobiaSettings();
    }
    
    private void ApplyAllBugphobiaSettings()
    {
        foreach (var mobType in mobList)
        {
            foreach (var mob in FindObjectsOfType(mobType))
            {
                var monsterComponent = mob as MonoBehaviour; 
                if (monsterComponent == null) continue;
                if (monsterComponent.TryGetComponent<BugPhobia>(out var bugPhobiaComponent))
                {
                    ApplyBugphobia(bugPhobiaComponent, mobType.Name);
                }
            }
        }
    }
    
    public static void ApplyBugphobia(BugPhobia bugPhobiaComponent, string monsterName)
    {
        if (!bugPhobiaMap.TryGetValue(monsterName, out var configEntry)) return;
        foreach(var go in bugPhobiaComponent.defaultGameObjects) go.SetActive(!configEntry.Value);
        foreach(var go in bugPhobiaComponent.bugPhobiaGameObjects) go.SetActive(configEntry.Value);
    }
}

