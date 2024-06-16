using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace VRCD.VRChatPackages.VRChatSDKPatcher.Editor.Editor.Views
{
    public class PatcherSettings : EditorWindow
    {
        [SerializeField] private VisualTreeAsset m_VisualTreeAsset = default;

        private Settings _settings;

        private TextField _httpProxyUriField;
        private Toggle _useProxyToggle;

        private Toggle _replaceUploadUrlToggle;

        [MenuItem("VRChat SDK Patcher/Settings")]
        public static void ShowSettings()
        {
            var window = GetWindow<PatcherSettings>();
            window.titleContent = new GUIContent("VRChat SDK Patcher Settings");
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;
            var content = m_VisualTreeAsset.Instantiate();
            root.Add(content);

            _httpProxyUriField = content.Query<TextField>("proxy-uri-field").First();
            _useProxyToggle = content.Query<Toggle>("proxy-toggle").First();

            // _replaceUploadUrlToggle = content.Query<Toggle>("replace-upload-url-toggle").First();

            LoadSettings();

            _httpProxyUriField.RegisterValueChangedCallback(_ => SaveSettings());
            _useProxyToggle.RegisterValueChangedCallback(_ => SaveSettings());

            // _replaceUploadUrlToggle.RegisterValueChangedCallback(_ => SaveSettings());
        }

        private void LoadSettings()
        {
            _settings = PatcherMain.PatcherSettings;

            _useProxyToggle.value = _settings.UseProxy;
            _httpProxyUriField.value = _settings.HttpProxyUri;

            // _replaceUploadUrlToggle.value = _settings.ReplaceUploadUrl;
        }

        private void SaveSettings()
        {
            _settings.UseProxy = _useProxyToggle.value;
            _settings.HttpProxyUri = _httpProxyUriField.value;

            _settings.ReplaceUploadUrl = _replaceUploadUrlToggle.value;

            _settings.Save();
        }
    }
}