using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FoodieSizzle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class LevelJsonImporter
{
    private const int PilotLevelCount = 10;
    private const int MaxSupportedRows = 4;
    private const int MaxSupportedColumns = 3;
    private const int MaxSupportedGrills = 12;
    private const string LevelFolder = "Assets/Levels";
    private const string ResourcesFolder = "Assets/Resources";
    private const string MappingAssetPath =
        LevelFolder + "/ImportedFoodIdMapping.asset";
    private const string DatabaseAssetPath =
        ResourcesFolder + "/LevelDatabase.asset";
    private const string AutoImportSessionKey =
        "FoodieSizzle.LevelJsonImporter.DatabaseSynced.v6";

    [Serializable]
    private class SourceRoot
    {
        public string min_client_version;
        public List<SourceLevel> levels = new List<SourceLevel>();
    }

    [Serializable]
    private class SourceLevel
    {
        public string id;
        public int rows;
        public int cols;
        public float timeLimit;
        public bool useDDA;
        public bool shuffleIcons;
        public float speedGrillConveyor = 1f;
        public int startGrillConveyor = -1;
        public int difficulty;
        public float visualSimilarity = -1f;
        public bool useSmartIconSelection;
        public List<SourceGrill> grills = new List<SourceGrill>();
        public List<SourceOrder> orders = new List<SourceOrder>();
        public List<SourceConveyorRow> conveyorRows =
            new List<SourceConveyorRow>();
    }

    [Serializable]
    private class SourceGrill
    {
        public int locked;
        public List<SourceFoodLayer> foodQueue = new List<SourceFoodLayer>();
    }

    [Serializable]
    private class SourceFoodLayer
    {
        public int[] foodIds;
    }

    [Serializable]
    private class SourceOrder
    {
        public float timeLimit;
        public int numberOfFood;
        public int[] layers;
        public bool[] isSpicy;
        public int matchesToTrigger;
    }

    [Serializable]
    private class SourceConveyorRow
    {
    }

    static LevelJsonImporter()
    {
        EditorApplication.delayCall += TrySynchronizeLevelDatabase;
    }

    [MenuItem("Foodie Sizzle/Level/Nhập 10 level thử nghiệm từ JSON")]
    public static void ImportPilotLevelsFromFile()
    {
        string sourcePath = EditorUtility.OpenFilePanel(
            "Chọn file JSON chứa LevelData",
            GetDefaultSourceDirectory(),
            "json");

        if (!string.IsNullOrWhiteSpace(sourcePath))
        {
            ImportLevels(sourcePath, PilotLevelCount, true);
        }
    }

    [MenuItem("Foodie Sizzle/Level/Nhập toàn bộ level từ JSON")]
    public static void ImportAllLevelsFromFile()
    {
        string sourcePath = EditorUtility.OpenFilePanel(
            "Chọn file JSON chứa toàn bộ LevelData",
            GetDefaultSourceDirectory(),
            "json");

        if (!string.IsNullOrWhiteSpace(sourcePath))
        {
            ImportLevels(sourcePath, 0, true);
        }
    }

    [MenuItem("Foodie Sizzle/Level/Nhập lại 10 level và giữ bảng ánh xạ")]
    public static void ReimportPilotLevels()
    {
        string sourcePath = FindDefaultSourcePath();
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            ImportPilotLevelsFromFile();
            return;
        }

        ImportLevels(sourcePath, PilotLevelCount, true);
    }

    [MenuItem("Foodie Sizzle/Level/Nhập lại toàn bộ và giữ bảng ánh xạ")]
    public static void ReimportAllLevels()
    {
        string sourcePath = FindDefaultSourcePath();
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            ImportAllLevelsFromFile();
            return;
        }

        ImportLevels(sourcePath, 0, true);
    }

    [MenuItem("Foodie Sizzle/Level/Đồng bộ LevelDatabase hiện có")]
    public static void SynchronizeExistingLevelDatabase()
    {
        SynchronizeLevelDatabase(true);
        Debug.Log("Đã đồng bộ LevelDatabase từ các LevelData hiện có.");
    }

    private static void TrySynchronizeLevelDatabase()
    {
        if (SessionState.GetBool(AutoImportSessionKey, false))
        {
            return;
        }

        SessionState.SetBool(AutoImportSessionKey, true);
        SynchronizeLevelDatabase(false);
    }

    private static void ImportLevels(
        string sourcePath,
        int requestedLevelCount,
        bool assignFirstLevel)
    {
        if (!File.Exists(sourcePath))
        {
            Debug.LogError($"Không tìm thấy file LevelData: {sourcePath}");
            return;
        }

        SourceRoot sourceRoot;
        try
        {
            sourceRoot = JsonUtility.FromJson<SourceRoot>(
                File.ReadAllText(sourcePath));
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"Không đọc được JSON LevelData.\n{exception.Message}");
            return;
        }

        if (sourceRoot == null ||
            sourceRoot.levels == null ||
            sourceRoot.levels.Count == 0)
        {
            Debug.LogError("File JSON không chứa danh sách 'levels' hợp lệ.");
            return;
        }

        EnsureFolderExists(LevelFolder);

        List<FoodItemData> foodItems = LoadAllFoodItems();
        if (foodItems.Count == 0)
        {
            Debug.LogError(
                "Chưa có FoodItemData. Hãy tạo Food Items trước khi nhập level.");
            return;
        }

        FoodIdMappingData mapping = GetOrCreateMapping();
        SynchronizeMapping(mapping, sourceRoot.levels, foodItems);

        Dictionary<int, FoodItemData> lookup = mapping.entries
            .Where(entry => entry != null && entry.foodItem != null)
            .GroupBy(entry => entry.sourceId)
            .ToDictionary(group => group.Key, group => group.First().foodItem);

        int targetCount = requestedLevelCount > 0
            ? requestedLevelCount
            : int.MaxValue;
        int skippedCount = 0;
        List<LevelData> importedLevels = new List<LevelData>();

        foreach (SourceLevel sourceLevel in sourceRoot.levels)
        {
            if (importedLevels.Count >= targetCount)
            {
                break;
            }

            if (!IsSourceLevelSupported(
                    sourceLevel,
                    out string unsupportedReason))
            {
                skippedCount++;
                Debug.LogWarning(
                    $"Bỏ qua '{sourceLevel?.id ?? "(không tên)"}': " +
                    unsupportedReason);
                continue;
            }

            int gameLevelNumber = importedLevels.Count + 1;
            if (CreateOrUpdateLevel(
                    sourceLevel,
                    gameLevelNumber,
                    lookup))
            {
                LevelData importedLevel =
                    AssetDatabase.LoadAssetAtPath<LevelData>(
                        GetLevelAssetPath(gameLevelNumber));
                if (importedLevel != null)
                {
                    importedLevels.Add(importedLevel);
                }
            }
            else
            {
                skippedCount++;
            }
        }

        EditorUtility.SetDirty(mapping);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        SynchronizeLevelDatabase(
            assignFirstLevel,
            Path.GetFileName(sourcePath),
            sourceRoot.min_client_version,
            importedLevels);

        Debug.Log(
            $"Đã nhập {importedLevels.Count} level phù hợp từ " +
            $"'{Path.GetFileName(sourcePath)}'. Bảng ánh xạ có " +
            $"{mapping.entries.Count} loại món; bỏ qua {skippedCount} " +
            "level không tương thích với gameplay hiện tại.");
    }

    private static bool IsSourceLevelSupported(
        SourceLevel source,
        out string reason)
    {
        if (source == null)
        {
            reason = "dữ liệu level trống.";
            return false;
        }

        if (source.rows < 1 || source.rows > MaxSupportedRows)
        {
            reason =
                $"số hàng {source.rows} nằm ngoài phạm vi 1-{MaxSupportedRows}.";
            return false;
        }

        if (source.cols < 1 || source.cols > MaxSupportedColumns)
        {
            reason =
                $"số cột {source.cols} nằm ngoài phạm vi 1-{MaxSupportedColumns}.";
            return false;
        }

        int grillCount = source.grills != null
            ? source.grills.Count
            : 0;
        if (grillCount < 1 || grillCount > MaxSupportedGrills)
        {
            reason =
                $"có {grillCount} bếp, game chỉ hỗ trợ tối đa " +
                $"{MaxSupportedGrills}.";
            return false;
        }
        if (grillCount > source.rows * source.cols)
        {
            reason =
                $"có {grillCount} bếp nhưng bố cục " +
                $"{source.rows}x{source.cols} chỉ có " +
                $"{source.rows * source.cols} vị trí.";
            return false;
        }

        reason = null;
        return true;
    }

    private static bool CreateOrUpdateLevel(
        SourceLevel source,
        int levelNumber,
        Dictionary<int, FoodItemData> mapping)
    {
        if (source == null || source.grills == null)
        {
            Debug.LogWarning($"Bỏ qua Level {levelNumber}: dữ liệu trống.");
            return false;
        }

        if (!TryConvertGrills(
                source,
                mapping,
                out List<GrillLevelData> convertedGrills,
                out int totalSkewers,
                out string validationError))
        {
            Debug.LogWarning(
                $"Bỏ qua '{source.id}': {validationError}");
            return false;
        }

        LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(
            GetLevelAssetPath(levelNumber));
        if (level == null)
        {
            level = ScriptableObject.CreateInstance<LevelData>();
            AssetDatabase.CreateAsset(
                level,
                GetLevelAssetPath(levelNumber));
        }

        level.schemaVersion = 4;
        level.levelNumber = levelNumber;
        level.sourceLevelId = source.id;
        level.rows = Mathf.Max(1, source.rows);
        level.columns = Mathf.Max(1, source.cols);
        level.timeLimitSeconds = Mathf.Max(1f, source.timeLimit);
        level.useDda = source.useDDA;
        level.shuffleIcons = source.shuffleIcons;
        level.difficulty = source.difficulty;
        level.sourceGrillConveyorSpeed = source.speedGrillConveyor;
        level.sourceGrillConveyorStart = source.startGrillConveyor;
        level.sourceVisualSimilarity = source.visualSimilarity;
        level.sourceUseSmartIconSelection =
            source.useSmartIconSelection;
        level.sourceOrderCount =
            source.orders != null ? source.orders.Count : 0;
        level.sourceConveyorRowCount =
            source.conveyorRows != null ? source.conveyorRows.Count : 0;
        level.grills = convertedGrills;
        level.orders = new List<OrderLevelData>();

        if (source.orders != null)
        {
            foreach (SourceOrder sourceOrder in source.orders)
            {
                if (sourceOrder == null) continue;

                level.orders.Add(new OrderLevelData
                {
                    timeLimitSeconds = Mathf.Max(1f, sourceOrder.timeLimit),
                    numberOfFood = Mathf.Clamp(sourceOrder.numberOfFood, 1, 3),
                    preferredLayers = sourceOrder.layers != null
                        ? new List<int>(sourceOrder.layers)
                        : new List<int>(),
                    isSpicy = sourceOrder.isSpicy != null
                        ? new List<bool>(sourceOrder.isSpicy)
                        : new List<bool>(),
                    matchesToTrigger = Mathf.Max(
                        0,
                        sourceOrder.matchesToTrigger)
                });
            }
        }

        EditorUtility.SetDirty(level);
        return true;
    }

    private static bool TryConvertGrills(
        SourceLevel source,
        Dictionary<int, FoodItemData> mapping,
        out List<GrillLevelData> convertedGrills,
        out int totalSkewers,
        out string error)
    {
        convertedGrills = new List<GrillLevelData>();
        totalSkewers = 0;
        error = null;
        int normalizedModifierCount = 0;
        Dictionary<string, int> matchCounts =
            new Dictionary<string, int>();

        foreach (SourceGrill sourceGrill in source.grills)
        {
            int sourceLockId = sourceGrill != null ? sourceGrill.locked : 0;
            GrillLevelData grill = new GrillLevelData
            {
                sourceLockId = sourceLockId,
                unlockItemId =
                    sourceLockId > 0 &&
                    mapping.TryGetValue(sourceLockId, out FoodItemData unlockItem)
                        ? unlockItem.itemId
                        : string.Empty,
                layers = new List<FoodLayerData>()
            };

            if (sourceGrill != null && sourceGrill.foodQueue != null)
            {
                foreach (SourceFoodLayer sourceLayer in sourceGrill.foodQueue)
                {
                    if (sourceLayer?.foodIds == null) continue;

                    FoodLayerData layer = new FoodLayerData();
                    foreach (int encodedSourceId in sourceLayer.foodIds)
                    {
                        // 0 là ô trống. Dấu âm và hàng nghìn là cờ trạng thái
                        // của món trong data gốc; gameplay hiện tại dùng món cơ sở.
                        if (encodedSourceId == 0) continue;

                        int sourceId =
                            NormalizeSourceFoodId(encodedSourceId);
                        if (sourceId <= 0 ||
                            !mapping.TryGetValue(
                                sourceId,
                                out FoodItemData foodItem))
                        {
                            error =
                                $"chưa ánh xạ được food ID " +
                                $"{encodedSourceId} (ID cơ sở {sourceId}).";
                            return false;
                        }

                        if (sourceId != encodedSourceId)
                        {
                            normalizedModifierCount++;
                        }

                        layer.itemIds.Add(foodItem.itemId);
                        totalSkewers++;
                        string matchKey = foodItem.GetMatchKey();
                        matchCounts.TryGetValue(
                            matchKey,
                            out int currentCount);
                        matchCounts[matchKey] = currentCount + 1;
                    }

                    if (layer.itemIds.Count > 3)
                    {
                        error =
                            $"một layer có {layer.itemIds.Count} xiên, " +
                            "vượt sức chứa 3.";
                        return false;
                    }
                    if (layer.itemIds.Count > 0)
                    {
                        grill.layers.Add(layer);
                    }
                }
            }

            convertedGrills.Add(grill);
        }

        if (totalSkewers == 0 || totalSkewers % 3 != 0)
        {
            error =
                $"tổng {totalSkewers} xiên không chia hết cho 3.";
            return false;
        }

        foreach (KeyValuePair<string, int> pair in matchCounts)
        {
            if (pair.Value % 3 != 0)
            {
                error =
                    $"nhóm món '{pair.Key}' có {pair.Value} xiên, " +
                    "không chia hết cho 3.";
                return false;
            }
        }

        if (normalizedModifierCount > 0)
        {
            Debug.Log(
                $"{source.id}: đã đưa {normalizedModifierCount} food ID " +
                "có cờ trạng thái về món cơ sở.");
        }

        return true;
    }

    private static int NormalizeSourceFoodId(int encodedSourceId)
    {
        long absoluteId = Math.Abs((long)encodedSourceId);
        return (int)(absoluteId % 1000L);
    }

    private static void SynchronizeMapping(
        FoodIdMappingData mapping,
        List<SourceLevel> levels,
        List<FoodItemData> availableFoodItems)
    {
        HashSet<int> sourceIds = new HashSet<int>();
        foreach (SourceLevel level in levels)
        {
            if (level == null || level.grills == null) continue;
            foreach (SourceGrill grill in level.grills)
            {
                if (grill == null || grill.foodQueue == null) continue;
                foreach (SourceFoodLayer layer in grill.foodQueue)
                {
                    if (layer?.foodIds == null) continue;
                    foreach (int encodedSourceId in layer.foodIds)
                    {
                        if (encodedSourceId == 0) continue;

                        int sourceId =
                            NormalizeSourceFoodId(encodedSourceId);
                        if (sourceId > 0)
                        {
                            sourceIds.Add(sourceId);
                        }
                    }
                }
            }
        }

        Dictionary<int, FoodIdMappingEntry> existing = mapping.entries
            .Where(entry => entry != null)
            .GroupBy(entry => entry.sourceId)
            .ToDictionary(group => group.Key, group => group.First());
        HashSet<FoodItemData> usedItems = new HashSet<FoodItemData>(
            existing.Values
                .Where(entry => entry.foodItem != null)
                .Select(entry => entry.foodItem));
        Queue<FoodItemData> freeItems = new Queue<FoodItemData>(
            availableFoodItems.Where(item => !usedItems.Contains(item)));

        foreach (int sourceId in sourceIds.OrderBy(value => value))
        {
            if (existing.ContainsKey(sourceId))
            {
                continue;
            }

            if (freeItems.Count == 0)
            {
                Debug.LogWarning(
                    $"Không còn FoodItemData để ánh xạ source ID {sourceId}.");
                break;
            }

            FoodIdMappingEntry entry = new FoodIdMappingEntry
            {
                sourceId = sourceId,
                foodItem = freeItems.Dequeue()
            };
            mapping.entries.Add(entry);
            existing[sourceId] = entry;
        }

        mapping.sourceName = "Grill Empire - 2.3.0 Base Cohort";
        mapping.entries = mapping.entries
            .Where(entry => entry != null)
            .OrderBy(entry => entry.sourceId)
            .ToList();
    }

    private static List<FoodItemData> LoadAllFoodItems()
    {
        return AssetDatabase.FindAssets("t:FoodItemData")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<FoodItemData>)
            .Where(item => item != null)
            .OrderBy(item =>
            {
                return int.TryParse(item.itemId, out int value)
                    ? value
                    : int.MaxValue;
            })
            .ThenBy(item => item.name)
            .ToList();
    }

    private static FoodIdMappingData GetOrCreateMapping()
    {
        FoodIdMappingData mapping =
            AssetDatabase.LoadAssetAtPath<FoodIdMappingData>(MappingAssetPath);
        if (mapping != null)
        {
            return mapping;
        }

        mapping = ScriptableObject.CreateInstance<FoodIdMappingData>();
        AssetDatabase.CreateAsset(mapping, MappingAssetPath);
        return mapping;
    }

    private static void SynchronizeLevelDatabase(
        bool assignToLoadedScene,
        string sourceName = null,
        string minimumClientVersion = null,
        IEnumerable<LevelData> selectedLevels = null)
    {
        List<LevelData> importedLevels = selectedLevels != null
            ? selectedLevels
                .Where(level => IsLevelAssetSupported(level))
                .OrderBy(level => level.levelNumber)
                .ToList()
            : LoadAllLevels()
                .Where(level => IsLevelAssetSupported(level))
                .ToList();
        if (importedLevels.Count == 0)
        {
            return;
        }

        EnsureFolderExists(ResourcesFolder);
        LevelDatabase database =
            AssetDatabase.LoadAssetAtPath<LevelDatabase>(DatabaseAssetPath);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<LevelDatabase>();
            AssetDatabase.CreateAsset(database, DatabaseAssetPath);
        }

        database.ReplaceLevels(importedLevels);
        if (!string.IsNullOrWhiteSpace(sourceName) ||
            !string.IsNullOrWhiteSpace(minimumClientVersion))
        {
            database.SetSourceInfo(sourceName, minimumClientVersion);
        }
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();

        if (!assignToLoadedScene)
        {
            return;
        }

        LevelData firstLevel = importedLevels[0];
        GameplayManager[] managers =
            UnityEngine.Object.FindObjectsByType<GameplayManager>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        foreach (GameplayManager manager in managers)
        {
            Undo.RecordObject(manager, "Đồng bộ LevelDatabase");
            manager.levelDatabase = database;
            if (manager.currentLevelData == null)
            {
                manager.currentLevelData = firstLevel;
            }
            EditorUtility.SetDirty(manager);
            if (manager.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            }
        }
    }

    private static bool IsLevelAssetSupported(LevelData level)
    {
        return level != null &&
            level.rows >= 1 &&
            level.rows <= MaxSupportedRows &&
            level.columns >= 1 &&
            level.columns <= MaxSupportedColumns &&
            level.grills != null &&
            level.grills.Count >= 1 &&
            level.grills.Count <= MaxSupportedGrills;
    }

    private static List<LevelData> LoadAllLevels()
    {
        return AssetDatabase.FindAssets(
                "t:LevelData",
                new[] { LevelFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<LevelData>)
            .Where(level => level != null)
            .OrderBy(level => level.levelNumber)
            .ThenBy(level => level.name)
            .ToList();
    }

    private static string GetLevelAssetPath(int levelNumber)
    {
        return $"{LevelFolder}/Level_{levelNumber:000}.asset";
    }

    private static void EnsureFolderExists(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int index = 1; index < parts.Length; index++)
        {
            string next = current + "/" + parts[index];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[index]);
            }
            current = next;
        }
    }

    private static string FindDefaultSourcePath()
    {
        string candidate = Path.GetFullPath(Path.Combine(
            Directory.GetParent(Application.dataPath).FullName,
            "..",
            "_GameExtract",
            "FoodieSizzle2",
            "ExportedProject",
            "Assets",
            "TextAsset",
            "2.3.0_BaseCohortFix.json"));

        return File.Exists(candidate) ? candidate : null;
    }

    private static string GetDefaultSourceDirectory()
    {
        string sourcePath = FindDefaultSourcePath();
        return string.IsNullOrWhiteSpace(sourcePath)
            ? Directory.GetParent(Application.dataPath).FullName
            : Path.GetDirectoryName(sourcePath);
    }
}
