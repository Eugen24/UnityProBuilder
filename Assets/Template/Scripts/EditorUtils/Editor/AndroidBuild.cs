using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEditor.Build.Reporting;
using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = System.Object;

namespace Template.Scripts.EditorUtils.Editor
{
    public class AndroidBuild : EditorWindow
    {
        private static string _keystorePathName = "Assets/Template/GPlay/user.keystore";
        private static string _keystorePassword;
        private static string _keyAliasName;
        private static string _keyAliasPassword;

        [MenuItem("UnityProBuilder/Build Project/Android Build/Publishing")]
        public static void Init()
        {
            GetWindow<AndroidBuild>("Publishing Android Build");
        }
        
        private void OnGUI()
        {
            DrawTitle();
            
            _keystorePathName = EditorGUILayout.TextField("Keystore Path Name", _keystorePathName);
            if (GUILayout.Button("Select Keystore"))
            {
                SetKeystorePath();
            }
            _keystorePassword = EditorGUILayout.TextField("Keystore Path Name", _keystorePassword);
            _keyAliasName = EditorGUILayout.TextField("Keystore Path Name", _keyAliasName);
            _keyAliasPassword = EditorGUILayout.TextField("Keystore Path Name", _keyAliasPassword);
            
            if (GUILayout.Button("Build All"))
            {
                Build_Android();
                WindowsBuild();
            }
        }
        
        private void DrawTitle()
        {
            EditorGUILayout.LabelField("Unity Pro Builder", UnityBuildGUIUtility.mainTitleStyle);
            EditorGUILayout.LabelField("by Elermond Softwares", UnityBuildGUIUtility.subTitleStyle);
            GUILayout.Space(25);
        }
        
        private void DrawBuildButtons()
        {
            int totalBuildCount = 2;

            EditorGUI.BeginDisabledGroup(totalBuildCount < 1);
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Perform All Enabled Builds (" + totalBuildCount + " Builds)", GUILayout.ExpandWidth(true), GUILayout.MinHeight(30)))
            {
                // EditorApplication.delayCall += BuildProject.BuildAll;
            }
            GUI.backgroundColor = UnityBuildGUIUtility.defaultBackgroundColor;
            EditorGUI.EndDisabledGroup();
        }
        
        //TODO: Clear unused functions
        static void SetKeystorePath()
        {
            string path = EditorUtility.OpenFilePanel("Select a valid Keystore file", "", "keystore");
            if (path.Length != 0)
            {
                string selectedPath = path;

                if (!selectedPath.Contains(".keystore"))
                {
                    EditorUtility.DisplayDialog("Select Keystore File", "You need to select a valid keystore file (Example: $ProductName.keystore)!", "OK");
                    return;
                }
                else
                {
                    selectedPath = selectedPath.Replace("/Assets", "");
                    _keystorePathName = selectedPath;
                }

                // Just in case we are saving to the asset folder, tell Unity to scan for modified or new assets
                AssetDatabase.Refresh();
            }
        }

        public static void Build_iOS()
        {
            var path = Environment.GetEnvironmentVariable("BUILD_PATH");
            if (string.IsNullOrEmpty(path))
                return;
            
            PreBuild();
#if UNITY_IOS
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            PlayerSettings.iOS.appleDeveloperTeamID = "HJ453WUND2";
#endif
            var b = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes
                , path, BuildTarget.iOS, BuildOptions.None);
            
            //PostBuildReport(b);
        }
        public static void Build_Android()
        {
            string path = EditorUtility.SaveFolderPanel("Choose Location of Built Game", "", "");
            if (string.IsNullOrEmpty(path))
                return;

            PreBuild();
#if UNITY_ANDROID

            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.All;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = _keystorePathName;
            PlayerSettings.Android.keystorePass = "marsdigger";
            PlayerSettings.Android.keyaliasName = "mars";
            PlayerSettings.Android.keyaliasPass = "digger";
#endif
            var b = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes
                , path + "/BuiltGameAndroid.apk", BuildTarget.Android, BuildOptions.None);
            //PostBuildReport(b);
        }
        
        private static void WindowsBuild()
        {
            // Get filename.
            string path = EditorUtility.SaveFolderPanel("Choose Location of Built Game", "", "");
            string[] levels = new string[] {"Assets/Scene1.unity", "Assets/Scene2.unity"};

            // Build player.
            BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, path + "/BuiltGameWindows.exe", BuildTarget.StandaloneWindows, BuildOptions.None);

            // Copy a file from the project folder to the build folder, alongside the built game.
            //FileUtil.CopyFileOrDirectory("Assets/Templates/Readme.txt", path + "Readme.txt");

            // Run the game (Process class from System.Diagnostics).
            // Process proc = new Process();
            // proc.StartInfo.FileName = path + "/BuiltGame.exe";
            // proc.Start();
        }

        private static void PreBuild()
        {
            Assert.IsTrue(EditorBuildSettings.scenes[0].path.Contains("FirstScene"), 
             "First Scene should be FirsScene.unity");
            var buildNumber = Environment.GetEnvironmentVariable("BUILD_NUMBER");

#if GEEKON_LIONSTUDIO
            PublisherIntegrator.SetIds();
            Assert.IsNotEmpty(LionStudios.LionSettings.Facebook.AppId, "Facebook is not set");
            Assert.IsNotEmpty(LionStudios.LionSettings.Adjust.Token, "Adjust is not set");
#endif

            var number = int.Parse(buildNumber ?? "0");
            PlayerSettings.bundleVersion = $"1.{number}";
            
            Assert.IsTrue(PlayerSettings.applicationIdentifier.Contains("com."), "Bundle ID should be set!");
            
#if UNITY_ANDROID
            PlayerSettings.Android.bundleVersionCode = number;
#endif

#if UNITY_IOS
            PlayerSettings.iOS.buildNumber = number.ToString();
#endif
        }

        private static void PostBuildReport(BuildReport result)
        {
            var fileOnFinish = Environment.GetEnvironmentVariable("UNITY_STATUS");
            Debug.Log(fileOnFinish);
            
#if UNITY_ANDROID
            Debug.Log("BuildNumber: " + PlayerSettings.Android.bundleVersionCode);
#endif

#if UNITY_IOS
            Debug.Log("BuildNumber: " + PlayerSettings.iOS.buildNumber);
#endif

            if (result.summary.result == BuildResult.Succeeded)
            {
                File.WriteAllText(fileOnFinish, "Success");
            }
            else
            {
                File.WriteAllText(fileOnFinish, "Fail");
            }
        }
    }
}
