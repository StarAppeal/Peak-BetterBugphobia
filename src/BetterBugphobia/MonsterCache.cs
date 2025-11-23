using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BetterBugphobia;

public static class MonsterCache
{
    private static readonly Dictionary<Type, List<Component>> activeMobs = new();

    public static void Add(Component mob)
    {
        var type = mob.GetType();
        if (!activeMobs.ContainsKey(type))
        {
            activeMobs[type] = [];
        }
        activeMobs[type].Add(mob);
    }

    public static void Remove(Component mob)
    {
        var type = mob.GetType();
        if (activeMobs.TryGetValue(type, out var mobList))
        {
            mobList.Remove(mob);
        }
    }

    public static IEnumerable<Component> GetActiveMobs(Type mobType)
    {
        if (activeMobs.TryGetValue(mobType, out var mobList))
        {
            return mobList.ToList();
        }
        return Enumerable.Empty<MonoBehaviour>();
    }
}