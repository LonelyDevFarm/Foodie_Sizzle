using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FoodieSizzle
{
    public class GameplayManager : MonoBehaviour
    {
        [Header("Bố cục bàn chơi")]
        public Grill[] grills = new Grill[12];
        [Min(1)] public int columnCount = 3;
        public float horizontalSpacing = 1.45f;
        public float verticalSpacing = 1.75f;
        public Vector2 boardCenter = new Vector2(0f, 0.55f);

        [Header("Danh sách món dùng để tạo màn thử nghiệm")]
        public List<FoodItemData> possibleFoodItems;

        [Header("Dữ liệu màn chơi")]
        [Tooltip("Có thể để trống để chạy Level 1 mẫu được tạo sẵn trong code.")]
        public LevelData currentLevelData;

        [Header("Level Goals")]
        public int skewersTarget = 18; // Must clear 18 skewers (6 sets of 3)
        public float timeRemaining = 300f; // 5 minutes

        [Header("Select Effect")]
        public float liftOffset = 0.5f;

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
        private float levelDuration;
        private bool isPaused;
        private bool boardWasLockedBeforePause;
        private bool isShufflingBoard;

        private void Start()
        {
            gameUIManager = GetComponent<GameUIManager>();
            if (gameUIManager == null)
            {
                gameUIManager = gameObject.AddComponent<GameUIManager>();
            }
            gameUIManager.Initialize(this);

            ApplyBoardLayout();
            StartNewLevel();
        }

        /// <summary>
        /// Tự xếp các bếp theo lưới từ trái sang phải, từ trên xuống dưới.
        /// Dữ liệu level về sau chỉ cần quyết định bếp nào được bật hoặc khóa.
        /// </summary>
        private void ApplyBoardLayout()
        {
            if (grills == null || grills.Length == 0) return;

            int columns = Mathf.Max(1, columnCount);
            int rows = Mathf.CeilToInt(grills.Length / (float)columns);
            float firstX = boardCenter.x - (columns - 1) * horizontalSpacing * 0.5f;
            float firstY = boardCenter.y + (rows - 1) * verticalSpacing * 0.5f;

            for (int i = 0; i < grills.Length; i++)
            {
                if (grills[i] == null) continue;

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
                UpdatePointerInput();
            }
        }

        private void UpdatePointerInput()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null || Camera.main == null) return;

            if (pointer.press.wasPressedThisFrame)
            {
                RecoverInterruptedInputIfNeeded();

                pointerDownPosition = pointer.position.ReadValue();
                pointerDownGrill = FindGrillAtScreenPosition(pointerDownPosition);
                hasDragged = false;
                draggedSkewer = null;

                if (pointerDownGrill != null &&
                    !isBoardLocked &&
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
                        draggedSkewer.BeginDrag();
                    }
                }
            }

            Vector2 currentPointerPosition = pointer.position.ReadValue();
            if (pointer.press.isPressed && draggedSkewer != null)
            {
                if (!hasDragged &&
                    Vector2.Distance(
                        pointerDownPosition,
                        currentPointerPosition) >= GetDragThresholdPixels())
                {
                    hasDragged = true;
                    ClearCurrentSelection();
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
                bool moved = TryCompleteDrag(
                    pointerDownGrill, pointerUpGrill, draggedSkewer);
                if (!moved)
                {
                    draggedSkewer.MoveTo(dragStartPosition, 0.15f);
                }
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
            if (isBoardLocked || !isGameActive || source.activeSkewers.Count == 0)
                return false;
            if (!source.activeSkewers.Contains(skewer))
                return false;

            if (source == target)
            {
                return source.MoveSkewerWithinGrill(
                    skewer, skewer.transform.position);
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
            return true;
        }

        private void ResetPointerDrag()
        {
            pointerDownGrill = null;
            draggedSkewer = null;
            hasDragged = false;
        }

        private void RecoverInterruptedInputIfNeeded()
        {
            // Nếu lần kéo trước bị ngắt bởi mất focus hoặc animation,
            // bảo đảm xiên không giữ sorting/state kéo mãi mãi.
            if (draggedSkewer != null)
            {
                draggedSkewer.EndDrag();
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

        private void UpdateTimer()
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
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
            isGameActive = true;
            isBoardLocked = false;
            isPaused = false;
            isShufflingBoard = false;
            skewersClearedCount = 0;
            selectedGrill = null;
            selectedSkewer = null;
            if (gameUIManager != null) gameUIManager.HideResult();

            if (!LoadLevelData())
            {
                isGameActive = false;
                Debug.LogError("Không thể khởi tạo level. Hãy gán ít nhất 3 FoodItemData.");
                return;
            }
            
            Debug.Log($"Foodie Sizzle level started! Target: {skewersTarget} skewers.");
            levelDuration = Mathf.Max(1f, timeRemaining);
        }

        public void RestartLevel()
        {
            StopAllCoroutines();
            isBoardLocked = false;
            StartNewLevel();
        }

        public void SetPaused(bool paused)
        {
            if (!isGameActive) return;

            if (paused)
            {
                boardWasLockedBeforePause = isBoardLocked;
                isPaused = true;
                isBoardLocked = true;
                ClearCurrentSelection();
                ResetPointerDrag();
            }
            else
            {
                isPaused = false;
                isBoardLocked = boardWasLockedBeforePause;
            }
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
                grill.MoveSkewerWithinGrill(
                    movingSkewer,
                    clickedWorldPosition,
                    0.2f);
                ClearCurrentSelection();
                return;
            }

            if (!grill.CanPush(selectedSkewer.GetData()) ||
                !grill.CanDropAtPosition(clickedWorldPosition))
            {
                ClearCurrentSelection();
                return;
            }

            Grill sourceGrill = selectedGrill;
            SkewerVisual movingToTarget = selectedSkewer;
            ClearCurrentSelection();

            sourceGrill.Pop(movingToTarget);
            grill.Push(movingToTarget, 0.25f, clickedWorldPosition);

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

        public void CheckGameStatus()
        {
            if (skewersClearedCount >= skewersTarget)
            {
                GameOver(true); // Win
            }
            else if (IsDeadlock())
            {
                Debug.LogWarning("Deadlock detected! Shuffling board...");
                StartCoroutine(ShuffleAllGrillsCoroutine());
            }
        }

        // Check if there are no possible moves left on the entire board
        private bool IsDeadlock()
        {
            // If any grill is empty, player can move any top skewer there (no deadlock)
            for (int i = 0; i < grills.Length; i++)
            {
                if (grills[i] != null && grills[i].activeSkewers.Count == 0)
                {
                    return false;
                }
            }

            // Check if any top skewer can be placed on any other grill
            for (int i = 0; i < grills.Length; i++)
            {
                if (grills[i] == null || grills[i].activeSkewers.Count == 0) continue;

                FoodItemData topItem = grills[i].activeSkewers[grills[i].activeSkewers.Count - 1].GetData();

                for (int j = 0; j < grills.Length; j++)
                {
                    if (i == j || grills[j] == null) continue;
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
            isShufflingBoard = true;
            isBoardLocked = true;
            yield return new WaitForSeconds(0.5f);

            // Shuffling algorithm for Skewer Sort:
            // 1. Gather all active items on the grills.
            // 2. Shuffle them.
            // 3. Redistribute them.
            // (In a real game, we would do this with check loops to ensure a valid move exists.
            // Here, we do a basic shuffle and check, repeating if still deadlocked).
            
            bool hasValidMove = false;
            int attempts = 0;

            while (!hasValidMove && attempts < 50)
            {
                attempts++;
                
                List<FoodItemData> allActiveData = new List<FoodItemData>();
                for (int i = 0; i < grills.Length; i++)
                {
                    if (grills[i] == null) continue;
                    foreach (var skewer in grills[i].activeSkewers)
                    {
                        allActiveData.Add(skewer.GetData());
                    }
                }

                // Shuffle list
                for (int i = 0; i < allActiveData.Count; i++)
                {
                    int rand = Random.Range(i, allActiveData.Count);
                    FoodItemData temp = allActiveData[i];
                    allActiveData[i] = allActiveData[rand];
                    allActiveData[rand] = temp;
                }

                // Redistribute visually and logic-wise
                int idx = 0;
                for (int i = 0; i < grills.Length; i++)
                {
                    if (grills[i] == null) continue;

                    // Keep same size but distribute new shuffled items
                    int currentCount = grills[i].activeSkewers.Count;
                    for (int k = 0; k < currentCount; k++)
                    {
                        if (idx < allActiveData.Count)
                        {
                            grills[i].activeSkewers[k].SetData(allActiveData[idx]);
                            grills[i].activeSkewers[k].MoveTo(grills[i].activeSlots[k].position, 0.3f);
                            idx++;
                        }
                    }
                }

                yield return new WaitForSeconds(0.35f);

                // Re-evaluate deadlock
                if (!IsDeadlock())
                {
                    hasValidMove = true;
                }
            }

            isBoardLocked = false;
            isShufflingBoard = false;
            Debug.Log($"Shuffled board successfully after {attempts} attempts.");
        }

        private void GameOver(bool isWin)
        {
            isGameActive = false;
            isBoardLocked = true;
            ClearCurrentSelection();
            ResetPointerDrag();
            if (gameUIManager != null) gameUIManager.ShowResult(isWin);

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

        public float GetRemainingTimeRatio()
        {
            return levelDuration <= 0f
                ? 0f
                : Mathf.Clamp01(timeRemaining / levelDuration);
        }
    }
}
