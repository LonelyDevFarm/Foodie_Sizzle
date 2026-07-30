using System.Collections.Generic;
using UnityEngine;

namespace FoodieSizzle
{
    /// <summary>
    /// Danh mục LevelData dùng chung cho toàn bộ game.
    /// Gameplay chỉ cần tham chiếu một asset này thay vì kéo từng level vào scene.
    /// </summary>
    [CreateAssetMenu(
        fileName = "LevelDatabase",
        menuName = "Foodie Sizzle/Level Database",
        order = 4)]
    public class LevelDatabase : ScriptableObject
    {
        [SerializeField] private int schemaVersion = 1;
        [SerializeField] private string sourceName;
        [SerializeField] private string sourceMinimumClientVersion;
        [SerializeField]
        private List<LevelData> levels = new List<LevelData>();

        public int SchemaVersion => schemaVersion;
        public string SourceName => sourceName;
        public string SourceMinimumClientVersion =>
            sourceMinimumClientVersion;
        public IReadOnlyList<LevelData> Levels => levels;
        public int Count => levels != null ? levels.Count : 0;

        public void SetSourceInfo(
            string importedSourceName,
            string minimumClientVersion)
        {
            sourceName = importedSourceName;
            sourceMinimumClientVersion = minimumClientVersion;
        }

        public LevelData GetAt(int index)
        {
            return index >= 0 && index < Count ? levels[index] : null;
        }

        public LevelData GetByLevelNumber(int levelNumber)
        {
            if (levels == null) return null;

            foreach (LevelData level in levels)
            {
                if (level != null && level.levelNumber == levelNumber)
                {
                    return level;
                }
            }

            return null;
        }

        public int IndexOf(LevelData level)
        {
            if (levels == null || level == null) return -1;

            int directIndex = levels.IndexOf(level);
            if (directIndex >= 0) return directIndex;

            return levels.FindIndex(
                candidate => candidate != null &&
                    candidate.levelNumber == level.levelNumber);
        }

        /// <summary>
        /// Được importer gọi sau khi nhập DataLV. Danh sách luôn được sắp theo
        /// levelNumber và loại bỏ level null hoặc trùng số.
        /// </summary>
        public void ReplaceLevels(IEnumerable<LevelData> sourceLevels)
        {
            levels = new List<LevelData>();
            if (sourceLevels == null) return;

            HashSet<int> usedLevelNumbers = new HashSet<int>();
            foreach (LevelData level in sourceLevels)
            {
                if (level == null ||
                    !usedLevelNumbers.Add(level.levelNumber))
                {
                    continue;
                }

                levels.Add(level);
            }

            levels.Sort((first, second) =>
                first.levelNumber.CompareTo(second.levelNumber));
        }
    }
}
