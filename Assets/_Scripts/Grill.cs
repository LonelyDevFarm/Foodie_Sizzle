using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FoodieSizzle
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class Grill : MonoBehaviour
    {
        [Header("Vị trí đặt món")]
        [Tooltip("Ba vị trí món trên lưới, theo thứ tự trái - giữa - phải.")]
        public Transform[] activeSlots = new Transform[3];
        [Tooltip("Ba vị trí món trên đĩa chờ, theo thứ tự trái - giữa - phải.")]
        public Transform[] waitingSlots = new Transform[3];

        [Header("State")]
        public List<SkewerVisual> activeSkewers = new List<SkewerVisual>();
        public List<SkewerVisual> waitingSkewers = new List<SkewerVisual>();
        private readonly Dictionary<SkewerVisual, int> activeSlotIndices =
            new Dictionary<SkewerVisual, int>();
        private readonly Queue<List<FoodItemData>> waitingLayerQueue =
            new Queue<List<FoodItemData>>();

        private GameplayManager gameplayManager;
        private bool isAnimating = false;
        public bool IsAnimating => isAnimating;
        private bool isLocked;
        private bool isSpecialLock;
        private string unlockItemId;
        private GameObject lockVisualRoot;
        public bool IsLocked => isLocked;
        public bool IsSpecialLock => isLocked && isSpecialLock;

        [Header("Bếp bị khóa")]
        [Tooltip("Ảnh nắp bếp đóng. Dùng IndicatedPack_12 trong bộ sprite gốc.")]
        [SerializeField] private Sprite lockedCoverSprite;
        [Tooltip("Tinh chỉnh mép trên của nắp sau khi tự động căn với thân bếp.")]
        [SerializeField] private float lockedCoverTopOffset;
        [Tooltip("Thời gian nắp bếp thu nhỏ rồi biến mất khi được mở.")]
        [Min(0.05f)] [SerializeField] private float unlockDuration = 0.25f;

        [Header("Thẻ mở bằng xiên")]
        [Tooltip("Kích thước thẻ dọc chứa hình xiên cần ghép.")]
        [SerializeField] private Vector2 foodUnlockTagSize =
            new Vector2(0.34f, 0.56f);
        [Tooltip("Vị trí thẻ dọc so với tâm bếp.")]
        [SerializeField] private Vector2 foodUnlockTagPosition =
            new Vector2(0f, 0.02f);
        [Tooltip("Kích thước tối đa của hình xiên bên trong thẻ.")]
        [SerializeField] private Vector2 foodUnlockIconSize =
            new Vector2(0.23f, 0.30f);

        [Header("Thẻ mở bằng vật phẩm đặc biệt")]
        [Tooltip("Kích thước thẻ ngang. Sau này biểu tượng vật phẩm sẽ nằm trên thẻ này.")]
        [SerializeField] private Vector2 specialUnlockTagSize =
            new Vector2(0.72f, 0.38f);
        [Tooltip("Vị trí thẻ ngang so với tâm bếp.")]
        [SerializeField] private Vector2 specialUnlockTagPosition =
            new Vector2(0f, 0.02f);
        [Tooltip("Icon vật phẩm Plus hiển thị trên bếp khóa đặc biệt.")]
        [SerializeField] private Sprite specialUnlockIconSprite;
        [Tooltip("Kích thước icon Plus trên thẻ mở khóa đặc biệt.")]
        [SerializeField] private Vector2 specialUnlockIconSize =
            new Vector2(0.26f, 0.26f);
        private Transform waitingPlate;
        private Transform waitingPlateBack1;
        private Transform waitingPlateBack2;
        private readonly List<Transform> waitingPlateLayers = new List<Transform>();
        private Vector3 lastWaitingPlatePosition;
        private Vector3 waitingPlateStartPosition;
        private bool hasWaitingPlateStartPosition;
        private int remainingPlateCount;

        [Header("Chồng đĩa chờ")]
        [Min(0)] public int initialPlateCount = 5;
        [Min(0.01f)] public float plateLayerSpacing = 0.05f;

        [Header("Hiệu ứng hoàn thành")]
        [Tooltip("Chờ xiên bay hẳn vào ô rồi mới làm cả bộ biến mất.")]
        [Min(0f)] public float clearArrivalDelay = 0.22f;
        [Tooltip("Thời gian thu nhỏ bộ ba khi hoàn thành.")]
        [Min(0.05f)] public float clearScaleDuration = 0.18f;
        [Tooltip("Thời gian đẩy lớp xiên chờ tiếp theo lên bếp.")]
        [Min(0.05f)] public float waitingSlideDuration = 0.2f;
        [Tooltip("Thời gian làm xiên chờ mới xuất hiện.")]
        [Min(0.05f)] public float waitingSpawnDuration = 0.14f;

        [Header("Phạm vi nhận thao tác thả")]
        [Tooltip("Nửa chiều rộng vùng nhận của mỗi ô so với khoảng cách giữa hai ô. Nên nhỏ hơn 0.5 để hai vùng không chồng nhau.")]
        [Range(0.1f, 0.5f)] public float horizontalDropRangeRatio = 0.48f;
        [Tooltip("Tỉ lệ chiều cao bên trong bếp được phép nhận thao tác thả.")]
        [Range(0.1f, 1f)] public float verticalDropRangeRatio = 0.8f;
        [Tooltip("Vùng đệm quanh sprite giúp thao tác chạm trên điện thoại không bị hụt.")]
        [Min(0f)] public float skewerTapPadding = 0.12f;

        private void Awake()
        {
            FindWaitingPlate();
            ResetWaitingPlateStack();
        }

        private void OnEnable()
        {
            // Khi Unity biên dịch lại script trong Play Mode, coroutine cũ bị mất.
            // Không để cờ animation sót lại và khóa bếp vĩnh viễn.
            if (Application.isPlaying)
            {
                isAnimating = false;
            }
        }

        private void LateUpdate()
        {
            if (waitingPlate == null)
            {
                FindWaitingPlate();
                if (waitingPlate == null) return;
            }

            // Nếu đĩa được dịch chuyển khi đang chạy, các slot và xiên chờ phải đi theo.
            if (waitingPlate.localPosition != lastWaitingPlatePosition)
            {
                SyncDynamicWaitingArea();
            }
        }

        public void Initialize(
            List<FoodItemData> initialActive,
            List<List<FoodItemData>> waitingLayers,
            GameplayManager manager)
        {
            gameplayManager = manager;
            StopAllCoroutines();
            isAnimating = false;
            ClearSpawnedSkewers();
            activeSkewers.Clear();
            activeSlotIndices.Clear();
            waitingSkewers.Clear();
            waitingLayerQueue.Clear();

            if (waitingLayers != null)
            {
                foreach (List<FoodItemData> layer in waitingLayers)
                {
                    waitingLayerQueue.Enqueue(
                        layer != null ? new List<FoodItemData>(layer) : new List<FoodItemData>());
                }
            }

            initialPlateCount = waitingLayerQueue.Count;
            FindWaitingPlate();
            ResetWaitingPlateStack();

            SpawnActiveLayer(initialActive);

            if (waitingLayerQueue.Count > 0)
            {
                SpawnWaitingLayer(waitingLayerQueue.Dequeue(), false);
            }
        }

        /// <summary>
        /// Khóa bếp bằng loại món được chỉ định. Bếp khóa vẫn giữ dữ liệu bên dưới
        /// nhưng không nhận chạm, kéo, thả hoặc gợi ý.
        /// </summary>
        public void ConfigureLock(FoodItemData unlockItem, int sourceLockId = 0)
        {
            unlockItemId = unlockItem != null ? unlockItem.itemId : string.Empty;
            isSpecialLock = sourceLockId < 0;
            isLocked = unlockItem != null || isSpecialLock;
            BuildOrRefreshLockVisual(unlockItem);
        }

        public bool TryUnlockForItem(string clearedItemId)
        {
            if (!isLocked ||
                isAnimating ||
                string.IsNullOrWhiteSpace(clearedItemId) ||
                clearedItemId != unlockItemId)
            {
                return false;
            }

            StartCoroutine(UnlockCoroutine());
            return true;
        }

        /// <summary>
        /// Điểm nối dành cho vật phẩm mở bếp đặc biệt sau này.
        /// Chỉ trả về true và tiêu thụ tác dụng khi bếp thực sự đang khóa đặc biệt.
        /// </summary>
        public bool TryUnlockSpecial()
        {
            if (!IsSpecialLock || isAnimating) return false;

            StartCoroutine(UnlockCoroutine());
            return true;
        }

        private void BuildOrRefreshLockVisual(FoodItemData unlockItem)
        {
            if (lockVisualRoot != null)
            {
                Destroy(lockVisualRoot);
                lockVisualRoot = null;
            }

            if (!isLocked || lockedCoverSprite == null) return;

            lockVisualRoot = new GameObject("LockedGrillVisual");
            lockVisualRoot.transform.SetParent(transform, false);

            GameObject coverObject = new GameObject("ClosedCover");
            coverObject.transform.SetParent(lockVisualRoot.transform, false);
            SpriteRenderer cover = coverObject.AddComponent<SpriteRenderer>();
            cover.sprite = lockedCoverSprite;
            cover.sortingOrder = 30;

            const float fallbackTargetWidth = 1.28f;
            float targetWidth = fallbackTargetWidth;
            float targetCenterX = 0f;
            float targetTopY = 0.59f;

            Transform grillBodyTransform = transform.Find("GrillBody");
            SpriteRenderer grillBody = grillBodyTransform != null
                ? grillBodyTransform.GetComponent<SpriteRenderer>()
                : null;
            if (grillBody != null && grillBody.sprite != null)
            {
                Vector3 bodyMinLocal =
                    transform.InverseTransformPoint(grillBody.bounds.min);
                Vector3 bodyMaxLocal =
                    transform.InverseTransformPoint(grillBody.bounds.max);
                targetWidth = Mathf.Abs(bodyMaxLocal.x - bodyMinLocal.x);
                targetCenterX = (bodyMinLocal.x + bodyMaxLocal.x) * 0.5f;
                targetTopY = bodyMaxLocal.y;
            }

            float coverScale = targetWidth / lockedCoverSprite.bounds.size.x;
            coverObject.transform.localScale =
                new Vector3(coverScale, coverScale, 1f);
            Vector3 scaledCenter = Vector3.Scale(
                lockedCoverSprite.bounds.center,
                coverObject.transform.localScale);
            float scaledTop =
                lockedCoverSprite.bounds.max.y * coverScale;
            coverObject.transform.localPosition =
                new Vector3(
                    targetCenterX - scaledCenter.x,
                    targetTopY + lockedCoverTopOffset - scaledTop,
                    0f);

            SpriteRenderer plateSource = waitingPlate != null
                ? waitingPlate.GetComponent<SpriteRenderer>()
                : null;
            if (plateSource == null || plateSource.sprite == null)
            {
                return;
            }

            GameObject tagObject = new GameObject("UnlockTag");
            tagObject.transform.SetParent(lockVisualRoot.transform, false);
            SpriteRenderer tag = tagObject.AddComponent<SpriteRenderer>();
            tag.sprite = plateSource.sprite;
            tag.color = new Color(1f, 0.91f, 0.68f, 1f);
            tag.sortingOrder = 31;
            Vector2 tagSize = isSpecialLock
                ? specialUnlockTagSize
                : foodUnlockTagSize;
            Vector2 tagPosition = isSpecialLock
                ? specialUnlockTagPosition
                : foodUnlockTagPosition;
            tagObject.transform.localScale =
                new Vector3(
                    tagSize.x /
                        Mathf.Max(0.001f, tag.sprite.bounds.size.x),
                    tagSize.y /
                        Mathf.Max(0.001f, tag.sprite.bounds.size.y),
                    1f);
            tagObject.transform.localPosition =
                new Vector3(tagPosition.x, tagPosition.y, 0f);

            Sprite requiredIcon = isSpecialLock
                ? specialUnlockIconSprite
                : unlockItem != null ? unlockItem.itemSprite : null;
            if (requiredIcon == null)
            {
                // Khóa đặc biệt chỉ hiện nắp và thẻ khóa. Biểu tượng vật phẩm
                // sẽ được gắn vào đây khi thiết kế vật phẩm mở khóa hoàn tất.
                return;
            }

            GameObject iconObject = new GameObject(
                isSpecialLock ? "RequiredPlusIcon" : "RequiredFoodIcon");
            iconObject.transform.SetParent(lockVisualRoot.transform, false);
            SpriteRenderer icon = iconObject.AddComponent<SpriteRenderer>();
            icon.sprite = requiredIcon;
            icon.sortingOrder = 32;
            Vector2 iconSize = isSpecialLock
                ? specialUnlockIconSize
                : foodUnlockIconSize;
            float iconScale = Mathf.Min(
                iconSize.x /
                    Mathf.Max(0.001f, icon.sprite.bounds.size.x),
                iconSize.y /
                    Mathf.Max(0.001f, icon.sprite.bounds.size.y));
            iconObject.transform.localScale =
                new Vector3(iconScale, iconScale, 1f);
            iconObject.transform.localPosition =
                new Vector3(tagPosition.x, tagPosition.y, 0f);
        }

        private IEnumerator UnlockCoroutine()
        {
            isAnimating = true;
            Transform visual = lockVisualRoot != null
                ? lockVisualRoot.transform
                : null;
            Vector3 startScale = visual != null
                ? visual.localScale
                : Vector3.one;
            float elapsed = 0f;

            while (visual != null && elapsed < unlockDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / unlockDuration);
                visual.localScale = Vector3.Lerp(
                    startScale,
                    Vector3.zero,
                    t * t);
                yield return null;
            }

            if (lockVisualRoot != null)
            {
                Destroy(lockVisualRoot);
                lockVisualRoot = null;
            }

            isLocked = false;
            isSpecialLock = false;
            unlockItemId = string.Empty;
            isAnimating = false;
            CheckAndClear();
            gameplayManager?.CheckGameStatus();
        }

        public void Initialize(
            List<FoodItemData> initialActive,
            List<FoodItemData> initialWaiting,
            GameplayManager manager)
        {
            List<List<FoodItemData>> layers = new List<List<FoodItemData>>();
            if (initialWaiting != null && initialWaiting.Count > 0)
            {
                layers.Add(initialWaiting);
            }

            Initialize(initialActive, layers, manager);
        }

        private void ClearSpawnedSkewers()
        {
            List<GameObject> oldSkewers = new List<GameObject>();
            foreach (Transform child in transform)
            {
                if (child.GetComponent<SkewerVisual>() != null)
                {
                    oldSkewers.Add(child.gameObject);
                }
            }

            foreach (GameObject oldSkewer in oldSkewers)
            {
                foreach (Renderer renderer in
                         oldSkewer.GetComponentsInChildren<Renderer>())
                {
                    renderer.enabled = false;
                }
                Destroy(oldSkewer);
            }
        }

        private void SpawnActiveLayer(List<FoodItemData> layer)
        {
            if (layer == null) return;

            for (int i = 0; i < layer.Count && i < activeSlots.Length; i++)
            {
                if (layer[i] == null) continue;
                SkewerVisual skewer = SpawnSkewer(layer[i]);
                skewer.transform.position = activeSlots[i].position;
                activeSkewers.Add(skewer);
                activeSlotIndices[skewer] = i;
            }
        }

        private void SpawnWaitingLayer(List<FoodItemData> layer, bool animate)
        {
            waitingSkewers.Clear();
            if (layer == null) return;

            for (int i = 0; i < layer.Count && i < waitingSlots.Length; i++)
            {
                if (layer[i] == null) continue;

                SkewerVisual skewer = SpawnSkewer(layer[i]);
                skewer.transform.position = waitingSlots[i].position;
                ConfigureWaitingSkewer(skewer, i);
                waitingSkewers.Add(skewer);

                if (animate)
                {
                    skewer.transform.localScale = Vector3.zero;
                    StartCoroutine(ScaleUpSkewerCoroutine(skewer, 0.2f));
                }
            }
        }

        private SkewerVisual SpawnSkewer(FoodItemData data)
        {
            GameObject skewerGo = new GameObject($"Skewer_{data.itemId}");
            skewerGo.transform.SetParent(transform);
            skewerGo.transform.localScale = Vector3.one;

            SpriteRenderer sr = skewerGo.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 5;

            SkewerVisual skewer = skewerGo.AddComponent<SkewerVisual>();
            skewer.SetData(data);

            return skewer;
        }

        public bool CanPush(FoodItemData item)
        {
            if (isAnimating || isLocked) return false;
            return activeSkewers.Count < activeSlots.Length;
        }

        public int CountActiveItem(string itemId)
        {
            if (isLocked || string.IsNullOrWhiteSpace(itemId)) return 0;

            int count = 0;
            foreach (SkewerVisual skewer in activeSkewers)
            {
                if (skewer != null && skewer.GetData() != null &&
                    skewer.GetData().itemId == itemId)
                {
                    count++;
                }
            }
            return count;
        }

        public int CountAllItem(string itemId)
        {
            if (isLocked || string.IsNullOrWhiteSpace(itemId)) return 0;

            int count = CountActiveItem(itemId);
            foreach (SkewerVisual skewer in waitingSkewers)
            {
                if (skewer != null && skewer.GetData() != null &&
                    skewer.GetData().itemId == itemId)
                {
                    count++;
                }
            }

            foreach (List<FoodItemData> layer in waitingLayerQueue)
            {
                if (layer == null) continue;
                foreach (FoodItemData item in layer)
                {
                    if (item != null && item.itemId == itemId)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        public void AppendAllFoodData(List<FoodItemData> output)
        {
            if (isLocked || output == null) return;

            foreach (SkewerVisual skewer in activeSkewers)
            {
                if (skewer != null && skewer.GetData() != null)
                    output.Add(skewer.GetData());
            }
            foreach (SkewerVisual skewer in waitingSkewers)
            {
                if (skewer != null && skewer.GetData() != null)
                    output.Add(skewer.GetData());
            }
            foreach (List<FoodItemData> layer in waitingLayerQueue)
            {
                if (layer == null) continue;
                foreach (FoodItemData item in layer)
                {
                    if (item != null) output.Add(item);
                }
            }
        }

        /// <summary>
        /// Lấy các món đủ gần để Order có thể yêu cầu một cách công bằng.
        /// waitingDepth = 0 chỉ xét món đang trên bếp; 1 tính thêm đĩa chờ
        /// hiện tại; các mức lớn hơn mới đi sâu dần vào hàng chờ.
        /// </summary>
        public void AppendOrderCandidateData(
            List<FoodItemData> output,
            int waitingDepth)
        {
            if (isLocked || output == null) return;

            foreach (SkewerVisual skewer in activeSkewers)
            {
                FoodItemData data =
                    skewer != null ? skewer.GetData() : null;
                if (data != null) output.Add(data);
            }

            if (waitingDepth <= 0) return;

            foreach (SkewerVisual skewer in waitingSkewers)
            {
                FoodItemData data =
                    skewer != null ? skewer.GetData() : null;
                if (data != null) output.Add(data);
            }

            int remainingQueuedLayers = waitingDepth - 1;
            foreach (List<FoodItemData> layer in waitingLayerQueue)
            {
                if (remainingQueuedLayers-- <= 0) break;
                if (layer == null) continue;
                foreach (FoodItemData item in layer)
                {
                    if (item != null) output.Add(item);
                }
            }
        }

        /// <summary>
        /// Thay loại món theo đúng số ô hiện có. Vị trí bếp, số lớp và số đĩa
        /// được giữ nguyên; chỉ FoodItemData bên trong bị xáo.
        /// </summary>
        public int ReplaceAllFoodData(
            IList<FoodItemData> shuffledItems,
            int startIndex)
        {
            if (isLocked || shuffledItems == null) return startIndex;

            foreach (SkewerVisual skewer in activeSkewers)
            {
                if (skewer == null || startIndex >= shuffledItems.Count)
                    continue;
                skewer.SetData(shuffledItems[startIndex++]);
                skewer.SetDisplayScale(1f);
                skewer.transform.localRotation = Quaternion.identity;
            }
            for (int waitingIndex = 0;
                 waitingIndex < waitingSkewers.Count;
                 waitingIndex++)
            {
                SkewerVisual skewer = waitingSkewers[waitingIndex];
                if (skewer == null || startIndex >= shuffledItems.Count)
                    continue;
                skewer.SetData(shuffledItems[startIndex++]);
                ConfigureWaitingSkewer(skewer, waitingIndex);
            }
            foreach (List<FoodItemData> layer in waitingLayerQueue)
            {
                if (layer == null) continue;
                for (int i = 0; i < layer.Count; i++)
                {
                    if (layer[i] == null || startIndex >= shuffledItems.Count)
                        continue;
                    layer[i] = shuffledItems[startIndex++];
                }
            }
            return startIndex;
        }

        /// <summary>
        /// Loại tối đa amount xiên đúng loại khỏi cả bếp và các lớp chờ.
        /// Sau đó dựng lại riêng bếp này để lớp/đĩa rỗng được thu gọn chính xác.
        /// </summary>
        public int RemoveItemsForBox(
            FoodItemData targetItem,
            int amount,
            List<FoodItemData> removedItems)
        {
            if (isLocked || isAnimating || targetItem == null || amount <= 0)
                return 0;

            List<List<FoodItemData>> layers = ExportCurrentLayers();
            int removed = 0;
            for (int layerIndex = 0;
                 layerIndex < layers.Count && removed < amount;
                 layerIndex++)
            {
                List<FoodItemData> layer = layers[layerIndex];
                for (int itemIndex = layer.Count - 1;
                     itemIndex >= 0 && removed < amount;
                    itemIndex--)
                {
                    FoodItemData item = layer[itemIndex];
                    if (!FoodItemData.AreMatching(item, targetItem)) continue;

                    layer.RemoveAt(itemIndex);
                    removedItems?.Add(item);
                    removed++;
                }
            }

            if (removed == 0) return 0;

            for (int i = layers.Count - 1; i >= 0; i--)
            {
                if (layers[i].Count == 0)
                {
                    layers.RemoveAt(i);
                }
            }

            List<FoodItemData> active = layers.Count > 0
                ? layers[0]
                : new List<FoodItemData>();
            List<List<FoodItemData>> waiting =
                layers.Count > 1
                    ? layers.GetRange(1, layers.Count - 1)
                    : new List<List<FoodItemData>>();
            Initialize(active, waiting, gameplayManager);
            return removed;
        }

        private List<List<FoodItemData>> ExportCurrentLayers()
        {
            List<List<FoodItemData>> layers =
                new List<List<FoodItemData>>();

            List<SkewerVisual> orderedActive =
                new List<SkewerVisual>(activeSkewers);
            orderedActive.Sort((a, b) =>
            {
                int aIndex = activeSlotIndices.TryGetValue(a, out int ai)
                    ? ai : int.MaxValue;
                int bIndex = activeSlotIndices.TryGetValue(b, out int bi)
                    ? bi : int.MaxValue;
                return aIndex.CompareTo(bIndex);
            });

            List<FoodItemData> active = new List<FoodItemData>();
            foreach (SkewerVisual skewer in orderedActive)
            {
                if (skewer != null && skewer.GetData() != null)
                    active.Add(skewer.GetData());
            }
            layers.Add(active);

            if (waitingSkewers.Count > 0)
            {
                List<FoodItemData> waiting = new List<FoodItemData>();
                foreach (SkewerVisual skewer in waitingSkewers)
                {
                    if (skewer != null && skewer.GetData() != null)
                        waiting.Add(skewer.GetData());
                }
                layers.Add(waiting);
            }

            foreach (List<FoodItemData> queuedLayer in waitingLayerQueue)
            {
                if (queuedLayer != null)
                    layers.Add(new List<FoodItemData>(queuedLayer));
            }
            return layers;
        }

        /// <summary>
        /// Tìm đúng xiên mà người chơi đang chỉ vào, thay vì luôn lấy xiên được thêm sau cùng.
        /// </summary>
        public SkewerVisual GetSkewerAtWorldPosition(Vector3 worldPosition)
        {
            if (isLocked) return null;
            SkewerVisual nearestSkewer = null;
            float nearestDistance = float.MaxValue;

            foreach (SkewerVisual skewer in activeSkewers)
            {
                if (skewer == null) continue;

                bool containsPointer = false;
                SpriteRenderer[] renderers =
                    skewer.GetComponentsInChildren<SpriteRenderer>();

                foreach (SpriteRenderer renderer in renderers)
                {
                    Bounds bounds = renderer.bounds;
                    bounds.Expand(new Vector3(
                        skewerTapPadding * 2f,
                        skewerTapPadding * 2f,
                        0f));
                    if (renderer.enabled && renderer.sprite != null &&
                        worldPosition.x >= bounds.min.x &&
                        worldPosition.x <= bounds.max.x &&
                        worldPosition.y >= bounds.min.y &&
                        worldPosition.y <= bounds.max.y)
                    {
                        containsPointer = true;
                        break;
                    }
                }

                if (!containsPointer) continue;

                float distance =
                    Vector2.SqrMagnitude(skewer.transform.position - worldPosition);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestSkewer = skewer;
                }
            }

            return nearestSkewer;
        }

        /// <summary>
        /// Trả về tâm slot thật của xiên. Không dùng transform hiện tại vì
        /// xiên có thể đang ở giữa animation và gây tích lũy sai vị trí.
        /// </summary>
        public bool TryGetSkewerSlotPosition(
            SkewerVisual skewer,
            out Vector3 slotPosition)
        {
            if (skewer != null &&
                activeSlotIndices.TryGetValue(skewer, out int slotIndex) &&
                slotIndex >= 0 &&
                slotIndex < activeSlots.Length &&
                activeSlots[slotIndex] != null)
            {
                slotPosition = activeSlots[slotIndex].position;
                return true;
            }

            slotPosition = skewer != null
                ? skewer.transform.position
                : transform.position;
            return false;
        }

        public void Push(
            SkewerVisual skewer,
            float duration = 0.25f,
            Vector3? preferredWorldPosition = null)
        {
            if (isLocked) return;
            int slotIndex = FindNearestEmptySlot(
                preferredWorldPosition ?? transform.position);
            if (slotIndex < 0) return;

            skewer.transform.SetParent(transform);
            activeSkewers.Add(skewer);
            activeSlotIndices[skewer] = slotIndex;

            Vector3 targetPosition = activeSlots[slotIndex].position;
            skewer.MoveTo(targetPosition, duration);
        }

        private int FindNearestEmptySlot(Vector3 worldPosition)
        {
            int nearestSlot = -1;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < activeSlots.Length; i++)
            {
                if (activeSlots[i] == null || activeSlotIndices.ContainsValue(i))
                    continue;

                float distance =
                    Vector2.SqrMagnitude(activeSlots[i].position - worldPosition);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestSlot = i;
                }
            }

            return nearestSlot;
        }

        private int FindNearestSlot(Vector3 worldPosition)
        {
            int nearestSlot = -1;
            float nearestDistance = float.MaxValue;
            float horizontalRange = GetHorizontalDropRange();
            float verticalRange = GetVerticalDropRange();

            for (int i = 0; i < activeSlots.Length; i++)
            {
                if (activeSlots[i] == null) continue;

                Vector3 slotPosition = activeSlots[i].position;
                float horizontalDistance =
                    Mathf.Abs(worldPosition.x - slotPosition.x);
                float verticalDistance =
                    Mathf.Abs(worldPosition.y - slotPosition.y);

                if (horizontalDistance > horizontalRange ||
                    verticalDistance > verticalRange)
                {
                    continue;
                }

                float distance =
                    Vector2.SqrMagnitude(slotPosition - worldPosition);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestSlot = i;
                }
            }

            return nearestSlot;
        }

        private float GetHorizontalDropRange()
        {
            float minimumSlotSpacing = float.MaxValue;

            for (int i = 0; i < activeSlots.Length; i++)
            {
                if (activeSlots[i] == null) continue;

                for (int j = i + 1; j < activeSlots.Length; j++)
                {
                    if (activeSlots[j] == null) continue;

                    float spacing = Mathf.Abs(
                        activeSlots[i].position.x -
                        activeSlots[j].position.x);
                    if (spacing > 0.001f)
                    {
                        minimumSlotSpacing =
                            Mathf.Min(minimumSlotSpacing, spacing);
                    }
                }
            }

            if (minimumSlotSpacing < float.MaxValue)
            {
                return minimumSlotSpacing * horizontalDropRangeRatio;
            }

            BoxCollider2D grillCollider = GetComponent<BoxCollider2D>();
            return grillCollider != null
                ? grillCollider.bounds.extents.x * 0.5f
                : 0.25f;
        }

        private float GetVerticalDropRange()
        {
            BoxCollider2D grillCollider = GetComponent<BoxCollider2D>();
            if (grillCollider != null)
            {
                return grillCollider.bounds.extents.y *
                    verticalDropRangeRatio;
            }

            return GetHorizontalDropRange();
        }

        private bool IsSlotOccupied(int slotIndex, SkewerVisual ignoredSkewer = null)
        {
            foreach (KeyValuePair<SkewerVisual, int> entry in activeSlotIndices)
            {
                if (entry.Key != ignoredSkewer && entry.Value == slotIndex)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Điểm thả chỉ hợp lệ nếu ô gần con trỏ thực sự đang trống.
        /// Không tự động chuyển xiên sang một ô trống khác.
        /// </summary>
        public bool CanDropAtPosition(
            Vector3 worldPosition, SkewerVisual ignoredSkewer = null)
        {
            if (isAnimating || isLocked) return false;

            int intendedSlot = FindNearestSlot(worldPosition);
            return intendedSlot >= 0 &&
                !IsSlotOccupied(intendedSlot, ignoredSkewer);
        }

        /// <summary>
        /// Đổi vị trí một xiên giữa các ô trống ngay trên cùng một bếp.
        /// </summary>
        public bool MoveSkewerWithinGrill(
            SkewerVisual skewer, Vector3 dropWorldPosition, float duration = 0.15f)
        {
            if (isAnimating || isLocked || skewer == null ||
                !activeSlotIndices.ContainsKey(skewer))
                return false;

            int newSlot = FindNearestSlot(dropWorldPosition);
            if (newSlot < 0 || IsSlotOccupied(newSlot, skewer))
            {
                return false;
            }

            activeSlotIndices[skewer] = newSlot;
            skewer.MoveTo(activeSlots[newSlot].position, duration);
            return true;
        }

        public SkewerVisual Pop()
        {
            if (activeSkewers.Count == 0 || isAnimating || isLocked) return null;

            return Pop(activeSkewers[activeSkewers.Count - 1]);
        }

        public SkewerVisual Pop(SkewerVisual skewer)
        {
            if (isAnimating || isLocked || skewer == null ||
                !activeSkewers.Contains(skewer))
                return null;

            SkewerVisual popped = skewer;
            activeSkewers.Remove(popped);
            activeSlotIndices.Remove(popped);
            return popped;
        }

        public void LiftTopSkewer(bool lift, float offset = 0.3f)
        {
            if (activeSkewers.Count == 0) return;

            SkewerVisual topSkewer = activeSkewers[activeSkewers.Count - 1];
            int slotIndex = activeSlotIndices.TryGetValue(topSkewer, out int savedSlot)
                ? savedSlot
                : Mathf.Clamp(activeSkewers.Count - 1, 0, activeSlots.Length - 1);
            Vector3 targetPos = activeSlots[slotIndex].position;
            if (lift)
            {
                targetPos.y += offset;
            }
            topSkewer.MoveTo(targetPos, 0.1f);
        }

        public void CheckAndClear()
        {
            if (isLocked) return;
            if (activeSkewers.Count == 0 && waitingSkewers.Count > 0 && !isAnimating)
            {
                StartCoroutine(RevealWaitingLayerCoroutine());
                return;
            }

            if (HasCompletedActiveMatch())
            {
                StartCoroutine(ClearGrillCoroutine());
            }
        }

        /// <summary>
        /// Dùng sau khi Refresh thay FoodItemData trực tiếp. Chỉ kiểm tra
        /// bộ ba trên bếp, không tự đẩy lớp chờ lên.
        /// </summary>
        public bool HasCompletedActiveMatch()
        {
            if (isLocked || isAnimating || activeSkewers.Count != 3)
                return false;

            FoodItemData firstItem = activeSkewers[0] != null
                ? activeSkewers[0].GetData()
                : null;
            if (firstItem == null) return false;

            for (int index = 1; index < activeSkewers.Count; index++)
            {
                FoodItemData item = activeSkewers[index] != null
                    ? activeSkewers[index].GetData()
                    : null;
                if (!FoodItemData.AreMatching(firstItem, item))
                {
                    return false;
                }
            }

            return true;
        }

        private IEnumerator ClearGrillCoroutine()
        {
            isAnimating = true;

            // Chỉ khóa bếp này. Các bếp khác vẫn nhận thao tác để nhịp chơi
            // không bị ngắt khi nhiều bộ ba được hoàn thành liên tiếp.
            yield return new WaitForSeconds(clearArrivalDelay);

            List<SkewerVisual> toClear = new List<SkewerVisual>(activeSkewers);
            activeSkewers.Clear();
            activeSlotIndices.Clear();

            foreach (SkewerVisual skewer in toClear)
            {
                skewer?.ClearSelectionEffectImmediately();
            }

            // Thu nhỏ dần về 0 (dùng CalculatedScale thay vì Vector3.one)
            float elapsed = 0f;
            float duration = clearScaleDuration;
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                foreach (var skewer in toClear)
                {
                    if (skewer != null)
                        skewer.transform.localScale = Vector3.Lerp(skewer.CalculatedScale, Vector3.zero, t);
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            foreach (var skewer in toClear)
            {
                if (skewer != null)
                {
                    gameplayManager.OnSkewerCleared(skewer.GetData());
                    Destroy(skewer.gameObject);
                }
            }

            if (toClear.Count > 0 && toClear[0] != null)
            {
                gameplayManager.OnMatchingSetCleared(toClear[0].GetData());
            }

            yield return new WaitForSeconds(0.03f);

            if (waitingSkewers.Count > 0)
            {
                yield return StartCoroutine(SlideUpWaitingSkewersCoroutine());
            }

            isAnimating = false;

            gameplayManager.CheckGameStatus();
        }

        private IEnumerator SlideUpWaitingSkewersCoroutine()
        {
            activeSkewers = new List<SkewerVisual>(waitingSkewers);
            activeSlotIndices.Clear();
            waitingSkewers.Clear();

            float duration = waitingSlideDuration;
            for (int i = 0; i < activeSkewers.Count; i++)
            {
                Vector3 targetPos = activeSlots[i].position;
                activeSlotIndices[activeSkewers[i]] = i;
                activeSkewers[i].SetDisplayScale(1f);
                activeSkewers[i].transform.localRotation = Quaternion.identity;
                activeSkewers[i].MoveTo(targetPos, duration);
            }

            yield return new WaitForSeconds(duration);

            ConsumeTopWaitingPlate();

            List<FoodItemData> newWaitingData = waitingLayerQueue.Count > 0
                ? waitingLayerQueue.Dequeue()
                : new List<FoodItemData>();
            for (int i = 0; i < newWaitingData.Count && i < 3; i++)
            {
                if (newWaitingData[i] == null) continue;
                SkewerVisual skewer = SpawnSkewer(newWaitingData[i]);
                skewer.transform.position = waitingSlots[i].position;
                ConfigureWaitingSkewer(skewer, i);
                waitingSkewers.Add(skewer);

                // Hiệu ứng phình to: từ 0 lên đúng CalculatedScale (không phải Vector3.one)
                skewer.transform.localScale = Vector3.zero;
                StartCoroutine(ScaleUpSkewerCoroutine(
                    skewer,
                    waitingSpawnDuration));
            }

            if (newWaitingData.Count > 0)
            {
                yield return new WaitForSeconds(waitingSpawnDuration);
            }
        }

        private IEnumerator ScaleUpSkewerCoroutine(SkewerVisual skewer, float duration)
        {
            float elapsed = 0f;
            // Phình to lên đúng CalculatedScale (tỉ lệ đã tính toán) thay vì Vector3.one
            Vector3 targetScale = skewer.CalculatedScale * 0.5f;
            while (elapsed < duration)
            {
                if (skewer == null) yield break;
                skewer.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (skewer != null) skewer.transform.localScale = targetScale;
        }

        /// <summary>
        /// Xiên trên đĩa chờ nhỏ hơn và hơi nghiêng để nhìn tự nhiên hơn.
        /// Ba vị trí lần lượt nghiêng trái, đứng gần thẳng và nghiêng phải.
        /// </summary>
        private static void ConfigureWaitingSkewer(SkewerVisual skewer, int slotIndex)
        {
            skewer.SetDisplayScale(0.5f);
            float tiltAngle = -(32f + slotIndex * 3f);
            skewer.transform.localRotation = Quaternion.Euler(0f, 0f, tiltAngle);
        }

        private void FindWaitingPlate()
        {
            if (waitingPlate == null)
            {
                waitingPlate = transform.Find("WaitingPlate");
            }

            if (waitingPlate != null && !hasWaitingPlateStartPosition)
            {
                waitingPlateStartPosition = waitingPlate.localPosition;
                hasWaitingPlateStartPosition = true;
            }
        }

        private IEnumerator RevealWaitingLayerCoroutine()
        {
            isAnimating = true;

            yield return StartCoroutine(SlideUpWaitingSkewersCoroutine());

            isAnimating = false;
            gameplayManager.CheckGameStatus();
        }

        /// <summary>
        /// Đưa chồng đĩa về trạng thái ban đầu khi bắt đầu hoặc chơi lại màn.
        /// </summary>
        private void ResetWaitingPlateStack()
        {
            if (waitingPlate == null || !hasWaitingPlateStartPosition) return;

            remainingPlateCount = Mathf.Max(0, initialPlateCount);
            waitingPlate.localPosition = waitingPlateStartPosition;
            RefreshDynamicPlateStack();
            SyncDynamicWaitingArea();
        }

        /// <summary>
        /// Bỏ đĩa trên cùng sau khi xiên chờ được đẩy lên bếp.
        /// Đĩa, slot và vị trí sinh xiên tiếp theo cùng hạ xuống một lớp.
        /// </summary>
        private void ConsumeTopWaitingPlate()
        {
            if (waitingPlate == null || remainingPlateCount <= 0) return;

            remainingPlateCount--;
            int consumedCount = Mathf.Max(0, initialPlateCount - remainingPlateCount);
            waitingPlate.localPosition =
                waitingPlateStartPosition + Vector3.down * plateLayerSpacing * consumedCount;

            RefreshDynamicPlateStack();
            SyncDynamicWaitingArea();
        }

        private void RefreshDynamicPlateStack()
        {
            if (waitingPlate == null) return;

            SpriteRenderer mainRenderer = waitingPlate.GetComponent<SpriteRenderer>();
            if (mainRenderer == null || mainRenderer.sprite == null) return;

            mainRenderer.enabled = remainingPlateCount > 0;
            int requiredBackLayers = Mathf.Max(0, remainingPlateCount - 1);

            while (waitingPlateLayers.Count < requiredBackLayers)
            {
                int layerIndex = waitingPlateLayers.Count;
                GameObject layerObject = new GameObject($"WaitingPlateLayer_{layerIndex + 1}");
                layerObject.transform.SetParent(transform);
                layerObject.AddComponent<SpriteRenderer>();
                waitingPlateLayers.Add(layerObject.transform);
            }

            for (int i = 0; i < waitingPlateLayers.Count; i++)
            {
                Transform plateLayer = waitingPlateLayers[i];
                bool isVisible = i < requiredBackLayers;
                plateLayer.gameObject.SetActive(isVisible);
                if (!isVisible) continue;

                SpriteRenderer layerRenderer = plateLayer.GetComponent<SpriteRenderer>();
                layerRenderer.sprite = mainRenderer.sprite;
                layerRenderer.sortingLayerID = mainRenderer.sortingLayerID;
                layerRenderer.sortingOrder = mainRenderer.sortingOrder - (i + 1);
                layerRenderer.color = new Color(0.78f, 0.76f, 0.84f, 1f);

                plateLayer.localPosition = waitingPlate.localPosition +
                    Vector3.down * plateLayerSpacing * (i + 1);
                plateLayer.localRotation = waitingPlate.localRotation;
                plateLayer.localScale = waitingPlate.localScale;
            }
        }

        /// <summary>
        /// Căn slot và xiên chờ theo tâm đĩa trên cùng.
        /// </summary>
        private void SyncDynamicWaitingArea()
        {
            if (waitingPlate == null || waitingSlots == null) return;

            lastWaitingPlatePosition = waitingPlate.localPosition;

            for (int i = 0; i < waitingSlots.Length; i++)
            {
                if (waitingSlots[i] == null) continue;

                float x = (i - 1) * 0.25f;
                waitingSlots[i].localPosition =
                    waitingPlate.localPosition + new Vector3(x, 0.02f, 0f);

                if (i < waitingSkewers.Count && waitingSkewers[i] != null)
                {
                    waitingSkewers[i].transform.position = waitingSlots[i].position;
                }
            }

            RefreshDynamicPlateStack();
        }

        /// <summary>
        /// Tạo thêm hai lớp đĩa phía sau để nhìn giống một chồng nhiều đĩa.
        /// Các lớp này dùng chung sprite với đĩa chính nên vẫn theo được khi đổi hình đĩa.
        /// </summary>
        private void EnsureWaitingPlateStack()
        {
            if (waitingPlate == null) return;

            SpriteRenderer mainRenderer = waitingPlate.GetComponent<SpriteRenderer>();
            if (mainRenderer == null || mainRenderer.sprite == null) return;

            waitingPlateBack1 = CreatePlateLayer(
                "WaitingPlateBack1", mainRenderer, waitingPlateBack1, 0.05f, 1);
            waitingPlateBack2 = CreatePlateLayer(
                "WaitingPlateBack2", mainRenderer, waitingPlateBack2, 0.10f, 2);
        }

        private Transform CreatePlateLayer(
            string objectName,
            SpriteRenderer mainRenderer,
            Transform currentLayer,
            float verticalOffset,
            int orderOffset)
        {
            if (currentLayer == null)
            {
                currentLayer = transform.Find(objectName);
            }

            if (currentLayer == null)
            {
                GameObject layerObject = new GameObject(objectName);
                currentLayer = layerObject.transform;
                currentLayer.SetParent(transform);
                layerObject.AddComponent<SpriteRenderer>();
            }

            SpriteRenderer layerRenderer = currentLayer.GetComponent<SpriteRenderer>();
            layerRenderer.sprite = mainRenderer.sprite;
            layerRenderer.sortingLayerID = mainRenderer.sortingLayerID;
            layerRenderer.sortingOrder = mainRenderer.sortingOrder - orderOffset;
            layerRenderer.color = new Color(0.78f, 0.76f, 0.84f, 1f);

            currentLayer.localRotation = waitingPlate.localRotation;
            currentLayer.localScale = waitingPlate.localScale;
            currentLayer.localPosition =
                waitingPlate.localPosition + Vector3.down * verticalOffset;

            return currentLayer;
        }

        /// <summary>
        /// Đặt slot và xiên chờ theo vị trí hiện tại của đĩa thay vì tọa độ cố định.
        /// </summary>
        private void SyncWaitingArea()
        {
            if (waitingPlate == null || waitingSlots == null) return;

            lastWaitingPlatePosition = waitingPlate.localPosition;

            for (int i = 0; i < waitingSlots.Length; i++)
            {
                if (waitingSlots[i] == null) continue;

                float x = (i - 1) * 0.25f;
                waitingSlots[i].localPosition =
                    waitingPlate.localPosition + new Vector3(x, 0.08f, 0f);

                if (i < waitingSkewers.Count && waitingSkewers[i] != null)
                {
                    waitingSkewers[i].transform.position = waitingSlots[i].position;
                }
            }

            UpdatePlateLayerTransform(waitingPlateBack1, 0.05f);
            UpdatePlateLayerTransform(waitingPlateBack2, 0.10f);
        }

        private void UpdatePlateLayerTransform(Transform plateLayer, float verticalOffset)
        {
            if (plateLayer == null || waitingPlate == null) return;

            plateLayer.localPosition =
                waitingPlate.localPosition + Vector3.down * verticalOffset;
            plateLayer.localRotation = waitingPlate.localRotation;
            plateLayer.localScale = waitingPlate.localScale;
        }
    }
}
