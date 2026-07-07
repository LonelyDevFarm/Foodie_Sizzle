using UnityEngine;

namespace FoodieSizzle
{
    [CreateAssetMenu(fileName = "NewFoodItem", menuName = "Foodie Sizzle/Food Item Data")]
    public class FoodItemData : ScriptableObject
    {
        [Header("General Info")]
        public string itemId;
        public Sprite itemSprite;
        
        [Header("Gameplay Info")]
        public int scoreValue = 10;
    }
}
