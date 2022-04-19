using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class IosBuild : System.Object
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
