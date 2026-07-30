using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FoodieSizzle
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class SkewerVisual : MonoBehaviour
    {
        // ===== CHỈ CẦN CHỈNH DUY NHẤT CON SỐ NÀY =====
        // Tỉ lệ thu nhỏ chung cho TẤT CẢ xiên nướng (giữ nguyên tỉ lệ PPU gốc)
        // Bạn muốn to hơn thì tăng (0.7, 0.8), nhỏ hơn thì giảm (0.5, 0.4)
        // Kích thước tối đa của một món sau khi tự động căn vào vỉ.
        private const float TARGET_HEIGHT = 0.72f;
        private const float TARGET_WIDTH = 0.3f;
        private const float STACK_HEIGHT_MULTIPLIER = 2.1f;

        [Header("Hiệu ứng khi chọn")]
        [SerializeField] [Range(1f, 1.25f)]
        private float selectedScaleMultiplier = 1.1f;
        [SerializeField] [Min(0.01f)]
        private float selectionScaleDuration = 0.08f;

        private FoodItemData itemData;
        private SpriteRenderer mainRenderer;
        private Coroutine moveRoutine;
        private Coroutine selectionScaleRoutine;
        private bool isDragging;
        private bool isSelected;
        private Vector3 scaleBeforeSelection;
        private readonly List<GameObject> selectionOutlines =
            new List<GameObject>();
        private static Material selectionOutlineMaterial;

        // Lưu lại tỉ lệ đã tính toán để Grill.cs dùng cho hiệu ứng
        public Vector3 CalculatedScale { get; private set; } = Vector3.one;

        private void Awake()
        {
            mainRenderer = GetComponent<SpriteRenderer>();
        }

        public void SetData(FoodItemData data)
        {
            if (selectionScaleRoutine != null)
            {
                StopCoroutine(selectionScaleRoutine);
                selectionScaleRoutine = null;
            }
            isSelected = false;
            itemData = data;

            // Dọn dẹp các nguyên liệu con cũ
            foreach (Transform child in transform)
            {
                SpriteRenderer oldRenderer =
                    child.GetComponent<SpriteRenderer>();
                if (oldRenderer != null)
                {
                    oldRenderer.enabled = false;
                }
                Destroy(child.gameObject);
            }

            if (itemData == null || itemData.itemSprite == null) return;

            // Áp dụng tỉ lệ thu nhỏ chung — giữ nguyên tỉ lệ PPU gốc giữa các món
            CalculatedScale = CalculateScale(itemData);
            transform.localScale = CalculatedScale;

            if (itemData.needsStacking)
            {
                // HƯỚNG A: Món đơn lẻ - nhân bản 3 cục xếp chồng
                mainRenderer.enabled = false;

                Sprite sp = itemData.itemSprite;
                float spriteHeight = sp.rect.height / sp.pixelsPerUnit;

                // Khoảng cách chồng = 55% chiều cao cục
                float stackOffset = spriteHeight * 0.55f;

                float[] yOffsets = new float[] { -stackOffset, 0f, stackOffset };

                for (int i = 0; i < 3; i++)
                {
                    GameObject itemGo = new GameObject($"Ingredient_{i}");
                    itemGo.transform.SetParent(transform);
                    itemGo.transform.localPosition = new Vector3(0, yOffsets[i], 0);
                    itemGo.transform.localScale = Vector3.one;

                    SpriteRenderer sr = itemGo.AddComponent<SpriteRenderer>();
                    sr.sprite = itemData.itemSprite;
                    sr.sortingOrder = 5 + i;
                }
            }
            else
            {
                // HƯỚNG B: Xiên nướng có sẵn que - vẽ 1 ảnh duy nhất
                mainRenderer.enabled = true;
                mainRenderer.sprite = itemData.itemSprite;
                mainRenderer.sortingOrder = 5;
            }
        }

        private static Vector3 CalculateScale(FoodItemData data)
        {
            Vector2 spriteSize = data.itemSprite.bounds.size;
            float visualScale = data.visualScale > 0f ? data.visualScale : 1f;
            float contentHeight = data.needsStacking
                ? spriteSize.y * STACK_HEIGHT_MULTIPLIER
                : spriteSize.y;

            // Trục Y luôn được căn theo chiều cao chuẩn để mọi xiên dài bằng nhau.
            float heightScale = TARGET_HEIGHT / Mathf.Max(contentHeight, 0.001f);

            // Trục X được giới hạn riêng. Nhờ vậy món rộng không làm cả xiên bị
            // thu ngắn, nhưng vẫn không tràn sang slot bên cạnh.
            float widthScale = TARGET_WIDTH / Mathf.Max(spriteSize.x, 0.001f);
            float finalWidthScale = Mathf.Min(heightScale, widthScale);

            return new Vector3(
                finalWidthScale * visualScale,
                heightScale * visualScale,
                1f);
        }

        public FoodItemData GetData()
        {
            return itemData;
        }

        /// <summary>
        /// Thu nhỏ hoặc phóng to phần hiển thị mà không làm mất kích thước chuẩn đã tính.
        /// Dùng để món trên đĩa chờ nhỏ hơn món đang nằm trên bếp.
        /// </summary>
        public void SetDisplayScale(float multiplier)
        {
            transform.localScale = CalculatedScale * Mathf.Max(0.01f, multiplier);
        }

        public void MoveTo(Vector3 targetPosition, float duration, bool isLocal = false)
        {
            if (moveRoutine != null)
            {
                StopCoroutine(moveRoutine);
            }

            moveRoutine = StartCoroutine(MoveCoroutine(targetPosition, duration, isLocal));
        }

        public void BeginDrag()
        {
            if (moveRoutine != null)
            {
                StopCoroutine(moveRoutine);
                moveRoutine = null;
            }

            SetDraggingSorting(true);
        }

        public void SetDragPosition(Vector3 worldPosition)
        {
            transform.position = worldPosition;
        }

        public void EndDrag()
        {
            SetDraggingSorting(false);
        }

        /// <summary>
        /// Hiển thị trạng thái được chọn mà không nâng hoặc đổi vị trí xiên.
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (isSelected == selected) return;
            isSelected = selected;

            if (selected)
            {
                scaleBeforeSelection = transform.localScale;
                OffsetSortingOrder(10);

                foreach (SpriteRenderer renderer in
                         GetComponentsInChildren<SpriteRenderer>())
                {
                    renderer.color = Color.white;
                }

                SetSelectionOutlineVisible(true);
                AnimateSelectionScale(
                    scaleBeforeSelection * selectedScaleMultiplier);
            }
            else
            {
                SetSelectionOutlineVisible(false);
                OffsetSortingOrder(-10);

                foreach (SpriteRenderer renderer in
                         GetComponentsInChildren<SpriteRenderer>())
                {
                    renderer.color = Color.white;
                }

                AnimateSelectionScale(scaleBeforeSelection);
            }
        }

        private void AnimateSelectionScale(Vector3 targetScale)
        {
            if (selectionScaleRoutine != null)
            {
                StopCoroutine(selectionScaleRoutine);
            }

            selectionScaleRoutine = StartCoroutine(
                AnimateSelectionScaleCoroutine(targetScale));
        }

        private IEnumerator AnimateSelectionScaleCoroutine(
            Vector3 targetScale)
        {
            Vector3 startScale = transform.localScale;
            float elapsed = 0f;

            while (elapsed < selectionScaleDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(
                    elapsed / selectionScaleDuration);
                // Ease-out nhẹ để phản hồi nhanh nhưng không bị giật.
                float easedT = 1f - (1f - t) * (1f - t);
                transform.localScale = Vector3.Lerp(
                    startScale,
                    targetScale,
                    easedT);
                yield return null;
            }

            transform.localScale = targetScale;
            selectionScaleRoutine = null;
        }

        /// <summary>
        /// Bỏ hiệu ứng chọn ngay trước khi xiên bị xóa hoặc được dựng lại.
        /// Nhờ vậy coroutine trả scale không đè lên animation biến mất.
        /// </summary>
        public void ClearSelectionEffectImmediately()
        {
            if (selectionScaleRoutine != null)
            {
                StopCoroutine(selectionScaleRoutine);
                selectionScaleRoutine = null;
            }

            if (!isSelected) return;

            isSelected = false;
            SetSelectionOutlineVisible(false);
            OffsetSortingOrder(-10);
            transform.localScale = scaleBeforeSelection;
        }

        private void SetSelectionOutlineVisible(bool visible)
        {
            if (visible && selectionOutlines.Count == 0)
            {
                SpriteRenderer[] sourceRenderers =
                    GetComponentsInChildren<SpriteRenderer>();
                int outlineSortingOrder = int.MaxValue;

                foreach (SpriteRenderer source in sourceRenderers)
                {
                    outlineSortingOrder = Mathf.Min(
                        outlineSortingOrder,
                        source.sortingOrder);
                }
                outlineSortingOrder =
                    outlineSortingOrder == int.MaxValue
                        ? 0
                        : outlineSortingOrder - 1;

                foreach (SpriteRenderer source in sourceRenderers)
                {
                    if (source.sprite == null) continue;

                    GameObject outlineObject =
                        new GameObject($"SelectionOutline_{source.name}");
                    outlineObject.transform.SetParent(source.transform, false);
                    outlineObject.transform.localPosition = Vector3.zero;
                    outlineObject.transform.localRotation = Quaternion.identity;
                    outlineObject.transform.localScale =
                        new Vector3(1.08f, 1.08f, 1f);

                    SpriteRenderer outline =
                        outlineObject.AddComponent<SpriteRenderer>();
                    outline.sprite = source.sprite;
                    outline.color = Color.white;
                    outline.sortingLayerID = source.sortingLayerID;
                    // Mọi lớp viền phải nằm sau toàn bộ sprite thật.
                    // Nếu dùng source.sortingOrder - 1, viền của miếng trước
                    // có thể đè lên ảnh thật của miếng phía sau.
                    outline.sortingOrder = outlineSortingOrder;
                    outline.maskInteraction = source.maskInteraction;
                    outline.sharedMaterial = GetSelectionOutlineMaterial();

                    selectionOutlines.Add(outlineObject);
                }
            }

            if (visible)
            {
                // Sorting của sprite thật có thể đổi khi kéo. Mỗi lần hiện
                // viền phải tính lại để viền luôn nằm sau toàn bộ sprite thật.
                int minimumSourceOrder = int.MaxValue;
                foreach (SpriteRenderer renderer in
                         GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (renderer.gameObject.name.StartsWith(
                            "SelectionOutline_"))
                    {
                        continue;
                    }

                    minimumSourceOrder = Mathf.Min(
                        minimumSourceOrder,
                        renderer.sortingOrder);
                }

                int correctedOutlineOrder =
                    minimumSourceOrder == int.MaxValue
                        ? 0
                        : minimumSourceOrder - 1;

                foreach (GameObject outlineObject in selectionOutlines)
                {
                    if (outlineObject == null) continue;

                    outlineObject.transform.localPosition = Vector3.zero;
                    outlineObject.transform.localRotation = Quaternion.identity;
                    outlineObject.transform.localScale =
                        new Vector3(1.08f, 1.08f, 1f);

                    SpriteRenderer outline =
                        outlineObject.GetComponent<SpriteRenderer>();
                    if (outline != null)
                    {
                        outline.sortingOrder = correctedOutlineOrder;
                    }
                }
            }

            foreach (GameObject outline in selectionOutlines)
            {
                if (outline != null)
                {
                    outline.SetActive(visible);
                }
            }
        }

        private static Material GetSelectionOutlineMaterial()
        {
            if (selectionOutlineMaterial != null)
            {
                return selectionOutlineMaterial;
            }

            Shader shader =
                Shader.Find("FoodieSizzle/WhiteSpriteSilhouette");
            if (shader == null)
            {
                Debug.LogError(
                    "Không tìm thấy shader viền trắng của xiên.");
                return null;
            }

            selectionOutlineMaterial = new Material(shader)
            {
                name = "Viền trắng xiên (Runtime)",
                hideFlags = HideFlags.HideAndDontSave
            };
            return selectionOutlineMaterial;
        }

        private void SetDraggingSorting(bool dragging)
        {
            if (isDragging == dragging) return;
            isDragging = dragging;

            int orderOffset = dragging ? 20 : -20;
            OffsetSortingOrder(orderOffset);
        }

        private void OffsetSortingOrder(int orderOffset)
        {
            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
            foreach (SpriteRenderer renderer in renderers)
            {
                renderer.sortingOrder += orderOffset;
            }
        }

        private IEnumerator MoveCoroutine(Vector3 targetPosition, float duration, bool isLocal)
        {
            Vector3 startPosition = isLocal ? transform.localPosition : transform.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (isLocal)
                {
                    transform.localPosition = Vector3.Lerp(startPosition, targetPosition, elapsed / duration);
                }
                else
                {
                    transform.position = Vector3.Lerp(startPosition, targetPosition, elapsed / duration);
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (isLocal)
            {
                transform.localPosition = targetPosition;
            }
            else
            {
                transform.position = targetPosition;
            }

            moveRoutine = null;
        }
    }
}
