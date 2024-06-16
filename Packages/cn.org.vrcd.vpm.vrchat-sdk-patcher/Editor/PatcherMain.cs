using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEditor;
using UnityEngine;
using VRCD.VRChatPackages.VRChatSDKPatcher.Editor.Patchers;

namespace VRCD.VRChatPackages.VRChatSDKPatcher.Editor
{
    [InitializeOnLoad]
    internal class PatcherMain
    {
        private const string HARMONY_ID = "cn.org.vrcd.vpm.upload-endpoint-patcher.harmony";

        public static Settings PatcherSettings;

        static PatcherMain()
        {
            PatcherSettings = Settings.LoadSettings();

            var harmony = new Harmony(HARMONY_ID);
            var packageAssembly = Assembly.GetExecutingAssembly();

            // ugly, but it works
            var patchers = new IPatcher[]
            {
                new UploadEndpointPatcher()
            };

            foreach (var patcher in patchers)
            {
                patcher.Patch(harmony);
            }

            harmony.PatchAll(packageAssembly);

            Debug.Log("Patcher Loaded!");
        }
    }
}