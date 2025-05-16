using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Editor;
using VRC.SDKBase.Editor.Api;
using VRCD.VRChatPackages.VRChatSDKPatcher.Editor.Utils;

namespace VRCD.VRChatPackages.VRChatSDKPatcher.Worlds.Editor.Utils;

internal static class WorldBuildActionUtils
{
    #region Build Action Methods

    public static void BuildGuard()
    {
        if (GetBuilderGuard() is not { } builder)
            return;

        builder.Build();
    }

    public static void BuildAndTestGuard()
    {
        if (GetBuilderGuard() is not { } builder)
            return;

        builder.BuildAndTest();
    }

    public static void TestLastBuildGuard()
    {
        if (GetBuilderGuard() is not { } builder)
            return;

        builder.TestLastBuild();
    }

    public static void BuildAndUploadGuard()
    {
        if (GetBuilderGuard() is not { } builder)
            return;

        if (GetCurrentWorldGuard() is not { } world)
            return;

        builder.BuildAndUpload(world);
    }

    public static void UploadLastBuildGuard()
    {
        if (GetBuilderGuard() is not { } builder)
            return;

        if (GetCurrentWorldGuard() is not { } world)
            return;

        builder.UploadLastBuild(world);
    }

    #endregion

    #region Get Methods

    public static IVRCSdkWorldBuilderApi? GetBuilderGuard()
    {
        if (!VRCSdkControlPanel.TryGetBuilder<IVRCSdkWorldBuilderApi>(out var api))
        {
            Debug.LogError("Open SDK Builder Panel First");
            if (EditorUtility.DisplayDialog("Open SDK Builder Panel First",
                    "Please Open SDK Builder Panel First", "Open SDK Builder Panel", "Cancel"))
            {
                SdkPanelUtils.OpenSdkPanelGuard();
            }

            return null;
        }

        return api;
    }

    public static VRCWorld? GetCurrentWorldGuard()
    {
        if (GetBuilderGuard() is not { } builder)
            return null;

        try
        {
            return GetCurrentWorldCore(builder);
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "Failed to get current world, please open the SDK Builder Panel and ensure there is a world data loaded");
            Debug.LogException(ex);

            EditorUtility.DisplayDialog("Failed to get current world",
                "Please Open the SDK Builder Panel and ensure there is a world data loaded\n" +
                "(Notice: VRChat SDK Patcher didn't support create a new world)", "OK");

            return null;
        }
    }

    private static VRCWorld GetCurrentWorldCore(IVRCSdkWorldBuilderApi builder)
    {
        if (builder is not VRCSdkControlPanelWorldBuilder)
            throw new InvalidOperationException(
                "Builder is not a VRCSdkControlPanelWorldBuilder, it may caused by sdk changes");

        var worldDataField = typeof(VRCSdkControlPanelWorldBuilder).GetField("_worldData",
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (worldDataField == null)
            throw new InvalidOperationException("Failed to get world data field, it may caused by sdk changes");

        if (worldDataField.GetValue(builder) is not VRCWorld world)
            throw new InvalidOperationException(
                "Failed to get value in world data field, it should never happen, but it happen");

        if (world.ID == "")
            throw new InvalidOperationException(
                "World ID is empty, please open the SDK Builder Panel and ensure there is a world data loaded");

        return world;
    }

    #endregion
}
