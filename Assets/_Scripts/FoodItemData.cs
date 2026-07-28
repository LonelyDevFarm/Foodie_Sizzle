using UnityEngine;

namespace FoodieSizzle
{
    [CreateAssetMenu(fileName = "NewFoodItem", menuName = "Foodie Sizzle/Food Item Data")]
    public class FoodItemData : ScriptableObject
    {
        [Header("General Info")]
        public string itemId;
        public Sprite itemSprite;

        [Header("Display settings")]
        public bool needsStacking;

        [Tooltip("Tinh chỉnh sau khi tự động căn kích thước. Hầu hết món nên giữ bằng 1.")]
        [Min(0.1f)]
        public float visualScale = 1f;

        [Tooltip("Độ lệch riêng cho những sprite có hình ảnh không nằm đúng tâm.")]
        public Vector2 visualOffset = Vector2.zero;
    }
}
