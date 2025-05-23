﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using BestHTTP;
using HarmonyLib;
using JetBrains.Annotations;
using VRC.SDKBase.Editor.Api;

namespace VRCD.VRChatPackages.VRChatSDKPatcher.Editor.Patchers
{
    [HarmonyPatch(typeof(VRCApi), "GetClient")]
    internal class UseProxyPatcher
    {
        [CanBeNull] private static HttpClient _httpClient;
        [CanBeNull] private static HttpClientHandler _httpClientHandler;

        private static bool Prefix(ref HttpClient __result, string url)
        {
            if (_httpClient != null && _httpClientHandler != null)
            {
                __result = _httpClient;
                return false;
            }

            _httpClient?.Dispose();
            _httpClient = null;
            _httpClientHandler = null;

            var cookies = GetCookies();
            var handler = new HttpClientHandler
            {
                UseProxy = PatcherMain.PatcherSettings.UseProxy,
                CookieContainer = cookies
            };

            handler.Proxy = new PatcherWebProxy();

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

        private static CookieContainer GetCookies()
        {
            var getCookiesMethod =
                typeof(VRCApi).GetMethod("GetCookies", BindingFlags.NonPublic | BindingFlags.Static);

            var url = GetVrcCookieBaseUrl();

            return (CookieContainer)getCookiesMethod.Invoke(null, new object[] { url });
        }

        private static Uri GetVrcCookieBaseUrl()
        {
            return AccessTools.Field(typeof(VRCApi), "VRC_COOKIE_BASE_URL").GetValue(null) as Uri;
        }

        private static Dictionary<string, string> GetHeaders()
        {
            var headersFeild =
                typeof(VRCApi).GetField("Headers", BindingFlags.NonPublic | BindingFlags.Static);

            return (Dictionary<string, string>)headersFeild.GetValue(null);
        }
    }

    internal class PatcherWebProxy : IWebProxy
    {
        public Uri GetProxy(Uri destination)
        {
            if (!PatcherMain.PatcherSettings.UseProxy)
                return destination;

            return GetWebProxyCore()?.GetProxy(destination) ?? destination;
        }

        public bool IsBypassed(Uri host)
        {
            if (!PatcherMain.PatcherSettings.UseProxy)
                return true;

            return GetWebProxyCore()?.IsBypassed(host) ?? true;

        }

        [CanBeNull]
        private static IWebProxy GetWebProxyCore()
        {
            if (!PatcherMain.PatcherSettings.UseProxy) return null;

            if (string.IsNullOrWhiteSpace(PatcherMain.PatcherSettings.HttpProxyUri))
            {
                return WebRequest.GetSystemWebProxy();
            }

            return new WebProxy(PatcherMain.PatcherSettings.HttpProxyUri);
        }

        public ICredentials Credentials { get; set; }
    }
}
