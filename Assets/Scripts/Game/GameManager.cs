using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Wuziqi.Core;

namespace Wuziqi.Game
{
    /// <summary>回合流程：玩家执黑先行，AI 白后手；管理落子/悔棋/重开/结束事件。</summary>
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
        public GameResult Result { get; private set; } = GameResult.InProgress;

        public bool CanPlayerPlaceNow => !IsGameOver && IsPlayerTurn && !IsAIThinking;
        public bool CanUndo => Board.MoveCount > 0 && !IsAIThinking;

        public event Action<Vector2Int, StoneColor> StonePlaced;
        public event Action<Vector2Int> StoneRemoved;
        public event Action<GameResult, IReadOnlyList<Vector2Int>> GameEnded;
        public event Action<bool> PlayerTurnChanged;
        public event Action BoardReset;

        private readonly System.Random rng = new System.Random();
        private Coroutine aiRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            IsPlayerTurn = playerColor == StoneColor.Black;
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
            if (FinishIfOver()) return;
            BeginAITurn();
        }

        /// <summary>直接落子并广播事件（不驱动回合流程；用于演示/回放布局）。</summary>
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
            yield return new WaitForSeconds(UnityEngine.Random.Range(aiThinkTimeMin, aiThinkTimeMax));

            StoneColor aiColor = GomokuAI.Other(playerColor);
            Vector2Int move = GomokuAI.FindBestMove(Board, aiColor, rng);
            IsAIThinking = false;
            aiRoutine = null;

            if (move.x < 0 || !Board.TryPlace(move.x, move.y, aiColor))
            {
                EndGame(GameResult.Draw, null); // 无处可下
                yield break;
            }
            StonePlaced?.Invoke(move, aiColor);
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
            GameEnded?.Invoke(result, winLine);
        }

        /// <summary>悔棋：撤销到玩家再次行动（通常撤 AI+玩家各一手）。</summary>
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
    }
}
