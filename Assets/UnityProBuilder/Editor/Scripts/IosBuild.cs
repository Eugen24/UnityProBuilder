using System;
using UnityEditor;

namespace UnityProBuilder.Editor.Scripts
{
    public class IosBuild : object
    {
        public static void Build_iOS()
        {
            var path = Environment.GetEnvironmentVariable("BUILD_PATH");

            if (string.IsNullOrEmpty(path)) return;

            //PreBuild();
#if UNITY_IOS
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            PlayerSettings.iOS.appleDeveloperTeamID = "HJ453WUND2";
#endif
            var b = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes
                , path, BuildTarget.iOS, BuildOptions.None);

            //PostBuildReport(b);
        }
    }
}
