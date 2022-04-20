using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = System.Object;

namespace UnityProBuilder.Editor.Scripts
{
    public class AndroidBuildManager : EditorWindow
    {
        [SerializeField]
        private string _telegramToken = "";
        [SerializeField]
        private string _telegramChatId = "";

        [SerializeField]
        private string _mailSendAddress = "";
        [SerializeField]
        private string _mailSendPass = "";

        [SerializeField]
        private string[] sendToList;

        [SerializeField] private string _keystorePathName = "Assets/Resources/UnityProBuilder/GPlay/user.keystore";
        private string _keystorePassword;
        [SerializeField] private string _keyAliasName;
        private string _keyAliasPassword;

        [SerializeField]
        private bool showBtn;

        [SerializeField]
        private bool notificationSend;
        [SerializeField]
        private bool telegramSend;
        [SerializeField]
        private bool mailSend;
        [SerializeField]
        private bool telegramApi = true;

        [SerializeField] private static bool _privateGlobalSettings = false;
        [SerializeField] private static string _defaultSettingsPath = "Assets/UnityProBuilder/Editor/Resources";
        [SerializeField] private static string _defaultSettingsName = "/AndroidSettings.asset";
        [SerializeField] private static AndroidBuildManager _settings;

        public static AndroidBuildManager SettingsGA
        {
            get
            {
                if (_settings == null)
                {
                    if (!_privateGlobalSettings) InitAPI();
                }
                return _settings;
            }
            private set { _settings = value; }
        }

        public static void InitSettings1()
        {
            try
            {
                _settings = (AndroidBuildManager)AssetDatabase.LoadAssetAtPath(_defaultSettingsPath + _defaultSettingsName, typeof(AndroidBuildManager));
            }
            catch (Exception ex)
            {
                Debug.Log("Could not get Settings during event validation \n" + ex.ToString());
            }
        }

        private static void InitAPI()
        {
            if (_privateGlobalSettings) return;

            InitSettings1();
            CreateSettingsInstance();
        }

        [MenuItem("UnityProBuilder/Create a new settings asset/Android")]
        private static void CreateSettingsInstance()
        {
            try
            {
#if UNITY_EDITOR
                if (_settings == null)
                {
                    //If the settings asset doesn't exist, then create it. We require a resources folder
                    //if (!Directory.Exists(Application.dataPath + "Assets/UnityProBuilder/Editor"))
                    //{
                    //    Directory.CreateDirectory(Application.dataPath + "Assets/UnityProBuilder/Editor");
                    //}
                    Debug.Log(Application.dataPath);
                    if (!Directory.Exists(Application.dataPath + "/UnityProBuilder/Editor/Resources"))
                    {
                        Directory.CreateDirectory(Application.dataPath + "/UnityProBuilder/Editor/Resources");
                        Debug.LogWarning("UnityProBuilder: Resources/UnityProBuilder folder is required to store settings. it was created ");
                    }

                    string path = _defaultSettingsPath + _defaultSettingsName;
                    if (File.Exists(path))
                    {
                        AssetDatabase.DeleteAsset(path);
                        AssetDatabase.Refresh();
                    }

                    var asset = CreateInstance<AndroidBuildManager>();
                    AssetDatabase.CreateAsset(asset, path);
                    AssetDatabase.Refresh();

                    AssetDatabase.SaveAssets();
                    Debug.LogWarning("UnityProBuilder: Settings file didn't exist and was created");
                    Selection.activeObject = asset;

                    //save reference
                    _settings = asset;
                }
#endif
            }
            catch (Exception e)
            {
                Debug.Log("Error getting Settings in InitAPI: " + e.Message);
            }
        }

        [MenuItem("UnityProBuilder/Build Project/Android Build")]
        public static void Init()
        {
            GetWindow<AndroidBuildManager>("Publishing Android Build");
        }

        private void OnGUI()
        {
            InitAPI();
            DrawTitle();

            _privateGlobalSettings = EditorGUILayout.Toggle("Private Settings", _privateGlobalSettings);
            _settings = (AndroidBuildManager)EditorGUILayout.ObjectField("Settings", _settings, typeof(AndroidBuildManager), _privateGlobalSettings);

            DrawSendNotifView();
            DrawBuildDataActionView();
        }

        private void DrawTitle()
        {
            EditorGUILayout.LabelField("Unity Pro Builder", UnityBuildGUIUtility.mainTitleStyle);
            EditorGUILayout.LabelField("by Elermond Softwares", UnityBuildGUIUtility.subTitleStyle);
            GUILayout.Space(25);
        }

        private void DrawSendNotifView()
        {
            notificationSend = EditorGUILayout.Toggle("Send Build Status Notif.", notificationSend);
            if (notificationSend)
            {
                telegramSend = EditorGUILayout.Toggle("Telegram Send", telegramSend);
                if (telegramSend)
                {
                    telegramApi = EditorGUILayout.Toggle("TelegramApi", true);

                    if (telegramApi)
                    {
                        _telegramToken = EditorGUILayout.TextField("Your Token", _telegramToken);
                        _telegramChatId = EditorGUILayout.TextField("Your Chat Id", _telegramChatId);

                        if (GUILayout.Button("Test Notification"))
                        {
                            if (ValidTelegramIds()) TelegramSendMessage(_telegramToken, _telegramChatId, "Test");
                            else Debug.LogError("Invalid Telegram Bot Token & Invalid Chat Id!");
                        }
                    }
                }

                mailSend = EditorGUILayout.Toggle("Mail Send", mailSend);
                if (mailSend)
                {
                    _mailSendAddress = EditorGUILayout.TextField("Your Send Mail", _mailSendAddress);
                    _mailSendPass = EditorGUILayout.TextField("Your Send Mail Pass", _mailSendPass);

                    if (GUILayout.Button("Test Notification"))
                    {
                        if (ValidMailAddress()) SendMail(_mailSendAddress, _mailSendPass, "Test");
                        else Debug.LogError("Invalid Mail Address & Password!");
                    }
                }
            }
        }

        private void DrawBuildDataActionView()
        {
            showBtn = EditorGUILayout.Toggle("Temporary Android Build", showBtn);
            if (!showBtn)
            {
                if (GUILayout.Button("Select Keystore"))
                {
                    SetKeystorePath();
                }

                _keystorePathName = EditorGUILayout.TextField("Keystore Path", _keystorePathName);
                _keystorePassword = EditorGUILayout.PasswordField("Keystore Password", _keystorePassword);
                _keyAliasName = EditorGUILayout.TextField("Keystore Alias Name", _keyAliasName);
                _keyAliasPassword = EditorGUILayout.PasswordField("Keystore Alias Password", _keyAliasPassword);

                if (GUILayout.Button("Build"))
                {
                    if (EditorUtility.DisplayDialog("Build Android With Keystore", "You are sure you want to Build for selected platform?", "Ok", "No"))
                    {
                        Build_Android();
                    }
                }
            }
            else
            {
                if (GUILayout.Button("Build with Temp Keystore"))
                {
                    if (EditorUtility.DisplayDialog("Build with Temp Keystore", "You are sure you want to build?", "Ok", "No"))
                    {
                        Build_Android();
                    }
                }
            }
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
                    _settings._keystorePathName = selectedPath;
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
            PlayerSettings.Android.keystoreName = _settings._keystorePathName;
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

        public static bool ValidMailAddress()
        {
            return !string.IsNullOrEmpty(_settings._mailSendAddress) && !string.IsNullOrEmpty(_settings._mailSendPass);
        }

        public void SendMail(string sendFrom = "digup.test1@gmail.com", string sendFromPass = "diguptest@2022", string mailBody = "Test Mail Body",
            string mailSubject = "Report Mail")
        {
            MailMessage mail = new MailMessage
            {
                From = new MailAddress(sendFrom)
            };

            for (int i = 0; i < sendToList.Length; i++)
            {
                mail.To.Add(sendToList[i]);
            }
            mail.Subject = mailSubject;
            mail.Body = mailBody;

            SmtpClient smtpServer = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(sendFrom, sendFromPass),
                EnableSsl = true
            };
            ServicePointManager.ServerCertificateValidationCallback =
                delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
                { return true; };
            smtpServer.Send(mail);

            //Debug.Log("Succesfully Sent! >>>" + sendTo + " Body: " + mailBody);
        }

        public static bool ValidTelegramIds()
        {
            return !string.IsNullOrEmpty(_settings._telegramToken) && !string.IsNullOrEmpty(_settings._telegramToken);
        }

        public static string TelegramSendMessage(string apilToken, string destID, string text)
        {
            string urlString = $"https://api.telegram.org/bot{apilToken}/sendMessage?chat_id={destID}&text={text}";
            WebClient webclient = new WebClient();
            return webclient.DownloadString(urlString);
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
