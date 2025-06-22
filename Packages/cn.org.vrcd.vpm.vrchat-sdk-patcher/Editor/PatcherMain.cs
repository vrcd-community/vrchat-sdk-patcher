using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEditor;
using UnityEngine;
using VRCD.VRChatPackages.VRChatSDKPatcher.Editor.Patchers;

namespace VRCD.VRChatPackages.VRChatSDKPatcher.Editor;

[InitializeOnLoad]
internal class PatcherMain
{
    private const string HarmonyID = "cn.org.vrcd.vpm.vrchat-sdk-patcher.harmony";

    public static readonly Settings PatcherSettings;

    static PatcherMain()
    {
        PatcherSettings = Settings.LoadSettings();

        var harmony = new Harmony(HarmonyID);
        var packageAssembles = new List<Assembly> { Assembly.GetExecutingAssembly() };

        try
        {
            packageAssembles.Add(Assembly.Load("VRCD.VRChatPackages.VRChatSDKPatcher.Worlds.Editor"));
            Debug.Log("Found World Patcher Assembly");
        }
        catch
        {
            // ignored
        }

        var patchersToLoad = packageAssembles.SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsClass && type.GetInterfaces().Contains(typeof(IPatcher)))
            .Select(type => (IPatcher)Activator.CreateInstance(type))
            .ToList();

        foreach (var patcher in patchersToLoad)
        {
            Debug.Log($"Loading Patcher: {patcher.GetType().Name}");
            patcher.Patch(harmony);
        }

        foreach (var assembly in packageAssembles)
        {
            Debug.Log($"Loading Patcher Assembly: {assembly.GetName().Name}");
            harmony.PatchAll(assembly);
        }

        Debug.Log("Patcher Loaded!");
    }
}