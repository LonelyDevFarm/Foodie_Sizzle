using System.Linq;
using FoodieSizzle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sửa các reference UI có thể bị mất khi texture nút được import lại.
/// Chỉ chạm vào GameplayScene đang mở, không tự mở hoặc ghi đè scene khác.
/// </summary>
[InitializeOnLoad]
public static class GameplayUiReferenceRepair
{
    private const string GameplayScenePath =
        "Assets/Scenes/GameplayScene.unity";
    private const string ContinueButtonPath =
        "Assets/Texture2D/Green Button.png";

    static GameplayUiReferenceRepair()
    {
        EditorApplication.delayCall += Repair;
    }

    private static void Repair()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            UnityEngine.SceneManagement.SceneManager
                .GetActiveScene().path != GameplayScenePath)
        {
            return;
        }

        GameUIManager manager =
            Object.FindFirstObjectByType<GameUIManager>(
                FindObjectsInactive.Include);
        Sprite continueSprite = AssetDatabase
            .LoadAllAssetsAtPath(ContinueButtonPath)
            .OfType<Sprite>()
            .FirstOrDefault();
        if (manager == null || continueSprite == null) return;

        SerializedObject managerData = new SerializedObject(manager);
        SerializedProperty spriteProperty =
            managerData.FindProperty("continueButtonSprite");
        SerializedProperty backgroundProperty =
            managerData.FindProperty("resultPrimaryBackground");
        Image background =
            backgroundProperty?.objectReferenceValue as Image;

        bool changed = false;
        if (spriteProperty != null &&
            spriteProperty.objectReferenceValue != continueSprite)
        {
            spriteProperty.objectReferenceValue = continueSprite;
            changed = true;
        }

        if (background != null &&
            (background.sprite != continueSprite ||
             background.type != Image.Type.Sliced ||
             background.preserveAspect))
        {
            Undo.RecordObject(
                background,
                "Sửa nền nút Tiếp tục");
            background.sprite = continueSprite;
            background.type = Image.Type.Sliced;
            background.preserveAspect = false;
            EditorUtility.SetDirty(background);
            changed = true;
        }

        if (!changed) return;

        managerData.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        EditorSceneManager.SaveScene(manager.gameObject.scene);
        Debug.Log(
            "[Foodie Sizzle] Đã khôi phục sprite nền của nút Tiếp tục.");
    }
}
