using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace VRCD.VRChatPackages.VRChatSDKPatcher.Editor.Patchers
{
    internal class VRCCopyrightAgreementPatcher : IPatcher
    {
        private const string CopyrightAgreementTypeName = "VRC.SDKBase.VRCCopyrightAgreement";

        private static bool Prefix(ref Task<bool> __result)
        {
            __result = Task.FromResult(true);

            Debug.LogWarning(
                "By using this patch, you agree that you have already read and accepted the VRChat copyright agreement.");

            return false;
        }

        public void Patch(Harmony harmony)
        {
            if (!PatcherMain.PatcherSettings.SkipCopyrightAgreement)
                return;

            var copyrightAgreementType = AccessTools.TypeByName(CopyrightAgreementTypeName);

            if (copyrightAgreementType == null)
            {
                Debug.LogWarning(
                    "Failed to find VRCCopyrightAgreement type, it may because you are using a old SDK or SDK changed");
                return;
            }

            var hasAgreementMethod = AccessTools.Method(copyrightAgreementType, "HasAgreement");

            if (hasAgreementMethod == null)
            {
                Debug.LogWarning(
                    "Failed to find HasAgreement method");
                return;
            }

            var patchMethod = AccessTools.Method(typeof(VRCCopyrightAgreementPatcher), nameof(Prefix));

            harmony.Patch(hasAgreementMethod, new HarmonyMethod(patchMethod));
        }
    }
}
