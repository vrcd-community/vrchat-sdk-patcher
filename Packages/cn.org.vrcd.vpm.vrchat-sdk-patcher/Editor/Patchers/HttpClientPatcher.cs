using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using VRC.SDKBase.Editor.Api;

namespace VRCD.VRChatPackages.VRChatSDKPatcher.Editor.Patchers
{
    [HarmonyPatch(typeof(VRCApi), "GetClient")]
    internal class UseProxyPatcher
    {
        [CanBeNull] private static HttpClient _httpClient;
        [CanBeNull] private static HttpClientHandler _httpClientHandler;

        [CanBeNull] private static WebProxy _webProxy;

        private static bool Prefix(ref HttpClient __result, string url)
        {
            if (_httpClient != null && _httpClientHandler != null)
            {
                _httpClientHandler.Proxy = GetProxy();

                __result = _httpClient;
                return false;
            }

            var cookies = GetCookies(url);
            var handler = new HttpClientHandler
            {
                UseProxy = PatcherMain.PatcherSettings.UseProxy,
                CookieContainer = cookies
            };

            if (!string.IsNullOrWhiteSpace(PatcherMain.PatcherSettings.HttpProxyUri) &&
                PatcherMain.PatcherSettings.UseProxy)
            {
                handler.Proxy = GetProxy();
            }

            var httpClient = new HttpClient(handler);

            var headers = GetHeaders();

            headers.Remove("X-MacAddress"); // Don't collect my device id

            foreach (var (headerName, headerValue) in headers)
            {
                httpClient.DefaultRequestHeaders.Add(headerName, headerValue);
            }

            _httpClient = httpClient;
            _httpClientHandler = handler;

            __result = httpClient;

            return false; // Skip original GetHttpClient method
        }

        private static CookieContainer GetCookies(string url)
        {
            var getCookiesMethod =
                typeof(VRCApi).GetMethod("GetCookies", BindingFlags.NonPublic | BindingFlags.Static);

            return (CookieContainer)getCookiesMethod.Invoke(null, new object[] { url });
        }

        private static Dictionary<string, string> GetHeaders()
        {
            var headersFeild =
                typeof(VRCApi).GetField("Headers", BindingFlags.NonPublic | BindingFlags.Static);

            return (Dictionary<string, string>)headersFeild.GetValue(null);
        }

        [CanBeNull]
        private static WebProxy GetProxy()
        {
            if (string.IsNullOrWhiteSpace(PatcherMain.PatcherSettings.HttpProxyUri) ||
                !PatcherMain.PatcherSettings.UseProxy) return null;

            if (_webProxy != null)
            {
                _webProxy.Address = new Uri(PatcherMain.PatcherSettings.HttpProxyUri);
            }
            else
            {
                _webProxy = new WebProxy(PatcherMain.PatcherSettings.HttpProxyUri);
            }

            return _webProxy;
        }
    }
}
