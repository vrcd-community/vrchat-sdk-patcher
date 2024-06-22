using System;
using BestHTTP;
using HarmonyLib;
using VRC;
using VRC.Core;

namespace VRCD.VRChatPackages.VRChatSDKPatcher.Editor.Patchers
{
    [HarmonyPatch(typeof(API), nameof(API.PopulateHTTPRequestHeaders))]
    internal class ApiClientPatcher
    {
        private static void Postfix(HTTPRequest request)
        {
            request.RemoveHeader("X-MacAddress");

            if (PatcherMain.PatcherSettings.UseProxy)
                request.Proxy = new HTTPProxy(new Uri(PatcherMain.PatcherSettings.HttpProxyUri));
        }
    }
}