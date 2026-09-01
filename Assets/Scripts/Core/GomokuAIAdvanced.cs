using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wuziqi.Core
{
    /// <summary>五子棋 AI：启发式评分 + N 层前瞻，难度由搜索深度控制。</summary>
    public static class GomokuAIAdvanced
    {
        // 棋型分值
        private const long WIN_SCORE       = 10_000_000;
        private const long OPEN_FOUR_SCORE =   1_000_000;
        private const long FOUR_SCORE      =     100_000;
        private const long OPEN_THREE_SCORE =    200_000;
        private const long THREE_SCORE     =      8_000;
        private const long OPEN_TWO_SCORE  =      2_000;
        private const long TWO_SCORE       =        200;

        private const float DEFENSE_WEIGHT = 0.9f;
        private const int NEIGHBOR_RADIUS = 3;

        // 方向
        private static readonly int[][] DIRS = new int[][] {
            new int[] {1, 0}, new int[] {0, 1},
            new int[] {1, 1}, new int[] {1, -1}
        };

        // ============================================================
        //  公开入口
        // ============================================================

        /// <param name="searchDepth">1=纯评分, 2+=前瞻层数（越大越强）</param>
        /// <param name="scoreMultiplier">攻击性系数，越大越凶（优先攻击而非防守）</param>
        public static Vector2Int FindBestMove(
            GomokuBoard board, StoneColor aiColor,
            int searchDepth = 1, float scoreMultiplier = 1.0f,
            System.Random rng = null)
        {
            rng ??= new System.Random();

            // 空盘下天元
            if (board.MoveCount == 0)
                return new Vector2Int(GomokuBoard.Size / 2, GomokuBoard.Size / 2);

            StoneColor oppColor = Other(aiColor);

            // 1. 必胜检测（下一步直接五连）
            Vector2Int win = FindImmediateWin(board, aiColor);
            if (win.x >= 0) return win;

            // 2. 必挡检测（对手下一步直接五连）
            Vector2Int block = FindImmediateWin(board, oppColor);
            if (block.x >= 0) return block;

            // 3. 紧急威胁检测（对手有开放三连，两步后必赢，必须堵）
            {
                Vector2Int urgent = FindUrgentThreat(board, oppColor, aiColor);
                if (urgent.x >= 0) return urgent;

                // 自己的紧急进攻机会
                Vector2Int attack = FindUrgentThreat(board, aiColor, oppColor);
                if (attack.x >= 0) return attack;
            }

            // 收集候选点
            List<Vector2Int> candidates = GetCandidates(board);
            if (candidates.Count == 0)
                return new Vector2Int(GomokuBoard.Size / 2, GomokuBoard.Size / 2);

            // depth <= 1：纯启发式评分（最快）
            if (searchDepth <= 1)
                return PickBestByScore(board, candidates, aiColor, oppColor, scoreMultiplier, rng);

            // depth >= 2：带前瞻的搜索
            return SearchWithLookahead(board, candidates, aiColor, oppColor, searchDepth, scoreMultiplier, rng);
        }

        // ============================================================
        //  纯启发式评分（depth=1，和原始算法一致）
        // ============================================================

        private static Vector2Int PickBestByScore(
            GomokuBoard board, List<Vector2Int> candidates,
            StoneColor aiColor, StoneColor oppColor, float mult, System.Random rng)
        {
            Vector2Int best = candidates[0];
            long bestScore = long.MinValue;

            foreach (Vector2Int p in candidates)
            {
                long attack  = EvaluatePoint(board, p.x, p.y, aiColor, 1.0f);
                long defend  = EvaluatePoint(board, p.x, p.y, oppColor, 1.0f);
                // mult>1时更凶（攻击权重高），<1时更保守（防御权重高）
                float defWeight = DEFENSE_WEIGHT / mult;
                long score   = attack + (long)(defend * defWeight);

                // 中心偏好
                int centerDist = Mathf.Abs(p.x - 7) + Mathf.Abs(p.y - 7);
                score += (14 - centerDist) * 3;

                // 随机扰动
                score += rng.Next(20);

                if (score > bestScore)
                {
                    bestScore = score;
                    best = p;
                }
            }
            return best;
        }

        // ============================================================
        //  带前瞻搜索（depth>=2）
        // ============================================================

        private static Vector2Int SearchWithLookahead(
            GomokuBoard board, List<Vector2Int> candidates,
            StoneColor aiColor, StoneColor oppColor, int depth, float mult, System.Random rng)
        {
            Vector2Int best = candidates[0];
            long bestScore = long.MinValue;

            // 按评分预排序，先评估最有潜力的走法（便于剪枝）
            float defWeight = DEFENSE_WEIGHT / mult;
            candidates.Sort((a, b) =>
            {
                long sa = EvaluatePoint(board, a.x, a.y, aiColor, 1.0f) + (long)(EvaluatePoint(board, a.x, a.y, oppColor, 1.0f) * defWeight);
                long sb = EvaluatePoint(board, b.x, b.y, aiColor, 1.0f) + (long)(EvaluatePoint(board, b.x, b.y, oppColor, 1.0f) * defWeight);
                return sb.CompareTo(sa);
            });

            // 限制前瞻的候选数（难度越高看得越多）
            int limit = Mathf.Min(candidates.Count, Mathf.Max(15, depth * 5));

            for (int i = 0; i < limit; i++)
            {
                Vector2Int p = candidates[i];

                // 模拟 AI 落子
                board.TryPlace(p.x, p.y, aiColor);

                // 检查是否直接赢了
                if (board.HasWinningPattern(p.x, p.y))
                {
                    board.TryUndoLast(out _);
                    return p;
                }

                // 递归评估对手最佳应对
                long score = -Lookahead(board, depth - 1, long.MinValue + 1, long.MaxValue - 1, oppColor, aiColor, mult);

                // 中心偏好
                int centerDist = Mathf.Abs(p.x - 7) + Mathf.Abs(p.y - 7);
                score += (14 - centerDist) * 3;
                score += rng.Next(10);

                board.TryUndoLast(out _);

                if (score > bestScore)
                {
                    bestScore = score;
                    best = p;
                }
            }

            return best;
        }

        /// <summary>N 层前瞻（Alpha-Beta 剪枝）。</summary>
        private static long Lookahead(GomokuBoard board, int depth, long alpha, long beta,
                                      StoneColor cur, StoneColor aiColor, float mult)
        {
            // 检查上一手是否已经赢了
            if (board.MoveCount > 0)
            {
                var last = board.History[board.MoveCount - 1];
                if (board.HasWinningPattern(last.X, last.Y))
                    return last.Color == aiColor ? WIN_SCORE : -WIN_SCORE;
            }

            // 叶子节点：返回当前局面评分
            if (depth == 0)
                return EvaluateBoard(board, cur, aiColor, mult);

            List<Vector2Int> candidates = GetCandidates(board);
            if (candidates.Count == 0)
                return EvaluateBoard(board, cur, aiColor, mult);

            // 预排序
            StoneColor opp = Other(cur);
            float innerDefWeight = DEFENSE_WEIGHT / mult;
            candidates.Sort((a, b) =>
            {
                long sa = EvaluatePoint(board, a.x, a.y, cur, 1.0f) + (long)(EvaluatePoint(board, a.x, a.y, opp, 1.0f) * innerDefWeight);
                long sb = EvaluatePoint(board, b.x, b.y, cur, 1.0f) + (long)(EvaluatePoint(board, b.x, b.y, opp, 1.0f) * innerDefWeight);
                return sb.CompareTo(sa);
            });

            int limit = Mathf.Min(candidates.Count, Mathf.Max(12, depth * 4));
            StoneColor next = Other(cur);
            long bestScore = long.MinValue;

            for (int i = 0; i < limit; i++)
            {
                Vector2Int p = candidates[i];

                board.TryPlace(p.x, p.y, cur);

                // 立即检测赢棋
                if (board.HasWinningPattern(p.x, p.y))
                {
                    board.TryUndoLast(out _);
                    return cur == aiColor ? WIN_SCORE : -WIN_SCORE;
                }

                long score = -Lookahead(board, depth - 1, -beta, -alpha, next, aiColor, mult);

                board.TryUndoLast(out _);

                if (score > bestScore) bestScore = score;
                if (score > alpha) alpha = score;
                if (alpha >= beta) break;
            }

            return bestScore;
        }

        // ============================================================
        //  叶子节点评估
        // ============================================================

        private static long EvaluateBoard(GomokuBoard board, StoneColor cur, StoneColor aiColor, float mult)
        {
            StoneColor opp = Other(cur);
            // mult影响攻防权重：cur==aiColor时用mult，否则用1/mult
            float aggression = cur == aiColor ? mult : (1f / Mathf.Max(mult, 0.1f));
            float defWeight = DEFENSE_WEIGHT / aggression;
            long score = 0;
            for (int x = 0; x < GomokuBoard.Size; x++)
            {
                for (int y = 0; y < GomokuBoard.Size; y++)
                {
                    if (!board.IsEmpty(x, y)) continue;
                    score += EvaluatePoint(board, x, y, cur, 1.0f);
                    score -= (long)(EvaluatePoint(board, x, y, opp, 1.0f) * defWeight);
                }
            }
            return cur == aiColor ? score : -score;
        }

        // ============================================================
        //  单点评分
        // ============================================================

        private static long EvaluatePoint(GomokuBoard board, int x, int y, StoneColor color, float mult)
        {
            long total = 0;
            foreach (int[] dir in DIRS)
                total += (long)(LineScore(board, x, y, color, dir[0], dir[1]) * mult);
            return total;
        }

        private static long LineScore(GomokuBoard board, int x, int y, StoneColor color, int dx, int dy)
        {
            int count = 1;
            int openEnds = 0;

            // 正向
            int nx = x + dx, ny = y + dy;
            while (board.IsInside(nx, ny) && board.GetCell(nx, ny) == color) { count++; nx += dx; ny += dy; }
            if (board.IsInside(nx, ny) && board.IsEmpty(nx, ny)) openEnds++;

            // 反向
            nx = x - dx; ny = y - dy;
            while (board.IsInside(nx, ny) && board.GetCell(nx, ny) == color) { count++; nx -= dx; ny -= dy; }
            if (board.IsInside(nx, ny) && board.IsEmpty(nx, ny)) openEnds++;

            return ScoreFor(count, openEnds);
        }

        private static long ScoreFor(int count, int openEnds)
        {
            if (count >= 5) return WIN_SCORE;
            if (openEnds == 0) return 0;
            switch (count)
            {
                case 4: return openEnds == 2 ? OPEN_FOUR_SCORE : FOUR_SCORE;
                case 3: return openEnds == 2 ? OPEN_THREE_SCORE : THREE_SCORE;
                case 2: return openEnds == 2 ? OPEN_TWO_SCORE  : TWO_SCORE;
                default: return openEnds == 2 ? 100 : 20;
            }
        }

        // ============================================================
        //  候选收集（已有棋子周围 2 格）
        // ============================================================

        private static List<Vector2Int> GetCandidates(GomokuBoard board)
        {
            List<Vector2Int> list = new List<Vector2Int>();
            bool[,] mark = new bool[GomokuBoard.Size, GomokuBoard.Size];

            for (int x = 0; x < GomokuBoard.Size; x++)
            {
                for (int y = 0; y < GomokuBoard.Size; y++)
                {
                    if (board.IsEmpty(x, y)) continue;
                    for (int dx = -NEIGHBOR_RADIUS; dx <= NEIGHBOR_RADIUS; dx++)
                        for (int dy = -NEIGHBOR_RADIUS; dy <= NEIGHBOR_RADIUS; dy++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (board.IsInside(nx, ny) && board.IsEmpty(nx, ny) && !mark[nx, ny])
                            {
                                mark[nx, ny] = true;
                                list.Add(new Vector2Int(nx, ny));
                            }
                        }
                }
            }
            return list;
        }

        // ============================================================
        //  必胜检测
        // ============================================================

        private static Vector2Int FindImmediateWin(GomokuBoard board, StoneColor color)
        {
            foreach (Vector2Int p in GetCandidates(board))
            {
                board.TryPlace(p.x, p.y, color);
                bool win = board.HasWinningPattern(p.x, p.y);
                board.TryUndoLast(out _);
                if (win) return p;
            }
            return new Vector2Int(-1, -1);
        }

        /// <summary>紧急威胁：对手有开放三连（两步后必赢），必须堵。</summary>
        private static Vector2Int FindUrgentThreat(GomokuBoard board, StoneColor threatColor, StoneColor blockColor)
        {
            foreach (Vector2Int p in GetCandidates(board))
            {
                // 模拟对手在此落子
                board.TryPlace(p.x, p.y, threatColor);
                bool urgent = false;
                int threatDx = 0, threatDy = 0;

                // 检查是否形成开放四连（4连+两端都空 → 不可阻挡）
                foreach (int[] dir in DIRS)
                {
                    int count = 1;
                    int openEnds = 0;

                    int nx = p.x + dir[0], ny = p.y + dir[1];
                    while (board.IsInside(nx, ny) && board.GetCell(nx, ny) == threatColor) { count++; nx += dir[0]; ny += dir[1]; }
                    if (board.IsInside(nx, ny) && board.IsEmpty(nx, ny)) openEnds++;

                    nx = p.x - dir[0]; ny = p.y - dir[1];
                    while (board.IsInside(nx, ny) && board.GetCell(nx, ny) == threatColor) { count++; nx -= dir[0]; ny -= dir[1]; }
                    if (board.IsInside(nx, ny) && board.IsEmpty(nx, ny)) openEnds++;

                    if (count >= 4 && openEnds >= 2) { urgent = true; threatDx = dir[0]; threatDy = dir[1]; break; }
                }

                board.TryUndoLast(out _);

                // 找到紧急威胁，直接抢占对手要下的位置
                if (urgent) return p;
            }
            return new Vector2Int(-1, -1);
        }

        public static StoneColor Other(StoneColor c)
            => c == StoneColor.Black ? StoneColor.White : StoneColor.Black;
    }
}
