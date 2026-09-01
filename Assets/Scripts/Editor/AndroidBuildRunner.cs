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
        // ── 版本号 ──
        PlayerSettings.bundleVersion = "1.2.1";
        PlayerSettings.Android.bundleVersionCode = 14;

        var scenes = new[] { "Assets/Scenes/SampleScene.unity" };
        var location = "Builds/Android/XianMiaoWuZiQi.apk";

        Directory.CreateDirectory("Builds/Android");

        // 只打包 ARM64（64位）
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

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

        // ── 物理禁用 Burst .so（反射禁用不阻止打包 .so 文件）──
        string burstPath = "Packages/com.unity.burst/libburst-llvm-16.so";
        string burstBak = burstPath + ".bak";
        bool burstRenamed = false;
        if (File.Exists(burstPath))
        {
            File.Move(burstPath, burstBak);
            burstRenamed = true;
            Debug.Log("[Build] 已重命名 libburst-llvm-16.so → .bak（省68MB）");
        }

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
        // IL2CPP + 裁剪（ARM64必须用IL2CPP）
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.High);

        Debug.Log($"开始打包: {location} (signed={signed}, ARM64, IL2CPP, Strip=High)");

        var options = signed
            ? BuildOptions.CompressWithLz4HC   // 签名包: LZ4HC 压缩
            : BuildOptions.Development | BuildOptions.AllowDebugging;

        var report = BuildPipeline.BuildPlayer(
            scenes,
            location,
            BuildTarget.Android,
            options
        );

        // ── 恢复 Burst .so ──
        if (burstRenamed && File.Exists(burstBak))
        {
            File.Move(burstBak, burstPath);
            Debug.Log("[Build] 已恢复 libburst-llvm-16.so");
        }

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
