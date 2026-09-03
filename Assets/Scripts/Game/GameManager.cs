using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Wuziqi.Core;

namespace Wuziqi.Game
{
    /// <summary>回合流程：玩家执黑先行，AI 白后手；管理落子/悔棋/重开/结束事件�?/summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("配置")]
        public StoneColor playerColor = StoneColor.Black;
        [Range(0.2f, 2f)] public float aiThinkTimeMin = 0.45f;
        [Range(0.2f, 3f)] public float aiThinkTimeMax = 1.0f;

        public GomokuBoard Board { get; } = new GomokuBoard();
        public bool IsPlayerTurn { get; private set; } = true;
        public bool IsAIThinking { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool IsPaused { get; private set; }
        public GameResult Result { get; private set; } = GameResult.InProgress;

        public bool IsGameStarted { get; private set; }
        public bool CanPlayerPlaceNow => IsGameStarted && !IsGameOver && !IsPaused && IsPlayerTurn && !IsAIThinking;
        public bool CanUndo => IsGameStarted && Board.MoveCount > 0 && !IsAIThinking;

        public event Action<Vector2Int, StoneColor> StonePlaced;
        public event Action<Vector2Int> StoneRemoved;
        public event Action<GameResult, IReadOnlyList<Vector2Int>> GameEnded;
        public event Action<bool> PlayerTurnChanged;
        public event Action BoardReset;
        public event Action<bool> GamePaused; // true=暂停, false=恢复

        private readonly System.Random rng = new System.Random();
        private Coroutine aiRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            IsGameStarted = false;
            IsPlayerTurn = playerColor == StoneColor.Black;
        }

        /// <summary>由主菜单调用，正式启动游戏</summary>
        public void StartGame()
        {
            IsGameStarted = true;
            IsPlayerTurn = playerColor == StoneColor.Black;
            PlayerTurnChanged?.Invoke(IsPlayerTurn);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void TryPlayerPlace(int x, int y)
        {
            if (!CanPlayerPlaceNow) return;
            if (!Board.TryPlace(x, y, playerColor)) return;
            StonePlaced?.Invoke(new Vector2Int(x, y), playerColor);
            if (Board.MoveCount == 1)
                GameRecordManager.Instance?.StartRecording(playerColor);
            GameRecordManager.Instance?.RecordMove(x, y, playerColor);
            if (FinishIfOver()) return;
            BeginAITurn();
        }

        /// <summary>直接落子并广播事件（不驱动回合流程；用于演示/回放布局）�?/summary>
        public event Action<Vector2Int> HintShown;

        /// <summary>显示提示：算出 AI 推荐点并广播（道具消耗由调用方负责）。</summary>
        public void ShowHint()
        {
            if (!CanPlayerPlaceNow) return;
            var move = GomokuAIAdvanced.FindBestMove(Board.Copy(), playerColor, 3, 1.0f, rng);
            HintShown?.Invoke(move);
        }

        public void PlaceStoneDirect(int x, int y, StoneColor color)
        {
            if (!Board.TryPlace(x, y, color)) return;
            StonePlaced?.Invoke(new Vector2Int(x, y), color);
        }

        private void BeginAITurn()
        {
            IsPlayerTurn = false;
            PlayerTurnChanged?.Invoke(false);
            if (aiRoutine != null) StopCoroutine(aiRoutine);
            aiRoutine = StartCoroutine(AITurnRoutine());
        }

        private IEnumerator AITurnRoutine()
        {
            IsAIThinking = true;
            float waitTime = UnityEngine.Random.Range(aiThinkTimeMin, aiThinkTimeMax);
            float elapsed = 0f;
            while (elapsed < waitTime)
            {
                if (!IsPaused) elapsed += UnityEngine.Time.deltaTime;
                yield return null;
            }

            StoneColor aiColor = GomokuAIAdvanced.Other(playerColor);
            CatProfile cat = CatManager.Instance?.Selected;
            int searchDepth = cat?.aiSearchDepth ?? 3;
            float scoreMultiplier = cat?.aiScoreMultiplier ?? 1.0f;

            // 拷贝棋盘，在后台线程跑 AI 搜索（避免卡主线程）
            GomokuBoard boardCopy = Board.Copy();
            var tcs = new TaskCompletionSource<Vector2Int>();
            Task.Run(() =>
            {
                try
                {
                    Vector2Int move = GomokuAIAdvanced.FindBestMove(boardCopy, aiColor, searchDepth, scoreMultiplier, rng);
                    tcs.SetResult(move);
                }
                catch (Exception ex)
                {
                    Debug.LogError("AI search error: " + ex);
                    tcs.SetResult(new Vector2Int(-1, -1));
                }
            });

            // 等待后台线程完成，期间不阻塞帧
            while (!tcs.Task.IsCompleted)
                yield return null;

            Vector2Int result = tcs.Task.Result;
            IsAIThinking = false;
            aiRoutine = null;

            if (result.x < 0 || !Board.TryPlace(result.x, result.y, aiColor))
            {
                EndGame(GameResult.Draw, null);
                yield break;
            }
            StonePlaced?.Invoke(result, aiColor);
            GameRecordManager.Instance?.RecordMove(result.x, result.y, aiColor);
            if (FinishIfOver()) yield break;

            IsPlayerTurn = true;
            PlayerTurnChanged?.Invoke(true);
        }

        private bool FinishIfOver()
        {
            if (Board.IsFull) { EndGame(GameResult.Draw, null); return true; }
            GomokuBoard.Move last = Board.History[Board.History.Count - 1];
            List<Vector2Int> line = Board.FindWinningLine(last.X, last.Y);
            if (line == null) return false;
            EndGame(last.Color == StoneColor.Black ? GameResult.BlackWin : GameResult.WhiteWin, line);
            return true;
        }

        private void EndGame(GameResult result, IReadOnlyList<Vector2Int> winLine)
        {
            IsGameOver = true;
            IsPlayerTurn = false;
            IsAIThinking = false;
            Result = result;
            GameRecordManager.Instance?.FinishRecording(result);

            GameEnded?.Invoke(result, winLine);
        }

        /// <summary>悔棋：撤销到玩家再次行动（通常�?AI+玩家各一手）�?/summary>
        public void Undo()
        {
            if (!CanUndo) return;
            if (aiRoutine != null) { StopCoroutine(aiRoutine); aiRoutine = null; }
            IsAIThinking = false;

            int guard = 4;
            while (Board.MoveCount > 0 && guard-- > 0)
            {
                Board.TryUndoLast(out GomokuBoard.Move m);
                StoneRemoved?.Invoke(new Vector2Int(m.X, m.Y));
                if (Board.CurrentTurnColor == playerColor) break;
            }

            IsGameOver = false;
            Result = GameResult.InProgress;
            IsPlayerTurn = Board.CurrentTurnColor == playerColor;
            if (IsPlayerTurn) PlayerTurnChanged?.Invoke(true);
            else BeginAITurn();
        }

        public void PauseGame()
        {
            if (IsPaused || IsGameOver) return;
            IsPaused = true;
            GamePaused?.Invoke(true);
        }

        public void ResumeGame()
        {
            if (!IsPaused) return;
            IsPaused = false;
            GamePaused?.Invoke(false);
        }

        public void Restart()
        {
            if (aiRoutine != null) { StopCoroutine(aiRoutine); aiRoutine = null; }
            IsAIThinking = false;
            Board.Reset();
            IsGameOver = false;
            Result = GameResult.InProgress;
            IsPlayerTurn = playerColor == StoneColor.Black;
            BoardReset?.Invoke();
            PlayerTurnChanged?.Invoke(IsPlayerTurn);
        }

        /// <summary>尝试重开一局，扣挑战费用。不足返回false。
        /// reason: 输出失败原因（"coins" / null）</summary>
        public bool TryRestart(out string reason)
        {
            reason = null;

            // 扣挑战费用
            var cat = CatManager.Instance?.Selected;
            if (cat != null && cat.challengeCost > 0 && EconomyManager.Instance != null)
            {
                if (!EconomyManager.Instance.SpendCoins(cat.challengeCost))
                {
                    reason = "coins";
                    return false; // 仙喵币不足
                }
            }

            Restart();
            return true;
        }

        /// <summary>兼容旧调用。</summary>
        public bool TryRestart()
        {
            return TryRestart(out _);
        }
    }
}



