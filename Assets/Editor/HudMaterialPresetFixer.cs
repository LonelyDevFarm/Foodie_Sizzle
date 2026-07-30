using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FoodieSizzle.EditorTools
{
    /// <summary>
    /// Tạo Material Preset riêng cho chữ HUD để chỉnh viền của từng nhóm
    /// mà không làm thay đổi Font Asset người dùng đã chọn.
    /// </summary>
    [InitializeOnLoad]
    public static class HudMaterialPresetFixer
    {
        private const string ScenePath = "Assets/Scenes/GameplayScene.unity";
        private const string MarkerName = "_HudMaterialPresets_v23";
        private const string SmallTextMaterialPath =
            "Assets/Generated/HUD_LevelTarget_V23.mat";
        private const string TimerMaterialPath =
            "Assets/Generated/HUD_Timer_V23.mat";

        static HudMaterialPresetFixer()
        {
            EditorApplication.update += ApplyOnce;
        }

        private static void ApplyOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling ||
                EditorApplication.isUpdating)
            {
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                return;
            }

            GameObject canvas = GameObject.Find("GameCanvas");
            if (canvas == null || FindChild(canvas.transform, MarkerName) != null)
            {
                EditorApplication.update -= ApplyOnce;
                return;
            }

            TextMeshProUGUI levelText = FindText(canvas.transform, "LevelText");
            TextMeshProUGUI timerText = FindText(canvas.transform, "TimerText");
            TextMeshProUGUI progressText = FindText(canvas.transform, "ProgressText");

            if (levelText == null || timerText == null || progressText == null ||
                levelText.font == null || timerText.font == null ||
                progressText.font == null)
            {
                return;
            }

            EnsureFolder("Assets/Generated");

            Material levelTargetMaterial = CreatePreset(
                SmallTextMaterialPath,
                levelText.font.material,
                0.35f);

            Material timerMaterial = CreatePreset(
                TimerMaterialPath,
                timerText.font.material,
                0.28f);

            if (levelTargetMaterial == null || timerMaterial == null)
            {
                return;
            }

            ApplyMaterial(levelText, levelTargetMaterial);
            ApplyMaterial(progressText, levelTargetMaterial);
            ApplyMaterial(timerText, timerMaterial);

            GameObject marker = new GameObject(MarkerName);
            marker.transform.SetParent(canvas.transform, false);
            marker.hideFlags = HideFlags.HideInHierarchy;

            EditorUtility.SetDirty(canvas);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);

            EditorApplication.update -= ApplyOnce;
            Debug.Log(
                "[Foodie Sizzle] Đã tách Material Preset HUD: " +
                "Lv./mục tiêu = 0.35, thời gian = 0.28.");
        }

        private static Material CreatePreset(
            string path,
            Material source,
            float outlineWidth)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                material = new Material(source)
                {
                    name = System.IO.Path.GetFileNameWithoutExtension(path)
                };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = source.shader;
                material.CopyPropertiesFromMaterial(source);
            }

            material.EnableKeyword("OUTLINE_ON");
            material.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, outlineWidth);
            material.SetFloat(ShaderUtilities.ID_OutlineSoftness, 0f);
            material.SetFloat(ShaderUtilities.ID_FaceDilate, 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ApplyMaterial(
            TextMeshProUGUI text,
            Material material)
        {
            // UI Outline cũ có thể khiến chữ nhìn thành hai lớp lệch nhau.
            Outline oldOutline = text.GetComponent<Outline>();
            if (oldOutline != null)
            {
                Object.DestroyImmediate(oldOutline);
            }

            text.fontSharedMaterial = material;
            text.UpdateMeshPadding();
            text.SetAllDirty();
            EditorUtility.SetDirty(text);
        }

        private static TextMeshProUGUI FindText(
            Transform root,
            string objectName)
        {
            Transform child = FindChild(root, objectName);
            return child == null ? null : child.GetComponent<TextMeshProUGUI>();
        }

        private static Transform FindChild(Transform root, string objectName)
        {
            if (root.name == objectName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform result = FindChild(root.GetChild(i), objectName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            AssetDatabase.CreateFolder("Assets", "Generated");
        }
    }
}
