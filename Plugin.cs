using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace Zaprillator;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInProcess("Lethal Company.exe")]
public class Plugin : BaseUnityPlugin
{
    private static ManualLogSource Log { get; set; }
    internal static PluginConfigStruct GameConfig { get; private set; }
    private static Harmony _globalHarmony;
    
    private void Awake()
    {
        Log = Logger;
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded! ;)");
        
        // Apply Evaisa's NetworkPatcher
        PatchNetwork();

        _globalHarmony = new Harmony("Zaprillator");
        _globalHarmony.PatchAll();

        GameConfig = new PluginConfigStruct
        {
            RequiresFullCharge = Config.Bind("Revive", "RequiresFullCharge", false, @"A full charge is required to revive a player."),
            RelativeHealth = Config.Bind("Revive", "RelativeHealth", false, @"Define if restored health is relative to the gun's charge."),
        };
    }
    
    private static void PatchNetwork()
    {
        try
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes =
                        method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length <= 0) continue;
                    Log.LogInfo("Initialize network patch for " + type.FullName);
                    method.Invoke(null, null);
                }
            }
        }
        catch (Exception e)
        {
        }
    }
}

internal struct PluginConfigStruct
{
    public ConfigEntry<bool> RequiresFullCharge;
    public ConfigEntry<bool> RelativeHealth;
}