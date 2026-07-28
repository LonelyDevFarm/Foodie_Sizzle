using System.Collections.Generic;
using UnityEngine;

namespace FoodieSizzle
{
    [System.Serializable]
    public class FoodLayerData
    {
        [Tooltip("Danh sách itemId trên một lớp, theo thứ tự trái sang phải.")]
        public List<string> itemIds = new List<string>();
    }

    [System.Serializable]
    public class GrillLevelData
    {
        [Tooltip("Lớp 0 nằm trên bếp; các lớp sau lần lượt nằm trên chồng đĩa.")]
        public List<FoodLayerData> layers = new List<FoodLayerData>();
    }

    [CreateAssetMenu(
        fileName = "Level_001",
        menuName = "Foodie Sizzle/Level Data",
        order = 2)]
    public class LevelData : ScriptableObject
    {
        public int schemaVersion = 1;
        public int levelNumber = 1;
        public float timeLimitSeconds = 300f;
        public List<GrillLevelData> grills = new List<GrillLevelData>();
    }
}
