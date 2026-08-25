using System.Collections.Generic;
using UnityEngine;

namespace Wuziqi.Core
{
    /// <summary>启发式AI：对每个候选点做攻防双向评分，攻略优于防，优先成五/挡五。</summary>
    public static class GomokuAI
    {
        public const long WinScore = 10_000_000;      // 成五
        public const long OpenFourScore = 1_000_000;  // 活四
        public const long FourScore = 100_000;        // 冲四
        public const long OpenThreeScore = 50_000;
        private const long ThreeScore = 8_000;
        private const long OpenTwoScore = 2_000;
        private const long TwoScore = 200;

        private const float DefenseWeight = 0.9f;
        private const int NeighborRadius = 2;

        public static Vector2Int FindBestMove(GomokuBoard board, StoneColor aiColor, System.Random rng = null)
        {
            rng ??= new System.Random();
            StoneColor oppColor = Other(aiColor);

            if (board.MoveCount == 0)
                return new Vector2Int(GomokuBoard.Size / 2, GomokuBoard.Size / 2);

            Vector2Int best = new Vector2Int(-1, -1);
            long bestScore = long.MinValue;

            foreach (Vector2Int p in Candidates(board))
            {
                long attack = EvaluatePoint(board, p.x, p.y, aiColor);
                long defend = EvaluatePoint(board, p.x, p.y, oppColor);
                long score = attack + (long)(defend * DefenseWeight);
                score += (GomokuBoard.Size - (Mathf.Abs(p.x - 7) + Mathf.Abs(p.y - 7))) * 3; // 中心偏好
                score += rng.Next(20); // 同分微扰，避免每局一模一样

                if (score > bestScore)
                {
                    bestScore = score;
                    best = p;
                }
            }
            return best;
        }

        /// <summary>评估 color 落/已在 (x,y) 的连子威胁值（供角色情绪系统使用）。</summary>
        public static long EvaluateThreat(GomokuBoard board, int x, int y, StoneColor color)
        {
            if (!board.IsInside(x, y)) return 0;
            if (!board.IsEmpty(x, y) && board.GetCell(x, y) != color) return 0;
            return EvaluatePoint(board, x, y, color);
        }

        /// <summary>已有棋子邻域内的空点。</summary>
        private static IEnumerable<Vector2Int> Candidates(GomokuBoard board)
        {
            var mark = new bool[GomokuBoard.Size, GomokuBoard.Size];
            for (int x = 0; x < GomokuBoard.Size; x++)
                for (int y = 0; y < GomokuBoard.Size; y++)
                {
                    if (board.GetCell(x, y) == StoneColor.None) continue;
                    for (int dx = -NeighborRadius; dx <= NeighborRadius; dx++)
                        for (int dy = -NeighborRadius; dy <= NeighborRadius; dy++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (board.IsInside(nx, ny) && board.IsEmpty(nx, ny)) mark[nx, ny] = true;
                        }
                }

            for (int x = 0; x < GomokuBoard.Size; x++)
                for (int y = 0; y < GomokuBoard.Size; y++)
                    if (mark[x, y]) yield return new Vector2Int(x, y);
        }

        /// <summary>假设 color 落在 (x,y) 后四个方向的连子价值之和。</summary>
        private static long EvaluatePoint(GomokuBoard board, int x, int y, StoneColor color)
        {
            return LineScore(board, x, y, color, 1, 0)
                 + LineScore(board, x, y, color, 0, 1)
                 + LineScore(board, x, y, color, 1, 1)
                 + LineScore(board, x, y, color, 1, -1);
        }

        private static long LineScore(GomokuBoard board, int x, int y, StoneColor color, int dx, int dy)
        {
            int count = 1; // 假设落下的这颗
            int openEnds = 0;

            int nx = x + dx, ny = y + dy;
            while (board.IsInside(nx, ny) && board.GetCell(nx, ny) == color) { count++; nx += dx; ny += dy; }
            if (board.IsInside(nx, ny) && board.IsEmpty(nx, ny)) openEnds++;

            nx = x - dx; ny = y - dy;
            while (board.IsInside(nx, ny) && board.GetCell(nx, ny) == color) { count++; nx -= dx; ny -= dy; }
            if (board.IsInside(nx, ny) && board.IsEmpty(nx, ny)) openEnds++;

            return ScoreFor(count, openEnds);
        }

        private static long ScoreFor(int count, int openEnds)
        {
            if (count >= 5) return WinScore;
            if (openEnds == 0) return 0;
            switch (count)
            {
                case 4: return openEnds == 2 ? OpenFourScore : FourScore;
                case 3: return openEnds == 2 ? OpenThreeScore : ThreeScore;
                case 2: return openEnds == 2 ? OpenTwoScore : TwoScore;
                default: return openEnds == 2 ? 100 : 20;
            }
        }

        public static StoneColor Other(StoneColor c) => c == StoneColor.Black ? StoneColor.White : StoneColor.Black;
    }
}
