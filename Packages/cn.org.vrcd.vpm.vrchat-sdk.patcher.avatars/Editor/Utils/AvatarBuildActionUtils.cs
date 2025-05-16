using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEditor;
using UnityEngine;
using VRC.Core;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Editor;
using VRC.SDK3A.Editor;
using VRC.SDKBase.Editor.Api;
using VRCD.VRChatPackages.VRChatSDKPatcher.Editor.Utils;

namespace VRCD.VRChatPackages.VRChatSDKPatcher.Avatars.Editor.Utils;

internal static class AvatarBuildActionUtils
{
    private static VRCAvatar? _lastAvatarBuildInfo;
    private static string? _lastAvatarBuildBundlePath;
    private static GameObject? _lastAvatarBuildGameObject;

    [InitializeOnLoadMethod]
    private static void HookAvatarBuildEvent()
    {
        VRCSdkControlPanel.OnSdkPanelEnable += (_, _) =>
        {
            if (!VRCSdkControlPanel.TryGetBuilder<IVRCSdkAvatarBuilderApi>(out var builder)) return;

            builder.OnSdkBuildError += (_, _) =>
            {
                _lastAvatarBuildInfo = null;
                _lastAvatarBuildBundlePath = null;

                Debug.Log("Build failed, last build info cleared");
            };

            builder.OnSdkBuildStart += (_, targetGameObject) =>
            {
                _lastAvatarBuildInfo = null;
                _lastAvatarBuildBundlePath = null;

                Debug.Log("Build started, last build info cleared");

                var target = targetGameObject as GameObject;
                if (!target)
                {
                    Debug.LogError(
                        "Failed to get target GameObject from build started event, Upload Last Build will not work");
                    return;
                }

                Debug.Log("Get target GameObject from build started event", target);

                _lastAvatarBuildGameObject = target;
            };

            builder.OnSdkBuildSuccess += (_, bundlePath) =>
            {
                try
                {
                    _lastAvatarBuildInfo = GetCurrentAvatarCore(builder);
                    _lastAvatarBuildBundlePath = bundlePath;

                    Debug.Log(
                        $"Build success, last build info set to {_lastAvatarBuildInfo?.Name} ({_lastAvatarBuildInfo?.ID}) " +
                        $"Bundle path: {_lastAvatarBuildBundlePath}");
                }
                catch (Exception ex)
                {
                    Debug.LogError("Failed to get last avatar build info, Upload Last Build will not work");
                    Debug.LogException(ex);

                    _lastAvatarBuildInfo = null;
                    _lastAvatarBuildBundlePath = null;
                }
            };
        };
    }

    #region Build Action Methods

    public static void BuildGuard()
    {
        if (GetBuilderGuard() is not { } builder)
            return;

        if (GetCurrentAvatarGameObjectGuard() is not { } avatarGameObject)
            return;

        builder.Build(avatarGameObject);
    }

    public static void BuildAndTestGuard()
    {
        if (GetBuilderGuard() is not { } builder)
            return;

        if (GetCurrentAvatarGameObjectGuard() is not { } avatarGameObject)
            return;

        builder.BuildAndTest(avatarGameObject);
    }

    public static void BuildAndUploadGuard()
    {
        if (GetBuilderGuard() is not { } builder)
            return;

        if (GetCurrentAvatarGameObjectGuard() is not { } avatarGameObject)
            return;

        if (GetCurrentAvatarGuard() is not { } avatar)
            return;

        builder.BuildAndUpload(avatarGameObject, avatar);
    }

    public static void UploadLastBuildGuard()
    {
        if (GetBuilderGuard() is not { } builder)
            return;

        if (builder is not VRCSdkControlPanelAvatarBuilder)
        {
            Debug.LogError("Builder is not a VRCSdkControlPanelAvatarBuilder");
            EditorUtility.DisplayDialog("Builder is not a VRCSdkControlPanelAvatarBuilder",
                "Builder is not a VRCSdkControlPanelAvatarBuilder", "OK");

            return;
        }

        var uploadMethod =
            typeof(VRCSdkControlPanelAvatarBuilder).GetMethod("Upload", BindingFlags.Instance | BindingFlags.NonPublic);

        if (uploadMethod is null)
        {
            Debug.LogError("Failed to get Upload method");
            EditorUtility.DisplayDialog("Failed to get Upload method",
                "Failed to get Upload method", "OK");

            return;
        }

        if (!ValidateUploadMethodArgs(uploadMethod))
        {
            Debug.LogError("Upload method args is not valid");
            EditorUtility.DisplayDialog("Upload method args is not valid",
                "Upload method args is not valid", "OK");

            return;
        }

        if (_lastAvatarBuildBundlePath is null || _lastAvatarBuildInfo is null || !_lastAvatarBuildGameObject ||
            // make nullable check happy
            _lastAvatarBuildGameObject == null)
        {
            Debug.LogError("No last build found, please build the avatar first");

            if (EditorUtility.DisplayDialog("No last build found",
                    "Please build the avatar first", "Build and Upload", "Cancel"))
            {
                BuildAndUploadGuard();
            }

            return;
        }

        if (_lastAvatarBuildInfo.Value.ID == "")
        {
            Debug.LogError("Avatar ID is empty, please publish the avatar first");

            EditorUtility.DisplayDialog("Avatar ID is empty",
                "Please publish the avatar first and build again", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Upload Last Build",
                $"Are you sure you want to upload the last build of avatar {_lastAvatarBuildInfo.Value.Name} ({_lastAvatarBuildInfo.Value.ID})?\n" +
                $"Attention: It **won't** show progress in sdk panel, please check the console",
                "Yes",
                "No"))
            return;

        try
        {
            uploadMethod.Invoke(builder, new object?[]
            {
                _lastAvatarBuildGameObject,
                _lastAvatarBuildInfo,
                _lastAvatarBuildBundlePath,
                null,
                CancellationToken.None
            });
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to trigger upload method");
            Debug.LogException(ex);

            EditorUtility.DisplayDialog("Upload Last Build Failed",
                "Failed to trigger upload method", "OK");
        }
    }

    #endregion

    #region Get Methods

    public static IVRCSdkAvatarBuilderApi? GetBuilderGuard()
    {
        if (!VRCSdkControlPanel.TryGetBuilder<IVRCSdkAvatarBuilderApi>(out var api))
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

    public static GameObject? GetCurrentAvatarGameObjectGuard()
    {
        var selectedGameObject = Selection.activeGameObject;

        if (!selectedGameObject)
        {
            Debug.LogError(
                "Failed to get current avatar, please select an avatar GameObject");

            EditorUtility.DisplayDialog("Failed to get current avatar",
                "Please select an avatar GameObject", "OK");

            return null;
        }

        var pipelineManager = selectedGameObject.GetComponentInChildren<PipelineManager>();

        if (!pipelineManager)
        {
            pipelineManager = selectedGameObject.GetComponentInParent<PipelineManager>();
        }

        if (!pipelineManager)
        {
            Debug.LogError(
                "The avatar you selected have no PipelineManager component or you select a GameObject no belong to an avatar");

            EditorUtility.DisplayDialog("Selected GameObject is not a valid avatar",
                "The avatar you selected have no PipelineManager component or you select a GameObject no belong to an avatar.",
                "OK");

            return null;
        }

        if (!pipelineManager.TryGetComponent<VRCAvatarDescriptor>(out _))
        {
            Debug.LogError(
                "The avatar you selected have no VRCAvatarDescriptor component or not in the PipelineManager GameObject, or you select a GameObject no belong to an avatar");

            EditorUtility.DisplayDialog("Selected GameObject is not a valid avatar",
                "The avatar you selected have no VRCAvatarDescriptor component or not in the PipelineManager GameObject, or you select a GameObject no belong to an avatar.",
                "OK");

            return null;
        }

        return pipelineManager.gameObject;
    }

    public static VRCAvatar? GetCurrentAvatarGuard()
    {
        if (GetBuilderGuard() is not { } builder)
            return null;

        try
        {
            return GetCurrentAvatarCore(builder);
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "Failed to get current avatar data, please open the SDK Builder Panel and ensure there is a avatar data loaded");
            Debug.LogException(ex);

            EditorUtility.DisplayDialog("Failed to get current avatar data",
                "Please Open the SDK Builder Panel and ensure there is a avatar data loaded\n" +
                "(Notice: VRChat SDK Patcher didn't support create a new avatar)", "OK");

            return null;
        }
    }

    private static VRCAvatar GetCurrentAvatarCore(IVRCSdkAvatarBuilderApi builder)
    {
        if (builder is not VRCSdkControlPanelAvatarBuilder)
            throw new InvalidOperationException(
                "Builder is not a VRCSdkControlPanelWorldBuilder, it may caused by sdk changes");

        var worldDataField = typeof(VRCSdkControlPanelAvatarBuilder).GetField("_avatarData",
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (worldDataField == null)
            throw new InvalidOperationException("Failed to get avatar data field, it may caused by sdk changes");

        if (worldDataField.GetValue(builder) is not VRCAvatar avatar)
            throw new InvalidOperationException(
                "Failed to get value in avatar data field, it should never happen, but it happen");

        if (avatar.ID == "")
            throw new InvalidOperationException(
                "Avatar ID is empty, please open the SDK Builder Panel and ensure there is a avatar data loaded");

        return avatar;
    }

    #endregion

    private static bool ValidateUploadMethodArgs(MethodInfo uploadMethod)
    {
        // method signature:
        // private async Task<bool> Upload(GameObject target, VRCAvatar avatar, string bundlePath, string thumbnailPath = null, CancellationToken cancellationToken = default)
        var argTypes = uploadMethod.GetParameters();

        if (argTypes.Length != 5)
            return false;

        if (argTypes.Any(param => param.IsOut || param.IsIn || param.IsLcid || param.IsRetval))
            return false;

        // target: GameObject
        if (argTypes[0].ParameterType != typeof(GameObject))
            return false;

        // avatar: VRCAvatar
        if (argTypes[1].ParameterType != typeof(VRCAvatar))
            return false;

        // bundlePath: string
        if (argTypes[2].ParameterType != typeof(string))
            return false;

        // thumbnailPath: string
        if (argTypes[3].ParameterType != typeof(string) && argTypes[3].HasDefaultValue && argTypes[3].IsOptional)
            return false;

        // cancellationToken: CancellationToken
        if (argTypes[4].ParameterType != typeof(CancellationToken) && argTypes[4].HasDefaultValue && argTypes[4].IsOptional)
            return false;

        return true;
    }
}
