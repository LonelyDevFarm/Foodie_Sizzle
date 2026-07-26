using System.Collections;
using UnityEngine;

namespace FoodieSizzle
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class SkewerVisual : MonoBehaviour
    {
        // ===== CHỈ CẦN CHỈNH DUY NHẤT CON SỐ NÀY =====
        // Tỉ lệ thu nhỏ chung cho TẤT CẢ xiên nướng (giữ nguyên tỉ lệ PPU gốc)
        // Bạn muốn to hơn thì tăng (0.7, 0.8), nhỏ hơn thì giảm (0.5, 0.4)
        private const float GLOBAL_SCALE = 0.4f;

        private FoodItemData itemData;
        private SpriteRenderer mainRenderer;

        // Lưu lại tỉ lệ đã tính toán để Grill.cs dùng cho hiệu ứng
        public Vector3 CalculatedScale { get; private set; } = Vector3.one;

        private void Awake()
        {
            mainRenderer = GetComponent<SpriteRenderer>();
        }

        public void SetData(FoodItemData data)
        {
            itemData = data;

            // Dọn dẹp các nguyên liệu con cũ
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            if (itemData == null || itemData.itemSprite == null) return;

            // Áp dụng tỉ lệ thu nhỏ chung — giữ nguyên tỉ lệ PPU gốc giữa các món
            CalculatedScale = new Vector3(GLOBAL_SCALE, GLOBAL_SCALE, 1f);
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

        public FoodItemData GetData()
        {
            return itemData;
        }

        public void MoveTo(Vector3 targetPosition, float duration, bool isLocal = false)
        {
            StartCoroutine(MoveCoroutine(targetPosition, duration, isLocal));
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
        }
    }
}