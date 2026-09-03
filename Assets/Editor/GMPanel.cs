using UnityEngine;
using UnityEditor;
using Wuziqi.Game;

/// <summary>GM 面板：运行时调试用（Editor 目录，不进包）。</summary>
public class GMPanel : EditorWindow
{
    [MenuItem("仙喵五子棋/GM 面板")]
    private static void Open() => GetWindow<GMPanel>("GM 面板");

    private Vector2 scroll;

    private void OnGUI()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("进入 Play Mode 后可用", MessageType.Info);
            return;
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);

        // ── 统计概览 ──
        GUILayout.Label("── 统计 ──", EditorStyles.boldLabel);
        var ps = PlayerStats.Instance;
        if (ps != null)
            EditorGUILayout.LabelField("对局/胜场/最高连胜/累计币",
                $"{ps.TotalGames} / {ps.TotalWins} / {ps.MaxStreak} / {ps.CoinEarnedTotal}");

        GUILayout.Space(8);

        // ── 成就 ──
        GUILayout.Label("── 成就 ──", EditorStyles.boldLabel);
        if (GUILayout.Button("一键完成全部成就（看弹窗效果）"))
            PlayerStats.Instance?.GM_FillAll();
        if (GUILayout.Button("重置全部成就与统计（可重复看）"))
            PlayerStats.Instance?.GM_ResetAll();

        GUILayout.Space(8);

        // ── 仙喵币 ──
        GUILayout.Label("── 仙喵币 ──", EditorStyles.boldLabel);
        var eco = EconomyManager.Instance;
        if (eco != null) EditorGUILayout.LabelField("当前", eco.Coins.ToString());
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("+100")) eco?.AddCoins(100);
        if (GUILayout.Button("+1000")) eco?.AddCoins(1000);
        if (GUILayout.Button("清零")) eco?.SpendCoins(eco.Coins);
        GUILayout.EndHorizontal();

        GUILayout.Space(8);

        // ── 道具 ──
        GUILayout.Label("── 道具 ──", EditorStyles.boldLabel);
        var inv = ItemInventory.Instance;
        if (inv != null) EditorGUILayout.LabelField("提示 / 双倍", $"{inv.HintCount} / {inv.DoubleCount}");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("提示+5")) inv?.GM_Set(inv.HintCount + 5, inv.DoubleCount);
        if (GUILayout.Button("双倍+5")) inv?.GM_Set(inv.HintCount, inv.DoubleCount + 5);
        if (GUILayout.Button("清空")) inv?.GM_Set(0, 0);
        GUILayout.EndHorizontal();

        GUILayout.Space(8);

        // ── 猫解锁 ──
        GUILayout.Label("── 猫解锁 ──", EditorStyles.boldLabel);
        var cm = CatManager.Instance;
        if (cm != null)
        {
            int unlocked = 0;
            for (int i = 0; i < cm.CatCount; i++) if (cm.IsUnlocked(i)) unlocked++;
            EditorGUILayout.LabelField("已解锁", $"{unlocked}/{cm.CatCount}");
        }
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("解锁全部猫")) SetAllCats(true);
        if (GUILayout.Button("恢复默认解锁")) SetAllCats(false);
        GUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();
    }

    private const string UnlockPrefix = "Wuziqi.CatUnlock.";

    private static void SetAllCats(bool unlock)
    {
        var cm = CatManager.Instance;
        if (cm == null) return;
        for (int i = 0; i < cm.CatCount; i++)
        {
            if (unlock) PlayerPrefs.SetInt(UnlockPrefix + i, 1);
            else PlayerPrefs.DeleteKey(UnlockPrefix + i);
        }
        PlayerPrefs.Save();
    }
}
