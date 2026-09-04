using UnityEngine;
using Wuziqi.Game;

namespace Wuziqi.UI
{
    /// <summary>段位系统：按战绩（胜场 + 最大连胜×2）计算段位，纯本地无服务器。</summary>
    public static class RankSystem
    {
        private static readonly (string name, int min)[] Tiers =
        {
            ("棋童", 0),
            ("棋士", 10),
            ("棋侠", 25),
            ("棋尊", 50),
            ("棋圣", 90),
            ("仙喵棋圣", 150),
        };

        public static int GetScore(PlayerStats ps) => ps != null ? ps.TotalWins + ps.MaxStreak * 2 : 0;

        public static string GetTierName(PlayerStats ps)
        {
            int score = GetScore(ps);
            var name = Tiers[0].name;
            foreach (var t in Tiers)
                if (score >= t.min) name = t.name;
            return name;
        }

        /// <summary>(当前积分, 下一档所需积分)；已满级返回 (score, -1)。</summary>
        public static (int cur, int next) GetTierProgress(PlayerStats ps)
        {
            int score = GetScore(ps);
            foreach (var t in Tiers)
                if (score < t.min) return (score, t.min);
            return (score, -1);
        }
    }
}
