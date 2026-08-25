using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wuziqi.Core
{
    public enum StoneColor { None = 0, Black = 1, White = 2 }

    public enum GameResult { InProgress, BlackWin, WhiteWin, Draw }

    /// <summary>纯数据棋盘：落子、悔棋、五连判定（含胜利连线坐标）。</summary>
    public class GomokuBoard
    {
        public const int Size = 15;

        public readonly struct Move
        {
            public readonly int X, Y;
            public readonly StoneColor Color;
            public Move(int x, int y, StoneColor color) { X = x; Y = y; Color = color; }
        }

        private static readonly Vector2Int[] Directions =
        {
            new Vector2Int(1, 0), new Vector2Int(0, 1),
            new Vector2Int(1, 1), new Vector2Int(1, -1),
        };

        private readonly StoneColor[,] cells = new StoneColor[Size, Size];
        private readonly List<Move> history = new List<Move>();

        public IReadOnlyList<Move> History => history;
        public int MoveCount => history.Count;
        public bool IsFull => MoveCount >= Size * Size;

        /// <summary>下一手该谁走：空盘黑先。</summary>
        public StoneColor CurrentTurnColor
        {
            get
            {
                if (history.Count == 0) return StoneColor.Black;
                return history[history.Count - 1].Color == StoneColor.Black
                    ? StoneColor.White
                    : StoneColor.Black;
            }
        }

        public bool IsInside(int x, int y) => x >= 0 && x < Size && y >= 0 && y < Size;
        public bool IsEmpty(int x, int y) => cells[x, y] == StoneColor.None;
        public StoneColor GetCell(int x, int y) => cells[x, y];

        public bool TryPlace(int x, int y, StoneColor color)
        {
            if (!IsInside(x, y) || !IsEmpty(x, y)) return false;
            cells[x, y] = color;
            history.Add(new Move(x, y, color));
            return true;
        }

        public bool TryUndoLast(out Move undone)
        {
            undone = default;
            if (history.Count == 0) return false;
            Move m = history[history.Count - 1];
            cells[m.X, m.Y] = StoneColor.None;
            history.RemoveAt(history.Count - 1);
            undone = m;
            return true;
        }

        public void Reset()
        {
            Array.Clear(cells, 0, cells.Length);
            history.Clear();
        }

        /// <summary>以 (x,y) 为中心找 ≥5 连；返回连线格子，无则 null。</summary>
        public List<Vector2Int> FindWinningLine(int x, int y)
        {
            StoneColor color = cells[x, y];
            if (color == StoneColor.None) return null;
            foreach (Vector2Int d in Directions)
            {
                var line = new List<Vector2Int> { new Vector2Int(x, y) };
                for (int s = 1; s < 5; s++)
                {
                    int nx = x + d.x * s, ny = y + d.y * s;
                    if (IsInside(nx, ny) && cells[nx, ny] == color) line.Add(new Vector2Int(nx, ny));
                    else break;
                }
                for (int s = 1; s < 5; s++)
                {
                    int nx = x - d.x * s, ny = y - d.y * s;
                    if (IsInside(nx, ny) && cells[nx, ny] == color) line.Insert(0, new Vector2Int(nx, ny));
                    else break;
                }
                if (line.Count >= 5) return line;
            }
            return null;
        }
    }
}
