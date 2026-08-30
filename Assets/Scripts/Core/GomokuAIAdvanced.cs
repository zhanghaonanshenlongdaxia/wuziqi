using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wuziqi.Core
{
    /// <summary>
    /// 高级五子棋AI：Alpha-Beta搜索 + 精细评估函数
    /// 支持3层搜索，识别多种棋型，具有较强的攻防能力
    /// </summary>
    public static class GomokuAIAdvanced
    {
        private static float currentScoreMultiplier = 1.0f;
        // 棋型评分常量
        private const long WIN_SCORE = 10000000;
        private const long OPEN_FOUR_SCORE = 1000000;
        private const long FOUR_SCORE = 100000;
        private const long OPEN_THREE_SCORE = 50000;
        private const long THREE_SCORE = 5000;
        private const long OPEN_TWO_SCORE = 1000;
        private const long TWO_SCORE = 100;
        
        // 搜索深度
        private static int searchDepth = 3;
        // 候选点数量限制（每层搜索）
        private const int MAX_CANDIDATES = 15;
        
        // 方向向量
        private static readonly int[][] DIRECTIONS = new int[][] {
            new int[] {1, 0},   // 水平
            new int[] {0, 1},   // 垂直
            new int[] {1, 1},   // 右下对角线
            new int[] {1, -1}   // 右上对角线
        };
        
        /// <summary>
        /// 查找最佳落子位置
        /// </summary>
        public static Vector2Int FindBestMove(GomokuBoard board, StoneColor aiColor, int searchDepthParam = 3, float scoreMultiplierParam = 1.0f, System.Random rng = null)
        {
            rng ??= new System.Random();
            StoneColor oppColor = Other(aiColor);
            searchDepth = searchDepthParam;
            currentScoreMultiplier = scoreMultiplierParam;
            
            // 空棋盘下中心点
            if (board.MoveCount == 0)
                return new Vector2Int(GomokuBoard.Size / 2, GomokuBoard.Size / 2);
            
            // 1. 检查是否有立即获胜的落子
            Vector2Int immediateWin = FindWinningMove(board, aiColor);
            if (immediateWin.x >= 0) return immediateWin;
            
            // 2. 检查是否需要阻止对手立即获胜
            Vector2Int immediateBlock = FindWinningMove(board, oppColor);
            if (immediateBlock.x >= 0) return immediateBlock;
            
            // 3. Alpha-Beta搜索
            List<Vector2Int> candidates = GetSortedCandidates(board, aiColor);
            Vector2Int bestMove = candidates[0];
            long bestScore = long.MinValue;
            
            foreach (Vector2Int move in candidates)
            {
                // 模拟落子
                board.TryPlace(move.x, move.y, aiColor);
                
                // 递归搜索
                long score = -AlphaBeta(board, searchDepth - 1, long.MinValue, long.MaxValue, oppColor, aiColor, rng);
                
                // 撤销落子
                board.TryUndoLast(out _);
                
                // 添加随机扰动避免完全相同
                score += rng.Next(10);
                
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
            }
            
            return bestMove;
        }
        
        /// <summary>
        /// Alpha-Beta搜索算法
        /// </summary>
        private static long AlphaBeta(GomokuBoard board, int depth, long alpha, long beta, 
                                      StoneColor currentColor, StoneColor aiColor, System.Random rng)
        {
            // 终止条件：搜索到底或游戏结束
            if (depth == 0 || board.IsFull)
            {
                return EvaluateBoard(board, aiColor);
            }
            
            // 检查是否有立即获胜/失败
            Vector2Int winMove = FindWinningMove(board, currentColor);
            if (winMove.x >= 0)
            {
                return currentColor == aiColor ? WIN_SCORE : -WIN_SCORE;
            }
            
            StoneColor nextColor = Other(currentColor);
            List<Vector2Int> candidates = GetSortedCandidates(board, currentColor);
            
            long bestScore = long.MinValue;
            
            foreach (Vector2Int move in candidates)
            {
                // 模拟落子
                board.TryPlace(move.x, move.y, currentColor);
                
                // 递归搜索
                long score = -AlphaBeta(board, depth - 1, -beta, -alpha, nextColor, aiColor, rng);
                
                // 撤销落子
                board.TryUndoLast(out _);
                
                if (score > bestScore)
                {
                    bestScore = score;
                }
                
                // Alpha-Beta剪枝
                if (score > alpha)
                {
                    alpha = score;
                }
                if (alpha >= beta)
                {
                    break;
                }
            }
            
            return bestScore;
        }
        
        /// <summary>
        /// 查找立即获胜的落子位置
        /// </summary>
        private static Vector2Int FindWinningMove(GomokuBoard board, StoneColor color)
        {
            for (int x = 0; x < GomokuBoard.Size; x++)
            {
                for (int y = 0; y < GomokuBoard.Size; y++)
                {
                    if (board.IsEmpty(x, y))
                    {
                        // 尝试落子
                        board.TryPlace(x, y, color);
                        
                        // 检查是否形成五连
                        List<Vector2Int> winLine = board.FindWinningLine(x, y);
                        
                        // 撤销落子
                        board.TryUndoLast(out _);
                        
                        if (winLine != null)
                        {
                            return new Vector2Int(x, y);
                        }
                    }
                }
            }
            
            return new Vector2Int(-1, -1);
        }
        
        /// <summary>
        /// 获取排序后的候选点列表（优先考虑威胁大的位置）
        /// </summary>
        private static List<Vector2Int> GetSortedCandidates(GomokuBoard board, StoneColor color)
        {
            List<Vector2Int> candidates = new List<Vector2Int>();
            StoneColor oppColor = Other(color);
            
            // 收集所有空位（限制数量以提高性能）
            for (int x = 0; x < GomokuBoard.Size; x++)
            {
                for (int y = 0; y < GomokuBoard.Size; y++)
                {
                    if (board.IsEmpty(x, y))
                    {
                        // 计算该位置的威胁值
                        long threat = EvaluatePointAdvanced(board, x, y, color) + 
                                     EvaluatePointAdvanced(board, x, y, oppColor);
                        
                        // 中心偏好
                        int centerDist = Mathf.Abs(x - 7) + Mathf.Abs(y - 7);
                        threat += (14 - centerDist) * 10;
                        
                        candidates.Add(new Vector2Int(x, y));
                    }
                }
            }
            
            // 按威胁值排序（这里简化处理，实际可以更精确）
            candidates.Sort((a, b) => 
            {
                long scoreA = EvaluatePointAdvanced(board, a.x, a.y, color) + 
                             EvaluatePointAdvanced(board, a.x, a.y, oppColor);
                long scoreB = EvaluatePointAdvanced(board, b.x, b.y, color) + 
                             EvaluatePointAdvanced(board, b.x, b.y, oppColor);
                return scoreB.CompareTo(scoreA);
            });
            
            // 限制候选点数量
            if (candidates.Count > MAX_CANDIDATES)
            {
                candidates = candidates.GetRange(0, MAX_CANDIDATES);
            }
            
            return candidates;
        }
        
        /// <summary>
        /// 高级点评估：考虑更多棋型
        /// </summary>
        private static long EvaluatePointAdvanced(GomokuBoard board, int x, int y, StoneColor color)
        {
            if (!board.IsInside(x, y) || !board.IsEmpty(x, y))
                return 0;
            
            long totalScore = 0;
            
            // 检查四个方向
            foreach (int[] dir in DIRECTIONS)
            {
                int dx = dir[0], dy = dir[1];
                
                // 向正方向扫描
                int count1 = 0;
                int empty1 = 0;
                int nx = x + dx, ny = y + dy;
                while (board.IsInside(nx, ny) && board.GetCell(nx, ny) == color)
                {
                    count1++;
                    nx += dx;
                    ny += dy;
                }
                if (board.IsInside(nx, ny) && board.IsEmpty(nx, ny))
                {
                    empty1++;
                    // 继续扫描空位后的棋子
                    int nx2 = nx + dx, ny2 = ny + dy;
                    while (board.IsInside(nx2, ny2) && board.GetCell(nx2, ny2) == color)
                    {
                        count1++;
                        nx2 += dx;
                        ny2 += dy;
                    }
                }
                
                // 向负方向扫描
                int count2 = 0;
                int empty2 = 0;
                nx = x - dx;
                ny = y - dy;
                while (board.IsInside(nx, ny) && board.GetCell(nx, ny) == color)
                {
                    count2++;
                    nx -= dx;
                    ny -= dy;
                }
                if (board.IsInside(nx, ny) && board.IsEmpty(nx, ny))
                {
                    empty2++;
                    // 继续扫描空位后的棋子
                    int nx2 = nx - dx, ny2 = ny - dy;
                    while (board.IsInside(nx2, ny2) && board.GetCell(nx2, ny2) == color)
                    {
                        count2++;
                        nx2 -= dx;
                        ny2 -= dy;
                    }
                }
                
                int totalCount = count1 + count2 + 1; // +1 代表当前落子
                int openEnds = empty1 + empty2;
                
                totalScore += (long)(ScorePattern(totalCount, openEnds) * currentScoreMultiplier);
            }
            
            return totalScore;
        }
        
        /// <summary>
        /// 根据棋型模式评分
        /// </summary>
        private static long ScorePattern(int count, int openEnds)
        {
            if (count >= 5) return WIN_SCORE;
            if (openEnds == 0) return 0;
            
            switch (count)
            {
                case 4:
                    return openEnds == 2 ? OPEN_FOUR_SCORE : FOUR_SCORE;
                case 3:
                    return openEnds == 2 ? OPEN_THREE_SCORE : THREE_SCORE;
                case 2:
                    return openEnds == 2 ? OPEN_TWO_SCORE : TWO_SCORE;
                case 1:
                    return openEnds == 2 ? 50 : 10;
                default:
                    return openEnds == 2 ? 200 : 50;
            }
        }
        
        /// <summary>
        /// 评估整个棋盘对AI的有利程度
        /// </summary>
        private static long EvaluateBoard(GomokuBoard board, StoneColor aiColor)
        {
            long score = 0;
            StoneColor oppColor = Other(aiColor);
            
            // 评估所有位置
            for (int x = 0; x < GomokuBoard.Size; x++)
            {
                for (int y = 0; y < GomokuBoard.Size; y++)
                {
                    if (!board.IsEmpty(x, y))
                    {
                        StoneColor cellColor = board.GetCell(x, y);
                        long pointScore = EvaluatePointAdvanced(board, x, y, cellColor);
                        
                        if (cellColor == aiColor)
                        {
                            score += pointScore;
                        }
                        else
                        {
                            score -= pointScore;
                        }
                    }
                }
            }
            
            return score;
        }
        
        public static StoneColor Other(StoneColor c) => c == StoneColor.Black ? StoneColor.White : StoneColor.Black;
    }
}




