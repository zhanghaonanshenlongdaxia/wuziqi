using UnityEditor;
using UnityEngine;

public static class PrivacyResetMenu
{
    [MenuItem("工具/清除隐私弹窗记录")]
    public static void ResetPrivacy()
    {
        PlayerPrefs.DeleteKey("PrivacyAccepted");
        PlayerPrefs.Save();
        Debug.Log("[Privacy] 已清除隐私弹窗同意记录");
    }
}
