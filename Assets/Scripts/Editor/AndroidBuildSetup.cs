// 设置 Android 打包必需项：包名 + IL2CPP + ARM64
using UnityEditor;
using UnityEngine;

public static class AndroidBuildSetup
{
    [MenuItem("仙喵五子棋/配置Android打包")]
    private static void Setup()
    {
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.zhanghaonan.xianmiao.wuziqi");
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
        PlayerSettings.bundleVersion = "1.0.0";
        PlayerSettings.Android.bundleVersionCode = 1;
        PlayerSettings.companyName = "ZhangHaoNan";
        PlayerSettings.productName = "仙喵五子棋";

        // minSdk 24 (Android 7.0, Dirichlet SDK 要求), targetSdk 自动
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;

        AssetDatabase.SaveAssets();
        Debug.Log("Android build setup done: package=com.zhanghaonan.xianmiao.wuziqi, IL2CPP, ARMv7+ARM64");
    }
}
