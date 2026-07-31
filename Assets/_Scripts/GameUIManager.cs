using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
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
        private OrderUIController orderUIController;
        private bool lastResultWasWin;
        private Vector2 resultPrimaryBaseSize;
        private Vector2 resultPrimaryBasePosition;
        private Color timerBaseColor = Color.white;
        private Vector3 timerBaseScale = Vector3.one;
        private RectTransform boosterEffectRoot;
        private Coroutine timeBoosterEffectRoutine;
        private GameObject timeBoosterEffectObject;

        public void Initialize(GameplayManager manager)
        {
            gameplayManager = manager;
            orderUIController =
                Object.FindFirstObjectByType<OrderUIController>(
                    FindObjectsInactive.Include);
            if (timerText != null)
            {
                timerBaseColor = timerText.color;
                timerBaseScale = timerText.rectTransform.localScale;
            }
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

            StretchOverlayToFullCanvas(resultPopup);
            StretchOverlayToFullCanvas(pausePopup);

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

        private static void StretchOverlayToFullCanvas(GameObject popup)
        {
            if (popup == null) return;

            Canvas canvas = popup.GetComponentInParent<Canvas>();
            RectTransform rect = popup.GetComponent<RectTransform>();
            if (canvas == null || rect == null) return;

            if (popup.transform.parent != canvas.transform)
            {
                popup.transform.SetParent(canvas.transform, false);
            }
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            popup.transform.SetAsLastSibling();
        }

        private void CacheResultButtonSize()
        {
            if (resultPrimaryButton == null) return;

            RectTransform rect =
                resultPrimaryButton.GetComponent<RectTransform>();
            if (rect != null)
            {
                resultPrimaryBaseSize = rect.rect.size;
                resultPrimaryBasePosition = rect.anchoredPosition;
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

            HandleSystemBackButton();
            if (timerText != null) timerText.text = gameplayManager.GetFormattedTime();
            if (progressText != null) progressText.text = gameplayManager.GetProgressString();
            if (levelText != null)
            {
                currentLevelNumber = gameplayManager.GetCurrentLevelNumber();
                levelText.text = $"Lv. {currentLevelNumber}";
            }
            UpdateBoosterUI();
            UpdateTimeBoosterVisual();
        }

        private void HandleSystemBackButton()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null ||
                !keyboard.escapeKey.wasPressedThisFrame)
            {
                return;
            }

            // Khi đã có kết quả, người chơi phải chọn Continue/Retry/Home
            // để tránh Back vô tình bỏ qua tiến trình.
            if (resultPopup != null && resultPopup.activeSelf)
            {
                return;
            }

            if (pausePopup != null && pausePopup.activeSelf)
            {
                ClosePause();
                return;
            }

            if (gameplayManager.IsGameActive() &&
                !gameplayManager.IsPaused())
            {
                OpenPause();
            }
        }

        private void OnApplicationPause(bool paused)
        {
#if UNITY_ANDROID || UNITY_IOS
            if (paused)
            {
                OpenPauseFromSystem();
            }
#endif
        }

        private void OnApplicationFocus(bool hasFocus)
        {
#if UNITY_ANDROID || UNITY_IOS
            if (!hasFocus)
            {
                OpenPauseFromSystem();
            }
#endif
        }

        private void OpenPauseFromSystem()
        {
            if (gameplayManager == null ||
                !gameplayManager.IsGameActive() ||
                gameplayManager.IsPaused() ||
                pausePopup == null ||
                (resultPopup != null && resultPopup.activeSelf))
            {
                return;
            }

            gameplayManager.SetPaused(true);
            orderUIController?.SetSuppressed(true);
            SetBoosterEffectsVisible(false);
            pausePopup.SetActive(true);
        }

        public void ShowResult(bool isWin)
        {
            if (resultPopup == null) return;

            CancelBoosterEffects();
            lastResultWasWin = isWin;
            orderUIController?.SetSuppressed(true);
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
                ConfigureResultButtonBackground(isWin);
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

                    // Sprite xanh gần như phủ kín toàn ảnh, còn sprite đỏ có
                    // viền trong suốt lớn hơn. Bù riêng hai trục để phần hình
                    // nhìn thấy của Continue bằng nút Chơi lại màn Thua.
                    primaryRect.sizeDelta = isWin
                        ? new Vector2(
                            resultPrimaryBaseSize.x * 0.967f,
                            resultPrimaryBaseSize.y * 0.875f)
                        : resultPrimaryBaseSize;
                    primaryRect.anchoredPosition = isWin
                        ? resultPrimaryBasePosition + new Vector2(0f, 18f)
                        : resultPrimaryBasePosition;
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
            if (pausePopup == null || !pausePopup.activeSelf)
            {
                orderUIController?.SetSuppressed(false);
            }
        }

        private void ConfigureResultButtonBackground(bool isWin)
        {
            resultPrimaryBackground.type = isWin
                ? Image.Type.Sliced
                : Image.Type.Simple;
            resultPrimaryBackground.preserveAspect = false;
            resultPrimaryBackground.enabled = true;
            resultPrimaryBackground.sprite =
                isWin ? continueButtonSprite : retryButtonSprite;
            resultPrimaryBackground.color = Color.white;
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
                RestoreButtonOpacity(pauseHomeButton);
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
            if (gameplayManager.TryBeginBoxBooster())
            {
                StartCoroutine(PlayBoxBoosterEffect());
            }
        }

        private void HandleRefreshBooster()
        {
            if (gameplayManager.TryUseRefreshBooster())
            {
                StartCoroutine(PlayRefreshBoosterEffect());
            }
        }

        private IEnumerator PlayRefreshBoosterEffect()
        {
            if (refreshBoosterButton == null) yield break;

            Transform iconTransform =
                refreshBoosterButton.transform.Find("Icon");
            Image sourceIcon = iconTransform != null
                ? iconTransform.GetComponent<Image>()
                : null;
            Canvas canvas = refreshBoosterButton
                .GetComponentInParent<Canvas>();
            RectTransform canvasRect = canvas != null
                ? canvas.GetComponent<RectTransform>()
                : null;
            if (sourceIcon == null || sourceIcon.sprite == null ||
                canvasRect == null)
            {
                yield break;
            }

            GameObject effectObject = new GameObject(
                "RefreshBoosterEffect",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Shadow));
            RectTransform effectRoot = EnsureBoosterEffectRoot(canvas);
            effectObject.transform.SetParent(effectRoot, false);
            effectObject.transform.SetAsLastSibling();

            RectTransform effectRect =
                effectObject.GetComponent<RectTransform>();
            effectRect.anchorMin = new Vector2(0.5f, 0.5f);
            effectRect.anchorMax = new Vector2(0.5f, 0.5f);
            effectRect.pivot = new Vector2(0.5f, 0.5f);
            effectRect.sizeDelta = sourceIcon.rectTransform.rect.size;
            effectRect.position = GetScreenPosition(
                sourceIcon.rectTransform,
                canvas);
            effectRect.localScale = Vector3.one;

            Image effectImage = effectObject.GetComponent<Image>();
            effectImage.sprite = sourceIcon.sprite;
            effectImage.preserveAspect = true;
            effectImage.raycastTarget = false;
            effectImage.color = Color.white;

            Shadow shadow = effectObject.GetComponent<Shadow>();
            shadow.effectColor = new Color(0.18f, 0.08f, 0.03f, 0.34f);
            shadow.effectDistance = new Vector2(0f, -8f);

            Vector3 startPosition = effectRect.position;
            Vector3 centerPosition = GetScreenCenter();
            const float growDuration = 0.48f;
            // Giữ nhịp bay lên nhanh, chỉ kéo dài đoạn tan biến để tổng thời
            // gian gần bằng hiệu ứng Bag và người chơi kịp nhìn chuyển động.
            const float shrinkDuration = 1f;
            float elapsed = 0f;

            while (elapsed < growDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / growDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                effectRect.position = Vector3.LerpUnclamped(
                    startPosition,
                    centerPosition,
                    eased);
                effectRect.localScale = Vector3.one *
                    Mathf.Lerp(1f, 4.2f, eased);
                effectRect.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    360f * t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < shrinkDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / shrinkDuration);
                float eased = t * t;
                effectRect.position = centerPosition;
                effectRect.localScale = Vector3.one *
                    Mathf.Lerp(4.2f, 0f, eased);
                effectRect.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    Mathf.Lerp(360f, 450f, t));
                effectImage.color = new Color(1f, 1f, 1f, 1f - t);
                yield return null;
            }

            Destroy(effectObject);
        }

        private void HandleTimeBooster()
        {
            if (!gameplayManager.TryUseTimeBooster()) return;

            if (timerText == null) return;

            if (timeBoosterEffectRoutine != null)
                StopCoroutine(timeBoosterEffectRoutine);
            if (timeBoosterEffectObject != null)
                Destroy(timeBoosterEffectObject);
            timeBoosterEffectRoutine =
                StartCoroutine(PlayTimeBoosterEffect());
        }

        private IEnumerator PlayTimeBoosterEffect()
        {
            Image source = FindBoosterIcon(timeBoosterButton);
            Canvas canvas = timeBoosterButton != null
                ? timeBoosterButton.GetComponentInParent<Canvas>()
                : null;
            RectTransform canvasRect = canvas != null
                ? canvas.GetComponent<RectTransform>()
                : null;
            if (source == null || source.sprite == null ||
                canvas == null || canvasRect == null || timerText == null)
            {
                timeBoosterEffectRoutine = null;
                yield break;
            }

            Image clock = CreateEffectImage(
                canvas,
                source.sprite,
                source.rectTransform.rect.size);
            if (clock == null)
            {
                timeBoosterEffectRoutine = null;
                yield break;
            }

            clock.gameObject.name = "TimeBoosterCountdown";
            timeBoosterEffectObject = clock.gameObject;
            RectTransform clockRect = clock.rectTransform;
            clockRect.position = GetScreenPosition(
                source.rectTransform,
                canvas);

            GameObject darkObject = new GameObject(
                "ClockDarkProgress",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            darkObject.transform.SetParent(clockRect, false);
            RectTransform darkRect = darkObject.GetComponent<RectTransform>();
            darkRect.anchorMin = Vector2.zero;
            darkRect.anchorMax = Vector2.one;
            darkRect.offsetMin = Vector2.zero;
            darkRect.offsetMax = Vector2.zero;
            Image darkProgress = darkObject.GetComponent<Image>();
            darkProgress.sprite = source.sprite;
            darkProgress.preserveAspect = true;
            darkProgress.raycastTarget = false;
            darkProgress.type = Image.Type.Filled;
            darkProgress.fillMethod = Image.FillMethod.Radial360;
            darkProgress.fillOrigin = (int)Image.Origin360.Top;
            darkProgress.fillClockwise = true;
            darkProgress.fillAmount = 0f;
            darkProgress.color = new Color(0.15f, 0.11f, 0.08f, 0.72f);

            Vector3 centerPosition = GetScreenCenter();
            Vector3[] timerCorners = new Vector3[4];
            timerText.rectTransform.GetWorldCorners(timerCorners);
            Vector3 timerBottomRight = RectTransformUtility.WorldToScreenPoint(
                GetCanvasCamera(canvas),
                timerCorners[3]);
            Vector3 targetPosition = timerBottomRight +
                new Vector3(18f, -32f, 0f) * canvas.scaleFactor;
            Vector3 startPosition = clockRect.position;

            const float riseDuration = 0.42f;
            float elapsed = 0f;
            while (elapsed < riseDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / riseDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                clockRect.position = Vector3.Lerp(
                    startPosition,
                    centerPosition,
                    eased);
                clockRect.localScale = Vector3.one *
                    Mathf.Lerp(1f, 3.15f, eased);
                clockRect.localRotation = Quaternion.Euler(
                    0f, 0f, 300f * t);
                yield return null;
            }

            const float settleDuration = 0.38f;
            elapsed = 0f;
            while (elapsed < settleDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / settleDuration);
                float eased = t * t * (3f - 2f * t);
                clockRect.position = Vector3.Lerp(
                    centerPosition,
                    targetPosition,
                    eased);
                clockRect.localScale = Vector3.one *
                    Mathf.Lerp(3.15f, 1.15f, eased);
                clockRect.localRotation = Quaternion.Euler(
                    0f, 0f, Mathf.Lerp(300f, 360f, eased));
                yield return null;
            }

            clockRect.position = targetPosition;
            clockRect.localScale = Vector3.one * 1.15f;
            clockRect.localRotation = Quaternion.identity;
            float displayDuration = Mathf.Max(
                0.01f,
                gameplayManager.GetTimeBoosterRemaining());

            while (gameplayManager != null &&
                   gameplayManager.GetTimeBoosterRemaining() > 0f)
            {
                float remaining = gameplayManager.GetTimeBoosterRemaining();
                darkProgress.fillAmount = Mathf.Clamp01(
                    1f - remaining / displayDuration);
                yield return null;
            }

            darkProgress.fillAmount = 1f;
            const float vanishDuration = 0.24f;
            elapsed = 0f;
            while (elapsed < vanishDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / vanishDuration);
                clockRect.localScale = Vector3.one *
                    Mathf.Lerp(1.15f, 0f, t * t);
                clock.color = new Color(1f, 1f, 1f, 1f - t);
                darkProgress.color = new Color(
                    0.15f, 0.11f, 0.08f, 0.72f * (1f - t));
                yield return null;
            }

            Destroy(clock.gameObject);
            timeBoosterEffectObject = null;
            timeBoosterEffectRoutine = null;
        }

        private void HandlePlusBooster()
        {
            if (!gameplayManager.TryBeginPlusBooster(
                    out Vector3 targetWorld)) return;

            Camera camera = Camera.main;
            Vector3 targetScreen = camera != null
                ? camera.WorldToScreenPoint(targetWorld)
                : GetScreenPosition(
                    plusBoosterButton.GetComponent<RectTransform>(),
                    plusBoosterButton.GetComponentInParent<Canvas>());
            StartCoroutine(PlayPlusBoosterSequence(targetScreen));
        }

        private IEnumerator PlayPlusBoosterSequence(Vector3 targetScreen)
        {
            Image source = FindBoosterIcon(plusBoosterButton);
            Canvas canvas = plusBoosterButton != null
                ? plusBoosterButton.GetComponentInParent<Canvas>()
                : null;
            RectTransform canvasRect = canvas != null
                ? canvas.GetComponent<RectTransform>()
                : null;
            if (source == null || source.sprite == null ||
                canvas == null || canvasRect == null)
            {
                gameplayManager?.CompletePlusBooster();
                yield break;
            }

            Image effect = CreateEffectImage(
                canvas,
                source.sprite,
                source.rectTransform.rect.size);
            if (effect == null)
            {
                gameplayManager?.CompletePlusBooster();
                yield break;
            }

            effect.gameObject.name = "PlusBoosterUnlockEffect";
            RectTransform rect = effect.rectTransform;
            Vector3 startPosition = GetScreenPosition(
                source.rectTransform,
                canvas);
            Vector3 centerPosition = GetScreenCenter();
            rect.position = startPosition;

            const float riseDuration = 0.42f;
            float elapsed = 0f;
            while (elapsed < riseDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / riseDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                rect.position = Vector3.Lerp(
                    startPosition,
                    centerPosition,
                    eased);
                rect.localScale = Vector3.one *
                    Mathf.Lerp(1f, 3.1f, eased);
                rect.localRotation = Quaternion.Euler(
                    0f, 0f, 280f * t);
                yield return null;
            }

            // Nắp bếp bắt đầu co đúng lúc dấu cộng lao xuống, để cả hai biến mất cùng nhau.
            gameplayManager?.CompletePlusBooster();
            const float unlockApproachDuration = 0.26f;
            elapsed = 0f;
            while (elapsed < unlockApproachDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / unlockApproachDuration);
                float eased = t * t * (3f - 2f * t);
                rect.position = Vector3.Lerp(
                    centerPosition,
                    targetScreen,
                    eased);
                rect.localScale = Vector3.one *
                    Mathf.Lerp(3.1f, 0f, eased);
                rect.localRotation = Quaternion.Euler(
                    0f, 0f, Mathf.Lerp(280f, 370f, eased));
                effect.color = new Color(
                    1f, 1f, 1f,
                    t < 0.68f ? 1f : 1f - (t - 0.68f) / 0.32f);
                yield return null;
            }

            Destroy(effect.gameObject);
        }

        private void UpdateTimeBoosterVisual()
        {
            if (timerText == null || gameplayManager == null) return;

            timerText.color = gameplayManager.GetTimeBoosterRemaining() > 0f
                ? new Color(0.32f, 1f, 0.94f, 1f)
                : timerBaseColor;
        }

        private IEnumerator PlayBoxBoosterEffect()
        {
            FoodItemData target = gameplayManager.GetLastBoxBoosterTarget();
            IReadOnlyList<Vector3> origins =
                gameplayManager.GetLastBoxBoosterOrigins();
            Image boxIcon = FindBoosterIcon(boxBoosterButton);
            Canvas canvas = boxBoosterButton != null
                ? boxBoosterButton.GetComponentInParent<Canvas>()
                : null;
            Camera camera = Camera.main;
            if (target == null || target.itemSprite == null ||
                origins == null || boxIcon == null || canvas == null)
            {
                gameplayManager.CancelPendingBoosterSequence();
                yield break;
            }

            int count = Mathf.Min(3, origins.Count);
            if (count < 3)
            {
                gameplayManager.CancelPendingBoosterSequence();
                yield break;
            }

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            Image bag = CreateEffectImage(
                canvas,
                boxIcon.sprite,
                boxIcon.rectTransform.rect.size);
            if (bag == null || canvasRect == null)
            {
                gameplayManager.CancelPendingBoosterSequence();
                yield break;
            }

            bag.gameObject.name = "BagBoosterEffect";
            RectTransform bagRect = bag.rectTransform;
            Vector3 bagStart = GetScreenPosition(
                boxIcon.rectTransform,
                canvas);
            Vector3 bagCenter = GetScreenCenter();
            bagRect.position = bagStart;

            const float bagRiseDuration = 0.44f;
            float elapsed = 0f;
            while (elapsed < bagRiseDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / bagRiseDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                bagRect.position = Vector3.Lerp(
                    bagStart,
                    bagCenter,
                    eased);
                bagRect.localScale = Vector3.one *
                    Mathf.Lerp(1f, 3.25f, eased);
                bagRect.localRotation = Quaternion.Euler(
                    0f, 0f, Mathf.Lerp(-12f, 4f, eased));
                yield return null;
            }

            for (int index = 0; index < count; index++)
            {
                Vector3 startScreen = camera != null
                    ? camera.WorldToScreenPoint(origins[index])
                    : GetScreenPosition(boxIcon.rectTransform, canvas);
                StartCoroutine(PlaySpriteFlight(
                    canvas,
                    target.itemSprite,
                    startScreen,
                    bagCenter,
                    index));
                // Giữ túi ở lớp trên để xiên có cảm giác rơi vào bên trong.
                bag.transform.SetAsLastSibling();
                yield return new WaitForSeconds(0.065f);
            }

            yield return new WaitForSeconds(0.62f);

            const float bagVanishDuration = 0.26f;
            elapsed = 0f;
            while (elapsed < bagVanishDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / bagVanishDuration);
                bagRect.localScale = Vector3.one *
                    Mathf.Lerp(3.25f, 0f, t * t);
                bagRect.localRotation = Quaternion.Euler(
                    0f, 0f, Mathf.Lerp(4f, 24f, t));
                bag.color = new Color(1f, 1f, 1f, 1f - t);
                yield return null;
            }

            Destroy(bag.gameObject);
            gameplayManager.CompleteBoxBooster();
        }

        private IEnumerator PlaySpriteFlight(
            Canvas canvas,
            Sprite sprite,
            Vector3 startScreen,
            Vector3 targetScreen,
            int index)
        {
            Image image = CreateEffectImage(
                canvas,
                sprite,
                new Vector2(105f, 145f));
            if (image == null) yield break;

            RectTransform rect = image.rectTransform;
            rect.position = startScreen;
            float horizontalOffset = (index - 1) * 36f * canvas.scaleFactor;
            Vector3 hoverPosition = targetScreen + new Vector3(
                horizontalOffset,
                (308f + Mathf.Abs(index - 1) * 8f) * canvas.scaleFactor,
                0f);
            const float riseDuration = 0.34f;
            const float dropDuration = 0.28f;
            float elapsed = 0f;
            float direction = index % 2 == 0 ? 1f : -1f;

            while (elapsed < riseDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / riseDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                rect.position = Vector3.Lerp(
                    startScreen,
                    hoverPosition,
                    eased);
                rect.localScale = Vector3.one *
                    Mathf.Lerp(1.05f, 1.28f, eased);
                rect.localRotation = Quaternion.Euler(
                    0f, 0f, direction * 24f * t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < dropDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dropDuration);
                float fall = t * t;
                rect.position = Vector3.Lerp(
                    hoverPosition,
                    targetScreen,
                    fall);
                rect.localScale = Vector3.one *
                    Mathf.Lerp(1.28f, 0.16f, fall);
                rect.localRotation = Quaternion.Euler(
                    0f, 0f, direction * Mathf.Lerp(24f, 150f, fall));
                image.color = new Color(
                    1f, 1f, 1f,
                    t < 0.76f ? 1f : 1f - (t - 0.76f) / 0.24f);
                yield return null;
            }
            Destroy(image.gameObject);
        }

        private IEnumerator PlayBoosterIconFlight(
            Button sourceButton,
            Vector3 targetScreen,
            float peakScale,
            float rotation)
        {
            Image source = FindBoosterIcon(sourceButton);
            Canvas canvas = sourceButton != null
                ? sourceButton.GetComponentInParent<Canvas>()
                : null;
            if (source == null || source.sprite == null || canvas == null)
                yield break;

            Image image = CreateEffectImage(
                canvas,
                source.sprite,
                source.rectTransform.rect.size);
            RectTransform rect = image.rectTransform;
            Vector3 startScreen = source.rectTransform.position;
            rect.position = startScreen;
            const float duration = 0.62f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                rect.position = Vector3.Lerp(startScreen, targetScreen, eased);
                float scale = t < 0.68f
                    ? Mathf.Lerp(1f, peakScale, t / 0.68f)
                    : Mathf.Lerp(peakScale, 0f, (t - 0.68f) / 0.32f);
                rect.localScale = Vector3.one * scale;
                rect.localRotation = Quaternion.Euler(
                    0f, 0f, rotation * t);
                image.color = new Color(
                    1f, 1f, 1f,
                    t < 0.72f ? 1f : 1f - (t - 0.72f) / 0.28f);
                yield return null;
            }
            Destroy(image.gameObject);
        }

        private static Image FindBoosterIcon(Button button)
        {
            if (button == null) return null;
            Transform icon = button.transform.Find("Icon");
            return icon != null ? icon.GetComponent<Image>() : null;
        }

        private Image CreateEffectImage(
            Canvas canvas,
            Sprite sprite,
            Vector2 size)
        {
            if (canvas == null || sprite == null) return null;

            GameObject effect = new GameObject(
                "BoosterEffect",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            effect.transform.SetParent(EnsureBoosterEffectRoot(canvas), false);
            effect.transform.SetAsLastSibling();
            RectTransform rect = effect.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            Image image = effect.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private RectTransform EnsureBoosterEffectRoot(Canvas canvas)
        {
            if (boosterEffectRoot != null) return boosterEffectRoot;

            GameObject root = new GameObject(
                "BoosterEffectCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            Canvas effectCanvas = root.GetComponent<Canvas>();
            effectCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            effectCanvas.overrideSorting = true;
            effectCanvas.sortingOrder = 30000;

            CanvasScaler effectScaler = root.GetComponent<CanvasScaler>();
            CanvasScaler sourceScaler =
                canvas != null ? canvas.GetComponent<CanvasScaler>() : null;
            effectScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            effectScaler.referenceResolution = sourceScaler != null
                ? sourceScaler.referenceResolution
                : new Vector2(1170f, 2532f);
            effectScaler.screenMatchMode = sourceScaler != null
                ? sourceScaler.screenMatchMode
                : CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            effectScaler.matchWidthOrHeight = sourceScaler != null
                ? sourceScaler.matchWidthOrHeight
                : 0.5f;

            boosterEffectRoot = root.GetComponent<RectTransform>();
            boosterEffectRoot.anchorMin = Vector2.zero;
            boosterEffectRoot.anchorMax = Vector2.one;
            boosterEffectRoot.offsetMin = Vector2.zero;
            boosterEffectRoot.offsetMax = Vector2.zero;
            return boosterEffectRoot;
        }

        private static Camera GetCanvasCamera(Canvas canvas)
        {
            if (canvas == null ||
                canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return canvas.worldCamera;
        }

        private static Vector3 GetScreenPosition(
            RectTransform rect,
            Canvas canvas)
        {
            if (rect == null) return GetScreenCenter();
            return RectTransformUtility.WorldToScreenPoint(
                GetCanvasCamera(canvas),
                rect.position);
        }

        private static Vector3 GetScreenCenter()
        {
            return new Vector3(
                Screen.width * 0.5f,
                Screen.height * 0.5f,
                0f);
        }

        private void SetBoosterEffectsVisible(bool visible)
        {
            if (boosterEffectRoot == null) return;
            boosterEffectRoot.gameObject.SetActive(visible);
            if (visible) boosterEffectRoot.SetAsLastSibling();
        }

        private void CancelBoosterEffects()
        {
            StopAllCoroutines();
            timeBoosterEffectRoutine = null;
            timeBoosterEffectObject = null;
            if (timerText != null)
                timerText.rectTransform.localScale = timerBaseScale;
            if (boosterEffectRoot != null)
            {
                Destroy(boosterEffectRoot.gameObject);
                boosterEffectRoot = null;
            }
        }

        private static void WireBoosterButton(
            Button button,
            UnityEngine.Events.UnityAction action)
        {
            if (button == null || action == null) return;
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private static void RestoreButtonOpacity(Button button)
        {
            foreach (Image image in
                     button.GetComponentsInChildren<Image>(true))
            {
                Color color = image.color;
                color.a = 1f;
                image.color = color;
            }
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

            CancelBoosterEffects();
            gameplayManager.PlayFeedback(FeedbackCue.UiButton);
            HideHome();
            gameplayManager.StartNewLevel();
        }

        private void ReturnHome()
        {
            if (gameplayManager == null) return;

            CancelBoosterEffects();
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
            if (gameplayManager == null ||
                !gameplayManager.IsGameActive() ||
                pausePopup == null)
            {
                return;
            }

            gameplayManager.PlayFeedback(FeedbackCue.UiButton);
            gameplayManager.SetPaused(true);
            orderUIController?.SetSuppressed(true);
            SetBoosterEffectsVisible(false);
            pausePopup.SetActive(true);
        }

        private void ClosePause()
        {
            if (gameplayManager == null || pausePopup == null) return;

            gameplayManager.PlayFeedback(FeedbackCue.UiButton);
            pausePopup.SetActive(false);
            gameplayManager.SetPaused(false);
            orderUIController?.SetSuppressed(false);
            SetBoosterEffectsVisible(true);
        }

        private void Restart()
        {
            CancelBoosterEffects();
            gameplayManager.PlayFeedback(FeedbackCue.UiButton);
            if (pausePopup != null) pausePopup.SetActive(false);
            orderUIController?.SetSuppressed(false);
            HideResult();
            gameplayManager.RestartLevel();
        }
    }
}
