using FoodieSizzle;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FoodieSizzle.EditorTools
{
    /// <summary>
    /// Dựng lại bảng tạm dừng bằng các sprite mới.
    /// Công cụ chỉ chạy một lần và vẫn giữ nguyên font người dùng đã chọn.
    /// </summary>
    [InitializeOnLoad]
    public static class PausePanelRebuilder
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const string MarkerName = "_PausePanel_v27";
        private const string TitleMaterialPath =
            "Assets/Generated/PauseTitle_V25.mat";
        private const string LargePanelPath =
            "Assets/BaseGame/UsedSprites/settings_panel_large.png";
        private const string SmallPanelPath =
            "Assets/BaseGame/UsedSprites/settings_panel_small.png";
        private const string RestartButtonPath =
            "Assets/BaseGame/UsedSprites/pause_button_restart.png";
        private const string HomeButtonPath =
            "Assets/BaseGame/UsedSprites/pause_button_home.png";
        private const string CloseIconPath =
            "Assets/BaseGame/UsedSprites/pause_close_x.png";

        static PausePanelRebuilder()
        {
            EditorApplication.update += RebuildOnce;
        }

        private static void RebuildOnce()
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

            ImportAsSprite(LargePanelPath);
            ImportAsSprite(SmallPanelPath);
            ImportAsSprite(RestartButtonPath);
            ImportAsSprite(HomeButtonPath);
            ImportAsSprite(CloseIconPath);

            GameObject pausePopup = FindSceneObject("PausePopup");
            Transform card = pausePopup == null
                ? null
                : FindChild(pausePopup.transform, "Card");

            if (card == null || FindChild(card, MarkerName) != null)
            {
                EditorApplication.update -= RebuildOnce;
                return;
            }

            Sprite largePanel = AssetDatabase.LoadAssetAtPath<Sprite>(LargePanelPath);
            Sprite restartPanel =
                AssetDatabase.LoadAssetAtPath<Sprite>(RestartButtonPath);
            Sprite homePanel =
                AssetDatabase.LoadAssetAtPath<Sprite>(HomeButtonPath);
            if (largePanel == null || restartPanel == null || homePanel == null)
            {
                return;
            }

            TextMeshProUGUI oldTitle = FindText(card, "PauseTitle");
            TMP_FontAsset selectedFont = oldTitle != null ? oldTitle.font : null;
            Material selectedMaterial =
                oldTitle != null ? oldTitle.fontSharedMaterial : null;

            if (selectedFont == null)
            {
                TextMeshProUGUI timer = FindText(
                    GameObject.Find("GameCanvas").transform,
                    "TimerText");
                selectedFont = timer.font;
                selectedMaterial = timer.fontSharedMaterial;
            }

            Material titleMaterial = CreateTitleMaterial(selectedFont);

            GameUIManager uiManager =
                Object.FindFirstObjectByType<GameUIManager>();
            SerializedObject managerData = new SerializedObject(uiManager);
            TextMeshProUGUI levelText =
                GetReference<TextMeshProUGUI>(managerData, "levelText");
            TextMeshProUGUI timerText =
                GetReference<TextMeshProUGUI>(managerData, "timerText");
            TextMeshProUGUI progressText =
                GetReference<TextMeshProUGUI>(managerData, "progressText");
            Image timeBar =
                GetReference<Image>(managerData, "timeBarFill");
            GameObject resultPopup =
                GetReference<GameObject>(managerData, "resultPopup");
            TextMeshProUGUI resultTitle =
                GetReference<TextMeshProUGUI>(managerData, "resultTitle");
            TextMeshProUGUI resultMessage =
                GetReference<TextMeshProUGUI>(managerData, "resultMessage");
            Button pauseButton =
                GetReference<Button>(managerData, "pauseButton");
            Button resultRestart =
                GetReference<Button>(managerData, "resultRestartButton");

            // Xóa toàn bộ giao diện cũ bên trong Card.
            for (int i = card.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(card.GetChild(i).gameObject);
            }

            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.075f, 0.17f);
            cardRect.anchorMax = new Vector2(0.925f, 0.79f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.sizeDelta = Vector2.zero;

            Image cardImage = card.GetComponent<Image>();
            cardImage.sprite = largePanel;
            cardImage.color = Color.white;
            cardImage.type = Image.Type.Simple;
            cardImage.preserveAspect = false;

            TextMeshProUGUI title = CreateText(
                "PauseTitle",
                card,
                selectedFont,
                titleMaterial,
                "CÀI ĐẶT",
                72f,
                Color.white);
            SetAnchors(title.rectTransform,
                new Vector2(0.16f, 0.755f),
                new Vector2(0.84f, 0.915f));

            Button closeButton = CreateButton(
                "ResumeButton",
                card,
                null,
                Color.clear,
                new Vector2(0.79f, 0.805f),
                new Vector2(0.93f, 0.945f));
            AddCloseIcon(closeButton.transform);

            Button restartButton = CreateButton(
                "PauseRestartButton",
                card,
                restartPanel,
                Color.white,
                new Vector2(0.28f, 0.245f),
                new Vector2(0.72f, 0.37f));
            AddButtonIcon(
                restartButton.transform,
                "RetryIcon",
                "Assets/BaseGame/UsedSprites/Retry.png");

            Button homeButton = CreateButton(
                "HomeButton",
                card,
                homePanel,
                Color.white,
                new Vector2(0.28f, 0.11f),
                new Vector2(0.72f, 0.235f));
            AddButtonIcon(
                homeButton.transform,
                "HomeIcon",
                "Assets/BaseGame/UsedSprites/Home.png");

            GameObject marker = new GameObject(MarkerName);
            marker.transform.SetParent(card, false);
            marker.hideFlags = HideFlags.HideInHierarchy;

            uiManager.Configure(
                levelText, timerText, progressText, timeBar,
                resultPopup, resultTitle, resultMessage, pausePopup,
                pauseButton, closeButton, restartButton, resultRestart);

            EditorUtility.SetDirty(uiManager);
            EditorUtility.SetDirty(card);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            EditorApplication.update -= RebuildOnce;

            Debug.Log(
                "[Foodie Sizzle] Đã xóa bảng pause cũ và dựng lại bảng pause mới.");
        }

        private static Material CreateTitleMaterial(TMP_FontAsset font)
        {
            if (font == null || font.material == null)
            {
                return null;
            }

            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(TitleMaterialPath);
            if (material == null)
            {
                material = new Material(font.material)
                {
                    name = "PauseTitle_V25"
                };
                AssetDatabase.CreateAsset(material, TitleMaterialPath);
            }
            else
            {
                material.shader = font.material.shader;
                material.CopyPropertiesFromMaterial(font.material);
            }

            material.EnableKeyword("OUTLINE_ON");
            material.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.22f);
            material.SetFloat(ShaderUtilities.ID_OutlineSoftness, 0f);
            material.SetFloat(ShaderUtilities.ID_FaceDilate, 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ImportAsSprite(string path)
        {
            TextureImporter importer =
                AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            bool changed = importer.textureType != TextureImporterType.Sprite ||
                           importer.spriteImportMode != SpriteImportMode.Single ||
                           importer.alphaIsTransparency == false;
            if (!changed)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static Button CreateButton(
            string name,
            Transform parent,
            Sprite sprite,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            GameObject go = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            SetAnchors(rect, anchorMin, anchorMax);

            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;

            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;
            return button;
        }

        private static void AddButtonIcon(
            Transform parent,
            string name,
            string spritePath)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
            {
                return;
            }

            GameObject go = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.39f, 0.18f);
            rect.anchorMax = new Vector2(0.61f, 0.82f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private static void AddCloseIcon(Transform parent)
        {
            Sprite sprite =
                AssetDatabase.LoadAssetAtPath<Sprite>(CloseIconPath);
            if (sprite == null)
            {
                return;
            }

            GameObject go = new GameObject(
                "CloseX",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.16f, 0.16f);
            rect.anchorMax = new Vector2(0.84f, 0.84f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private static TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            TMP_FontAsset font,
            Material material,
            string value,
            float fontSize,
            Color color)
        {
            GameObject go = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            text.font = font;
            if (material != null)
            {
                text.fontSharedMaterial = material;
            }
            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = false;
            text.raycastTarget = false;
            text.UpdateMeshPadding();
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetAnchors(
            RectTransform rect,
            Vector2 min,
            Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private static T GetReference<T>(
            SerializedObject source,
            string propertyName) where T : Object
        {
            return source.FindProperty(propertyName).objectReferenceValue as T;
        }

        private static TextMeshProUGUI FindText(
            Transform root,
            string name)
        {
            Transform result = FindChild(root, name);
            return result == null ? null : result.GetComponent<TextMeshProUGUI>();
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }
            if (root.name == name)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform result = FindChild(root.GetChild(i), name);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        private static GameObject FindSceneObject(string name)
        {
            GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (GameObject candidate in objects)
            {
                if (candidate.name == name &&
                    candidate.scene.IsValid() &&
                    candidate.scene == SceneManager.GetActiveScene())
                {
                    return candidate;
                }
            }
            return null;
        }
    }
}
