using System;
using System.Collections.Generic;
using UnityEngine;
using Wuziqi.Core;

namespace Wuziqi.Game
{
    [Serializable]
    public class GameRecord
    {
        public string gameId;
        public string date;
        public string playerCatName;
        public string aiCatName;
        public string result;
        public string playerColor;
        public int totalMoves;
        public float totalTime;
        public List<MoveRecord> moves = new List<MoveRecord>();

        public string GetResultText()
        {
            bool playerIsBlack = playerColor == "Black";
            bool playerWon = (result == "BlackWin" && playerIsBlack)
                          || (result == "WhiteWin" && !playerIsBlack);
            if (result == "Draw") return "平局";
            return playerWon ? "胜利" : "失败";
        }

        public string GetResultColorHex()
        {
            bool playerIsBlack = playerColor == "Black";
            bool playerWon = (result == "BlackWin" && playerIsBlack)
                          || (result == "WhiteWin" && !playerIsBlack);
            if (result == "Draw") return "#FFD700";
            return playerWon ? "#4CAF50" : "#F44336";
        }
    }

    [Serializable]
    public class MoveRecord
    {
        public int x;
        public int y;
        public string color;
        public float time;

        public StoneColor GetStoneColor()
        {
            return color == "Black" ? StoneColor.Black : StoneColor.White;
        }
    }
}
