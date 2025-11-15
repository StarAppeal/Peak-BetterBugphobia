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

    private List<Setting> mobList = [
        new() {MobType = typeof(Spider), SettingName = "Spider"},
        new () {MobType = typeof(Scorpion), SettingName = "Scorpion"},
         new() {MobType = typeof(Antlion), SettingName = "Antlion"},
        new () {MobType = typeof(Beetle), SettingName = "Beetle"}, 
        new() {MobType = typeof(Bugfix), SettingName = "Tick"},
        new () {MobType = typeof(BeeSwarm), SettingName = "Bees"},
    ];
    
    public static Dictionary<string, ConfigEntry<bool>> bugPhobiaMap = new();
    
    private void Awake()
    {
        Log = Logger;

        foreach (var mob in mobList)
        {
            var configEntry = Config.Bind("General", mob.SettingName, false, 
                $"Toggle the {mob.SettingName}s appearance. false = normal appearance, true = Bing Bong");
            configEntry.SettingChanged += OnSettingChanged;
            bugPhobiaMap.Add(mob.MobType.Name, configEntry);
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
        foreach(var go in bugPhobiaComponent.defaultGameObjects) go.SetActive(!configEntry.Value);
        foreach(var go in bugPhobiaComponent.bugPhobiaGameObjects) go.SetActive(configEntry.Value);
    }
}

public struct Setting
{
    public Type MobType;
    public string SettingName;
}