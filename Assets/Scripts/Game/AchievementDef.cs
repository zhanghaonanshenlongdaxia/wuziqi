using UnityEngine;

namespace Wuziqi.Game
{
    [CreateAssetMenu(menuName = "Wuziqi/Achievement Def")]
    public class AchievementDef : ScriptableObject
    {
        public string achId;            // 唯一 ID（如 win_20）
        public string displayName;      // 名称（水墨+幽默风）
        [TextArea] public string desc;  // 条件描述
        public AchType type;            // 判定类型
        public int targetValue;         // 目标值
        public int rewardCoins;         // 奖励仙喵币
        public bool hidden;             // 隐藏成就（达成前不显示条件）
    }

    /// <summary>判定类型：BeatAllCats=每只猫至少赢 1 局；SongUnlock=累计解锁歌曲数。</summary>
    public enum AchType { TotalWins, MaxStreak, TotalGames, CollectCats, SameCatGames, BeatAllCats, CoinEarned, LoseStreak, SongUnlock }
}
