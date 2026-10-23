#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ParkIt.Editor
{
    public static class BuildAPKCommand
    {
        [MenuItem("Park It/Build Android APK (1-Click)", false, 10)]
        public static void BuildAndroidAPK()
        {
            string buildFolder = Path.Combine(Directory.GetCurrentDirectory(), "Builds/Android");
            if (!Directory.Exists(buildFolder))
            {
                Directory.CreateDirectory(buildFolder);
            }

            string apkPath = Path.Combine(buildFolder, "ParkIt_Mobile.apk");

            Debug.Log("[Park It] Starting automated Android APK build in Unity 6...");
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                throw new System.InvalidOperationException("Install Android Build Support for this Unity Editor before building.");
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                throw new System.InvalidOperationException("Could not switch to the Android build target.");
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.Android.bundleVersionCode = 1;

            string[] scenes = new string[] { "Assets/Scenes/ParkIt_MobileMain.unity" };

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[Park It] APK Build Succeeded! Size: {summary.totalSize / (1024 * 1024)} MB");
                EditorUtility.DisplayDialog("APK Build Complete", $"Successfully generated Android APK at:\n{apkPath}", "Open Folder");
                EditorUtility.RevealInFinder(apkPath);
            }
            else
            {
                Debug.LogError($"[Park It] APK Build Failed with {summary.totalErrors} errors.");
                EditorUtility.DisplayDialog("APK Build Failed", "Check Unity Console for details.", "OK");
            }
        }
    }
}
#endif
