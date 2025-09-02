using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BestHTTP;
using BestHTTP.JSON;
using HarmonyLib;
using UnityEngine;
using VRC.Core;

namespace VRCD.VRChatPackages.VRChatSDKPatcher.Editor.Patchers;

internal class RemoteConfigCachePatcher : IPatcher
{
    public const string ApiConfigCacheFileName = "vrchat-api-config.json";
    public const string ApiConfigCacheTimestampFileName = "vrchat-api-config-timestamp";

    public const int CacheValiditySeconds = 3600; // 1 hour

    private static readonly FieldInfo? ConfigField = AccessTools.Field(typeof(BaseConfig), "config");

    public void Patch(Harmony harmony)
    {
        var original = AccessTools.Method(typeof(RemoteConfig), "FetchConfig");
        var prefix = AccessTools.Method(typeof(RemoteConfigCachePatcher), nameof(FetchConfigPrefix));

        if (original == null)
        {
            Debug.LogError(
                "[VRChat SDK Patcher] Failed to get MethodInfo of RemoteConfig.FetchConfig(), Remote Config Cache Patch won't work.");
            return;
        }

        if (ConfigField == null)
        {
            Debug.LogError(
                "[VRChat SDK Patcher] Failed to get FieldInfo of BaseConfig.config, Remote Config Cache Patch won't work.");
        }

        harmony.Patch(original, new HarmonyMethod(prefix));
    }

    private static bool FetchConfigPrefix(object? __instance, Action? onFetched, Action? onError)
    {
        if (!PatcherMain.PatcherSettings.CacheRemoteConfig)
            return true;
        
        if (__instance == null)
        {
            Debug.LogError(
                "[VRChat SDK Patcher] Failed to get instance of RemoteConfig which shouldn't happen. Execute the original method.");
            return true;
        }

        var cacheBasePath = Settings.GetProjectWideCachePath();
        var apiSettingsCachePath = Path.Combine(cacheBasePath, ApiConfigCacheFileName);
        var apiSettingsCacheTimestampPath = Path.Combine(cacheBasePath, ApiConfigCacheTimestampFileName);

        if (TryGetCachedConfig() is { } cachedConfig)
        {
#pragma warning disable CS8602
            ConfigField.SetValue(__instance, cachedConfig);
#pragma warning restore CS8602
            onFetched?.Invoke();
            return false;
        }

        var responseContainer = new ApiDictContainer();
        responseContainer.OnSuccess += container =>
        {
            if (container is not ApiDictContainer dictContainer)
            {
                Debug.LogError(
                    "[VRChat SDK Patcher] Api Config Response Container is null or not ApiDictContainer. Please report this to the developer and disable this patch.");
                onError?.Invoke();
                return;
            }

            var responseDict = dictContainer.ResponseDictionary;

            var json = ConfigToJson(responseDict);
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            File.WriteAllText(apiSettingsCachePath, json);
            File.WriteAllText(apiSettingsCacheTimestampPath, timestamp);

            var config = ProcessConfig(dictContainer.ResponseDictionary);
#pragma warning disable CS8602
            ConfigField.SetValue(__instance, config);
#pragma warning restore CS8602

            Debug.Log("[VRChat SDK Patcher] Fetched and cached new api config file.");

            onFetched?.Invoke();
        };

        responseContainer.OnError += container =>
        {
            Debug.LogError(
                $"[VRChat SDK Patcher] Failed to fetch api config file, will use cached version if available. (code: {container.Code} msg: {container.Text} err: {container.Error})");

            var rawConfig = TryGetCachedConfig();
            if (rawConfig is not null)
            {
                Debug.Log("[VRChat SDK Patcher] Using stale cached api config file due to fetch error.");
#pragma warning disable CS8602
                ConfigField.SetValue(__instance, rawConfig);
#pragma warning restore CS8602
            }

            onError?.Invoke();
        };

        API.SendRequest("config", HTTPMethods.Get, responseContainer, authenticationRequired: false, disableCache: true,
            priority: UpdateDelegator.JobPriority.ApiBlocking);

        return false;
    }

    private static Dictionary<string, object>? TryGetCachedConfig(bool ignoreExpiry = false)
    {
        var cacheBasePath = Settings.GetProjectWideCachePath();
        var apiSettingsCachePath = Path.Combine(cacheBasePath, ApiConfigCacheFileName);
        var apiSettingsCacheTimestampPath = Path.Combine(cacheBasePath, ApiConfigCacheTimestampFileName);

        if (!File.Exists(apiSettingsCachePath) || !File.Exists(apiSettingsCacheTimestampPath))
        {
            Debug.Log("[VRChat SDK Patcher] No cached api config file found.");
            return null;
        }

        var timestampStr = File.ReadAllText(apiSettingsCacheTimestampPath);

        if (!long.TryParse(timestampStr, out var timestamp))
        {
            Debug.LogWarning("[VRChat SDK Patcher] Cached api config file is corrupted. (invalid timestamp)");
            return null;
        }

        if (!ignoreExpiry)
        {
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var age = currentTime - timestamp;

            if (age >= CacheValiditySeconds)
            {
                Debug.Log("[VRChat SDK Patcher] Cached api config file is stale, fetching new one.");
                return null;
            }   
        }
        else
        {
            Debug.Log("[VRChat SDK Patcher] Ignoring cache expiry as requested.");
        }

        var configRaw = File.ReadAllText(apiSettingsCachePath);

        IReadOnlyDictionary<string, Json.Token>? preProcessedConfig;
        try
        {
            preProcessedConfig = TryJsonToConfig(configRaw);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[VRChat SDK Patcher] Cached api config file is corrupted. (json deserialize exception)");
            Debug.LogException(ex);
            return null;
        }

        if (preProcessedConfig == null)
        {
            Debug.LogWarning("[VRChat SDK Patcher] Cached api config file is corrupted. (null json)");
            return null;
        }

        var config = ProcessConfig(preProcessedConfig);
        Debug.Log("[VRChat SDK Patcher] Using cached api config file.");
        return config;
    }

    private static Dictionary<string, object> ProcessConfig(IReadOnlyDictionary<string, Json.Token> config)
    {
        return config.ToDictionary(
            (Func<KeyValuePair<string, Json.Token>, string>)(kv => kv.Key),
            (Func<KeyValuePair<string, Json.Token>, object>)(kv => kv.Value.Value));
    }

    private static string ConfigToJson(IReadOnlyDictionary<string, Json.Token> config)
    {
        return Json.Encode(config, true);
    }

    private static IReadOnlyDictionary<string, Json.Token>? TryJsonToConfig(string json)
    {
        return Json.Decode(json).Object;
    }
}