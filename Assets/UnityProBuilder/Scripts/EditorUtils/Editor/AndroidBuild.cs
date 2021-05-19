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
        [SerializeField] private static string _keystorePathName = "Assets/Template/GPlay/user.keystore";
        private static string _keystorePassword;
        [SerializeField] private static string _keyAliasName;
        private static string _keyAliasPassword;

        private bool showBtn;
        private bool telegramSend;

        [MenuItem("UnityProBuilder/Build Project/Android Build/Publishing")]
        public static void Init()
        {
            GetWindow<AndroidBuild>("Publishing Android Build");
        }
        
        private void OnGUI()
        {
            DrawTitle();

            telegramSend = EditorGUILayout.Toggle("Send After Build Status to telegram", telegramSend);

            showBtn = EditorGUILayout.Toggle("Temporary Build", showBtn);
            if (!showBtn)
            {
                if (GUILayout.Button("Select Keystore"))
                {
                    SetKeystorePath();
                }

                _keystorePathName = EditorGUILayout.TextField("Keystore Path", _keystorePathName);
                _keystorePassword = EditorGUILayout.PasswordField("Keystore Password", _keystorePassword);
                //Alias Data
                _keyAliasName = EditorGUILayout.TextField("Keystore Alias Name", _keyAliasName);
                _keyAliasPassword = EditorGUILayout.PasswordField("Keystore Alias Password", _keyAliasPassword);

                if (GUILayout.Button("Build All"))
                {
                    if (EditorUtility.DisplayDialog("Build All", "You are sure you want to Build for all selected platforms?", "Ok", "No"))
                    {
                        Build_Android();
                        WindowsBuild();
                    }
                }
            }
            else
            {
                if (GUILayout.Button("Build with Temp Keystore"))
                {
                    if(EditorUtility.DisplayDialog("Build with Temp Keystore", "You are sure you want to build?", "Ok", "No"))
                    {
                        Build_Android();
                    }
                }
            }
        }
        
        private void DrawTitle()
        {
            EditorGUILayout.LabelField("Unity Pro Builder", UnityBuildGUIUtility.mainTitleStyle);
            EditorGUILayout.LabelField("by Elermond Softwares", UnityBuildGUIUtility.subTitleStyle);
            GUILayout.Space(25);
        }
        
        //TODO: Clear unused functions
        private static void SetKeystorePath()
        {
            var path = EditorUtility.OpenFilePanel("Select a valid Keystore file", "", "keystore");
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

//        public static void Build_iOS()
//        {
//            var path = Environment.GetEnvironmentVariable("BUILD_PATH");

//            if (string.IsNullOrEmpty(path)) return;
            
//            PreBuild();
//#if UNITY_IOS
//            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
//            PlayerSettings.iOS.appleDeveloperTeamID = "HJ453WUND2";
//#endif
//            var b = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes
//                , path, BuildTarget.iOS, BuildOptions.None);
            
//            //PostBuildReport(b);
//        }
        
        private static void Build_Android()
        {
            BumpAndroidBundleVersion();
            PreBuildAndroid();

            var path = EditorUtility.SaveFilePanel("Choose file location and set application name!", Path.GetDirectoryName(Application.dataPath),
                PlayerSettings.productName + "_" + PlayerSettings.bundleVersion, "apk");
            if (string.IsNullOrEmpty(path)) return;

            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.All;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = _keystorePathName;
            PlayerSettings.Android.keystorePass = "marsdigger";
            PlayerSettings.Android.keyaliasName = "mars";
            PlayerSettings.Android.keyaliasPass = "digger";

            var b = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes
                , path, BuildTarget.Android, BuildOptions.None);

            // Run the game (Process class from System.Diagnostics).
            Process proc = new Process();
            proc.StartInfo.FileName = path;
            proc.Start();

            PostBuildReport(b);
        }
        
        private static void WindowsBuild()
        {
            // Get filename.
            var path = EditorUtility.SaveFolderPanel("Choose Location of Built Game", "", "");
            if (string.IsNullOrEmpty(path)) return;
            
            //string[] levels = new string[] {"Assets/Scene1.unity", "Assets/Scene2.unity"};

            // Build player.
            BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, path, BuildTarget.StandaloneWindows, BuildOptions.None);

            // Copy a file from the project folder to the build folder, alongside the built game.
            //FileUtil.CopyFileOrDirectory("Assets/Templates/Readme.txt", path + "Readme.txt");

            // Run the game (Process class from System.Diagnostics).
            // Process proc = new Process();
            // proc.StartInfo.FileName = path + "/BuiltGame.exe";
            // proc.Start();
        }

        // Bump version number in PlayerSettings.bundleVersion
        private static void BumpAndroidBundleVersion()
        {
            if (float.TryParse(PlayerSettings.bundleVersion, out float bundleVersion))
            {
                bundleVersion += 0.01f;
                PlayerSettings.bundleVersion = bundleVersion.ToString();
                PlayerSettings.Android.bundleVersionCode = (int)bundleVersion;
            }

            if(PlayerSettings.Android.bundleVersionCode <= 0)
            {
                PlayerSettings.Android.bundleVersionCode = 1;
            }
            else
            {
                int androidBundleVer = PlayerSettings.Android.bundleVersionCode;
                PlayerSettings.Android.bundleVersionCode = androidBundleVer+1;
            }
        }

        //TODO: Clear unused functions.
        private static void PreBuildAndroid()
        {
            Assert.IsTrue(EditorBuildSettings.scenes[0].path.Contains("FirstScene"), 
             "First Scene should be FirstScene.unity");

            Assert.IsTrue(PlayerSettings.applicationIdentifier.Contains("com."), "Bundle ID should be set!");

            //var lastBuildNumber = int.Parse(PlayerSettings.bundleVersion);
            //var number = int.Parse("0." + (lastBuildNumber++));
            //PlayerSettings.bundleVersion = $"{number}";

            ////#if UNITY_ANDROID
            //            PlayerSettings.Android.bundleVersionCode = number;
            ////#endif

            ////#if UNITY_IOS
            //            PlayerSettings.iOS.buildNumber = number.ToString();
            //#endif
        }

        private static void PostBuildReport(BuildReport result)
        {
            var fileOnFinish = result.strippingInfo;
            Debug.Log("All build Log: " + fileOnFinish);
            
#if UNITY_ANDROID
            Debug.Log("BuildNumber: " + PlayerSettings.Android.bundleVersionCode);
#endif

#if UNITY_IOS
            Debug.Log("BuildNumber: " + PlayerSettings.iOS.buildNumber);
#endif

            if (result.summary.result == BuildResult.Succeeded)
            {
                WriteToFile("AndroidLogSucces", fileOnFinish.ToString());
            }
            else
            {
                WriteToFile("AndroidLogError", fileOnFinish.ToString());
            }
        }

        /// <summary>
        /// <param> Writes the string message into the logFilesGPSUnity.txt in the internal storage\android\data\com.armis.arimarn\files\</para>
        /// You need to write a '\n' in the end or beginning of each message, otherwise, the message will be printed in a row.
        /// <param name="newFileName">String to file new name (Only set name) (Auto extension set > .txt)</param>
        /// <param name="messageToAdd">String to print in the file</param>
        /// </summary>
        public static void WriteToFile(string newFileName, string messageToAdd)
        {
            var gpsFilePath = Application.persistentDataPath + "/" + newFileName + ".txt";

            //if you want to empty the file every time the app starts, only delete and create a new one.
            //if file exists
            if (File.Exists(gpsFilePath))
            {
                //delete file
                try
                {
                    File.Delete(gpsFilePath);
                    Debug.Log("[Utils Script]: " + newFileName + ".txt Log Deleted Successfully!");
                }
                catch (System.Exception e)
                {
                    Debug.LogError("[Utils Script]: Cannot delete " + newFileName + ".txt Log - Exception: " + e);
                }
            }

            //Write log data in created file
            try
            {
                //Create the stream writer to the specified file path with specified fileName at end
                StreamWriter fileWriter = new StreamWriter(gpsFilePath, true);

                //write the string into the file
                fileWriter.Write(messageToAdd + '\n');

                // close the Stream Writer
                fileWriter.Close();
            }
            catch (System.Exception e)
            {
                Debug.LogError("[Utils Script]: Cannot write in the GPS File Log - Exception: " + e);
            }
        }

    }
}
