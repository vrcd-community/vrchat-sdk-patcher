using UnityEditor;
using VRCD.VRChatPackages.VRChatSDKPatcher.Worlds.Editor.Utils;

namespace VRCD.VRChatPackages.VRChatSDKPatcher.Worlds.Editor
{
    internal static class BetterWorldBuildActionTrigger
    {
        [MenuItem("VRChat SDK Patcher/[Worlds] Build Actions/[Build] Only", priority = 2000)]
        private static void Build()
        {
            WorldBuildActionUtils.BuildGuard();
        }

        [MenuItem("VRChat SDK Patcher/[Worlds] Build Actions/[Build] and [Test]", priority = 3000)]
        private static void BuildAndTest()
        {
            WorldBuildActionUtils.BuildAndTestGuard();
        }

        [MenuItem("VRChat SDK Patcher/[Worlds] Build Actions/[Test] Last Build", priority = 3000)]
        private static void TestLastBuild()
        {
            WorldBuildActionUtils.TestLastBuildGuard();
        }

        [MenuItem("VRChat SDK Patcher/[Worlds] Build Actions/[Build] and [Upload]", priority = 4000)]
        private static void BuildAndUpload()
        {
            WorldBuildActionUtils.BuildAndUploadGuard();
        }

        [MenuItem("VRChat SDK Patcher/[Worlds] Build Actions/[Upload] Last Build", priority = 4000)]
        private static void UploadLastBuild()
        {
            WorldBuildActionUtils.UploadLastBuildGuard();
        }
    }
}
