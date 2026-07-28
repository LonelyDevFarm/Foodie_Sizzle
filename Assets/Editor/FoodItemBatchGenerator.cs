using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FoodieSizzle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Tạo và đồng bộ FoodItemData từ toàn bộ sub-sprite trong ảnh xiên lớn.
/// Tên asset và itemId giữ đúng số thứ tự ở cuối tên sub-sprite.
/// </summary>
[InitializeOnLoad]
public static class FoodItemBatchGenerator
{
    private const string SpriteSheetPath =
        "Assets/BaseGame/UsedSprites/sactx-0-2048x2048-Crunch-Skewer-467c83a6_0.png";
    private const string OutputFolder = "Assets/Food Items";
    private const float StackingHeightThreshold = 120f;

    private static readonly Regex SpriteIndexPattern =
        new Regex(@"_(\d+)$", RegexOptions.Compiled);

    static FoodItemBatchGenerator()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.delayCall += GenerateAndSynchronize;
    }

    [MenuItem("Foodie Sizzle/Tạo và đồng bộ toàn bộ Food Items")]
    public static void GenerateAndSynchronize()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning(
                "Đang chờ thoát Play Mode để tạo và đồng bộ Food Items.");
            return;
        }

        UnityEngine.Object[] loadedAssets =
            AssetDatabase.LoadAllAssetsAtPath(SpriteSheetPath);

        List<(int index, Sprite sprite)> sprites = loadedAssets
            .OfType<Sprite>()
            .Select(sprite => (match: SpriteIndexPattern.Match(sprite.name), sprite))
            .Where(entry => entry.match.Success)
            .Select(entry =>
                (int.Parse(entry.match.Groups[1].Value), entry.sprite))
            .OrderBy(entry => entry.Item1)
            .ToList();

        if (sprites.Count == 0)
        {
            Debug.LogError(
                $"Không tìm thấy sub-sprite xiên trong '{SpriteSheetPath}'.");
            return;
        }

        EnsureOutputFolderExists();

        int createdCount = 0;
        List<FoodItemData> allFoodItems = new List<FoodItemData>(sprites.Count);

        foreach ((int index, Sprite sprite) in sprites)
        {
            string assetPath = $"{OutputFolder}/Skewer_{index}.asset";
            FoodItemData foodItem =
                AssetDatabase.LoadAssetAtPath<FoodItemData>(assetPath);

            bool wasCreated = foodItem == null;
            if (wasCreated)
            {
                foodItem = ScriptableObject.CreateInstance<FoodItemData>();
                foodItem.name = $"Skewer_{index}";

                // Những sprite rất thấp là một miếng nguyên liệu đơn.
                // SkewerVisual sẽ nhân thành ba miếng xếp dọc trên một xiên.
                foodItem.needsStacking =
                    sprite.rect.height <= StackingHeightThreshold;
                foodItem.visualScale = 1f;
                foodItem.visualOffset = Vector2.zero;

                AssetDatabase.CreateAsset(foodItem, assetPath);
                createdCount++;
            }

            // Luôn đồng bộ hai trường nhận dạng; các chỉnh sửa hiển thị thủ công
            // trên asset đã tồn tại vẫn được giữ nguyên.
            foodItem.itemId = index.ToString();
            foodItem.itemSprite = sprite;

            if (foodItem.visualScale <= 0f)
            {
                foodItem.visualScale = 1f;
            }

            EditorUtility.SetDirty(foodItem);
            allFoodItems.Add(foodItem);
        }

        AssetDatabase.SaveAssets();
        SynchronizeLoadedGameplayManagers(allFoodItems);

        Debug.Log(
            $"Đã đồng bộ {allFoodItems.Count} FoodItemData " +
            $"(tạo mới {createdCount}). Các số bị khuyết là do sprite sheet " +
            "không có sub-sprite tương ứng.");
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.delayCall += GenerateAndSynchronize;
        }
    }

    private static void EnsureOutputFolderExists()
    {
        if (AssetDatabase.IsValidFolder(OutputFolder))
        {
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Food Items"))
        {
            AssetDatabase.CreateFolder("Assets", "Food Items");
        }
    }

    private static void SynchronizeLoadedGameplayManagers(
        List<FoodItemData> allFoodItems)
    {
        GameplayManager[] managers =
            UnityEngine.Object.FindObjectsByType<GameplayManager>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        foreach (GameplayManager manager in managers)
        {
            bool alreadySynchronized =
                manager.possibleFoodItems != null &&
                manager.possibleFoodItems.Count == allFoodItems.Count &&
                manager.possibleFoodItems.SequenceEqual(allFoodItems);

            if (alreadySynchronized)
            {
                continue;
            }

            Undo.RecordObject(manager, "Đồng bộ toàn bộ Food Items");
            manager.possibleFoodItems =
                new List<FoodItemData>(allFoodItems);
            EditorUtility.SetDirty(manager);

            if (manager.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            }
        }
    }
}
