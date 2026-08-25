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
        [SerializeField] private GameObject resultDialog;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private TMP_Text turnText;

        [Header("音频")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip stoneClip;
        [SerializeField] private AudioClip winClip;
        [SerializeField] private AudioClip loseClip;
        [SerializeField] private AudioClip clickClip;

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

            resultDialog.SetActive(false);
            UpdateTurnText(gameManager.IsPlayerTurn);
            StartBGM();
        }

        private void OnDestroy()
        {
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

        private void StartBGM()
        {
            if (bgmSource == null) return;
            if (bgmSource.clip == null)
            {
                GameObject go = GameObject.Find("BGM_AudioSource");
                if (go != null && go.TryGetComponent(out AudioSource src) && src.clip != null)
                    bgmSource.clip = src.clip;
            }
            if (bgmSource.clip != null && !bgmSource.isPlaying)
            {
                bgmSource.loop = true;
                bgmSource.Play();
            }
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
            PlayOneShot(result == GameResult.Draw ? winClip : (playerWon ? winClip : loseClip));
            StartCoroutine(ShowDialogAfter(0.7f));
        }

        private IEnumerator ShowDialogAfter(float delay)
        {
            int token = ++dialogToken;
            yield return new WaitForSeconds(delay);
            if (token == dialogToken && gameManager.IsGameOver)
                resultDialog.SetActive(true);
        }

        private void OnTurnChanged(bool isPlayerTurn) => UpdateTurnText(isPlayerTurn);

        private void UpdateTurnText(bool isPlayerTurn)
        {
            if (turnText == null || gameManager.IsGameOver) return;
            turnText.text = isPlayerTurn ? "你的回合" : "AI 思索中…";
        }

        // ---------- 按钮 ----------

        private void OnUndoClicked()
        {
            PlayOneShot(clickClip);
            gameManager.Undo();
        }

        private void OnRestartClicked()
        {
            PlayOneShot(clickClip);
            gameManager.Restart();
        }

        /// <summary>查看棋盘：仅关闭结算弹窗，保留终局局面。</summary>
        private void OnReviewClicked()
        {
            PlayOneShot(clickClip);
            resultDialog.SetActive(false);
            dialogToken++;
        }

        // ---------- 手数统计（左侧信息名牌）----------

        private void UpdateMoveCountText()
        {
            var t = GameObject.Find("Canvas/LeftPanel/GameInfo/InfoText");
            if (t != null && t.TryGetComponent(out TMPro.TMP_Text txt))
                txt.text = $"第 {gameManager.Board.MoveCount} 手";
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (sfxSource != null && clip != null) sfxSource.PlayOneShot(clip);
        }
    }
}
