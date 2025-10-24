using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDKBase.Editor.Source.Helpers;

namespace VRCD.VRChatPackages.VRChatSDKPatcher.Editor.Editor.Views;

public class PatcherSettings : EditorWindow
{
    private TextField _httpProxyUriField;
    private HelpBox _proxySystemHelpBox;

    private HelpBox _proxyUriValidationHelpBox;

    private Button _reloadSdkButton;

    private Toggle _replaceUploadUrlToggle;

    private Settings _settings;
    private Toggle _skipCopyrightAgreementToggle;
    private Toggle _useProxyToggle;
    
    private Toggle _cacheRemoteConfigToggle;

    private const string VisualTreeAssetPath = "Packages/cn.org.vrcd.vpm.vrchat-sdk-patcher/Editor/Views/PatcherSettings.uxml";

    public void CreateGUI()
    {
        var root = rootVisualElement;
        var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(VisualTreeAssetPath);
        var content = visualTreeAsset.Instantiate();

        root.Add(content);

        minSize = new Vector2(430, 600);

        _httpProxyUriField = content.Query<TextField>("proxy-uri-field").First();
        _useProxyToggle = content.Query<Toggle>("proxy-toggle").First();

        _replaceUploadUrlToggle = content.Query<Toggle>("replace-upload-url-toggle").First();
        _skipCopyrightAgreementToggle = content.Query<Toggle>("skip-copyright-agreement-toggle").First();

        _reloadSdkButton = content.Query<Button>("reload-sdk-button").First();

        _proxyUriValidationHelpBox = content.Query<HelpBox>("proxy-uri-validation").First();
        _proxySystemHelpBox = content.Query<HelpBox>("proxy-system").First();
        
        _cacheRemoteConfigToggle = content.Query<Toggle>("cache-remote-config-toggle").First();

        LoadSettings();

        _httpProxyUriField.RegisterValueChangedCallback(_ => SaveSettings());
        _useProxyToggle.RegisterValueChangedCallback(_ => SaveSettings());

        _replaceUploadUrlToggle.RegisterValueChangedCallback(_ => SaveSettings());

        _skipCopyrightAgreementToggle.RegisterValueChangedCallback(_ => SaveSettings());
        
        _cacheRemoteConfigToggle.RegisterValueChangedCallback(_ => SaveSettings());

        _reloadSdkButton.clicked += () => ReloadUtil.ReloadSDK();
    }

    [MenuItem("VRChat SDK Patcher (No longer supported)/Settings")]
    [MenuItem("VRChat SDK Patcher (No longer supported)/VRChat SDK Patcher 现已停止维护，不建议继续使用，感谢您的一路陪伴。")]
    [MenuItem("VRChat SDK Patcher (No longer supported)/VRChat SDK Patcher are no longer supported and not recommend to use, thanks you for your support.")]
    public static void ShowSettings()
    {
        var window = GetWindow<PatcherSettings>();
        window.titleContent = new GUIContent("VRChat SDK Patcher Settings");
    }

    private void LoadSettings()
    {
        _settings = PatcherMain.PatcherSettings;

        _useProxyToggle.value = _settings.UseProxy;
        _httpProxyUriField.value = _settings.HttpProxyUri;

        _replaceUploadUrlToggle.value = _settings.ReplaceUploadUrl;
        _skipCopyrightAgreementToggle.value = _settings.SkipCopyrightAgreement;
        
        _cacheRemoteConfigToggle.value = _settings.CacheRemoteConfig;

        _proxyUriValidationHelpBox.style.display = !string.IsNullOrWhiteSpace(_settings.HttpProxyUri) &&
                                                   !IsValidUri(_settings.HttpProxyUri)
            ? DisplayStyle.Flex
            : DisplayStyle.None;

        _proxySystemHelpBox.style.display = _settings.UseProxy && string.IsNullOrWhiteSpace(_settings.HttpProxyUri)
            ? DisplayStyle.Flex
            : DisplayStyle.None;
    }

    private void SaveSettings()
    {
        _settings.UseProxy = _useProxyToggle.value;
        _settings.HttpProxyUri = _httpProxyUriField.value;

        _settings.ReplaceUploadUrl = _replaceUploadUrlToggle.value;
        _settings.SkipCopyrightAgreement = _skipCopyrightAgreementToggle.value;
        
        _settings.CacheRemoteConfig = _cacheRemoteConfigToggle.value;

        _proxyUriValidationHelpBox.style.display = !string.IsNullOrWhiteSpace(_settings.HttpProxyUri) &&
                                                   !IsValidUri(_settings.HttpProxyUri)
            ? DisplayStyle.Flex
            : DisplayStyle.None;

        _proxySystemHelpBox.style.display = _settings.UseProxy && string.IsNullOrWhiteSpace(_settings.HttpProxyUri)
            ? DisplayStyle.Flex
            : DisplayStyle.None;

        _settings.Save();
    }

    private static bool IsValidUri(string uri)
    {
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var url))
            return false;

        return url.Scheme == Uri.UriSchemeHttp || url.Scheme == Uri.UriSchemeHttps;
    }
}
