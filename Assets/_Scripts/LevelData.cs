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
        [Tooltip("Mã khóa của dữ liệu nguồn. Hiện gameplay chưa sử dụng, nhưng được giữ lại để bổ sung cơ chế khóa sau này.")]
        public int sourceLockId;

        [Tooltip("itemId nội bộ của món cần ghép thành bộ ba để mở bếp.")]
        public string unlockItemId;

        [Tooltip("Lớp 0 nằm trên bếp; các lớp sau lần lượt nằm trên chồng đĩa.")]
        public List<FoodLayerData> layers = new List<FoodLayerData>();
    }

    [System.Serializable]
    public class OrderLevelData
    {
        [Min(1f)] public float timeLimitSeconds = 90f;
        [Range(1, 3)] public int numberOfFood = 1;
        [Tooltip("Lớp món được dữ liệu nguồn ưu tiên khi chọn món cho Order.")]
        public List<int> preferredLayers = new List<int>();
        public List<bool> isSpicy = new List<bool>();
        [Min(0)]
        [Tooltip("Order xuất hiện sau khi người chơi đã hoàn thành từng này bộ ba.")]
        public int matchesToTrigger;
    }

    [CreateAssetMenu(
        fileName = "Level_001",
        menuName = "Foodie Sizzle/Level Data",
        order = 2)]
    public class LevelData : ScriptableObject
    {
        public int schemaVersion = 3;
        public int levelNumber = 1;
        [Tooltip("Tên level trong dữ liệu nguồn, dùng để đối chiếu khi nhập lại.")]
        public string sourceLevelId;
        [Min(1)] public int rows = 4;
        [Min(1)] public int columns = 3;
        public float timeLimitSeconds = 300f;

        [Header("Tùy chọn giữ lại từ dữ liệu nguồn")]
        public bool useDda;
        public bool shuffleIcons;
        public int difficulty;
        [Tooltip("Tốc độ băng chuyền bếp từ dữ liệu nguồn. Gameplay chưa sử dụng.")]
        public float sourceGrillConveyorSpeed = 1f;
        [Tooltip("Mốc bắt đầu băng chuyền bếp từ dữ liệu nguồn. Gameplay chưa sử dụng.")]
        public int sourceGrillConveyorStart = -1;
        [Tooltip("Ngưỡng tương đồng hình ảnh từ dữ liệu nguồn. Gameplay chưa sử dụng.")]
        public float sourceVisualSimilarity = -1f;
        [Tooltip("Cờ chọn icon thông minh từ dữ liệu nguồn. Gameplay chưa sử dụng.")]
        public bool sourceUseSmartIconSelection;
        [Tooltip("Số đơn hàng trong dữ liệu nguồn, giữ lại để tương thích và kiểm tra.")]
        public int sourceOrderCount;
        [Tooltip("Số hàng băng chuyền trong dữ liệu nguồn. Gameplay hiện chưa xử lý băng chuyền.")]
        public int sourceConveyorRowCount;

        public List<GrillLevelData> grills = new List<GrillLevelData>();
        public List<OrderLevelData> orders = new List<OrderLevelData>();
    }
}
