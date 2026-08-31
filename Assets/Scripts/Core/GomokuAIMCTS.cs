using System;
using System.Collections.Generic;
using UnityEngine;
using Wuziqi.Game;

namespace Wuziqi.Core
{
    /// <summary>五子棋 AI：蒙特卡洛树搜索（MCTS），难度由迭代次数控制。</summary>
    public static class GomokuAIMCTS
    {
        // 后台线程安全的随机数生成器
        [ThreadStatic] private static System.Random rng;
        private static System.Random Rng => rng ??= new System.Random();
        // ============================================================
        //  节点定义
        // ============================================================

        private class Node
        {
            public Node Parent;
            public List<Node> Children;
            public int X, Y;
            public StoneColor Color;        // 谁下了这步棋
            public int Visits;
            public double Wins;             // 从当前节点视角的胜场
            public List<(int x, int y)> UntriedMoves;

            public Node(Node parent, int x, int y, StoneColor color, List<(int x, int y)> untried)
            {
                Parent = parent;
                X = x; Y = y;
                Color = color;
                UntriedMoves = untried;
                Children = new List<Node>();
            }

            /// <summary>UCT 选分最高的子节点。</summary>
            public Node BestChild(double c)
            {
                Node best = null;
                double bestScore = double.MinValue;

                foreach (Node child in Children)
                {
                    // UCT = 胜率 + 探索项
                    double winRate = child.Wins / child.Visits;
                    double explore = c * Math.Sqrt(Math.Log(Visits) / child.Visits);
                    double score = winRate + explore;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = child;
                    }
                }
                return best;
            }

            /// <summary>选实际胜率最高的子节点（最终决策用）。</summary>
            public Node BestChildByWinRate()
            {
                Node best = null;
                double bestRate = -1;

                foreach (Node child in Children)
                {
                    double rate = child.Visits > 0 ? child.Wins / child.Visits : 0;
                    if (rate > bestRate)
                    {
                        bestRate = rate;
                        best = child;
                    }
                }
                return best;
            }
        }

        // ============================================================
        //  公开入口
        // ============================================================

        /// <summary>MCTS 搜索最佳落子。</summary>
        /// <param name="board">棋盘（会修改，外部需传副本）</param>
        /// <param name="aiColor">AI 颜色</param>
        /// <param name="searchDepth">1-10，映射为迭代次数：depth × 500</param>
        /// <param name="scoreMultiplier">0.1-3.0，映射为 UCT 探索常数</param>
        public static Vector2Int FindBestMove(
            GomokuBoard board, StoneColor aiColor,
            int searchDepth = 3, float scoreMultiplier = 1.0f)
        {
            // 空盘下天元
            if (board.MoveCount == 0)
                return new Vector2Int(GomokuBoard.Size / 2, GomokuBoard.Size / 2);

            // 迭代次数：depth 1→500, 5→2500, 10→5000
            int iterations = Math.Clamp(searchDepth * 500, 200, 5000);

            // UCT 探索常数：默认 √2 ≈ 1.414，scoreMultiplier 缩放
            double c = scoreMultiplier * 1.414;

            StoneColor oppColor = Other(aiColor);

            // ---- 快速必胜/必防检测 ----
            Vector2Int win = FindImmediateWin(board, aiColor);
            if (win.x >= 0) return win;

            Vector2Int block = FindImmediateWin(board, oppColor);
            if (block.x >= 0) return block;

            // ---- MCTS 搜索 ----
            // 根节点：轮到 AI 下棋
            List<(int x, int y)> rootMoves = GetEmptyCells(board);
            Node root = new Node(null, -1, -1, oppColor, rootMoves);
            // root.Color = oppColor 因为上一手是对手，现在轮 AI

            for (int i = 0; i < iterations; i++)
            {
                // 1. Selection — 从根沿 UCT 向下走
                Node node = root;
                StoneColor turnColor = aiColor;

                while (node.UntriedMoves.Count == 0 && node.Children.Count > 0)
                {
                    node = node.BestChild(c);
                    turnColor = Other(turnColor);
                }

                // 2. Expansion — 展开一个未尝试的走法
                if (node.UntriedMoves.Count > 0)
                {
                    var moves = node.UntriedMoves;
                    int idx = Rng.Next(moves.Count);
                    var (mx, my) = moves[idx];
                    moves.RemoveAt(idx);

                    board.TryPlace(mx, my, turnColor);

                    List<(int x, int y)> childMoves = GetEmptyCellsFiltered(board, mx, my);
                    Node child = new Node(node, mx, my, turnColor, childMoves);
                    node.Children.Add(child);
                    node = child;
                    turnColor = Other(turnColor);
                }

                // 3. Simulation — 随机下棋直到结束
                StoneColor simWinner = Simulate(board, turnColor);

                // 4. Backpropagation — 回传结果
                Backpropagate(node, simWinner, aiColor);

                // 撤销展开时落的子
                // （Simulation 中落的子在 Simulate 结束后已全部撤销）
                // 只需撤销 Expansion 阶段落的那一步
                if (node.Parent != null)
                    board.TryUndoLast(out _);
            }

            // ---- 决策：选访问次数最多的子节点（更稳健） ----
            Node best = BestByVisits(root);
            return best != null ? new Vector2Int(best.X, best.Y) : new Vector2Int(-1, -1);
        }

        // ============================================================
        //  Selection 辅助：选访问次数最多的子（最终决策更稳）
        // ============================================================

        private static Node BestByVisits(Node root)
        {
            Node best = null;
            int bestVisits = -1;
            foreach (Node child in root.Children)
            {
                if (child.Visits > bestVisits)
                {
                    bestVisits = child.Visits;
                    best = child;
                }
            }
            return best;
        }

        // ============================================================
        //  Simulation：随机对局直到分出胜负
        // ============================================================

        private static StoneColor Simulate(GomokuBoard board, StoneColor turnColor)
        {
            // 快速检查当前是否已经有人赢了（展开的那步可能直接赢）
            if (board.MoveCount > 0)
            {
                var last = board.History[board.MoveCount - 1];
                if (board.HasWinningPattern(last.X, last.Y))
                    return last.Color;
            }

            // 记录模拟中下的所有棋子，结束后全部回退
            int simStartCount = board.MoveCount;
            StoneColor winner = StoneColor.None;

            while (!board.IsFull)
            {
                List<(int x, int y)> empties = GetEmptyCells(board);
                if (empties.Count == 0) break;

                var (x, y) = empties[Rng.Next(empties.Count)];
                board.TryPlace(x, y, turnColor);

                if (board.HasWinningPattern(x, y))
                {
                    winner = turnColor;
                    break;
                }

                turnColor = Other(turnColor);
            }

            // 回退所有模拟落子
            while (board.MoveCount > simStartCount)
                board.TryUndoLast(out _);

            return winner;
        }

        // ============================================================
        //  Backpropagation：沿路径回传结果
        // ============================================================

        private static void Backpropagate(Node node, StoneColor winner, StoneColor aiColor)
        {
            while (node != null)
            {
                node.Visits++;

                // 从 AI 视角：AI 赢了 +1，对手赢了不加分，平局 +0.5
                if (winner == aiColor)
                    node.Wins += 1.0;
                else if (winner == StoneColor.None)
                    node.Wins += 0.5;
                // 对手赢了：Wins 不增加

                node = node.Parent;
            }
        }

        // ============================================================
        //  必胜检测
        // ============================================================

        private static Vector2Int FindImmediateWin(GomokuBoard board, StoneColor color)
        {
            for (int x = 0; x < GomokuBoard.Size; x++)
            {
                for (int y = 0; y < GomokuBoard.Size; y++)
                {
                    if (!board.IsEmpty(x, y)) continue;

                    board.TryPlace(x, y, color);
                    bool win = board.HasWinningPattern(x, y);
                    board.TryUndoLast(out _);

                    if (win) return new Vector2Int(x, y);
                }
            }
            return new Vector2Int(-1, -1);
        }

        // ============================================================
        //  获取空位
        // ============================================================

        private static List<(int x, int y)> GetEmptyCells(GomokuBoard board)
        {
            List<(int x, int y)> cells = new List<(int x, int y)>();
            for (int x = 0; x < GomokuBoard.Size; x++)
                for (int y = 0; y < GomokuBoard.Size; y++)
                    if (board.IsEmpty(x, y))
                        cells.Add((x, y));
            return cells;
        }

        /// <summary>只取已有棋子周围 2 格内的空位，大幅减少无效模拟。</summary>
        private static List<(int x, int y)> GetEmptyCellsFiltered(GomokuBoard board, int lastX, int lastY)
        {
            List<(int x, int y)> cells = new List<(int x, int y)>();
            bool[,] added = new bool[GomokuBoard.Size, GomokuBoard.Size];

            // 扫描所有已有棋子，收集周围空位
            for (int x = 0; x < GomokuBoard.Size; x++)
            {
                for (int y = 0; y < GomokuBoard.Size; y++)
                {
                    if (board.IsEmpty(x, y)) continue;

                    for (int dx = -2; dx <= 2; dx++)
                    {
                        for (int dy = -2; dy <= 2; dy++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (!board.IsInside(nx, ny) || !board.IsEmpty(nx, ny)) continue;
                            if (added[nx, ny]) continue;
                            added[nx, ny] = true;
                            cells.Add((nx, ny));
                        }
                    }
                }
            }

            // 兜底：如果过滤后为空（极端情况），返回所有空位
            if (cells.Count == 0)
                return GetEmptyCells(board);

            return cells;
        }

        public static StoneColor Other(StoneColor c)
            => c == StoneColor.Black ? StoneColor.White : StoneColor.Black;
    }
}
