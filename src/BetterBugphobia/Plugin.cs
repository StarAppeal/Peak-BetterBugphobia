using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Sirenix.Utilities;
using UnityEngine;

namespace BetterBugphobia;

[BepInAutoPlugin]
public partial class Plugin : BaseUnityPlugin
{
    public static List<Setting> mobList =
    [
        new(typeof(Antlion), "Antlion"),
        new(typeof(BeeSwarm), "Bee") { AdditionalOnSettingChanged = [OnBeesSettingChanged] },
        new(typeof(Beetle), "Beetle"),
        new(typeof(Scorpion), "Scorpion") { AdditionalOnSettingChanged = [UpdateScorpionIcon, UpdateScorpionInWorldTextures] }, 
        new(typeof(Spider), "Spider"),
        new(typeof(Bugfix), "Tick") { AdditionalOnSettingChanged = [UpdateTickIcon, UpdateTickInWorldTextures] }
    ];

    internal static ManualLogSource Log { get; private set; } = null!;
    public static Dictionary<Type, ConfigEntry<bool>> bugPhobiaMap = new();
    public static Material? OriginalBeeSwarmMaterial;
    public static HashSet<Type> MonsterTypesHashSet { get; private set; } = [..mobList.ConvertAll(mob => mob.MobType)];

    private void Awake()
    {
        Log = Logger;
        InitializeConfig();
        Harmony.CreateAndPatchAll(typeof(Plugin).Assembly);
        Log.LogInfo($"Plugin {Name} is loaded!");
    }

    private void InitializeConfig()
    {
        foreach (var mob in mobList)
        {
            var configEntry = Config.Bind("General", mob.SettingName, false,
                $"Toggle the {mob.SettingName}s appearance. false = normal appearance, true = Bing Bong");

            foreach (var onSettingChanged in mob.AdditionalOnSettingChanged)
            {
                configEntry.SettingChanged += onSettingChanged;
            }

            configEntry.SettingChanged += (_, _) => OnBugPhobiaSettingChanged(mob.MobType);
            bugPhobiaMap.Add(mob.MobType, configEntry);
        }
    }

    private static void OnBugPhobiaSettingChanged(Type mobType)
    {
        if (!bugPhobiaMap.TryGetValue(mobType, out var config)) return;
        var isPhobiaActive = config.Value;

        foreach (var mobObject in MonsterCache.GetActiveMobs(mobType))
        {
            if (mobObject == null)
            {
                continue;
            }
            if (mobObject is MonoBehaviour monsterComponent &&
                monsterComponent.TryGetComponent<BugPhobia>(out var bugPhobiaComponent))
            {
                foreach (var go in bugPhobiaComponent.defaultGameObjects) go.SetActive(!isPhobiaActive);
                foreach (var go in bugPhobiaComponent.bugPhobiaGameObjects) go.SetActive(isPhobiaActive);
                if (bugPhobiaComponent.bbas) bugPhobiaComponent.bbas.Init();
            }
        }
    }

    private static void OnBeesSettingChanged(object? sender, EventArgs e)
    {
        Log.LogInfo("Bee setting changed, applying new settings");
        foreach (var beeSwarm in MonsterCache.GetActiveMobs(typeof(BeeSwarm)))
        {
            ApplyBeeSwarmSettings((BeeSwarm)beeSwarm);
        }
    }


    private static void UpdateTickIcon(object s, EventArgs e) => UpdateItemIcons("Tick");
    private static void UpdateTickInWorldTextures(object s, EventArgs e) => UpdateItemInWorldTextures("Tick", typeof(Bugfix));

    private static void UpdateScorpionIcon(object s, EventArgs e) => UpdateItemIcons("Scorpion");
    private static void UpdateScorpionInWorldTextures(object s, EventArgs e) => UpdateItemInWorldTextures("Scorpion", typeof(Scorpion));


    private static void UpdateItemIcons(string itemName)
    {
        Log.LogInfo($"{itemName} setting changed, refreshing icons...");
        var localPlayer = Character.localCharacter;
        var guiManager = GUIManager.instance;

        if (guiManager == null || localPlayer == null)
        {
            Log.LogWarning("GUIManager or LocalCharacter not found. Cannot refresh icons.");
            return;
        }

        RefreshItemIconForUiSlot(localPlayer.player.tempFullSlot, guiManager.temporaryItem, itemName, "temporary slot");

        var uiSlots = guiManager.items;
        var playerInventory = localPlayer.player.itemSlots;
        for (var i = 0; i < playerInventory.Length; i++)
        {
            if (i >= uiSlots.Length) continue;
            RefreshItemIconForUiSlot(playerInventory[i], uiSlots[i], itemName, $"slot {i}");
        }

        Log.LogInfo($"{itemName} icon refresh complete.");
    }
    
    private static void RefreshItemIconForUiSlot(ItemSlot itemSlot, InventoryItemUI uiSlot, string itemName, string slotIdentifier)
    {
        if (uiSlot != null && IsMatchingItemSlot(itemSlot, itemName))
        {
            Log.LogInfo($"Found {itemName} in {slotIdentifier}. Forcing icon texture update.");
            uiSlot.icon.texture = itemSlot.prefab.UIData.GetIcon();
        }
    }

    public static bool IsMatchingItemSlot(ItemSlot slot, string itemName)
    {
        return !slot.IsEmpty() && slot.prefab.UIData.itemName == itemName;
    }

    private static void UpdateItemInWorldTextures(string itemName, Type mobType)
    {
        Log.LogInfo($"{itemName} setting changed, refreshing in-world textures...");
        
        if (!bugPhobiaMap.TryGetValue(mobType, out var configEntry)) return;

        FindObjectsByType<Item>(FindObjectsSortMode.None)
            .Where(item => item.UIData.itemName == itemName)
            .Select(item => item.GetComponent<BugPhobia>())
            .Where(component => component != null)
            .ForEach(bugPhobiaComponent =>
            {
                bugPhobiaComponent.defaultGameObjects.ForEach(go => go.SetActive(!configEntry.Value));
                bugPhobiaComponent.bugPhobiaGameObjects.ForEach(go => go.SetActive(configEntry.Value));
                if (bugPhobiaComponent.bbas) bugPhobiaComponent.bbas.Init();
            });
    }

    public static void ApplyBeeSwarmSettings(BeeSwarm beeSwarm)
    {
        Log.LogInfo("Applying BeeSwarm settings");
        if (!bugPhobiaMap.TryGetValue(typeof(BeeSwarm), out var configEntry)) return;

        var renderer = beeSwarm.beeParticles.GetComponent<ParticleSystemRenderer>();
        if (configEntry.Value)
        {
            renderer.material = beeSwarm.bingBongMaterial;
        }
        else if (OriginalBeeSwarmMaterial != null)
        {
            renderer.material = OriginalBeeSwarmMaterial;
        }
        else
        {
            Log.LogWarning("Original BeeSwarm Material not found, ignoring setting for now");
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