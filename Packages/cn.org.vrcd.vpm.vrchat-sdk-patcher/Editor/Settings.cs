using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace VRCD.VRChatPackages.VRChatSDKPatcher.Editor;

public class Settings
{
    public const string SettingsFileName = "settings.json";
    public bool UseProxy { get; set; } = true;
    public string HttpProxyUri { get; set; } = "";

    public bool ReplaceUploadUrl { get; set; }
    public bool SkipCopyrightAgreement { get; set; }
    
    public bool CacheRemoteConfig { get; set; }

    public static Settings LoadSettings()
    {
        var basePath = GetSettingsBasePath();
        var path = Path.Combine(basePath, SettingsFileName);

        if (!File.Exists(path))
        {
            if (!Directory.Exists(basePath))
                Directory.CreateDirectory(basePath);

            var emptySettings = new Settings();
            File.WriteAllText(path, JsonConvert.SerializeObject(emptySettings));

            return emptySettings;
        }

        var rawSettingsJson = File.ReadAllText(path);
        var settings = JsonConvert.DeserializeObject<Settings>(rawSettingsJson);

        return settings;
    }

    public void Save()
    {
        var path = GetSettingsPath();
        File.WriteAllText(path, JsonConvert.SerializeObject(this));
    }

    public static string GetSettingsPath()
    {
        return Path.Combine(GetSettingsBasePath(), SettingsFileName);
    }

    public static string GetSettingsBasePath()
    {
        return Path.Combine(GetProjectPath(), "ProjectSettings", "Packages",
            "cn.org.vrcd.vpm.vrchat-sdk-patcher");
    }

    public static string GetProjectWideCachePath()
    {
        var path = Path.Combine(GetProjectPath(), "Library/vrchat-sdk-patcher-a4d72eee/");
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        
        return path;
    }

    public static string GetProjectPath()
    {
        var parent = Directory.GetParent(Application.dataPath);
        if (parent == null)
        {
            // Thanks for inverting a "nice" workaround for getting project path, Unity.
            throw new InvalidOperationException("Failed to get project path");
        }
        
        return parent.ToString();
    }
}