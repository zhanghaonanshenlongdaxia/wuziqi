using UnityEngine;

namespace Wuziqi.Game
{
    /// <summary>猫猫角色数据：难度、奖励、解锁条件、帧素材路径。</summary>
    [CreateAssetMenu(fileName = "Cat_", menuName = "Wuziqi/Cat Profile")]
    public class CatProfile : ScriptableObject
    {
        [Header("基础")]
        public string catName = "小白";
        [TextArea] public string description = "初出茅庐的小白猫，对弈风格温和。";
        public Sprite portrait;

        [Header("难度")]
        [Range(1, 5)] public int difficulty = 1;
        [Range(1, 10)] public int aiSearchDepth = 4;
        [Range(0.1f, 3f)] public float aiScoreMultiplier = 1.0f;

        [Header("奖励")]
        public int winReward = 10;
        [Range(0, 50)] public int challengeCost = 2;

        [Header("解锁")]
        public UnlockType unlockType = UnlockType.Free;
        public int coinCost = 0;

        [Header("帧素材")]
        public string framesDir = "idle";

        public enum UnlockType { Free, Coins }
    }
}
