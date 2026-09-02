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

        // 迭代加深的时间预算（AI 搜索同一时刻只有一份在跑，静态字段安全）
        private static System.Diagnostics.Stopwatch s_watch;
        private static long s_deadlineMs;
        private static bool s_aborted;

        // 置换表：相同局面（且深度足够）直接复用已有搜索结果，是深层搜索的主要提速手段
        private struct TtEntry { public int depth; public long score; public byte flag; } // flag: 0精确 1下界 2上界
        private static readonly Dictionary<ulong, TtEntry> s_tt = new Dictionary<ulong, TtEntry>(1 << 16);
        private static readonly ulong[,,] s_zobrist = BuildZobrist();

        private static ulong[,,] BuildZobrist()
        {
            var z = new ulong[GomokuBoard.Size, GomokuBoard.Size, 2];
            var r = new System.Random(0x5DEECE66);
            var buf = new byte[8];
            for (int x = 0; x < GomokuBoard.Size; x++)
                for (int y = 0; y < GomokuBoard.Size; y++)
                    for (int c = 0; c < 2; c++)
                    {
                        r.NextBytes(buf);
                        z[x, y, c] = BitConverter.ToUInt64(buf, 0);
                    }
            return z;
        }

        private static ulong HashBoard(GomokuBoard board)
        {
            ulong h = 0;
            for (int x = 0; x < GomokuBoard.Size; x++)
                for (int y = 0; y < GomokuBoard.Size; y++)
                {
                    var c = board.GetCell(x, y);
                    if (c == StoneColor.None) continue;
                    h ^= s_zobrist[x, y, c == StoneColor.Black ? 0 : 1];
                }
            return h;
        }

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

            // 奇数前瞻（3、5）的搜索终点落在"自己刚落子"之后，评估看不到对手下一手的反击，
            // 会系统性高估局面（实测 S3/S5 反而输给 S2/S4），统一加一层让终点落在对手落子后
            if (searchDepth >= 3 && (searchDepth & 1) == 1) searchDepth++;

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
            // 时间预算：深度越高思考越久，但有硬上限（否则深层搜索一步可达十几秒甚至几分钟）
            s_watch = System.Diagnostics.Stopwatch.StartNew();
            s_deadlineMs = 800 + depth * 400; // D4≈2.4s D6≈3.2s D8≈4s
            s_aborted = false;
            s_tt.Clear();
            ulong rootHash = HashBoard(board);

            // 按评分预排序，先评估最有潜力的走法（便于剪枝）；分数只算一次再排序，避免比较器内重复评估。
            // 注意：排序用标准防御权重——mult 若压低此权重会把关键防守点挤出搜索窗口，越凶反而越菜（实测）
            var scored = new List<(Vector2Int p, long s)>(candidates.Count);
            foreach (var c in candidates)
                scored.Add((c, EvaluatePoint(board, c.x, c.y, aiColor, 1.0f) + (long)(EvaluatePoint(board, c.x, c.y, oppColor, 1.0f) * DEFENSE_WEIGHT)));
            scored.Sort((x, y) => y.s.CompareTo(x.s));

            // 迭代加深：从浅到深反复搜索，超时保留上一层完整结果；
            // 上一层的最佳点提到队首，能显著加速深层的剪枝
            Vector2Int best = scored[0].p;

            for (int d = 2; d <= depth; d++)
            {
                int limit = Mathf.Min(scored.Count, Mathf.Max(15, d * 5));
                Vector2Int iterBest = scored[0].p;
                long iterBestScore = long.MinValue;
                bool iterCompleted = true;

                for (int i = 0; i < limit; i++)
                {
                    Vector2Int p = scored[i].p;

                    // 模拟 AI 落子
                    board.TryPlace(p.x, p.y, aiColor);

                    // 检查是否直接赢了
                    if (board.HasWinningPattern(p.x, p.y))
                    {
                        board.TryUndoLast(out _);
                        return p;
                    }

                    // 递归评估对手最佳应对（子节点值从对手视角返回，取负即 AI 视角）
                    ulong childHash = rootHash ^ s_zobrist[p.x, p.y, aiColor == StoneColor.Black ? 0 : 1];
                    long score = -Lookahead(board, d - 1, long.MinValue + 1, long.MaxValue - 1, oppColor, mult, childHash);

                    board.TryUndoLast(out _);

                    if (s_aborted) { iterCompleted = false; break; }

                    // 中心偏好
                    int centerDist = Mathf.Abs(p.x - 7) + Mathf.Abs(p.y - 7);
                    score += (14 - centerDist) * 3;
                    score += rng.Next(10);

                    if (score > iterBestScore)
                    {
                        iterBestScore = score;
                        iterBest = p;
                    }
                }

                if (!iterCompleted) break;
                best = iterBest;

                int bi = scored.FindIndex(t => t.p == best);
                if (bi > 0) { var tmp = scored[0]; scored[0] = scored[bi]; scored[bi] = tmp; }

                // 已找到必胜线，无需更深
                if (iterBestScore >= WIN_SCORE / 2) break;
            }

            s_watch = null;
            return best;
        }

        /// <summary>N 层前瞻（Alpha-Beta 剪枝 + 置换表）。negamax 约定：返回值始终从当前行棋方 cur 的视角出发。</summary>
        private static long Lookahead(GomokuBoard board, int depth, long alpha, long beta,
                                      StoneColor cur, float mult, ulong hash)
        {
            // 上一手（Other(cur) 所下）若已成五，对 cur 是最差局面
            if (board.MoveCount > 0)
            {
                var last = board.History[board.MoveCount - 1];
                if (board.HasWinningPattern(last.X, last.Y))
                    return last.Color == cur ? WIN_SCORE : -WIN_SCORE;
            }

            // 超时：快速上抛（本轮迭代结果会被根节点整层丢弃）
            if (s_watch != null && s_watch.ElapsedMilliseconds > s_deadlineMs)
            {
                s_aborted = true;
                return alpha;
            }

            long origAlpha = alpha;

            // 置换表命中：同一局面且已搜过不低于当前深度，直接复用结果
            if (s_tt.TryGetValue(hash, out var hit))
            {
                if (hit.flag == 0 && hit.depth >= depth) return hit.score;
                if (hit.flag == 1 && hit.depth >= depth && hit.score >= beta) return hit.score;
                if (hit.flag == 2 && hit.depth >= depth && hit.score <= alpha) return hit.score;
            }

            // 叶子节点：返回当前局面评分（cur 视角）
            if (depth == 0)
                return EvaluateBoard(board, cur);

            List<Vector2Int> candidates = GetCandidates(board);
            if (candidates.Count == 0)
                return EvaluateBoard(board, cur);

            // 预排序（分数只算一次再排序；同样用标准防御权重，mult 不参与，理由同根节点排序）
            StoneColor opp = Other(cur);
            var scored = new List<(Vector2Int p, long s)>(candidates.Count);
            foreach (var c in candidates)
                scored.Add((c, EvaluatePoint(board, c.x, c.y, cur, 1.0f) + (long)(EvaluatePoint(board, c.x, c.y, opp, 1.0f) * DEFENSE_WEIGHT)));
            scored.Sort((x, y) => y.s.CompareTo(x.s));

            // 固定分支宽度：越深越宽会让节点数指数爆炸，剪枝收益远大于宽度收益
            int limit = Mathf.Min(scored.Count, 12);
            StoneColor next = Other(cur);
            long bestScore = long.MinValue;

            for (int i = 0; i < limit; i++)
            {
                Vector2Int p = scored[i].p;

                ulong childHash = hash ^ s_zobrist[p.x, p.y, cur == StoneColor.Black ? 0 : 1];
                board.TryPlace(p.x, p.y, cur);

                // cur 亲手成五：从 cur 视角必是最高分
                if (board.HasWinningPattern(p.x, p.y))
                {
                    board.TryUndoLast(out _);
                    return WIN_SCORE;
                }

                long score = -Lookahead(board, depth - 1, -beta, -alpha, next, mult, childHash);

                board.TryUndoLast(out _);

                if (s_aborted) return 0;

                if (score > bestScore) bestScore = score;
                if (score > alpha) alpha = score;
                if (alpha >= beta) break;
            }

            // 存入置换表（浅层不存以控制内存；被超时中断的本轮不存）
            if (depth >= 2 && s_tt.Count < 1_500_000)
            {
                byte flag = bestScore <= origAlpha ? (byte)2 : (bestScore >= beta ? (byte)1 : (byte)0);
                s_tt[hash] = new TtEntry { depth = depth, score = bestScore, flag = flag };
            }

            return bestScore;
        }

        // ============================================================
        //  叶子节点评估
        // ============================================================

        /// <summary>叶子局面评估：从当前行棋方 cur 视角返回分值（攻分 − 防分×防御权重），供 negamax 直接取用。</summary>
        private static long EvaluateBoard(GomokuBoard board, StoneColor cur)
        {
            StoneColor opp = Other(cur);
            long score = 0;
            // 只统计已有棋子邻域内的空点：比扫全盘快，也滤掉远端空点的恒定噪声
            foreach (Vector2Int p in GetCandidates(board))
            {
                score += EvaluatePoint(board, p.x, p.y, cur, 1.0f);
                score -= (long)(EvaluatePoint(board, p.x, p.y, opp, 1.0f) * DEFENSE_WEIGHT);
            }
            return score;
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
