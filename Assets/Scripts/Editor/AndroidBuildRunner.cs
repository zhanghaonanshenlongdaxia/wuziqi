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

        // 只打包 ARMv7（兼容性好，比 ARM64 的 IL2CPP 包体更小）
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;

        // ── 禁用 Burst（省掉 68MB libburst-llvm-16.so）──
        try
        {
            var burstType = System.Type.GetType("Unity.Burst.BurstCompiler, Unity.Burst");
            if (burstType != null)
            {
                var optProp = burstType.GetProperty("Options", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (optProp != null)
                {
                    var burstOpts = optProp.GetValue(null);
                    var enableProp = burstOpts.GetType().GetProperty("EnableBurstCompilation");
                    if (enableProp != null)
                    {
                        enableProp.SetValue(burstOpts, false);
                        Debug.Log("[Build] 已禁用 Burst 编译（省68MB）");
                    }
                }
            }
        }
        catch (System.Exception e) { Debug.LogWarning("[Build] 禁用 Burst 失败: " + e.Message); }

        // 签名
        if (signed)
        {
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = System.IO.Path.GetFullPath("release.jks");
            PlayerSettings.Android.keystorePass = "wuziqi2026";
            PlayerSettings.Android.keyaliasName = "wuziqi";
            PlayerSettings.Android.keyaliasPass = "wuziqi2026";
            Debug.Log("签名: keystore=release.jks, alias=wuziqi");
        }

        // ── 包体优化 ──
        // Mono + 裁剪: 移除未使用的引擎代码（比 IL2CPP 包体更小）
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.Mono2x);
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.High);

        Debug.Log($"开始打包: {location} (signed={signed}, ARMv7, Mono, Strip=High)");

        var options = signed
            ? BuildOptions.CompressWithLz4HC   // 签名包: LZ4HC 压缩
            : BuildOptions.Development | BuildOptions.AllowDebugging;

        var report = BuildPipeline.BuildPlayer(
            scenes,
            location,
            BuildTarget.Android,
            options
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
