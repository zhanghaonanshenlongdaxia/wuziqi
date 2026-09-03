using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Wuziqi.Core;
using Wuziqi.Game;

namespace Wuziqi.UI
{
    /// <summary>按钮、回合指示、结算弹窗与音频播放。</summary>
    public class GameUIController : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Button undoButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button reviewButton;
        [SerializeField] private Button historyButton;
        [SerializeField] private Button replayButton;
        [SerializeField] private GameObject resultDialog;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private TMP_Text turnText;
        [SerializeField] private TMP_Text moveCountText;
        [Header("道具")]
        [SerializeField] private Button hintButton;
        [SerializeField] private Button doubleButton;
        [SerializeField] private GameObject hintCostGroup;   // 提示按钮的价格提示（库存0时显示）
        [SerializeField] private GameObject doubleCostGroup; // 双倍按钮的价格提示

        [Header("音频")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip stoneClip;
        [SerializeField] private AudioClip winClip;
        [SerializeField] private AudioClip loseClip;
        [SerializeField] private AudioClip drawClip;
        [SerializeField] private AudioClip clickClip;
        [SerializeField] private AudioClip undoClip;
        [SerializeField] private AudioClip thinkingClip;
        [SerializeField] private AudioClip meowClip;
        [SerializeField] private AudioClip warningClip;

        private int dialogToken;

        private void Start()
        {
            if (gameManager == null) gameManager = GameManager.Instance;

            gameManager.StonePlaced += OnStonePlaced;
            gameManager.StoneRemoved += OnStoneRemoved;
            gameManager.BoardReset += OnBoardReset;
            gameManager.GameEnded += OnGameEnded;
            gameManager.PlayerTurnChanged += OnTurnChanged;

            undoButton.onClick.AddListener(OnUndoClicked);
            restartButton.onClick.AddListener(OnRestartClicked);
            if (playAgainButton != null) playAgainButton.onClick.AddListener(OnRestartClicked);
            if (reviewButton != null) reviewButton.onClick.AddListener(OnReviewClicked);
            if (historyButton != null) historyButton.onClick.AddListener(OnHistoryClicked);
            if (replayButton != null) replayButton.onClick.AddListener(OnReplayFromResult);
            if (hintButton != null) hintButton.onClick.AddListener(OnHintClicked);
            if (doubleButton != null) doubleButton.onClick.AddListener(OnDoubleClicked);
            if (ItemInventory.Instance != null)
            {
                ItemInventory.Instance.OnChanged += UpdateItemButtons;
                UpdateItemButtons();
            }

            resultDialog.SetActive(false);
            UpdateTurnText(gameManager.IsPlayerTurn);
        }

        private void OnDestroy()
        {
            if (ItemInventory.Instance != null) ItemInventory.Instance.OnChanged -= UpdateItemButtons;
            if (gameManager == null) return;
            gameManager.StonePlaced -= OnStonePlaced;
            gameManager.StoneRemoved -= OnStoneRemoved;
            gameManager.BoardReset -= OnBoardReset;
            gameManager.GameEnded -= OnGameEnded;
            gameManager.PlayerTurnChanged -= OnTurnChanged;
        }

        private void Update()
        {
            undoButton.interactable = gameManager.CanUndo;
        }

        // ---------- 事件处理 ----------

        private void OnStonePlaced(Vector2Int cell, StoneColor color)
        {
            PlayOneShot(stoneClip);
            UpdateMoveCountText();
        }

        private void OnStoneRemoved(Vector2Int cell)
        {
            resultDialog.SetActive(false);
            dialogToken++;
        }

        private void OnBoardReset()
        {
            resultDialog.SetActive(false);
            dialogToken++;
            UpdateTurnText(gameManager.IsPlayerTurn);
            UpdateMoveCountText();
        }

        private void OnGameEnded(GameResult result, IReadOnlyList<Vector2Int> line)
        {
            bool playerWon = (result == GameResult.BlackWin && gameManager.playerColor == StoneColor.Black)
                          || (result == GameResult.WhiteWin && gameManager.playerColor == StoneColor.White);
            resultText.text = result == GameResult.Draw ? "势均力敌 · 平局"
                            : (playerWon ? "妙手连连 · 你赢了" : "棋差一着 · 再战一局？");
            PlayOneShot(result == GameResult.Draw ? drawClip : (playerWon ? winClip : loseClip));

            // 胜利给仙喵币（基础 + 连胜加成）
            if (playerWon && CatManager.Instance != null && CatManager.Instance.Selected != null
                && EconomyManager.Instance != null)
            {
                int baseReward = CatManager.Instance.Selected.winReward;
                int streak = CharacterController.WinStreak.Get();
                int streakBonus = Mathf.Min(streak * 5, 50); // 每连胜+5，上限+50
                int totalReward = baseReward + streakBonus;

                // 双倍卡：本局激活则奖励翻倍
                if (ItemInventory.Instance != null && ItemInventory.Instance.DoubleActive)
                {
                    totalReward *= 2;
                    ItemInventory.Instance.ConsumeDouble();
                }

                EconomyManager.Instance.AddCoins(totalReward);
                if (PlayerStats.Instance != null) PlayerStats.Instance.AddCoinEarned(totalReward);

                if (streakBonus > 0)
                    Debug.Log($"[GameUIController] 金币奖励: 基础{baseReward} + 连胜加成{streakBonus} = {totalReward}");
            }

            UpdateItemButtons();
            StartCoroutine(ShowDialogAfter(0.7f));
        }

        private IEnumerator ShowDialogAfter(float delay)
        {
            int token = ++dialogToken;
            yield return new WaitForSeconds(delay);
            if (token == dialogToken && gameManager.IsGameOver)
                resultDialog.SetActive(true);
        }

        private void OnTurnChanged(bool isPlayerTurn)
        {
            UpdateTurnText(isPlayerTurn);
            if (!isPlayerTurn) PlayOneShot(thinkingClip);
        }

        private void UpdateTurnText(bool isPlayerTurn)
        {
            if (turnText == null || gameManager.IsGameOver) return;
            if (isPlayerTurn)
                turnText.text = "你的回合";
            else
            {
                string catName = CatManager.Instance != null && CatManager.Instance.Selected != null
                    ? CatManager.Instance.Selected.catName : "猫猫";
                turnText.text = $"{catName} 思索中…";
            }
        }

        // ---------- 按钮 ----------

        private void OnUndoClicked()
        {
            PlayOneShot(undoClip != null ? undoClip : clickClip);
            gameManager.Undo();
        }

        private void OnRestartClicked()
        {
            PlayOneShot(clickClip);
            if (!gameManager.TryRestart(out string reason))
            {
                if (reason == "coins")
                    Debug.Log("[GameUIController] 仙喵币不足，无法挑战");
            }
        }

        /// <summary>查看棋盘：仅关闭结算弹窗，保留终局局面。</summary>
        private void OnReviewClicked()
        {
            PlayOneShot(clickClip);
            resultDialog.SetActive(false);
            dialogToken++;
        }

        /// <summary>点击猫立绘：播放猫叫。</summary>
        public void OnCatPortraitClicked()
        {
            PlayOneShot(meowClip);
        }

        // ---------- 手数统计（左侧信息名牌）----------

        private void UpdateMoveCountText()
        {
            if (moveCountText != null)
                moveCountText.text = $"第 {gameManager.Board.MoveCount} 手";
        }

        // ---------- 道具 ----------

        private void OnHintClicked()
        {
            PlayOneShot(clickClip);
            if (gameManager == null || !gameManager.CanPlayerPlaceNow) return;
            var inv = ItemInventory.Instance;
            if (inv == null) return;

            if (inv.TryUseHint())
            {
                gameManager.ShowHint();
            }
            else if (EconomyManager.Instance != null && EconomyManager.Instance.SpendCoins(ItemInventory.HINT_COST))
            {
                inv.AddHint();
                gameManager.ShowHint();
            }
            else
            {
                SetButtonLabel(hintButton, "币不足");
                Invoke(nameof(UpdateItemButtons), 2f);
                return;
            }
            UpdateItemButtons();
        }

        private void OnDoubleClicked()
        {
            PlayOneShot(clickClip);
            var inv = ItemInventory.Instance;
            if (inv == null || inv.DoubleActive) return;
            if (gameManager == null || !gameManager.IsGameStarted || gameManager.IsGameOver) return;

            if (inv.TryUseDouble()) { UpdateItemButtons(); return; }
            if (EconomyManager.Instance != null && EconomyManager.Instance.SpendCoins(ItemInventory.DOUBLE_COST))
            {
                inv.AddDouble();
                inv.TryUseDouble();
                UpdateItemButtons();
            }
            else
            {
                SetButtonLabel(doubleButton, "币不足");
                Invoke(nameof(UpdateItemButtons), 2f);
            }
        }

        private void UpdateItemButtons()
        {
            var inv = ItemInventory.Instance;
            if (inv == null) return;
            SetButtonLabel(hintButton, $"提示 ×{inv.HintCount}");
            SetButtonLabel(doubleButton, inv.DoubleActive ? "双倍生效中" : $"双倍 ×{inv.DoubleCount}");
            if (doubleButton != null) doubleButton.interactable = !inv.DoubleActive;

            // 价格提示：没库存时显示购买价，提醒玩家（与双倍是否生效无关）
            if (hintCostGroup != null) hintCostGroup.SetActive(inv.HintCount <= 0);
            if (doubleCostGroup != null) doubleCostGroup.SetActive(inv.DoubleCount <= 0);
        }

        private static void SetButtonLabel(Button b, string v)
        {
            if (b == null) return;
            var tmp = b.GetComponentInChildren<TMPro.TMP_Text>();
            if (tmp != null) { tmp.text = v; return; }
            var legacy = b.GetComponentInChildren<UnityEngine.UI.Text>();
            if (legacy != null) legacy.text = v;
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (sfxSource != null && clip != null) sfxSource.PlayOneShot(clip);
        }

        private void OnHistoryClicked()
        {
            PlayOneShot(clickClip);
            HistoryPanel hp = FindObjectOfType<HistoryPanel>();
            if (hp != null) hp.Show();
        }

        private void OnReplayFromResult()
        {
            PlayOneShot(clickClip);
            var records = GameRecordManager.Instance?.Records;
            if (records == null || records.Count == 0)
            {
                Debug.LogWarning("[GameUIController] No records to replay");
                return;
            }
            ReplayPanel rp = FindObjectOfType<ReplayPanel>();
            if (rp != null)
            {
                resultDialog.SetActive(false);
                rp.StartReplay(records[0]);
            }
        }
    }
}
