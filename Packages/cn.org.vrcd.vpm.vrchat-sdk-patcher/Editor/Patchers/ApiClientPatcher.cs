using System;
using System.Reflection;
using BestHTTP;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using VRC.Core;

namespace VRCD.VRChatPackages.VRChatSDKPatcher.Editor.Patchers;

[HarmonyPatch(typeof(API), nameof(API.PopulateHTTPRequestHeaders))]
internal class ApiClientPatcher
{
    [CanBeNull] private static object _proxyScriptEngine;

    [CanBeNull] private static MethodInfo _getWebProxyDataMethod;

    [CanBeNull] private static Type _webProxyDataType;

    [CanBeNull] private static FieldInfo _proxyAddressField;

    private static void Postfix(HTTPRequest request)
    {
        request.RemoveHeader("X-MacAddress");

        if (!PatcherMain.PatcherSettings.UseProxy)
            return;

        if (string.IsNullOrWhiteSpace(PatcherMain.PatcherSettings.HttpProxyUri))
        {
            if (Environment.OSVersion.Platform == PlatformID.MacOSX)
            {
                Debug.LogWarning("Use System Proxy on MacOSX is not supported yet. Please set a proxy manually.");
                return;
            }

            try
            {
                // Get the proxy address from the system settings
                var proxyHostAddressesField = GetProxyAddressField();

                var getWebProxyDataMethod = GetProxyScriptEngineMethod();
                var scriptEngine = GetProxyScriptEngine();

                var webProxyData = getWebProxyDataMethod.Invoke(scriptEngine, null);

                if (webProxyData == null)
                    return;

                var proxyAddress = proxyHostAddressesField.GetValue(webProxyData) as Uri;

                if (proxyAddress == null)
                    return;

                request.Proxy = new HTTPProxy(proxyAddress);
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to get system proxy for VRChat API client");
                Debug.LogException(ex);
            }

            return;
        }

        request.Proxy = new HTTPProxy(new Uri(PatcherMain.PatcherSettings.HttpProxyUri));
    }

    private static object GetProxyScriptEngine()
    {
        if (_proxyScriptEngine != null && _proxyScriptEngine.GetType().Name == "AutoWebProxyScriptEngine")
            return _proxyScriptEngine;

        var proxyScriptEngineType = AccessTools.TypeByName("AutoWebProxyScriptEngine");
        var webProxyType = AccessTools.TypeByName("WebProxy");
        var webProxy = Activator.CreateInstance(webProxyType);

        _proxyScriptEngine = Activator.CreateInstance(proxyScriptEngineType, webProxy, true);
        return _proxyScriptEngine;
    }

    private static MethodInfo GetProxyScriptEngineMethod()
    {
        if (_getWebProxyDataMethod != null && _getWebProxyDataMethod.Name == "GetWebProxyData")
            return _getWebProxyDataMethod;

        var proxyScriptEngineType = AccessTools.TypeByName("AutoWebProxyScriptEngine");
        _getWebProxyDataMethod = AccessTools.Method(proxyScriptEngineType, "GetWebProxyData");

        return _getWebProxyDataMethod;
    }

    private static Type GetWebProxyDataType()
    {
        if (_webProxyDataType != null && _webProxyDataType.Name == "WebProxyData")
            return _webProxyDataType;

        _webProxyDataType = AccessTools.TypeByName("WebProxyData");
        return _webProxyDataType;
    }

    private static FieldInfo GetProxyAddressField()
    {
        if (_proxyAddressField != null && _proxyAddressField.Name == "proxyAddress")
            return _proxyAddressField;

        _proxyAddressField = AccessTools.Field(GetWebProxyDataType(), "proxyAddress");
        return _proxyAddressField;
    }
}