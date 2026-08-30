using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Wuziqi.Core;

namespace Wuziqi.Game
{
    public class GameRecordManager : MonoBehaviour
    {
        public static GameRecordManager Instance { get; private set; }

        private const string RECORDS_FOLDER = "records";
        private const string RECORDS_FILE = "game_records.json";
        private const int MAX_RECORDS = 100;

        private List<GameRecord> records = new List<GameRecord>();
        private string RecordsPath => Path.Combine(Application.persistentDataPath, RECORDS_FOLDER, RECORDS_FILE);

        public IReadOnlyList<GameRecord> Records => records;
        public int RecordCount => records.Count;

        private GameRecord currentRecord;
        private float gameStartTime;
        private float moveStartTime;

        public event Action<GameRecord> OnRecordSaved;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadRecords();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void StartRecording(StoneColor playerColor)
        {
            string playerCatName = CatManager.Instance?.Selected?.catName ?? "Unknown";
            StoneColor aiColor = playerColor == StoneColor.Black ? StoneColor.White : StoneColor.Black;

            currentRecord = new GameRecord
            {
                gameId = Guid.NewGuid().ToString("N").Substring(0, 8),
                date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                playerCatName = playerCatName,
                aiCatName = "AI",
                playerColor = playerColor.ToString(),
                moves = new List<MoveRecord>()
            };

            gameStartTime = Time.time;
            moveStartTime = 0f;
        }

        public void RecordMove(int x, int y, StoneColor color)
        {
            if (currentRecord == null) return;
            float currentTime = Time.time - gameStartTime;
            currentRecord.moves.Add(new MoveRecord
            {
                x = x, y = y,
                color = color.ToString(),
                time = currentTime
            });
            moveStartTime = currentTime;
        }

        public GameRecord FinishRecording(GameResult result)
        {
            if (currentRecord == null) return null;
            currentRecord.result = result.ToString();
            currentRecord.totalMoves = currentRecord.moves.Count;
            currentRecord.totalTime = Time.time - gameStartTime;

            records.Insert(0, currentRecord);
            if (records.Count > MAX_RECORDS)
                records.RemoveRange(MAX_RECORDS, records.Count - MAX_RECORDS);

            SaveRecords();
            GameRecord saved = currentRecord;
            currentRecord = null;
            OnRecordSaved?.Invoke(saved);
            Debug.Log($"[GameRecordManager] Saved: {saved.gameId}, result: {saved.GetResultText()}");
            return saved;
        }

        public void CancelRecording()
        {
            currentRecord = null;
        }

        private void SaveRecords()
        {
            try
            {
                string directory = Path.GetDirectoryName(RecordsPath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                string json = JsonUtility.ToJson(new RecordData { records = records }, true);
                File.WriteAllText(RecordsPath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameRecordManager] Save failed: {e.Message}");
            }
        }

        private void LoadRecords()
        {
            try
            {
                if (File.Exists(RecordsPath))
                {
                    string json = File.ReadAllText(RecordsPath);
                    RecordData data = JsonUtility.FromJson<RecordData>(json);
                    records = data?.records ?? new List<GameRecord>();
                }
                else
                {
                    records = new List<GameRecord>();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameRecordManager] Load failed: {e.Message}");
                records = new List<GameRecord>();
            }
        }

        public void DeleteRecord(string gameId)
        {
            records.RemoveAll(r => r.gameId == gameId);
            SaveRecords();
        }

        public void ClearAllRecords()
        {
            records.Clear();
            SaveRecords();
        }

        public GameRecord GetRecord(string gameId)
        {
            return records.Find(r => r.gameId == gameId);
        }

        [Serializable]
        private class RecordData
        {
            public List<GameRecord> records;
        }
    }
}
