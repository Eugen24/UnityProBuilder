using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif
using System.IO;

public class PostBuildStep
{
    // Set the IDFA request description:
    const string k_TrackingDescription = "This identifier will be used to deliver personalized ads, maps and offers to you.";
    const string _NSLocationWhenInUseUsageDescription = "This is for targeting users for different map types based on their region.";
    const bool _GADIsAdManagerApp = true;

    [PostProcessBuild(0)]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string pathToXcode)
    {
        if (buildTarget == BuildTarget.iOS)
        {
            AddPListValues(pathToXcode);
        }
    }

    // Implement a function to read and write values to the plist file:
    static void AddPListValues(string pathToXcode)
    {
        // Retrieve the plist file from the Xcode project directory:
        string plistPath = pathToXcode + "/Info.plist";
        PlistDocument plistObj = new PlistDocument();

        // Read the values from the plist file:
        plistObj.ReadFromString(File.ReadAllText(plistPath));

        // Set values from the root object:
        PlistElementDict plistRoot = plistObj.root;

        // Set the description key-value in the plist:
        plistRoot.SetString("NSUserTrackingUsageDescription", k_TrackingDescription);
        plistRoot.SetString("NSLocationWhenInUseUsageDescription", _NSLocationWhenInUseUsageDescription);
        plistRoot.SetBoolean("GADIsAdManagerApp", true);

        // Save changes to the plist:
        File.WriteAllText(plistPath, plistObj.WriteToString());
    }
}