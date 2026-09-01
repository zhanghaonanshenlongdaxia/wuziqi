// 执行 Android APK 构建（IL2CPP, ARMv7+ARM64, 签名）
using UnityEditor;
using UnityEngine;
using System.IO;

public static class AndroidBuildRunner
{
    [MenuItem("仙喵五子棋/打包APK(签名)")]
    private static void BuildSigned()
    {
        Build(true);
    }

    [MenuItem("仙喵五子棋/打包APK(调试)")]
    private static void BuildDebug()
    {
        Build(false);
    }

    private static void Build(bool signed)
    {
        var scenes = new[] { "Assets/Scenes/SampleScene.unity" };
        var location = "Builds/Android/XianMiaoWuZiQi.apk";

        Directory.CreateDirectory("Builds/Android");

        if (signed)
        {
            // 本地开发用固定密码，发布时改用环境变量
            PlayerSettings.Android.keystorePass = "canglongqixiu";
            PlayerSettings.Android.keyaliasPass = "canglongqixiu";
            Debug.Log("签名: keystore=canglongqixiu.keystore, alias=canglongqixiu");
        }

        Debug.Log($"开始打包: {location} (signed={signed})");

        var report = BuildPipeline.BuildPlayer(
            scenes,
            location,
            BuildTarget.Android,
            signed ? BuildOptions.None : BuildOptions.Development | BuildOptions.AllowDebugging
        );

        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            var sizeMB = report.summary.totalSize / 1048576.0;
            Debug.Log($"APK BUILD SUCCESS: {location} | size={sizeMB:F1}MB | time={report.summary.totalTime}");
        }
        else
        {
            Debug.LogError($"APK BUILD FAILED: {report.summary.result}");
        }
    }
}
