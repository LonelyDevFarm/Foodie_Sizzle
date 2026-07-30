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

        [Header("Các cửa sổ")]
        [SerializeField] private GameObject resultPopup;
        [SerializeField] private TextMeshProUGUI resultTitle;
        [SerializeField] private TextMeshProUGUI resultMessage;
        [SerializeField] private Image resultRibbon;
        [SerializeField] private Image resultCharacter;
        [SerializeField] private Sprite winRibbonSprite;
        [SerializeField] private Sprite loseRibbonSprite;
        [SerializeField] private Sprite winCharacterSprite;
        [SerializeField] private Sprite loseCharacterSprite;
        [SerializeField] private Button resultPrimaryButton;
        [SerializeField] private Image resultPrimaryBackground;
        [SerializeField] private Image resultPrimaryIcon;
        [SerializeField] private Sprite continueButtonSprite;
        [SerializeField] private Sprite retryButtonSprite;
        [SerializeField] private Sprite continueIconSprite;
        [SerializeField] private Sprite retryIconSprite;
        [SerializeField] private Button resultHomeButton;
        [SerializeField] private GameObject pausePopup;

        [Header("Màn hình Home")]
        [SerializeField] private GameObject homeScreen;
        [SerializeField] private Button homePlayButton;
        [SerializeField] private TextMeshProUGUI homeLevelText;
        [SerializeField] private Button pauseHomeButton;

        [Header("Các nút")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button pauseRestartButton;
        [SerializeField] private Button resultRestartButton;

        [Header("Vật phẩm hỗ trợ")]
        [SerializeField] private Button boxBoosterButton;
        [SerializeField] private Button refreshBoosterButton;
        [SerializeField] private Button timeBoosterButton;
        [SerializeField] private Button plusBoosterButton;
        [SerializeField] private TextMeshProUGUI boxBoosterCount;
        [SerializeField] private TextMeshProUGUI refreshBoosterCount;
        [SerializeField] private TextMeshProUGUI timeBoosterCount;
        [SerializeField] private TextMeshProUGUI plusBoosterCount;

        private GameplayManager gameplayManager;
        private bool lastResultWasWin;
        private Vector2 resultPrimaryBaseSize;

        public void Initialize(GameplayManager manager)
        {
            gameplayManager = manager;
            ConfigureProgressText();
            ConfigurePopupOverlays();
            CacheResultButtonSize();
            WireButtons();
            HideResult();
            if (resultMessage != null)
                resultMessage.gameObject.SetActive(false);
            if (pausePopup != null) pausePopup.SetActive(false);
        }

        private void ConfigurePopupOverlays()
        {
            Color overlayColor = new Color(0.01f, 0.006f, 0.002f, 0.94f);

            Image resultOverlay =
                resultPopup != null ? resultPopup.GetComponent<Image>() : null;
            if (resultOverlay != null)
            {
                resultOverlay.color = overlayColor;
            }

            Image pauseOverlay =
                pausePopup != null ? pausePopup.GetComponent<Image>() : null;
            if (pauseOverlay != null)
            {
                pauseOverlay.color = overlayColor;
            }
        }

        private void CacheResultButtonSize()
        {
            if (resultPrimaryButton == null) return;

            RectTransform rect =
                resultPrimaryButton.GetComponent<RectTransform>();
            if (rect != null)
            {
                resultPrimaryBaseSize = rect.rect.size;
            }
        }

        private void ConfigureProgressText()
        {
            if (progressText == null) return;

            // Mục tiêu ở các level sau có thể lên tới hai hoặc ba chữ số.
            // Cho TMP tự thu nhỏ trong một dòng để số cuối không bị rơi xuống hàng dưới.
            progressText.enableAutoSizing = true;
            progressText.fontSize = 46f;
            progressText.fontSizeMin = 32f;
            progressText.fontSizeMax = 46f;
            progressText.textWrappingMode = TextWrappingModes.NoWrap;
            progressText.overflowMode = TextOverflowModes.Overflow;
            progressText.margin = new Vector4(4f, 2f, 4f, 2f);
        }

        private void Update()
        {
            if (gameplayManager == null) return;

            if (timerText != null) timerText.text = gameplayManager.GetFormattedTime();
            if (progressText != null) progressText.text = gameplayManager.GetProgressString();
            if (levelText != null)
            {
                currentLevelNumber = gameplayManager.GetCurrentLevelNumber();
                levelText.text = $"Lv. {currentLevelNumber}";
            }
            UpdateBoosterUI();
        }

        public void ShowResult(bool isWin)
        {
            if (resultPopup == null) return;

            lastResultWasWin = isWin;
            resultPopup.SetActive(true);
            if (resultTitle != null)
            {
                resultTitle.text = isWin ? "CHIẾN THẮNG" : "THUA";
                resultTitle.color = Color.white;
            }
            if (resultMessage != null)
                resultMessage.gameObject.SetActive(false);
            if (resultRibbon != null)
            {
                resultRibbon.sprite =
                    isWin ? winRibbonSprite : loseRibbonSprite;
            }
            if (resultCharacter != null)
            {
                resultCharacter.sprite =
                    isWin ? winCharacterSprite : loseCharacterSprite;
                resultCharacter.enabled = resultCharacter.sprite != null;
            }
            if (resultPrimaryBackground != null)
            {
                resultPrimaryBackground.sprite =
                    isWin ? continueButtonSprite : retryButtonSprite;
            }
            if (resultPrimaryButton != null)
            {
                RectTransform primaryRect =
                    resultPrimaryButton.GetComponent<RectTransform>();
                if (primaryRect != null)
                {
                    if (resultPrimaryBaseSize.x <= 0f ||
                        resultPrimaryBaseSize.y <= 0f)
                    {
                        resultPrimaryBaseSize = primaryRect.rect.size;
                    }

                    primaryRect.sizeDelta = isWin
                        ? resultPrimaryBaseSize * 0.9f
                        : resultPrimaryBaseSize;
                }
            }
            if (resultPrimaryIcon != null)
            {
                resultPrimaryIcon.sprite =
                    isWin ? continueIconSprite : retryIconSprite;
            }
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
            if (resultPrimaryButton != null)
            {
                resultPrimaryButton.onClick.RemoveListener(HandleResultPrimary);
                resultPrimaryButton.onClick.AddListener(HandleResultPrimary);
            }
            if (homePlayButton != null)
            {
                homePlayButton.onClick.RemoveListener(StartFromHome);
                homePlayButton.onClick.AddListener(StartFromHome);
            }
            if (pauseHomeButton != null)
            {
                pauseHomeButton.onClick.RemoveListener(ReturnHome);
                pauseHomeButton.onClick.AddListener(ReturnHome);
                pauseHomeButton.interactable = true;
            }
            if (resultHomeButton != null)
            {
                resultHomeButton.onClick.RemoveListener(ReturnHome);
                resultHomeButton.onClick.AddListener(ReturnHome);
                resultHomeButton.interactable = true;
            }
            WireBoosterButton(
                boxBoosterButton,
                HandleBoxBooster);
            WireBoosterButton(
                refreshBoosterButton,
                HandleRefreshBooster);
            WireBoosterButton(
                timeBoosterButton,
                HandleTimeBooster);
            WireBoosterButton(
                plusBoosterButton,
                HandlePlusBooster);
        }

        private void HandleBoxBooster()
        {
            gameplayManager.TryUseBoxBooster();
        }

        private void HandleRefreshBooster()
        {
            gameplayManager.TryUseRefreshBooster();
        }

        private void HandleTimeBooster()
        {
            gameplayManager.TryUseTimeBooster();
        }

        private void HandlePlusBooster()
        {
            gameplayManager.TryUsePlusBooster();
        }

        private static void WireBoosterButton(
            Button button,
            UnityEngine.Events.UnityAction action)
        {
            if (button == null || action == null) return;
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private void UpdateBoosterUI()
        {
            UpdateBooster(
                boxBoosterButton,
                boxBoosterCount,
                GameplayManager.BoxBoosterId);
            UpdateBooster(
                refreshBoosterButton,
                refreshBoosterCount,
                GameplayManager.RefreshBoosterId);
            UpdateBooster(
                timeBoosterButton,
                timeBoosterCount,
                GameplayManager.TimeBoosterId);
            UpdateBooster(
                plusBoosterButton,
                plusBoosterCount,
                GameplayManager.PlusBoosterId);
        }

        private void UpdateBooster(
            Button button,
            TextMeshProUGUI countText,
            string boosterId)
        {
            if (countText != null)
            {
                countText.text =
                    gameplayManager.GetBoosterCount(boosterId).ToString();
            }
            if (button != null)
            {
                button.interactable =
                    gameplayManager.CanUseBooster(boosterId);
            }
        }

        public void ConfigureBoosters(
            Button boxButton,
            TextMeshProUGUI boxCount,
            Button refreshButton,
            TextMeshProUGUI refreshCount,
            Button timeButton,
            TextMeshProUGUI timeCount,
            Button plusButton,
            TextMeshProUGUI plusCount)
        {
            boxBoosterButton = boxButton;
            boxBoosterCount = boxCount;
            refreshBoosterButton = refreshButton;
            refreshBoosterCount = refreshCount;
            timeBoosterButton = timeButton;
            timeBoosterCount = timeCount;
            plusBoosterButton = plusButton;
            plusBoosterCount = plusCount;
        }

        private void HandleResultPrimary()
        {
            if (lastResultWasWin)
            {
                gameplayManager.PlayFeedback(FeedbackCue.UiButton);
                gameplayManager.ContinueToNextLevel();
            }
            else
            {
                Restart();
            }
        }

        public void ConfigureResultVisuals(
            TextMeshProUGUI title,
            TextMeshProUGUI message,
            Image ribbon,
            Sprite winRibbon,
            Sprite loseRibbon,
            Image character,
            Sprite winCharacter,
            Sprite loseCharacter,
            Button primaryButton,
            Image primaryBackground,
            Image primaryIcon,
            Sprite continueButton,
            Sprite retryButton,
            Sprite continueIcon,
            Sprite retryIcon,
            Button homeButton)
        {
            resultTitle = title;
            resultMessage = message;
            resultRibbon = ribbon;
            winRibbonSprite = winRibbon;
            loseRibbonSprite = loseRibbon;
            resultCharacter = character;
            winCharacterSprite = winCharacter;
            loseCharacterSprite = loseCharacter;
            resultPrimaryButton = primaryButton;
            resultPrimaryBackground = primaryBackground;
            resultPrimaryIcon = primaryIcon;
            continueButtonSprite = continueButton;
            retryButtonSprite = retryButton;
            continueIconSprite = continueIcon;
            retryIconSprite = retryIcon;
            resultHomeButton = homeButton;
        }

        public void ConfigureHome(
            GameObject screen,
            Button playButton,
            TextMeshProUGUI levelLabel,
            Button pauseHome)
        {
            homeScreen = screen;
            homePlayButton = playButton;
            homeLevelText = levelLabel;
            pauseHomeButton = pauseHome;
        }

        public bool HasHomeScreen()
        {
            return homeScreen != null && homePlayButton != null;
        }

        public void ShowHome()
        {
            if (homeScreen == null) return;

            if (pausePopup != null) pausePopup.SetActive(false);
            HideResult();
            RefreshHomeLevel();
            homeScreen.SetActive(true);
            homeScreen.transform.SetAsLastSibling();
        }

        public void HideHome()
        {
            if (homeScreen != null) homeScreen.SetActive(false);
        }

        private void RefreshHomeLevel()
        {
            if (homeLevelText == null || gameplayManager == null) return;

            currentLevelNumber = gameplayManager.GetCurrentLevelNumber();
            homeLevelText.text = $"LEVEL {currentLevelNumber}";
        }

        private void StartFromHome()
        {
            if (gameplayManager == null) return;

            gameplayManager.PlayFeedback(FeedbackCue.UiButton);
            HideHome();
            gameplayManager.StartNewLevel();
        }

        private void ReturnHome()
        {
            if (gameplayManager == null) return;

            gameplayManager.PlayFeedback(FeedbackCue.UiButton);
            gameplayManager.EnterHomeState();
            if (AppSceneFlow.CanLoadHome())
            {
                AppSceneFlow.LoadHome();
            }
            else
            {
                // Giữ tương thích với SampleScene cũ trong Editor.
                ShowHome();
            }
        }

        private void OpenPause()
        {
            gameplayManager.PlayFeedback(FeedbackCue.UiButton);
            gameplayManager.SetPaused(true);
            pausePopup.SetActive(true);
        }

        private void ClosePause()
        {
            gameplayManager.PlayFeedback(FeedbackCue.UiButton);
            pausePopup.SetActive(false);
            gameplayManager.SetPaused(false);
        }

        private void Restart()
        {
            gameplayManager.PlayFeedback(FeedbackCue.UiButton);
            if (pausePopup != null) pausePopup.SetActive(false);
            HideResult();
            gameplayManager.RestartLevel();
        }
    }
}
