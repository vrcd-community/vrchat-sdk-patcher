using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VRCD.VRChatPackages.VRChatSDKPatcher.Editor.Patchers;

namespace VRCD.VRChatPackages.VRChatSDKPatcher.Editor
{
    [InitializeOnLoad]
    internal class PatcherMain
    {
        private const string HARMONY_ID = "cn.org.vrcd.vpm.vrchat-sdk-patcher.harmony";

        public static Settings PatcherSettings;

        static PatcherMain()
        {
            PatcherSettings = Settings.LoadSettings();

            var harmony = new Harmony(HARMONY_ID);
            var packageAssembly = Assembly.GetExecutingAssembly();

            // ugly, but it works
            var patchersToLoad = new List<IPatcher>();

            if (PatcherSettings.ReplaceUploadUrl)
                patchersToLoad.Add(new UploadEndpointPatcher());

            foreach (var patcher in patchersToLoad)
            {
                patcher.Patch(harmony);
            }

            harmony.PatchAll(packageAssembly);

            Debug.Log("Patcher Loaded!");
        }
    }
}