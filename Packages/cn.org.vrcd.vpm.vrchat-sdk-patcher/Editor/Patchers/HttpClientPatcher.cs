using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using VRC.SDKBase.Editor.Api;

namespace VRCD.VRChatPackages.VRChatSDKPatcher.Editor.Patchers
{
    [HarmonyPatch(typeof(VRCApi), "GetClient")]
    internal class UseProxyPatcher
    {
        private static bool Prefix(ref HttpClient __result, string url)
        {
            var cookies = GetCookies(url);
            var handler = new HttpClientHandler
            {
                UseProxy = PatcherMain.PatcherSettings.UseProxy,
                Proxy = string.IsNullOrWhiteSpace(PatcherMain.PatcherSettings.HttpProxyUri)
                    ? null
                    : new WebProxy(PatcherMain.PatcherSettings.HttpProxyUri),
                CookieContainer = cookies
            };

            var httpClient = new HttpClient(handler);

            var headers = GetHeaders();

            headers.Remove("X-MacAddress"); // Don't collect my device id

            foreach (var (headerName, headerValue) in headers)
            {
                httpClient.DefaultRequestHeaders.Add(headerName, headerValue);
            }

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
    }
}
