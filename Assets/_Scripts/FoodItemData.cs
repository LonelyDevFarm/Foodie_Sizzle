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
    }
}
