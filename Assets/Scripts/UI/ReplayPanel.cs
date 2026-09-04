using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Wuziqi.Core;
using Wuziqi.Game;

namespace Wuziqi.UI
{
    public class ReplayPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button rewindButton;
        [SerializeField] private Button stepBackButton;
        [SerializeField] private Button playPauseButton;
        [SerializeField] private Button stepForwardButton;
        [SerializeField] private Button forwardButton;
        [SerializeField] private TMP_Text moveCountText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Image playPauseIcon;
        [SerializeField] private Sprite playSprite;
        [SerializeField] private Sprite pauseSprite;
        [SerializeField] private Slider speedSlider;
        [SerializeField] private TMP_Text speedText;
        [SerializeField] private BoardView boardView;

        private GameRecord currentRecord;
        private int currentMoveIndex = -1;
        private bool isPlaying = false;
        private float playSpeed = 1.0f;
        private Coroutine autoPlayCoroutine;
        private float[] moveTimestamps;

        private void Start()
        {
            if (closeButton != null) closeButton.onClick.AddListener(OnClose);
            if (rewindButton != null) rewindButton.onClick.AddListener(OnRewind);
            if (stepBackButton != null) stepBackButton.onClick.AddListener(OnStepBack);
            if (playPauseButton != null) playPauseButton.onClick.AddListener(OnPlayPause);
            if (stepForwardButton != null) stepForwardButton.onClick.AddListener(OnStepForward);
            if (forwardButton != null) forwardButton.onClick.AddListener(OnForward);
            if (speedSlider != null)
            {
                speedSlider.minValue = 0.5f;
                speedSlider.maxValue = 3.0f;
                speedSlider.value = 1.0f;
                speedSlider.onValueChanged.AddListener(OnSpeedChanged);
            }
            panel.SetActive(false);
        }

        public void StartReplay(GameRecord record)
        {
            if (record == null || record.moves == null || record.moves.Count == 0)
            {
                Debug.LogWarning("[ReplayPanel] Invalid record");
                return;
            }

            currentRecord = record;
            currentMoveIndex = -1;
            isPlaying = false;

            moveTimestamps = new float[record.moves.Count];
            for (int i = 0; i < record.moves.Count; i++)
            {
                moveTimestamps[i] = record.moves[i].time;
            }

            // 复盘期间暂停当前对局：AI 停止思索/落子，玩家不能落子
            GameManager.Instance?.PauseGame();

            if (boardView != null) boardView.ClearAllStones();
            panel.SetActive(true);
            UpdateUI();
            if (statusText != null) statusText.text = "回放中...";
        }

        private void Update()
        {
            if (currentRecord == null) return;
            if (stepBackButton != null)
                stepBackButton.interactable = currentMoveIndex >= 0;
            if (rewindButton != null)
                rewindButton.interactable = currentMoveIndex >= 0;
            if (stepForwardButton != null)
                stepForwardButton.interactable = currentMoveIndex < currentRecord.moves.Count - 1;
            if (forwardButton != null)
                forwardButton.interactable = currentMoveIndex < currentRecord.moves.Count - 1;
        }

        private void OnRewind()
        {
            StopAutoPlay();
            currentMoveIndex = -1;
            if (boardView != null) boardView.ClearAllStones();
            UpdateUI();
        }

        private void OnStepBack()
        {
            if (currentMoveIndex < 0) return;
            StopAutoPlay();
            if (boardView != null) boardView.RemoveLastStone();
            currentMoveIndex--;
            UpdateUI();
        }

        private void OnPlayPause()
        {
            if (isPlaying)
                StopAutoPlay();
            else
                StartAutoPlay();
            UpdateUI();
        }

        private void OnStepForward()
        {
            if (currentMoveIndex >= currentRecord.moves.Count - 1) return;
            StopAutoPlay();
            currentMoveIndex++;
            PlaceStone(currentMoveIndex);
            UpdateUI();
        }

        private void OnForward()
        {
            StopAutoPlay();
            while (currentMoveIndex < currentRecord.moves.Count - 1)
            {
                currentMoveIndex++;
                PlaceStone(currentMoveIndex);
            }
            UpdateUI();
        }

        private void OnSpeedChanged(float value)
        {
            playSpeed = value;
            if (speedText != null) speedText.text = value.ToString("F1") + "x";
        }

        private void OnClose()
        {
            StopAutoPlay();
            panel.SetActive(false);
            currentRecord = null;
            GameManager.Instance?.ResumeGame(); // 关闭复盘恢复对局
        }

        private void StartAutoPlay()
        {
            if (currentMoveIndex >= currentRecord.moves.Count - 1)
            {
                currentMoveIndex = -1;
                if (boardView != null) boardView.ClearAllStones();
            }
            isPlaying = true;
            autoPlayCoroutine = StartCoroutine(AutoPlayRoutine());
        }

        private void StopAutoPlay()
        {
            isPlaying = false;
            if (autoPlayCoroutine != null)
            {
                StopCoroutine(autoPlayCoroutine);
                autoPlayCoroutine = null;
            }
        }

        private IEnumerator AutoPlayRoutine()
        {
            while (currentMoveIndex < currentRecord.moves.Count - 1)
            {
                currentMoveIndex++;
                PlaceStone(currentMoveIndex);
                UpdateUI();
                float waitTime = 1.0f / playSpeed;
                if (currentMoveIndex > 0)
                {
                    float timeDiff = moveTimestamps[currentMoveIndex] - moveTimestamps[currentMoveIndex - 1];
                    waitTime = Mathf.Max(0.2f, timeDiff / playSpeed);
                }
                yield return new WaitForSeconds(waitTime);
            }
            isPlaying = false;
            if (statusText != null) statusText.text = "回放结束";
            UpdateUI();
        }

        private void PlaceStone(int moveIndex)
        {
            if (moveIndex < 0 || moveIndex >= currentRecord.moves.Count) return;
            var move = currentRecord.moves[moveIndex];
            if (boardView != null)
                boardView.PlaceStoneDirect(move.x, move.y, move.GetStoneColor());
        }

        private void UpdateUI()
        {
            int totalCount = currentRecord?.moves?.Count ?? 0;
            int currentIndex = currentMoveIndex + 1;
            if (moveCountText != null)
                moveCountText.text = currentIndex.ToString() + " / " + totalCount.ToString();
            if (playPauseIcon != null)
                playPauseIcon.sprite = isPlaying ? pauseSprite : playSprite;
            if (statusText != null && !isPlaying)
            {
                if (currentMoveIndex < 0)
                    statusText.text = "准备开始";
                else if (currentMoveIndex >= totalCount - 1)
                    statusText.text = "回放结束";
                else
                    statusText.text = "已暂停";
            }
        }
    }
}