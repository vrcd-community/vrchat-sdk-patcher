using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Editor.Builder;

namespace VRCD.VRChatPackages.VRChatSDKPatcher.Worlds.Editor;

[HarmonyPatch(typeof(VRCWorldAssetExporter),
    nameof(VRCWorldAssetExporter.ExportCurrentSceneResource),
    typeof(bool), typeof(Action<string>), typeof(Action<object>))]
internal class ExportCurrentSceneResourcePatch
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        var originalCodes = codes.ToList();

        var buildAssetBundleMethodInfo = AccessTools.Method(typeof(BuildPipeline),
            nameof(BuildPipeline.BuildAssetBundles),
            new[]
            {
                typeof(string), typeof(AssetBundleBuild[]), typeof(BuildAssetBundleOptions), typeof(BuildTarget)
            });

        var callIndex = codes.FindIndex(code =>
            code.opcode == OpCodes.Call && code.operand is MethodInfo callMethodInfo &&
            callMethodInfo == buildAssetBundleMethodInfo);

        var popNullIndex = codes.FindIndex(callIndex, code => code.opcode == OpCodes.Pop);

        if (callIndex == -1)
        {
            Debug.LogError("Failed to find BuildPipeline.BuildAssetBundles call");
            return codes;
        }

        codes.RemoveAt(popNullIndex);

        var stringConcatWithThreeArgsMethodInfo = AccessTools.Method(typeof(string),
            nameof(string.Concat),
            new[] { typeof(string), typeof(string), typeof(string) });
        var stringConcatWithArrayArgMethodInfo = AccessTools.Method(typeof(string),
            nameof(string.Concat),
            new[] { typeof(string[]) });
        var assetBundlePathConcatIndex = codes.FindIndex(code =>
            code.opcode == OpCodes.Call && code.operand is MethodInfo callMethodInfo &&
            (callMethodInfo == stringConcatWithThreeArgsMethodInfo ||
             callMethodInfo == stringConcatWithArrayArgMethodInfo));

        if (assetBundlePathConcatIndex == -1)
        {
            Debug.LogError("Failed to find string.Concat call");
            return originalCodes;
        }

        var assetBundlePathFieldIndex = codes.FindIndex(assetBundlePathConcatIndex,
            code => code.opcode == OpCodes.Stloc_S && code.operand is LocalBuilder assetBundlePathLocalBuilder &&
                    assetBundlePathLocalBuilder.LocalType == typeof(string));
        if (assetBundlePathFieldIndex == -1)
        {
            Debug.LogError("Failed to find assetBundlePathFieldInfo");
            return originalCodes;
        }

        var assetBundlePathLocalBuilder = (LocalBuilder)codes[assetBundlePathFieldIndex].operand;

        var pathCombineMethodInfo = AccessTools.Method(typeof(Path),
            nameof(Path.Combine),
            new[] { typeof(string), typeof(string) });
        var getAllAssetBundlesMethodInfo = AccessTools.Method(typeof(AssetBundleManifest),
            nameof(AssetBundleManifest.GetAllAssetBundles));
        var getApplicationTemporaryCachePathGetterMethodInfo = AccessTools.PropertyGetter(typeof(Application),
            nameof(Application.temporaryCachePath));

        var debugLogMethodInfo = AccessTools.Method(typeof(Debug),
            nameof(Debug.Log),
            new[] { typeof(object) });
        var stringConcatWithTwoArgsMethodInfo = AccessTools.Method(typeof(string),
            nameof(string.Concat),
            new[] { typeof(string), typeof(string) });

        codes.InsertRange(popNullIndex, new[]
        {
            // assetBundlePath = BuildPipeline.BuildAssetBundles(Application.temporaryCachePath, builds, BuildAssetBundleOptions.ForceRebuildAssetBundle, EditorUserBuildSettings.activeBuildTarget).GetAllAssetBundles()[0];
            new CodeInstruction(OpCodes.Callvirt, getAllAssetBundlesMethodInfo),
            new CodeInstruction(OpCodes.Ldc_I4_0),
            new CodeInstruction(OpCodes.Ldelem_Ref),
            new CodeInstruction(OpCodes.Stloc_S, assetBundlePathLocalBuilder),
            // assetBundlePath = Path.Combine(Application.temporaryCachePath, assetBundlePath);
            new CodeInstruction(OpCodes.Call, getApplicationTemporaryCachePathGetterMethodInfo),
            new CodeInstruction(OpCodes.Ldloc_S, assetBundlePathLocalBuilder),
            new CodeInstruction(OpCodes.Call, pathCombineMethodInfo),
            new CodeInstruction(OpCodes.Stloc_S, assetBundlePathLocalBuilder),
            // Debug.Log("Asset Bundle Path: " + assetBundlePath);
            new CodeInstruction(OpCodes.Ldstr, "Asset Bundle Path: "),
            new CodeInstruction(OpCodes.Ldloc_S, assetBundlePathLocalBuilder),
            new CodeInstruction(OpCodes.Call, stringConcatWithTwoArgsMethodInfo),
            new CodeInstruction(OpCodes.Call, debugLogMethodInfo)
        });

        return codes;
    }
}