using UnityEngine;

namespace FoodieSizzle
{
    [CreateAssetMenu(fileName = "NewFoodItem", menuName = "Foodie Sizzle/Food Item Data")]
    public class FoodItemData : ScriptableObject
    {
        [Header("General Info")]
        public string itemId;
        public Sprite itemSprite;

        [Tooltip("Các món nhìn giống nhau nhưng có ID nguồn khác nhau có thể dùng chung khóa này để vẫn ghép được. Để trống sẽ tự nhận theo sprite.")]
        public string matchGroupId;

        [Header("Display settings")]
        public bool needsStacking;

        [Tooltip("Tinh chỉnh sau khi tự động căn kích thước. Hầu hết món nên giữ bằng 1.")]
        [Min(0.1f)]
        public float visualScale = 1f;

        [Tooltip("Độ lệch riêng cho những sprite có hình ảnh không nằm đúng tâm.")]
        public Vector2 visualOffset = Vector2.zero;

        /// <summary>
        /// Khóa dùng cho luật ghép ba. Ưu tiên nhóm được khai báo thủ công,
        /// sau đó đến sprite thật và cuối cùng mới dùng ID của dữ liệu nguồn.
        /// </summary>
        public string GetMatchKey()
        {
            if (!string.IsNullOrWhiteSpace(matchGroupId))
                return $"group:{matchGroupId.Trim()}";

            if (itemSprite != null)
            {
                string textureName =
                    itemSprite.texture != null ? itemSprite.texture.name : "";
                return $"sprite:{textureName}/{itemSprite.name}";
            }

            return $"id:{itemId}";
        }

        public static bool AreMatching(FoodItemData first, FoodItemData second)
        {
            return first != null &&
                second != null &&
                first.GetMatchKey() == second.GetMatchKey();
        }
    }
}
