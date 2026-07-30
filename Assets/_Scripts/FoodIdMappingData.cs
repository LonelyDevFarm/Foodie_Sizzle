using System;
using System.Collections.Generic;
using UnityEngine;

namespace FoodieSizzle
{
    [Serializable]
    public class FoodIdMappingEntry
    {
        public int sourceId;
        public FoodItemData foodItem;
    }

    /// <summary>
    /// Bảng tách ID món của dữ liệu nguồn khỏi itemId nội bộ.
    /// Nhờ vậy có thể đổi sprite hoặc nhập dữ liệu từ game khác mà không phải sửa LevelData.
    /// </summary>
    [CreateAssetMenu(
        fileName = "FoodIdMapping",
        menuName = "Foodie Sizzle/Food ID Mapping",
        order = 3)]
    public class FoodIdMappingData : ScriptableObject
    {
        public string sourceName;
        public List<FoodIdMappingEntry> entries = new List<FoodIdMappingEntry>();

        public bool TryGetFoodItem(int sourceId, out FoodItemData foodItem)
        {
            foreach (FoodIdMappingEntry entry in entries)
            {
                if (entry != null && entry.sourceId == sourceId)
                {
                    foodItem = entry.foodItem;
                    return foodItem != null;
                }
            }

            foodItem = null;
            return false;
        }
    }
}
