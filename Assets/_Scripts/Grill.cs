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
        [Min(0f)] public float clearArrivalDelay = 0.28f;

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
            if (isAnimating) return false;
            return activeSkewers.Count < activeSlots.Length;
        }

        /// <summary>
        /// Tìm đúng xiên mà người chơi đang chỉ vào, thay vì luôn lấy xiên được thêm sau cùng.
        /// </summary>
        public SkewerVisual GetSkewerAtWorldPosition(Vector3 worldPosition)
        {
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
            if (isAnimating) return false;

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
            if (isAnimating || skewer == null ||
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
            if (activeSkewers.Count == 0 || isAnimating) return null;

            return Pop(activeSkewers[activeSkewers.Count - 1]);
        }

        public SkewerVisual Pop(SkewerVisual skewer)
        {
            if (isAnimating || skewer == null || !activeSkewers.Contains(skewer))
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
            if (activeSkewers.Count == 0 && waitingSkewers.Count > 0 && !isAnimating)
            {
                StartCoroutine(RevealWaitingLayerCoroutine());
                return;
            }

            if (activeSkewers.Count == 3)
            {
                string firstId = activeSkewers[0].GetData().itemId;
                bool isMatch = true;

                for (int i = 1; i < 3; i++)
                {
                    if (activeSkewers[i].GetData().itemId != firstId)
                    {
                        isMatch = false;
                        break;
                    }
                }

                if (isMatch)
                {
                    StartCoroutine(ClearGrillCoroutine());
                }
            }
        }

        private IEnumerator ClearGrillCoroutine()
        {
            isAnimating = true;
            gameplayManager.SetBoardLocked(true);

            // Chờ xiên vừa thả bay vào đúng vị trí rồi mới bắt đầu hiệu ứng xóa.
            yield return new WaitForSeconds(clearArrivalDelay);

            List<SkewerVisual> toClear = new List<SkewerVisual>(activeSkewers);
            activeSkewers.Clear();
            activeSlotIndices.Clear();

            // Thu nhỏ dần về 0 (dùng CalculatedScale thay vì Vector3.one)
            float elapsed = 0f;
            float duration = 0.3f;
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

            yield return new WaitForSeconds(0.1f);

            if (waitingSkewers.Count > 0)
            {
                yield return StartCoroutine(SlideUpWaitingSkewersCoroutine());
            }

            isAnimating = false;
            gameplayManager.SetBoardLocked(false);

            gameplayManager.CheckGameStatus();
        }

        private IEnumerator SlideUpWaitingSkewersCoroutine()
        {
            activeSkewers = new List<SkewerVisual>(waitingSkewers);
            activeSlotIndices.Clear();
            waitingSkewers.Clear();

            float duration = 0.3f;
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
                StartCoroutine(ScaleUpSkewerCoroutine(skewer, 0.2f));
            }

            if (newWaitingData.Count > 0)
            {
                yield return new WaitForSeconds(0.2f);
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
            gameplayManager.SetBoardLocked(true);

            yield return StartCoroutine(SlideUpWaitingSkewersCoroutine());

            isAnimating = false;
            gameplayManager.SetBoardLocked(false);
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
