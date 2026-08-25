using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Wuziqi.Core;
using Wuziqi.Game;

namespace Wuziqi.UI
{
    /// <summary>玩家限时回合：香炉燃香倒计时（香从顶部烧短，新回合香重新长满）；无香素材时退化为进度条。</summary>
    public class TurnTimerController : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private CharacterController character;
        [SerializeField] private GameObject timerRoot;
        [SerializeField] private Image fillImage;
        [SerializeField] private TMP_Text timeText;

        [Header("香炉模式")]
        [SerializeField] private GameObject incenseRoot;
        [SerializeField] private Image incenseStick;
        [SerializeField] private TMP_Text incenseTimeText;
        [SerializeField] private Color emberColor = new Color(0.80f, 0.25f, 0.12f);

        [Header("配置")]
        [SerializeField] private float timeLimit = 20f;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip warningClip;
        [SerializeField] private float warningThreshold = 5f;

        private static readonly Color InkColor = new Color(0.23f, 0.22f, 0.20f);
        private static readonly Color DangerColor = new Color(0.76f, 0.15f, 0.16f);
        private float remaining;
        private bool warnedThisTurn;

        private void Start()
        {
            if (gameManager == null) gameManager = GameManager.Instance;
            gameManager.StonePlaced += OnAnyStonePlaced;
            gameManager.PlayerTurnChanged += OnTurnChanged;
            gameManager.BoardReset += OnReset;
            gameManager.StoneRemoved += OnStoneRemoved;
            remaining = timeLimit;
            if (timerRoot != null) timerRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            if (gameManager == null) return;
            gameManager.StonePlaced -= OnAnyStonePlaced;
            gameManager.PlayerTurnChanged -= OnTurnChanged;
            gameManager.BoardReset -= OnReset;
            gameManager.StoneRemoved -= OnStoneRemoved;
        }

        private void OnAnyStonePlaced(Vector2Int cell, StoneColor color) => ResetTimer();
        private void OnTurnChanged(bool isPlayerTurn) => ResetTimer();
        private void OnReset() => ResetTimer();
        private void OnStoneRemoved(Vector2Int cell) => ResetTimer();
        private void ResetTimer() { remaining = timeLimit; warnedThisTurn = false; }

        private void Update()
        {
            bool active = gameManager != null && gameManager.CanPlayerPlaceNow;
            if (timerRoot != null && timerRoot.activeSelf != active) timerRoot.SetActive(active);
            if (incenseRoot != null && incenseRoot.activeSelf != active) incenseRoot.SetActive(active);
            if (!active) return;

            remaining -= Time.deltaTime;
            if (remaining <= 0f)
            {
                remaining = timeLimit;
                warnedThisTurn = false;
                TimeoutPlace();
                return;
            }

            if (!warnedThisTurn && remaining <= warningThreshold)
            {
                warnedThisTurn = true;
                if (sfxSource != null && warningClip != null) sfxSource.PlayOneShot(warningClip);
            }

            float ratio = remaining / timeLimit;

            // 进度条模式（fallback）
            if (fillImage != null && incenseStick == null)
            {
                fillImage.fillAmount = ratio;
                fillImage.color = remaining < 5f ? DangerColor : InkColor;
            }
            if (timeText != null && incenseTimeText == null)
                timeText.text = Mathf.CeilToInt(remaining).ToString();

            // 香炉模式：fillAmount=1 满香，随时间从顶部燃短
            if (incenseStick != null)
            {
                incenseStick.fillAmount = ratio;
                // 快烧完时香头渐红
                incenseStick.color = Color.Lerp(emberColor, Color.white, Mathf.Clamp01(ratio * 3f));
            }
            if (incenseTimeText != null)
                incenseTimeText.text = Mathf.CeilToInt(remaining).ToString();
        }

        /// <summary>超时：在已有棋子邻域随机落一子（空盘则下天元）。</summary>
        private void TimeoutPlace()
        {
            GomokuBoard b = gameManager.Board;
            var candidates = new List<Vector2Int>();
            for (int x = 0; x < GomokuBoard.Size; x++)
                for (int y = 0; y < GomokuBoard.Size; y++)
                {
                    if (b.GetCell(x, y) == StoneColor.None) continue;
                    for (int dx = -1; dx <= 1; dx++)
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (b.IsInside(nx, ny) && b.IsEmpty(nx, ny))
                                candidates.Add(new Vector2Int(nx, ny));
                        }
                }

            Vector2Int pick;
            if (candidates.Count > 0) pick = candidates[Random.Range(0, candidates.Count)];
            else pick = new Vector2Int(GomokuBoard.Size / 2, GomokuBoard.Size / 2);

            gameManager.TryPlayerPlace(pick.x, pick.y);
            if (character != null) character.ShowBubble("超时了！本仙猫替你落子～", 2.5f, true);
        }
    }
}
