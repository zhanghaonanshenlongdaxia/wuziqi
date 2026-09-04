using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Wuziqi.Core;
using Wuziqi.Game;

namespace Wuziqi.UI
{
    public class HistoryPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button clearAllButton;
        [SerializeField] private Transform contentParent;
        [SerializeField] private GameObject historyItemPrefab;
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private TMP_Text detailDateText;
        [SerializeField] private TMP_Text detailCatsText;
        [SerializeField] private TMP_Text detailResultText;
        [SerializeField] private TMP_Text detailMovesText;
        [SerializeField] private TMP_Text detailTimeText;
        [SerializeField] private Button replayButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button revengeButton; // 复仇按钮（只对失败局显示）
        [SerializeField] private GameObject emptyHint;

        private GameRecord selectedRecord;
        private List<GameObject> itemListObjects = new List<GameObject>();

        private void Start()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
            if (clearAllButton != null) clearAllButton.onClick.AddListener(OnClearAll);
            if (replayButton != null) replayButton.onClick.AddListener(OnReplayClicked);
            if (deleteButton != null) deleteButton.onClick.AddListener(OnDeleteClicked);
            if (revengeButton != null) revengeButton.onClick.AddListener(OnRevengeClicked);
            panel.SetActive(false);
        }

        public void Show()
        {
            panel.SetActive(true);
            RefreshList();
            GameManager.Instance?.PauseGame();
        }

        public void Hide()
        {
            panel.SetActive(false);
            if (detailPanel != null) detailPanel.SetActive(false);
            GameManager.Instance?.ResumeGame();
        }

        public void RefreshList()
        {
            foreach (var obj in itemListObjects)
            {
                if (obj != null) Destroy(obj);
            }
            itemListObjects.Clear();

            var records = GameRecordManager.Instance?.Records;
            if (emptyHint != null)
                emptyHint.SetActive(records == null || records.Count == 0);
            if (records == null || records.Count == 0) return;

            foreach (var record in records)
            {
                CreateHistoryItem(record);
            }
        }

        private void CreateHistoryItem(GameRecord record)
        {
            if (historyItemPrefab == null || contentParent == null) return;
            GameObject item = Instantiate(historyItemPrefab, contentParent);
            itemListObjects.Add(item);

            var dateText = item.transform.Find("DateText")?.GetComponent<TMP_Text>();
            var resultText = item.transform.Find("ResultText")?.GetComponent<TMP_Text>();
            var catsText = item.transform.Find("CatsText")?.GetComponent<TMP_Text>();
            var movesText = item.transform.Find("MovesText")?.GetComponent<TMP_Text>();
            var button = item.GetComponent<Button>();

            if (dateText != null) dateText.text = record.date;
            if (resultText != null)
            {
                resultText.text = record.GetResultText();
                if (ColorUtility.TryParseHtmlString(record.GetResultColorHex(), out Color color))
                    resultText.color = color;
            }
            if (catsText != null) catsText.text = "\""+record.playerCatName+"\"" + " vs " + "\""+record.aiCatName+"\"";
            if (movesText != null) movesText.text = record.totalMoves.ToString() + " 手";

            if (button != null)
            {
                string gameId = record.gameId;
                button.onClick.AddListener(() => OnItemSelected(gameId));
            }
        }

        private void OnItemSelected(string gameId)
        {
            selectedRecord = GameRecordManager.Instance?.GetRecord(gameId);
            if (selectedRecord == null) return;

            // 复仇按钮：只对失败局显示
            if (revengeButton != null)
                revengeButton.gameObject.SetActive(selectedRecord.GetResultText() == "失败");

            if (detailPanel != null) detailPanel.SetActive(true);
            if (detailDateText != null) detailDateText.text = "日期：" + selectedRecord.date;
            if (detailCatsText != null) detailCatsText.text = "\""+selectedRecord.playerCatName+"\"" + " vs " + "\""+selectedRecord.aiCatName+"\"";
            if (detailResultText != null)
            {
                detailResultText.text = selectedRecord.GetResultText();
                if (ColorUtility.TryParseHtmlString(selectedRecord.GetResultColorHex(), out Color color))
                    detailResultText.color = color;
            }
            if (detailMovesText != null) detailMovesText.text = selectedRecord.totalMoves.ToString() + " 手";
            if (detailTimeText != null)
            {
                int minutes = (int)(selectedRecord.totalTime / 60);
                int seconds = (int)(selectedRecord.totalTime % 60);
                detailTimeText.text = "用时：" + minutes.ToString() + ":" + seconds.ToString("D2");
            }
        }

        private void OnReplayClicked()
        {
            if (selectedRecord == null) return;
            Hide();
            ReplayPanel replayPanel = FindObjectOfType<ReplayPanel>();
            if (replayPanel != null)
            {
                replayPanel.StartReplay(selectedRecord);
            }
            else
            {
                Debug.LogError("[HistoryPanel] ReplayPanel not found");
            }
        }

        /// <summary>复仇挑战：AI 按当时的棋谱重演走法（扣挑战费，不足则留在面板）。</summary>
        private void OnRevengeClicked()
        {
            if (selectedRecord == null || GameManager.Instance == null) return;
            if (!GameManager.Instance.StartGhostGame(selectedRecord, out string reason))
            {
                if (reason == "coins")
                    Debug.Log("[HistoryPanel] 仙喵币不足，无法复仇");
                return;
            }
            Hide();
        }

        private void OnDeleteClicked()
        {
            if (selectedRecord == null) return;
            GameRecordManager.Instance?.DeleteRecord(selectedRecord.gameId);
            if (detailPanel != null) detailPanel.SetActive(false);
            selectedRecord = null;
            RefreshList();
        }

        private void OnClearAll()
        {
            GameRecordManager.Instance?.ClearAllRecords();
            if (detailPanel != null) detailPanel.SetActive(false);
            selectedRecord = null;
            RefreshList();
        }
    }
}