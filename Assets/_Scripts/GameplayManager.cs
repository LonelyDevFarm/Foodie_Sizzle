using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace FoodieSizzle
{
    public class GameplayManager : MonoBehaviour
    {
        [Header("Bố cục bàn chơi")]
        public Grill[] grills = new Grill[12];
        [Min(1)] public int columnCount = 3;
        public float horizontalSpacing = 1.45f;
        public float verticalSpacing = 1.5f;
        public Vector2 boardCenter = new Vector2(0f, 0.18f);
        [Tooltip("Thu đồng bộ bếp, xiên và đĩa để chừa vùng Order phía trên.")]
        [Range(0.7f, 1f)] public float boardVisualScale = 0.91f;

        [Header("Danh sách món dùng để tạo màn thử nghiệm")]
        public List<FoodItemData> possibleFoodItems;

        [Header("Dữ liệu màn chơi")]
        [Tooltip("Danh mục level được importer tự đồng bộ. Nếu để trống, game sẽ tự tải Resources/LevelDatabase.")]
        public LevelDatabase levelDatabase;
        [Tooltip("Có thể để trống để chạy Level 1 mẫu được tạo sẵn trong code.")]
        public LevelData currentLevelData;
        [Tooltip("Danh sách cũ để tương thích scene hiện tại. LevelDatabase được ưu tiên khi có dữ liệu.")]
        public List<LevelData> levelSequence = new List<LevelData>();
        [Tooltip("Tự đọc level gần nhất từ PlayerPrefs khi mở game.")]
        public bool loadSavedLevelOnStart = true;
        [Tooltip("Chỉ dùng trong Editor. Đặt lớn hơn 0 để mở thẳng level cần test.")]
        [Min(0)] public int editorStartLevelOverride;

        [Header("Level Goals")]
        public int skewersTarget = 18; // Must clear 18 skewers (6 sets of 3)
        public float timeRemaining = 300f; // 5 minutes

        [Header("Select Effect")]
        public float liftOffset = 0.5f;

        [Header("Gợi ý khi người chơi không thao tác")]
        [Min(1f)] public float hintDelaySeconds = 6f;
        [Min(0.2f)] public float hintShakeDuration = 1.1f;
        [Min(0.005f)] public float hintShakeDistance = 0.025f;
        [Min(1f)] public float hintShakeAngle = 5f;

        [Header("Vật phẩm hỗ trợ")]
        [Min(0)] public int defaultBoosterCount = 999;
        [Min(1f)] public float timeBoosterDuration = 10f;
        [Range(0.1f, 0.9f)] public float timeBoosterMultiplier = 0.5f;

        public const string BoxBoosterId = "Box";
        public const string RefreshBoosterId = "Refresh";
        public const string TimeBoosterId = "Time";
        public const string PlusBoosterId = "Plus";
        private const string BoosterPrefPrefix =
            "FoodieSizzle.Booster.";
        private const string Booster999MigrationKey =
            "FoodieSizzle.Booster.Count999.v1";
        private float timeBoosterRemaining;

        private Queue<FoodItemData> levelSkewersPool = new Queue<FoodItemData>();
        private Grill selectedGrill = null;
        private SkewerVisual selectedSkewer = null;
        private bool isBoardLocked = false;
        private int skewersClearedCount = 0;
        private bool isGameActive = false;
        private Grill pointerDownGrill;
        private SkewerVisual draggedSkewer;
        private Vector3 dragStartPosition;
        private Vector2 pointerDownPosition;
        private bool hasDragged;
        private const float FallbackDragThreshold = 24f;
        private GameUIManager gameUIManager;
        private GameFeedbackManager feedbackManager;
        private float levelDuration;
        private bool isPaused;
        private bool boardWasLockedBeforePause;
        private bool isShufflingBoard;
        private float hintIdleTime;
        private Coroutine hintRoutine;
        private readonly List<SkewerVisual> hintedSkewers =
            new List<SkewerVisual>();
        private readonly List<Vector3> hintBasePositions =
            new List<Vector3>();
        private readonly List<Quaternion> hintBaseRotations =
            new List<Quaternion>();
        private readonly List<OrderLevelData> runtimeOrders =
            new List<OrderLevelData>();
        private readonly List<FoodItemData> activeOrderItems =
            new List<FoodItemData>();
        private readonly List<bool> activeOrderCompleted =
            new List<bool>();
        private int completedMatchingSets;
        private int nextOrderIndex;
        private float activeOrderTimeRemaining;
        private float activeOrderDuration;
        private bool hasActiveOrder;
        private bool activeOrderWarningPlayed;
        private int orderRevision;

        private void OnEnable()
        {
            if (!Application.isPlaying) return;

            // Phục hồi input sau khi Unity hot reload trong lúc đang Play.
            // GameOver vẫn an toàn vì isGameActive lúc đó bằng false.
            isShufflingBoard = false;
            if (isGameActive && !isPaused)
            {
                isBoardLocked = false;
            }
            ResetPointerDrag();
        }

        private void Start()
        {
            EnsureBoosterTestCounts();
            feedbackManager = GetComponent<GameFeedbackManager>();
            if (feedbackManager == null)
            {
                feedbackManager =
                    gameObject.AddComponent<GameFeedbackManager>();
            }
            gameUIManager = GetComponent<GameUIManager>();
            if (gameUIManager == null)
            {
                gameUIManager = gameObject.AddComponent<GameUIManager>();
            }
            gameUIManager.Initialize(this);

            ResolveStartingLevel();
            ApplyBoardLayout();
            if (SceneManager.GetActiveScene().name ==
                AppSceneFlow.GameplaySceneName)
            {
                StartNewLevel();
            }
            else if (gameUIManager.HasHomeScreen())
            {
                EnterHomeState();
                gameUIManager.ShowHome();
            }
            else
            {
                // Scene cũ chưa có Home vẫn vào game như trước.
                StartNewLevel();
            }
        }

        /// <summary>
        /// Chọn level khởi đầu từ LevelDatabase và tiến trình đã lưu.
        /// Scene vẫn chạy được bằng levelSequence cũ nếu database chưa tồn tại.
        /// </summary>
        private void ResolveStartingLevel()
        {
            if (levelDatabase == null)
            {
                levelDatabase =
                    Resources.Load<LevelDatabase>("LevelDatabase");
            }

            IReadOnlyList<LevelData> levels = GetAvailableLevels();
            if (levels.Count == 0)
            {
                return;
            }

            LevelData firstLevel = FindFirstAvailableLevel(levels);
            if (firstLevel == null)
            {
                return;
            }

            int requestedLevelNumber = firstLevel.levelNumber;
            bool editorOverrideApplied = false;
            if (!loadSavedLevelOnStart && currentLevelData != null)
            {
                requestedLevelNumber = currentLevelData.levelNumber;
            }
            else if (loadSavedLevelOnStart)
            {
                requestedLevelNumber = PlayerPrefs.GetInt(
                    AppSceneFlow.CurrentLevelPrefKey,
                    requestedLevelNumber);
            }

#if UNITY_EDITOR
            if (editorStartLevelOverride > 0)
            {
                requestedLevelNumber = editorStartLevelOverride;
                editorOverrideApplied = true;
            }
#endif

            currentLevelData =
                FindLevelByNumber(levels, requestedLevelNumber) ??
                firstLevel;

            if (loadSavedLevelOnStart &&
                !editorOverrideApplied &&
                currentLevelData.levelNumber != requestedLevelNumber)
            {
                SaveCurrentLevelProgress();
            }
        }

        private IReadOnlyList<LevelData> GetAvailableLevels()
        {
            if (levelDatabase != null && levelDatabase.Count > 0)
            {
                return levelDatabase.Levels;
            }

            if (levelSequence != null)
            {
                return levelSequence;
            }

            return System.Array.Empty<LevelData>();
        }

        private static LevelData FindFirstAvailableLevel(
            IReadOnlyList<LevelData> levels)
        {
            LevelData first = null;
            for (int index = 0; index < levels.Count; index++)
            {
                LevelData level = levels[index];
                if (level == null) continue;
                if (first == null ||
                    level.levelNumber < first.levelNumber)
                {
                    first = level;
                }
            }

            return first;
        }

        private static LevelData FindLevelByNumber(
            IReadOnlyList<LevelData> levels,
            int levelNumber)
        {
            for (int index = 0; index < levels.Count; index++)
            {
                LevelData level = levels[index];
                if (level != null && level.levelNumber == levelNumber)
                {
                    return level;
                }
            }

            return null;
        }

        private void SaveCurrentLevelProgress()
        {
            if (currentLevelData == null) return;

            PlayerPrefs.SetInt(
                AppSceneFlow.CurrentLevelPrefKey,
                currentLevelData.levelNumber);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Cấp 999 vật phẩm một lần cho bản học tập/thử nghiệm.
        /// Dùng khóa phiên bản để số lượng vẫn giảm bình thường sau khi sử dụng.
        /// </summary>
        private void EnsureBoosterTestCounts()
        {
            if (PlayerPrefs.GetInt(Booster999MigrationKey, 0) == 1) return;

            string[] boosterIds =
            {
                BoxBoosterId,
                RefreshBoosterId,
                TimeBoosterId,
                PlusBoosterId
            };
            foreach (string boosterId in boosterIds)
            {
                PlayerPrefs.SetInt(BoosterPrefPrefix + boosterId, 999);
            }
            PlayerPrefs.SetInt(Booster999MigrationKey, 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Tự xếp các bếp theo lưới từ trái sang phải, từ trên xuống dưới.
        /// Dữ liệu level về sau chỉ cần quyết định bếp nào được bật hoặc khóa.
        /// </summary>
        private void ApplyBoardLayout()
        {
            ApplyBoardLayout(grills != null ? grills.Length : 0, columnCount);
        }

        private void ApplyBoardLayout(int activeGrillCount, int columns)
        {
            if (grills == null || grills.Length == 0) return;

            activeGrillCount = Mathf.Clamp(activeGrillCount, 0, grills.Length);
            columns = Mathf.Max(1, columns);
            int rows = Mathf.Max(1, Mathf.CeilToInt(activeGrillCount / (float)columns));
            float firstX = boardCenter.x - (columns - 1) * horizontalSpacing * 0.5f;
            float firstY = boardCenter.y + (rows - 1) * verticalSpacing * 0.5f;

            for (int i = 0; i < grills.Length; i++)
            {
                if (grills[i] == null) continue;

                bool shouldBeActive = i < activeGrillCount;
                grills[i].gameObject.SetActive(shouldBeActive);
                if (!shouldBeActive) continue;

                grills[i].transform.localScale =
                    Vector3.one * boardVisualScale;
                int column = i % columns;
                int row = i / columns;
                grills[i].transform.position = new Vector3(
                    firstX + column * horizontalSpacing,
                    firstY - row * verticalSpacing,
                    0f);
            }
        }

        private void Update()
        {
            if (isGameActive && !isPaused)
            {
                UpdateTimer();
                UpdateOrderTimer();
                UpdatePointerInput();
                UpdateHintTimer();
            }
        }

        private void UpdatePointerInput()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null || Camera.main == null) return;

            if (pointer.press.wasPressedThisFrame)
            {
                RegisterPlayerActivity();
                RecoverInterruptedInputIfNeeded();

                pointerDownPosition = pointer.position.ReadValue();
                pointerDownGrill = FindGrillAtScreenPosition(pointerDownPosition);
                hasDragged = false;
                draggedSkewer = null;

                if (pointerDownGrill != null &&
                    !isBoardLocked &&
                    !pointerDownGrill.IsAnimating &&
                    pointerDownGrill.activeSkewers.Count > 0)
                {
                    Vector3 pointerWorldPosition =
                        Camera.main.ScreenToWorldPoint(pointerDownPosition);
                    draggedSkewer =
                        pointerDownGrill.GetSkewerAtWorldPosition(pointerWorldPosition);

                    if (draggedSkewer != null)
                    {
                        pointerDownGrill.TryGetSkewerSlotPosition(
                            draggedSkewer,
                            out dragStartPosition);
                        draggedSkewer.SetSelected(true);
                        draggedSkewer.BeginDrag();
                        PlayFeedback(FeedbackCue.SelectSkewer);
                    }
                }
            }

            Vector2 currentPointerPosition = pointer.position.ReadValue();
            if (pointer.press.isPressed && draggedSkewer != null)
            {
                hintIdleTime = 0f;

                if (!hasDragged &&
                    Vector2.Distance(
                        pointerDownPosition,
                        currentPointerPosition) >= GetDragThresholdPixels())
                {
                    hasDragged = true;
                    PrepareSelectionForDrag(draggedSkewer);
                }

                if (hasDragged)
                {
                    Vector3 worldPosition =
                        Camera.main.ScreenToWorldPoint(currentPointerPosition);
                    worldPosition.z = dragStartPosition.z;
                    draggedSkewer.SetDragPosition(worldPosition);
                }
            }

            if (!pointer.press.wasReleasedThisFrame) return;

            Grill pointerUpGrill =
                FindGrillAtScreenPosition(currentPointerPosition, pointerDownGrill);

            if (pointerDownGrill == null)
            {
                ClearCurrentSelection();
                ResetPointerDrag();
                return;
            }

            if (draggedSkewer != null && hasDragged)
            {
                draggedSkewer.EndDrag();
                float settleDuration =
                    pointerDownGrill == pointerUpGrill ? 0.15f : 0.25f;
                bool moved = TryCompleteDrag(
                    pointerDownGrill, pointerUpGrill, draggedSkewer);
                if (!moved)
                {
                    settleDuration = 0.15f;
                    draggedSkewer.MoveTo(dragStartPosition, 0.15f);
                    PlayFeedback(FeedbackCue.InvalidDrop);
                }
                StartCoroutine(
                    DeselectSkewerAfterDelay(draggedSkewer, settleDuration));
            }
            else if (draggedSkewer != null)
            {
                // Chạm nhanh vẫn dùng được theo kiểu chọn nguồn rồi chọn đích.
                draggedSkewer.EndDrag();
                draggedSkewer.MoveTo(dragStartPosition, 0.05f);
                Vector3 clickWorldPosition =
                    Camera.main.ScreenToWorldPoint(currentPointerPosition);
                OnGrillClicked(
                    pointerDownGrill,
                    draggedSkewer,
                    clickWorldPosition);
            }
            else
            {
                Vector3 clickWorldPosition =
                    Camera.main.ScreenToWorldPoint(currentPointerPosition);
                OnGrillClicked(
                    pointerDownGrill,
                    null,
                    clickWorldPosition);
            }

            ResetPointerDrag();
        }

        private static float GetDragThresholdPixels()
        {
            // Khoảng 0,08 inch: đủ bỏ qua rung tay khi tap nhưng vẫn bắt đầu
            // kéo nhanh. Trong Editor Screen.dpi thường bằng 0 nên dùng 24 px.
            if (Screen.dpi <= 0f)
            {
                return FallbackDragThreshold;
            }

            return Mathf.Clamp(Screen.dpi * 0.08f, 20f, 48f);
        }

        private static Grill FindGrillAtScreenPosition(
            Vector2 screenPosition, Grill excludedGrill = null)
        {
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
            Collider2D[] hits = Physics2D.OverlapPointAll(worldPosition);
            Grill fallback = null;

            foreach (Collider2D hit in hits)
            {
                Grill grill = hit.GetComponentInParent<Grill>();
                if (grill == null) continue;
                if (grill != excludedGrill) return grill;
                fallback = grill;
            }

            return fallback;
        }

        private bool TryCompleteDrag(
            Grill source, Grill target, SkewerVisual skewer)
        {
            if (source == null || target == null) return false;
            if (source.IsAnimating || target.IsAnimating) return false;
            if (isBoardLocked || !isGameActive || source.activeSkewers.Count == 0)
                return false;
            if (!source.activeSkewers.Contains(skewer))
                return false;

            if (source == target)
            {
                bool movedWithinGrill = source.MoveSkewerWithinGrill(
                    skewer, skewer.transform.position);
                if (movedWithinGrill)
                {
                    PlayFeedback(FeedbackCue.ValidDrop);
                }
                return movedWithinGrill;
            }

            if (!target.CanPush(skewer.GetData())) return false;
            if (!target.CanDropAtPosition(skewer.transform.position)) return false;

            ClearCurrentSelection();
            source.Pop(skewer);
            // Giữ vị trí thả để bếp chọn ô trống gần con trỏ nhất.
            target.Push(skewer, 0.25f, skewer.transform.position);

            if (source.activeSkewers.Count == 0 && source.waitingSkewers.Count > 0)
            {
                source.CheckAndClear();
            }

            target.CheckAndClear();
            PlayFeedback(FeedbackCue.ValidDrop);
            return true;
        }

        public void PlayFeedback(FeedbackCue cue)
        {
            if (feedbackManager == null)
            {
                feedbackManager = GetComponent<GameFeedbackManager>();
            }
            feedbackManager?.Play(cue);
        }

        private void ResetPointerDrag()
        {
            pointerDownGrill = null;
            draggedSkewer = null;
            hasDragged = false;
        }

        /// <summary>
        /// Kết thúc đầy đủ lần chạm/kéo hiện tại trước khi một nút UI thay đổi
        /// dữ liệu bàn chơi. Không được chỉ xóa biến tham chiếu vì xiên sẽ còn
        /// giữ sorting, viền và trạng thái kéo từ lần thao tác trước.
        /// </summary>
        private void CancelPointerInteraction()
        {
            if (draggedSkewer != null)
            {
                draggedSkewer.EndDrag();
                draggedSkewer.SetSelected(false);

                if (pointerDownGrill != null &&
                    pointerDownGrill.activeSkewers.Contains(draggedSkewer))
                {
                    draggedSkewer.MoveTo(dragStartPosition, 0.05f);
                }
            }

            ClearCurrentSelection();
            ResetPointerDrag();
        }

        private void RecoverInterruptedInputIfNeeded()
        {
            // Nếu lần kéo trước bị ngắt bởi mất focus hoặc animation,
            // bảo đảm xiên không giữ sorting/state kéo mãi mãi.
            if (draggedSkewer != null)
            {
                draggedSkewer.EndDrag();
                draggedSkewer.SetSelected(false);
                draggedSkewer.MoveTo(dragStartPosition, 0.1f);
                ResetPointerDrag();
            }

            // Một coroutine bếp bị ngắt có thể để lại khóa bàn dù không còn
            // animation nào chạy. Chỉ tự mở khi game vẫn hoạt động và
            // GameplayManager cũng không trong quá trình xáo bàn.
            if (isBoardLocked &&
                isGameActive &&
                !isPaused &&
                !isShufflingBoard &&
                !HasAnimatingGrill())
            {
                isBoardLocked = false;
            }
        }

        private bool HasAnimatingGrill()
        {
            foreach (Grill grill in grills)
            {
                if (grill != null && grill.IsAnimating)
                {
                    return true;
                }
            }

            return false;
        }

        private void ClearCurrentSelection()
        {
            if (selectedSkewer != null)
            {
                selectedSkewer.SetSelected(false);
            }
            selectedGrill = null;
            selectedSkewer = null;
        }

        private void PrepareSelectionForDrag(SkewerVisual skewer)
        {
            if (selectedSkewer != null && selectedSkewer != skewer)
            {
                selectedSkewer.SetSelected(false);
            }

            selectedGrill = null;
            selectedSkewer = null;
            if (skewer != null)
            {
                skewer.SetSelected(true);
            }
        }

        private void DetachCurrentSelectionWithoutHidingEffect()
        {
            selectedGrill = null;
            selectedSkewer = null;
        }

        private static IEnumerator DeselectSkewerAfterDelay(
            SkewerVisual skewer,
            float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            if (skewer != null)
            {
                skewer.SetSelected(false);
            }
        }

        private void UpdateTimer()
        {
            if (timeBoosterRemaining > 0f)
            {
                timeBoosterRemaining = Mathf.Max(
                    0f,
                    timeBoosterRemaining - Time.deltaTime);
            }

            if (timeRemaining > 0)
            {
                timeRemaining -= GetCountdownDeltaTime();
                if (timeRemaining <= 0)
                {
                    timeRemaining = 0;
                    GameOver(false); // Time out
                }
            }
        }

        public void SetBoardLocked(bool locked)
        {
            isBoardLocked = locked;
        }

        public bool IsBoardLocked()
        {
            return isBoardLocked;
        }

        public void StartNewLevel()
        {
            CancelHint();
            isGameActive = true;
            isBoardLocked = false;
            isPaused = false;
            isShufflingBoard = false;
            timeBoosterRemaining = 0f;
            skewersClearedCount = 0;
            ResetOrders();
            selectedGrill = null;
            selectedSkewer = null;
            hintIdleTime = 0f;
            if (gameUIManager != null)
            {
                gameUIManager.HideHome();
                gameUIManager.HideResult();
            }

            if (!LoadLevelData())
            {
                isGameActive = false;
                Debug.LogError("Không thể khởi tạo level. Hãy gán ít nhất 3 FoodItemData.");
                return;
            }
            
            Debug.Log($"Foodie Sizzle level started! Target: {skewersTarget} skewers.");
            levelDuration = Mathf.Max(1f, timeRemaining);
            PrepareOrders();
            TryActivateNextOrder();
        }

        public void RestartLevel()
        {
            StopAllCoroutines();
            isBoardLocked = false;
            StartNewLevel();
        }

        /// <summary>
        /// Dừng phiên chơi hiện tại và đưa game về trạng thái an toàn trước
        /// khi hiện Home. Level đang chọn vẫn được giữ để nút Play chơi tiếp.
        /// </summary>
        public void EnterHomeState()
        {
            CancelHint();
            CancelPointerInteraction();
            StopAllCoroutines();
            ResetOrders();

            isGameActive = false;
            isPaused = false;
            isBoardLocked = true;
            boardWasLockedBeforePause = false;
            isShufflingBoard = false;
            timeBoosterRemaining = 0f;
            hintIdleTime = 0f;
        }

        /// <summary>
        /// Tải một level theo số. Dùng chung cho Home, chọn màn và công cụ test.
        /// </summary>
        public bool TryLoadLevel(int levelNumber, bool saveProgress = true)
        {
            LevelData level = FindLevelByNumber(
                GetAvailableLevels(),
                levelNumber);
            if (level == null)
            {
                Debug.LogWarning(
                    $"Không tìm thấy LevelData cho level {levelNumber}.");
                return false;
            }

            StopAllCoroutines();
            currentLevelData = level;
            if (saveProgress)
            {
                SaveCurrentLevelProgress();
            }
            StartNewLevel();
            return true;
        }

        public void ContinueToNextLevel()
        {
            IReadOnlyList<LevelData> levels = GetAvailableLevels();
            if (levels.Count == 0)
            {
                RestartLevel();
                return;
            }

            int currentIndex = -1;
            for (int index = 0; index < levels.Count; index++)
            {
                LevelData candidate = levels[index];
                if (candidate == currentLevelData ||
                    (candidate != null &&
                     currentLevelData != null &&
                     candidate.levelNumber == currentLevelData.levelNumber))
                {
                    currentIndex = index;
                    break;
                }
            }

            int nextIndex = currentIndex + 1;
            if (nextIndex < 0 || nextIndex >= levels.Count)
            {
                // Tạm thời chơi lại level cuối cho tới khi có thêm dữ liệu.
                RestartLevel();
                return;
            }

            currentLevelData = levels[nextIndex];
            SaveCurrentLevelProgress();
            StartNewLevel();
        }

        public void SetPaused(bool paused)
        {
            if (!isGameActive) return;

            if (paused)
            {
                RegisterPlayerActivity();
                boardWasLockedBeforePause = isBoardLocked;
                // Kết thúc đầy đủ lần kéo trước khi khóa bàn. Chỉ xóa các biến
                // con trỏ sẽ làm xiên giữ sorting, viền và trạng thái kéo.
                CancelPointerInteraction();
                isPaused = true;
                isBoardLocked = true;
            }
            else
            {
                isPaused = false;
                isBoardLocked = boardWasLockedBeforePause;
            }
        }

        private void UpdateHintTimer()
        {
            if (!isGameActive || isPaused || isBoardLocked ||
                isShufflingBoard || HasAnimatingGrill())
            {
                hintIdleTime = 0f;
                return;
            }

            if (hintRoutine != null)
            {
                return;
            }

            hintIdleTime += Time.deltaTime;
            if (hintIdleTime < hintDelaySeconds)
            {
                return;
            }

            hintIdleTime = 0f;
            List<SkewerVisual> triplet = FindHintTriplet();
            if (triplet.Count == 3)
            {
                hintRoutine = StartCoroutine(ShakeHintCoroutine(triplet));
            }
        }

        private List<SkewerVisual> FindHintTriplet()
        {
            Dictionary<string, List<SkewerVisual>> itemsById =
                new Dictionary<string, List<SkewerVisual>>();

            foreach (Grill grill in grills)
            {
                if (grill == null ||
                    !grill.gameObject.activeInHierarchy ||
                    grill.IsAnimating ||
                    grill.IsLocked)
                {
                    continue;
                }

                foreach (SkewerVisual skewer in grill.activeSkewers)
                {
                    FoodItemData data =
                        skewer != null ? skewer.GetData() : null;
                    if (data == null)
                    {
                        continue;
                    }

                    string matchKey = data.GetMatchKey();
                    if (!itemsById.TryGetValue(
                            matchKey,
                            out List<SkewerVisual> group))
                    {
                        group = new List<SkewerVisual>();
                        itemsById[matchKey] = group;
                    }
                    group.Add(skewer);
                }
            }

            foreach (KeyValuePair<string, List<SkewerVisual>> entry in itemsById)
            {
                if (entry.Value.Count < 3)
                {
                    continue;
                }

                foreach (Grill target in grills)
                {
                    if (target == null ||
                        !target.gameObject.activeInHierarchy ||
                        target.IsAnimating ||
                        target.IsLocked)
                    {
                        continue;
                    }

                    List<SkewerVisual> matchingOnTarget =
                        new List<SkewerVisual>();
                    bool containsDifferentItem = false;

                    foreach (SkewerVisual skewer in target.activeSkewers)
                    {
                        FoodItemData data =
                            skewer != null ? skewer.GetData() : null;
                        if (data != null &&
                            data.GetMatchKey() == entry.Key)
                        {
                            matchingOnTarget.Add(skewer);
                        }
                        else
                        {
                            containsDifferentItem = true;
                            break;
                        }
                    }

                    if (containsDifferentItem ||
                        matchingOnTarget.Count >= 3)
                    {
                        continue;
                    }

                    int neededFromOtherGrills = 3 - matchingOnTarget.Count;
                    List<SkewerVisual> result =
                        new List<SkewerVisual>(matchingOnTarget);

                    foreach (SkewerVisual candidate in entry.Value)
                    {
                        if (matchingOnTarget.Contains(candidate))
                        {
                            continue;
                        }

                        result.Add(candidate);
                        neededFromOtherGrills--;
                        if (neededFromOtherGrills == 0)
                        {
                            return result;
                        }
                    }
                }
            }

            return new List<SkewerVisual>();
        }

        private IEnumerator ShakeHintCoroutine(List<SkewerVisual> triplet)
        {
            hintedSkewers.Clear();
            hintBasePositions.Clear();
            hintBaseRotations.Clear();

            foreach (SkewerVisual skewer in triplet)
            {
                if (skewer == null) continue;
                hintedSkewers.Add(skewer);
                hintBasePositions.Add(skewer.transform.localPosition);
                hintBaseRotations.Add(skewer.transform.localRotation);
            }

            float elapsed = 0f;
            while (elapsed < hintShakeDuration)
            {
                float wave = Mathf.Sin(elapsed * 30f);
                for (int index = 0; index < hintedSkewers.Count; index++)
                {
                    SkewerVisual skewer = hintedSkewers[index];
                    if (skewer == null) continue;

                    skewer.transform.localPosition =
                        hintBasePositions[index] +
                        Vector3.right * (wave * hintShakeDistance);
                    skewer.transform.localRotation =
                        hintBaseRotations[index] *
                        Quaternion.Euler(0f, 0f, wave * hintShakeAngle);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            RestoreHintTransforms();
            hintRoutine = null;
            hintIdleTime = 0f;
        }

        private void RegisterPlayerActivity()
        {
            hintIdleTime = 0f;
            CancelHint();
        }

        private void CancelHint()
        {
            if (hintRoutine != null)
            {
                StopCoroutine(hintRoutine);
                hintRoutine = null;
            }

            RestoreHintTransforms();
            hintIdleTime = 0f;
        }

        private void RestoreHintTransforms()
        {
            int count = Mathf.Min(
                hintedSkewers.Count,
                Mathf.Min(hintBasePositions.Count, hintBaseRotations.Count));
            for (int index = 0; index < count; index++)
            {
                SkewerVisual skewer = hintedSkewers[index];
                if (skewer == null) continue;

                skewer.transform.localPosition = hintBasePositions[index];
                skewer.transform.localRotation = hintBaseRotations[index];
            }

            hintedSkewers.Clear();
            hintBasePositions.Clear();
            hintBaseRotations.Clear();
        }

        private bool LoadLevelData()
        {
            if (possibleFoodItems == null || possibleFoodItems.Count < 3)
            {
                return false;
            }

            return currentLevelData != null
                ? LoadConfiguredLevel(currentLevelData)
                : LoadBuiltInLevelOne();
        }

        private bool LoadConfiguredLevel(LevelData levelData)
        {
            int requiredGrillCount =
                levelData.grills != null ? levelData.grills.Count : 0;
            if (requiredGrillCount > grills.Length)
            {
                Debug.LogError(
                    $"Level {levelData.levelNumber} có {requiredGrillCount} bếp, " +
                    $"vượt cấu hình tối đa {grills.Length} bếp của game. " +
                    "Level này cần được loại khỏi LevelDatabase.");
                return false;
            }

            int configuredGrillCount = Mathf.Min(
                grills.Length,
                requiredGrillCount);
            ApplyBoardLayout(configuredGrillCount, levelData.columns);

            Dictionary<string, FoodItemData> itemLookup =
                new Dictionary<string, FoodItemData>();

            foreach (FoodItemData item in possibleFoodItems)
            {
                if (item != null && !string.IsNullOrWhiteSpace(item.itemId))
                {
                    itemLookup[item.itemId] = item;
                }
            }

            int totalSkewers = 0;
            timeRemaining = levelData.timeLimitSeconds;

            for (int grillIndex = 0; grillIndex < grills.Length; grillIndex++)
            {
                List<FoodItemData> activeLayer = new List<FoodItemData>();
                List<List<FoodItemData>> waitingLayers = new List<List<FoodItemData>>();

                if (grillIndex < levelData.grills.Count &&
                    levelData.grills[grillIndex] != null)
                {
                    List<FoodLayerData> sourceLayers =
                        levelData.grills[grillIndex].layers;

                    for (int layerIndex = 0; layerIndex < sourceLayers.Count; layerIndex++)
                    {
                        List<FoodItemData> resolvedLayer =
                            ResolveLayer(sourceLayers[layerIndex], itemLookup);
                        totalSkewers += resolvedLayer.Count;

                        if (layerIndex == 0)
                        {
                            activeLayer = resolvedLayer;
                        }
                        else
                        {
                            waitingLayers.Add(resolvedLayer);
                        }
                    }
                }

                if (grills[grillIndex] != null)
                {
                    grills[grillIndex].Initialize(activeLayer, waitingLayers, this);

                    FoodItemData unlockItem = null;
                    if (grillIndex < levelData.grills.Count)
                    {
                        GrillLevelData configuredGrill =
                            levelData.grills[grillIndex];
                        if (configuredGrill != null &&
                            configuredGrill.sourceLockId > 0)
                        {
                            string unlockItemId =
                                configuredGrill.unlockItemId;

                            // Tương thích các LevelData đã nhập trước khi có
                            // trường unlockItemId. Bảng ánh xạ mặc định hiện tại
                            // dùng source ID N cho Skewer_(N - 1).
                            if (string.IsNullOrWhiteSpace(unlockItemId))
                            {
                                unlockItemId =
                                    (configuredGrill.sourceLockId - 1).ToString();
                            }

                            itemLookup.TryGetValue(unlockItemId, out unlockItem);
                        }
                    }
                    int sourceLockId =
                        grillIndex < levelData.grills.Count &&
                        levelData.grills[grillIndex] != null
                            ? levelData.grills[grillIndex].sourceLockId
                            : 0;
                    grills[grillIndex].ConfigureLock(
                        unlockItem,
                        sourceLockId);
                }
            }

            skewersTarget = totalSkewers;
            if (skewersTarget % 3 != 0)
            {
                Debug.LogWarning(
                    $"Level có {skewersTarget} xiên, không chia hết cho bộ ba.");
            }
            return totalSkewers > 0;
        }

        private static List<FoodItemData> ResolveLayer(
            FoodLayerData layerData,
            Dictionary<string, FoodItemData> itemLookup)
        {
            List<FoodItemData> result = new List<FoodItemData>();
            if (layerData == null) return result;

            foreach (string itemId in layerData.itemIds)
            {
                if (itemLookup.TryGetValue(itemId, out FoodItemData item))
                {
                    result.Add(item);
                }
                else
                {
                    Debug.LogWarning($"Không tìm thấy FoodItemData có itemId '{itemId}'.");
                }
            }

            return result;
        }

        /// <summary>
        /// Level 1 mẫu: ba loại món, ba lớp và nhiều bếp trống.
        /// Mỗi cặp bếp tạo sẵn nhóm 2 + 1 nên luôn có nước giải rõ ràng.
        /// </summary>
        private bool LoadBuiltInLevelOne()
        {
            ApplyBoardLayout(grills.Length, columnCount);

            FoodItemData itemA = possibleFoodItems[0];
            FoodItemData itemB = possibleFoodItems[1];
            FoodItemData itemC = possibleFoodItems[2];
            if (itemA == null || itemB == null || itemC == null) return false;

            FoodItemData[] types = { itemA, itemB, itemC };
            int totalSkewers = 0;
            const int layerCount = 3;

            for (int grillIndex = 0; grillIndex < grills.Length; grillIndex++)
            {
                List<FoodItemData> activeLayer = new List<FoodItemData>();
                List<List<FoodItemData>> waitingLayers = new List<List<FoodItemData>>();

                if (grillIndex < 6)
                {
                    FoodItemData type = types[grillIndex / 2];
                    bool isPairGrill = grillIndex % 2 == 0;

                    for (int layerIndex = 0; layerIndex < layerCount; layerIndex++)
                    {
                        List<FoodItemData> layer = isPairGrill
                            ? new List<FoodItemData> { type, type }
                            : new List<FoodItemData> { type };

                        totalSkewers += layer.Count;
                        if (layerIndex == 0)
                        {
                            activeLayer = layer;
                        }
                        else
                        {
                            waitingLayers.Add(layer);
                        }
                    }
                }

                if (grills[grillIndex] != null)
                {
                    grills[grillIndex].Initialize(activeLayer, waitingLayers, this);
                    grills[grillIndex].ConfigureLock(null);
                }
            }

            timeRemaining = 300f;
            skewersTarget = totalSkewers;
            return true;
        }

        // Generate a pool of skewers that matches in sets of 3 to guarantee they can all be cleared
        private void GenerateSkewersPool()
        {
            levelSkewersPool.Clear();
            List<FoodItemData> tempPool = new List<FoodItemData>();

            int setsCount = Mathf.CeilToInt(skewersTarget / 3f);
            for (int i = 0; i < setsCount; i++)
            {
                // Select a random food item data
                FoodItemData randomItem = possibleFoodItems[Random.Range(0, possibleFoodItems.Count)];
                
                // Add 3 copies of this item
                tempPool.Add(randomItem);
                tempPool.Add(randomItem);
                tempPool.Add(randomItem);
            }

            // Shuffle the pool
            for (int i = 0; i < tempPool.Count; i++)
            {
                int randIdx = Random.Range(i, tempPool.Count);
                FoodItemData temp = tempPool[i];
                tempPool[i] = tempPool[randIdx];
                tempPool[randIdx] = temp;
            }

            // Enqueue all elements
            foreach (var item in tempPool)
            {
                levelSkewersPool.Enqueue(item);
            }
        }

        private void DistributeSkewersToGrills()
        {
            // For each of the 9 grills:
            // Spawn 1 or 2 active skewers on the grill, and 1 or 2 waiting skewers on the plate
            for (int i = 0; i < grills.Length; i++)
            {
                if (grills[i] == null) continue;

                List<FoodItemData> activeList = new List<FoodItemData>();
                List<FoodItemData> waitingList = new List<FoodItemData>();

                // Distribute from pool: 1 to 2 active skewers
                int activeCount = Random.Range(1, 3);
                for (int a = 0; a < activeCount; a++)
                {
                    if (levelSkewersPool.Count > 0)
                    {
                        activeList.Add(levelSkewersPool.Dequeue());
                    }
                }

                // Distribute from pool: 1 to 2 waiting skewers
                int waitingCount = Random.Range(1, 3);
                for (int w = 0; w < waitingCount; w++)
                {
                    if (levelSkewersPool.Count > 0)
                    {
                        waitingList.Add(levelSkewersPool.Dequeue());
                    }
                }

                grills[i].Initialize(activeList, waitingList, this);
                grills[i].ConfigureLock(null);
            }
        }

        // Called by a Grill when it becomes empty and its plate is empty
        // Returns next waiting skewers from the pool
        public List<FoodItemData> RequestReplacementWaitingSkewers()
        {
            List<FoodItemData> newWaiting = new List<FoodItemData>();
            
            // Spawn up to 2 new waiting skewers from the pool
            int spawnCount = Random.Range(1, 3);
            for (int i = 0; i < spawnCount; i++)
            {
                if (levelSkewersPool.Count > 0)
                {
                    newWaiting.Add(levelSkewersPool.Dequeue());
                }
            }

            return newWaiting;
        }

        /// <summary>
        /// Chạm lần đầu chỉ chọn đúng xiên. Chạm lần hai vào ô trống hợp lệ
        /// mới di chuyển; mọi đích không hợp lệ đều hủy trạng thái chọn.
        /// </summary>
        public void OnGrillClicked(
            Grill grill,
            SkewerVisual clickedSkewer,
            Vector3 clickedWorldPosition)
        {
            if (isBoardLocked || !isGameActive || grill == null) return;
            if (grill.IsAnimating)
            {
                // clickedSkewer có thể mới chỉ được bật hiệu ứng ở PointerDown,
                // chưa được gán thành selectedSkewer nên phải tự bỏ hiệu ứng.
                clickedSkewer?.SetSelected(false);
                ClearCurrentSelection();
                return;
            }

            if (selectedSkewer == null)
            {
                if (clickedSkewer != null &&
                    grill.activeSkewers.Contains(clickedSkewer))
                {
                    selectedGrill = grill;
                    selectedSkewer = clickedSkewer;
                    selectedSkewer.SetSelected(true);
                }
                return;
            }

            // Bấm lại đúng xiên đang chọn thì hủy chọn.
            // Bấm một xiên khác thì chuyển lựa chọn trực tiếp sang xiên đó,
            // tránh cảm giác lần bấm thứ hai không được nhận.
            if (clickedSkewer != null)
            {
                bool clickedCurrentSkewer =
                    clickedSkewer == selectedSkewer;
                ClearCurrentSelection();

                if (!clickedCurrentSkewer &&
                    grill.activeSkewers.Contains(clickedSkewer))
                {
                    selectedGrill = grill;
                    selectedSkewer = clickedSkewer;
                    selectedSkewer.SetSelected(true);
                }
                return;
            }

            if (grill == selectedGrill)
            {
                SkewerVisual movingSkewer = selectedSkewer;
                DetachCurrentSelectionWithoutHidingEffect();
                bool moved = grill.MoveSkewerWithinGrill(
                    movingSkewer,
                    clickedWorldPosition,
                    0.2f);
                if (moved)
                {
                    PlayFeedback(FeedbackCue.ValidDrop);
                    StartCoroutine(
                        DeselectSkewerAfterDelay(movingSkewer, 0.2f));
                }
                else
                {
                    movingSkewer.SetSelected(false);
                    PlayFeedback(FeedbackCue.InvalidDrop);
                }
                return;
            }

            if (!grill.CanPush(selectedSkewer.GetData()) ||
                !grill.CanDropAtPosition(clickedWorldPosition))
            {
                ClearCurrentSelection();
                PlayFeedback(FeedbackCue.InvalidDrop);
                return;
            }

            Grill sourceGrill = selectedGrill;
            SkewerVisual movingToTarget = selectedSkewer;
            DetachCurrentSelectionWithoutHidingEffect();

            sourceGrill.Pop(movingToTarget);
            grill.Push(movingToTarget, 0.25f, clickedWorldPosition);
            PlayFeedback(FeedbackCue.ValidDrop);
            StartCoroutine(
                DeselectSkewerAfterDelay(movingToTarget, 0.25f));

            if (sourceGrill.activeSkewers.Count == 0 &&
                sourceGrill.waitingSkewers.Count > 0)
            {
                sourceGrill.CheckAndClear();
            }

            grill.CheckAndClear();
        }

        // Tap input handling cũ được giữ để tương thích với scene/prefab cũ.
        public void OnGrillClicked(Grill grill)
        {
            if (isBoardLocked || !isGameActive) return;

            if (selectedGrill == null)
            {
                // Select grill if it has skewers on it
                if (grill.activeSkewers.Count > 0)
                {
                    selectedGrill = grill;
                    selectedGrill.LiftTopSkewer(true, liftOffset);
                }
            }
            else
            {
                // Target is clicked
                if (grill == selectedGrill)
                {
                    // Deselect
                    selectedGrill.LiftTopSkewer(false, liftOffset);
                    selectedGrill = null;
                }
                else
                {
                    // Check if we can move the top skewer from selected to target grill
                    SkewerVisual poppedSkewer = selectedGrill.activeSkewers[selectedGrill.activeSkewers.Count - 1];
                    
                    if (grill.CanPush(poppedSkewer.GetData()))
                    {
                        // Execute move
                        selectedGrill.Pop();
                        selectedGrill.LiftTopSkewer(false, liftOffset); // Put down selection state

                        grill.Push(poppedSkewer);
                        
                        // Check if selected grill became empty (needs waiting skewers to slide up)
                        if (selectedGrill.activeSkewers.Count == 0 && selectedGrill.waitingSkewers.Count > 0)
                        {
                            selectedGrill.CheckAndClear();
                        }

                        // Check target grill for completions
                        grill.CheckAndClear();

                        selectedGrill = null;
                    }
                    else
                    {
                        // Target grill cannot accept it.
                        // Automatically switch selection to target grill if target has skewers,
                        // otherwise just deselect.
                        selectedGrill.LiftTopSkewer(false, liftOffset);
                        
                        if (grill.activeSkewers.Count > 0)
                        {
                            selectedGrill = grill;
                            selectedGrill.LiftTopSkewer(true, liftOffset);
                        }
                        else
                        {
                            selectedGrill = null;
                        }
                    }
                }
            }
        }

        public void OnSkewerCleared(FoodItemData skewerData)
        {
            skewersClearedCount++;
            Debug.Log($"Skewer cleared! Total: {skewersClearedCount}/{skewersTarget}");
        }

        public void OnMatchingSetCleared(
            FoodItemData clearedItem,
            bool playMatchFeedback = true)
        {
            if (clearedItem == null ||
                string.IsNullOrWhiteSpace(clearedItem.itemId))
            {
                return;
            }

            foreach (Grill grill in grills)
            {
                if (grill != null)
                {
                    grill.TryUnlockForItem(clearedItem.itemId);
                }
            }

            completedMatchingSets++;
            if (playMatchFeedback)
            {
                PlayFeedback(FeedbackCue.MatchingSet);
            }
            ApplyMatchingSetToOrder(clearedItem);
            TryActivateNextOrder();
        }

        private void ResetOrders()
        {
            runtimeOrders.Clear();
            activeOrderItems.Clear();
            activeOrderCompleted.Clear();
            completedMatchingSets = 0;
            nextOrderIndex = 0;
            activeOrderTimeRemaining = 0f;
            activeOrderDuration = 0f;
            hasActiveOrder = false;
            activeOrderWarningPlayed = false;
            orderRevision++;
        }

        private void PrepareOrders()
        {
            runtimeOrders.Clear();

            if (currentLevelData != null &&
                currentLevelData.orders != null &&
                currentLevelData.orders.Count > 0)
            {
                runtimeOrders.AddRange(currentLevelData.orders);
                return;
            }

            // Giữ các LevelData cũ vẫn chạy được trước khi công cụ nhập JSON
            // kịp bổ sung chi tiết Order.
            int fallbackCount = currentLevelData != null
                ? currentLevelData.sourceOrderCount
                : 0;
            int totalSets = Mathf.Max(1, skewersTarget / 3);
            for (int index = 0; index < fallbackCount; index++)
            {
                runtimeOrders.Add(new OrderLevelData
                {
                    timeLimitSeconds = 90f + index * 20f,
                    numberOfFood = Mathf.Clamp(index + 1, 1, 3),
                    matchesToTrigger = Mathf.Max(
                        1,
                        Mathf.RoundToInt(
                            totalSets * (index + 1f) /
                            (fallbackCount + 1f)))
                });
            }
        }

        private void UpdateOrderTimer()
        {
            if (!hasActiveOrder)
            {
                TryActivateNextOrder();
                return;
            }

            activeOrderTimeRemaining = Mathf.Max(
                0f,
                activeOrderTimeRemaining - GetCountdownDeltaTime());

            float warningThreshold = Mathf.Min(
                10f,
                activeOrderDuration * 0.2f);
            if (!activeOrderWarningPlayed &&
                activeOrderTimeRemaining > 0f &&
                activeOrderTimeRemaining <= warningThreshold)
            {
                activeOrderWarningPlayed = true;
                PlayFeedback(FeedbackCue.OrderWarning);
            }

            if (activeOrderTimeRemaining <= 0f)
            {
                FinishActiveOrder(false);
                GameOver(false);
            }
        }

        private void TryActivateNextOrder()
        {
            if (hasActiveOrder ||
                nextOrderIndex < 0 ||
                nextOrderIndex >= runtimeOrders.Count)
            {
                return;
            }

            OrderLevelData order = runtimeOrders[nextOrderIndex];
            if (order == null ||
                completedMatchingSets < order.matchesToTrigger)
            {
                return;
            }

            int requestedCount =
                Mathf.Clamp(order.numberOfFood, 1, 3);
            int waitingDepth = ResolveOrderWaitingDepth(
                order,
                requestedCount);
            List<FoodItemData> targets = SelectOrderTargets(
                requestedCount,
                waitingDepth);

            if (targets.Count == 0)
            {
                // Chưa có món đủ gần và đủ bộ ba: chờ trạng thái bàn thay đổi,
                // không phát một Order mà người chơi chưa thể hoàn thành.
                return;
            }
            nextOrderIndex++;

            activeOrderItems.Clear();
            activeOrderItems.AddRange(targets);
            activeOrderCompleted.Clear();
            for (int index = 0; index < activeOrderItems.Count; index++)
            {
                activeOrderCompleted.Add(false);
            }

            activeOrderDuration = Mathf.Max(1f, order.timeLimitSeconds);
            activeOrderTimeRemaining = activeOrderDuration;
            hasActiveOrder = true;
            activeOrderWarningPlayed = false;
            orderRevision++;
            PlayFeedback(FeedbackCue.OrderAppears);
        }

        /// <summary>
        /// Dữ liệu nguồn chỉ dùng layer 0 và 1. Layer 0 là món đang trên bếp,
        /// layer 1 cho phép Order xét thêm lớp chờ đang hiện trên đĩa.
        /// Chỉ đọc số phần tử tương ứng số món vì một số level cũ lưu mảng
        /// cố định ba phần tử dù Order chỉ yêu cầu một hoặc hai món.
        /// </summary>
        private static int ResolveOrderWaitingDepth(
            OrderLevelData order,
            int requestedCount)
        {
            if (order == null ||
                order.preferredLayers == null ||
                order.preferredLayers.Count == 0)
            {
                return 0;
            }

            int waitingDepth = 0;
            int usableCount = Mathf.Min(
                requestedCount,
                order.preferredLayers.Count);
            for (int index = 0; index < usableCount; index++)
            {
                waitingDepth = Mathf.Max(
                    waitingDepth,
                    order.preferredLayers[index]);
            }

            return Mathf.Clamp(waitingDepth, 0, 2);
        }

        private List<FoodItemData> SelectOrderTargets(
            int requestedCount,
            int waitingDepth)
        {
            Dictionary<string, FoodItemData> dataById =
                new Dictionary<string, FoodItemData>();
            Dictionary<string, int> counts =
                new Dictionary<string, int>();

            foreach (Grill grill in grills)
            {
                if (grill == null || grill.IsLocked) continue;

                List<FoodItemData> items = new List<FoodItemData>();
                grill.AppendOrderCandidateData(items, waitingDepth);
                foreach (FoodItemData item in items)
                {
                    AddBoosterItemCount(item, dataById, counts);
                }
            }

            List<FoodItemData> candidates = new List<FoodItemData>();
            foreach (KeyValuePair<string, int> entry in counts)
            {
                if (entry.Value >= 3 &&
                    dataById.TryGetValue(
                        entry.Key,
                        out FoodItemData candidate))
                {
                    candidates.Add(candidate);
                }
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                int swapIndex = Random.Range(i, candidates.Count);
                FoodItemData temp = candidates[i];
                candidates[i] = candidates[swapIndex];
                candidates[swapIndex] = temp;
            }

            if (candidates.Count < requestedCount)
            {
                return new List<FoodItemData>();
            }

            return candidates.GetRange(0, requestedCount);
        }

        private void ApplyMatchingSetToOrder(FoodItemData clearedItem)
        {
            if (!hasActiveOrder || clearedItem == null) return;

            for (int index = 0; index < activeOrderItems.Count; index++)
            {
                if (!activeOrderCompleted[index] &&
                    activeOrderItems[index] != null &&
                    FoodItemData.AreMatching(
                        activeOrderItems[index],
                        clearedItem))
                {
                    activeOrderCompleted[index] = true;
                    orderRevision++;
                    break;
                }
            }

            for (int index = 0; index < activeOrderCompleted.Count; index++)
            {
                if (!activeOrderCompleted[index]) return;
            }

            FinishActiveOrder(true);
        }

        private void FinishActiveOrder(bool completed)
        {
            if (!hasActiveOrder) return;

            hasActiveOrder = false;
            activeOrderTimeRemaining = 0f;
            activeOrderWarningPlayed = false;
            orderRevision++;
            PlayFeedback(
                completed
                    ? FeedbackCue.OrderCompleted
                    : FeedbackCue.OrderFailed);
            Debug.Log(completed
                ? "Order completed!"
                : "Order expired.");
        }

        /// <summary>
        /// Bỏ món Order hiện tại đồng nghĩa từ chối phục vụ và thua màn.
        /// </summary>
        public bool TrySkipActiveOrder()
        {
            if (!isGameActive || isPaused || !hasActiveOrder) return false;

            RegisterPlayerActivity();
            FinishActiveOrder(false);
            GameOver(false);
            return true;
        }

        public bool HasActiveOrder()
        {
            return hasActiveOrder;
        }

        public int GetOrderRevision()
        {
            return orderRevision;
        }

        public float GetOrderRemainingTime()
        {
            return activeOrderTimeRemaining;
        }

        public float GetOrderTimeRatio()
        {
            return activeOrderDuration <= 0f
                ? 0f
                : Mathf.Clamp01(
                    activeOrderTimeRemaining / activeOrderDuration);
        }

        public IReadOnlyList<FoodItemData> GetActiveOrderItems()
        {
            return activeOrderItems;
        }

        public bool IsOrderItemCompleted(int index)
        {
            return index >= 0 &&
                index < activeOrderCompleted.Count &&
                activeOrderCompleted[index];
        }

        public bool HasLockedSpecialGrill()
        {
            foreach (Grill grill in grills)
            {
                if (grill != null && grill.IsSpecialLock)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Dùng cho nút vật phẩm đặc biệt sau này. Vật phẩm chỉ có tác dụng
        /// nếu bàn đang có bếp khóa đặc biệt và hiện mở một bếp mỗi lần dùng.
        /// </summary>
        public bool TryUseSpecialGrillUnlock()
        {
            foreach (Grill grill in grills)
            {
                if (grill != null && grill.TryUnlockSpecial())
                {
                    RegisterPlayerActivity();
                    return true;
                }
            }

            return false;
        }

        public int GetBoosterCount(string boosterId)
        {
            string key = BoosterPrefPrefix + boosterId;
            if (!PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.SetInt(key, defaultBoosterCount);
                PlayerPrefs.Save();
            }
            return Mathf.Max(0, PlayerPrefs.GetInt(key, defaultBoosterCount));
        }

        public bool CanUseBooster(string boosterId)
        {
            if (!CanUseBoosterNow() || GetBoosterCount(boosterId) <= 0)
                return false;

            switch (boosterId)
            {
                case BoxBoosterId:
                    return FindBoxTarget() != null;
                case RefreshBoosterId:
                    return CountUnlockedFoodItems() > 1;
                case TimeBoosterId:
                    return timeRemaining > 0f;
                case PlusBoosterId:
                    return HasLockedSpecialGrill();
                default:
                    return false;
            }
        }

        public bool TryUseBoxBooster()
        {
            if (!CanUseBooster(BoxBoosterId)) return false;

            FoodItemData target = FindBoxTarget();
            if (target == null) return false;

            CancelPointerInteraction();
            RegisterPlayerActivity();

            List<FoodItemData> removedItems = new List<FoodItemData>();
            int remaining = 3;
            foreach (Grill grill in grills)
            {
                if (grill == null || grill.IsLocked || remaining <= 0)
                    continue;
                int removed = grill.RemoveItemsForBox(
                    target,
                    remaining,
                    removedItems);
                remaining -= removed;
            }

            if (removedItems.Count != 3)
            {
                Debug.LogWarning(
                    "Box không tìm đủ ba xiên nên không tiêu hao vật phẩm.");
                return false;
            }

            foreach (FoodItemData removed in removedItems)
            {
                OnSkewerCleared(removed);
            }
            OnMatchingSetCleared(target, false);
            ConsumeBooster(BoxBoosterId);
            PlayFeedback(FeedbackCue.BoxBooster);
            CheckGameStatus();
            return true;
        }

        public bool TryUseRefreshBooster()
        {
            if (!CanUseBooster(RefreshBoosterId)) return false;

            CancelPointerInteraction();
            RegisterPlayerActivity();
            ConsumeBooster(RefreshBoosterId);
            PlayFeedback(FeedbackCue.RefreshBooster);
            StartCoroutine(ShuffleAllGrillsCoroutine());
            return true;
        }

        public bool TryUseTimeBooster()
        {
            if (!CanUseBooster(TimeBoosterId)) return false;

            timeBoosterRemaining += timeBoosterDuration;
            RegisterPlayerActivity();
            ConsumeBooster(TimeBoosterId);
            PlayFeedback(FeedbackCue.TimeBooster);
            return true;
        }

        public bool TryUsePlusBooster()
        {
            if (!CanUseBooster(PlusBoosterId)) return false;
            if (!TryUseSpecialGrillUnlock()) return false;

            ConsumeBooster(PlusBoosterId);
            PlayFeedback(FeedbackCue.PlusBooster);
            return true;
        }

        /// <summary>
        /// Order sau này dùng cùng hàm này để thời gian màn và Order chậm đồng bộ,
        /// trong khi animation và thao tác kéo thả vẫn giữ nguyên tốc độ.
        /// </summary>
        public float GetCountdownDeltaTime()
        {
            float multiplier =
                timeBoosterRemaining > 0f ? timeBoosterMultiplier : 1f;
            return Time.deltaTime * multiplier;
        }

        public float GetTimeBoosterRemaining()
        {
            return timeBoosterRemaining;
        }

        private bool CanUseBoosterNow()
        {
            return isGameActive &&
                !isPaused &&
                !isBoardLocked &&
                !isShufflingBoard &&
                !HasAnimatingGrill();
        }

        private void ConsumeBooster(string boosterId)
        {
            string key = BoosterPrefPrefix + boosterId;
            int newCount = Mathf.Max(0, GetBoosterCount(boosterId) - 1);
            PlayerPrefs.SetInt(key, newCount);
            PlayerPrefs.Save();
        }

        private int CountUnlockedFoodItems()
        {
            List<FoodItemData> items = new List<FoodItemData>();
            foreach (Grill grill in grills)
            {
                grill?.AppendAllFoodData(items);
            }
            return items.Count;
        }

        private FoodItemData FindBoxTarget()
        {
            Dictionary<string, FoodItemData> dataById =
                new Dictionary<string, FoodItemData>();
            Dictionary<string, int> activeCounts =
                new Dictionary<string, int>();
            Dictionary<string, int> totalCounts =
                new Dictionary<string, int>();

            foreach (Grill grill in grills)
            {
                if (grill == null || grill.IsLocked) continue;

                foreach (SkewerVisual skewer in grill.activeSkewers)
                {
                    FoodItemData data =
                        skewer != null ? skewer.GetData() : null;
                    AddBoosterItemCount(
                        data,
                        dataById,
                        activeCounts);
                }

                List<FoodItemData> allItems = new List<FoodItemData>();
                grill.AppendAllFoodData(allItems);
                foreach (FoodItemData data in allItems)
                {
                    AddBoosterItemCount(
                        data,
                        dataById,
                        totalCounts);
                }
            }

            if (hasActiveOrder)
            {
                for (int index = 0; index < activeOrderItems.Count; index++)
                {
                    FoodItemData orderItem = activeOrderItems[index];
                    if (!activeOrderCompleted[index] &&
                        orderItem != null &&
                        totalCounts.TryGetValue(
                            orderItem.GetMatchKey(),
                            out int orderItemCount) &&
                        orderItemCount >= 3)
                    {
                        return orderItem;
                    }
                }
            }

            foreach (KeyValuePair<string, int> entry in activeCounts)
            {
                if (entry.Value >= 3 &&
                    dataById.TryGetValue(entry.Key, out FoodItemData data))
                {
                    return data;
                }
            }

            List<FoodItemData> candidates = new List<FoodItemData>();
            foreach (KeyValuePair<string, int> entry in totalCounts)
            {
                if (entry.Value >= 3 &&
                    dataById.TryGetValue(entry.Key, out FoodItemData data))
                {
                    candidates.Add(data);
                }
            }

            return candidates.Count > 0
                ? candidates[Random.Range(0, candidates.Count)]
                : null;
        }

        private static void AddBoosterItemCount(
            FoodItemData data,
            Dictionary<string, FoodItemData> dataById,
            Dictionary<string, int> counts)
        {
            if (data == null)
                return;

            string matchKey = data.GetMatchKey();
            dataById[matchKey] = data;
            counts.TryGetValue(matchKey, out int oldCount);
            counts[matchKey] = oldCount + 1;
        }

        public void CheckGameStatus()
        {
            if (skewersClearedCount >= skewersTarget)
            {
                GameOver(true); // Win
            }
            else if (HasAnimatingGrill())
            {
                // Chờ animation cuối cùng kết thúc rồi mới xét deadlock.
                // Nếu không, một bếp khác đang xóa có thể bị tính nhầm là
                // trạng thái bàn chơi cố định và kích hoạt xáo bài.
                return;
            }
            else if (IsDeadlock())
            {
                if (CountUnlockedFoodItems() > 1)
                {
                    Debug.LogWarning("Deadlock detected! Shuffling board...");
                    StartCoroutine(ShuffleAllGrillsCoroutine());
                }
            }
        }

        // Check if there are no possible moves left on the entire board
        private bool IsDeadlock()
        {
            // If any grill is empty, player can move any top skewer there (no deadlock)
            for (int i = 0; i < grills.Length; i++)
            {
                if (grills[i] != null &&
                    !grills[i].IsLocked &&
                    grills[i].activeSkewers.Count == 0)
                {
                    return false;
                }
            }

            // Check if any top skewer can be placed on any other grill
            for (int i = 0; i < grills.Length; i++)
            {
                if (grills[i] == null ||
                    grills[i].IsLocked ||
                    grills[i].activeSkewers.Count == 0) continue;

                FoodItemData topItem = grills[i].activeSkewers[grills[i].activeSkewers.Count - 1].GetData();

                for (int j = 0; j < grills.Length; j++)
                {
                    if (i == j || grills[j] == null || grills[j].IsLocked)
                        continue;
                    if (grills[j].CanPush(topItem))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private IEnumerator ShuffleAllGrillsCoroutine()
        {
            CancelHint();
            isShufflingBoard = true;
            isBoardLocked = true;
            yield return new WaitForSeconds(0.15f);
            
            bool hasValidMove = false;
            int attempts = 0;
            List<FoodItemData> allFoodData = new List<FoodItemData>();
            foreach (Grill grill in grills)
            {
                grill?.AppendAllFoodData(allFoodData);
            }

            while (!hasValidMove &&
                   attempts < 50 &&
                   allFoodData.Count > 1)
            {
                attempts++;

                // Xáo cả bếp, lớp chờ đang hiện và các lớp chờ sâu hơn.
                for (int i = 0; i < allFoodData.Count; i++)
                {
                    int rand = Random.Range(i, allFoodData.Count);
                    FoodItemData temp = allFoodData[i];
                    allFoodData[i] = allFoodData[rand];
                    allFoodData[rand] = temp;
                }

                int idx = 0;
                foreach (Grill grill in grills)
                {
                    if (grill == null) continue;
                    idx = grill.ReplaceAllFoodData(allFoodData, idx);
                }

                yield return new WaitForSeconds(0.12f);

                hasValidMove =
                    FindHintTriplet().Count == 3 || !IsDeadlock();
            }

            isBoardLocked = false;
            isShufflingBoard = false;
            RegisterPlayerActivity();
            Debug.Log($"Shuffled board successfully after {attempts} attempts.");
        }

        private void GameOver(bool isWin)
        {
            CancelHint();
            // Thời gian/order có thể kết thúc đúng lúc người chơi đang kéo.
            // Trả xiên và sorting về trạng thái chuẩn trước khi hiện popup.
            CancelPointerInteraction();
            isGameActive = false;
            isBoardLocked = true;
            hasActiveOrder = false;
            orderRevision++;
            if (gameUIManager != null) gameUIManager.ShowResult(isWin);
            PlayFeedback(isWin ? FeedbackCue.Win : FeedbackCue.Lose);

            if (isWin)
            {
                Debug.Log("LEVEL COMPLETED! You won!");
            }
            else
            {
                Debug.Log("GAME OVER! Time ran out.");
            }
        }

        public string GetFormattedTime()
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);
            return string.Format(
                "<mspace=0.70em>{0:00}:{1:00}</mspace>", minutes, seconds);
        }

        public string GetProgressString()
        {
            int completedSets = skewersClearedCount / 3;
            int targetSets = skewersTarget / 3;
            return $"{completedSets}/{targetSets}";
        }

        public int GetCurrentLevelNumber()
        {
            return currentLevelData != null
                ? Mathf.Max(1, currentLevelData.levelNumber)
                : 1;
        }

        public float GetRemainingTimeRatio()
        {
            return levelDuration <= 0f
                ? 0f
                : Mathf.Clamp01(timeRemaining / levelDuration);
        }
    }
}
