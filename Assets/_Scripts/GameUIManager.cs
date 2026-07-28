using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieSizzle
{
    public class GameUIManager : MonoBehaviour
    {
        [Header("Thông tin màn chơi")]
        public int currentLevelNumber = 1;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Image timeBarFill;

        [Header("Các cửa sổ")]
        [SerializeField] private GameObject resultPopup;
        [SerializeField] private TextMeshProUGUI resultTitle;
        [SerializeField] private TextMeshProUGUI resultMessage;
        [SerializeField] private GameObject pausePopup;

        [Header("Các nút")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button pauseRestartButton;
        [SerializeField] private Button resultRestartButton;

        private GameplayManager gameplayManager;

        public void Initialize(GameplayManager manager)
        {
            gameplayManager = manager;
            WireButtons();
            HideResult();
            if (pausePopup != null) pausePopup.SetActive(false);
        }

        private void Update()
        {
            if (gameplayManager == null) return;

            if (timerText != null) timerText.text = gameplayManager.GetFormattedTime();
            if (progressText != null) progressText.text = gameplayManager.GetProgressString();
            if (levelText != null) levelText.text = $"Lv. {currentLevelNumber}";
            if (timeBarFill != null)
                timeBarFill.fillAmount = gameplayManager.GetRemainingTimeRatio();
        }

        public void ShowResult(bool isWin)
        {
            if (resultPopup == null) return;

            resultPopup.SetActive(true);
            resultTitle.text = isWin ? "HOÀN THÀNH!" : "HẾT GIỜ!";
            resultMessage.text = isWin
                ? "Bạn đã hoàn thành màn chơi."
                : "Thử lại lần nữa nhé!";
            resultTitle.color = isWin
                ? new Color(0.24f, 0.82f, 0.25f)
                : new Color(0.95f, 0.32f, 0.22f);
        }

        public void HideResult()
        {
            if (resultPopup != null) resultPopup.SetActive(false);
        }

        public void Configure(
            TextMeshProUGUI level, TextMeshProUGUI timer,
            TextMeshProUGUI progress, Image timeFill,
            GameObject result, TextMeshProUGUI title,
            TextMeshProUGUI message, GameObject pause,
            Button pauseBtn, Button resumeBtn,
            Button pauseRestart, Button resultRestart)
        {
            levelText = level;
            timerText = timer;
            progressText = progress;
            timeBarFill = timeFill;
            resultPopup = result;
            resultTitle = title;
            resultMessage = message;
            pausePopup = pause;
            pauseButton = pauseBtn;
            resumeButton = resumeBtn;
            pauseRestartButton = pauseRestart;
            resultRestartButton = resultRestart;
        }

        private void WireButtons()
        {
            if (pauseButton != null)
            {
                pauseButton.onClick.RemoveListener(OpenPause);
                pauseButton.onClick.AddListener(OpenPause);
            }
            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveListener(ClosePause);
                resumeButton.onClick.AddListener(ClosePause);
            }
            if (pauseRestartButton != null)
            {
                pauseRestartButton.onClick.RemoveListener(Restart);
                pauseRestartButton.onClick.AddListener(Restart);
            }
            if (resultRestartButton != null)
            {
                resultRestartButton.onClick.RemoveListener(Restart);
                resultRestartButton.onClick.AddListener(Restart);
            }
        }

        private void OpenPause()
        {
            gameplayManager.SetPaused(true);
            pausePopup.SetActive(true);
        }

        private void ClosePause()
        {
            pausePopup.SetActive(false);
            gameplayManager.SetPaused(false);
        }

        private void Restart()
        {
            if (pausePopup != null) pausePopup.SetActive(false);
            HideResult();
            gameplayManager.RestartLevel();
        }
    }
}
