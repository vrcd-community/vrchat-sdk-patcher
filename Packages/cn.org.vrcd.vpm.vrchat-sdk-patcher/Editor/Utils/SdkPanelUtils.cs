using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace VRCD.VRChatPackages.VRChatSDKPatcher.Editor.Utils;

internal static class SdkPanelUtils
{
    public static void OpenSdkPanel()
    {
        var controlPanelType = typeof(VRCSdkControlPanel);

        var showControlPanelMethod =
            controlPanelType.GetMethod("ShowControlPanel", BindingFlags.Static | BindingFlags.NonPublic);

        if (showControlPanelMethod == null)
            throw new MissingMethodException(nameof(VRCSdkControlPanel), "ShowControlPanel");

        showControlPanelMethod.Invoke(null, null);
    }

    public static bool OpenSdkPanelGuard()
    {
        try
        {
            OpenSdkPanel();
        }
        catch (Exception ex)
        {
            Debug.LogError("Unable to find ShowControlPanel method, please open it manually");
            Debug.LogException(ex);

            EditorUtility.DisplayDialog("Unable to open SDK Panel", "Please Open SDK Panel manually", "OK");
            return false;
        }

        return true;
    }
}