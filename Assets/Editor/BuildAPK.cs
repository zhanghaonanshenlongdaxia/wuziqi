using UnityEditor;
using UnityEngine;
using System.IO;

public class BuildAPK
{
    [MenuItem("Build/Build Android APK")]
    public static void Build()
    {
        string[] scenes = { "Assets/Scenes/SampleScene.unity" };
        string buildPath = "Builds/Android/XianMiaoWuZiQi.apk";
        
        // 确保目录存在
        string dir = Path.GetDirectoryName(buildPath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        // 设置 Android 签名
        PlayerSettings.Android.keystoreName = "canglongqixiu.keystore";
        PlayerSettings.Android.keyaliasName = "canglongqixiu";
        PlayerSettings.Android.keystorePass = "canglongqixiu";
        PlayerSettings.Android.keyaliasPass = "canglongqixiu";

        var report = BuildPipeline.BuildPlayer(options);
        
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("Build succeeded: " + buildPath);
            EditorUtility.RevealInFinder(buildPath);
        }
        else
        {
            Debug.LogError("Build failed");
        }
    }
}
