using HarmonyLib;

namespace VRCD.VRChatPackages.VRChatSDKPatcher.Editor.Patchers;

internal interface IPatcher
{
    public void Patch(Harmony harmony);
}