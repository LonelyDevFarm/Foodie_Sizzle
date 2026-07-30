using System.Linq;
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
    /// Dựng màn hình Home tối giản trong cùng scene gameplay.
    /// Home là lớp phủ toàn màn hình nên không cần nhân đôi camera và manager.
    /// </summary>
    [InitializeOnLoad]
    public static class HomeScreenBuilder
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const string RootName = "HomeScreen_v1";
        private const string BackgroundPath =
            "Assets/BaseGame/UsedSprites/Back Loading.png";
        private const string MascotPath =
            "Assets/BaseGame/Sprite/Logo/Adaptive/ic_capy_foreground.png";
        private const string LevelPath = "Assets/Texture2D/Level.png";
        private const string PlayButtonPath =
            "Assets/BaseGame/UsedSprites/btn.png";
        private const string TextMaterialPath =
            "Assets/Generated/HomeText_v1.mat";

        static HomeScreenBuilder()
        {
            EditorApplication.delayCall += BuildOnce;
        }

        [MenuItem("Foodie Sizzle/Giao diện/Dựng lại màn hình Home")]
        public static void RebuildManually()
        {
            Build(true);
        }

        private static void BuildOnce()
        {
            Build(false);
        }

        private static void Build(bool force)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling ||
                EditorApplication.isUpdating)
            {
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath) return;

            GameObject canvasObject = GameObject.Find("GameCanvas");
            GameUIManager uiManager =
                Object.FindFirstObjectByType<GameUIManager>();
            if (canvasObject == null || uiManager == null) return;

            Transform oldRoot = canvasObject.transform.Find(RootName);
            if (oldRoot != null && !force) return;
            if (oldRoot != null)
            {
                Object.DestroyImmediate(oldRoot.gameObject);
            }

            ImportAsSprite(BackgroundPath);
            ImportAsSprite(MascotPath);
            ImportAsSprite(LevelPath);
            ImportAsSprite(PlayButtonPath);

            Sprite background =
                AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
            Sprite mascot =
                AssetDatabase.LoadAssetAtPath<Sprite>(MascotPath);
            Sprite levelBadge =
                AssetDatabase.LoadAssetAtPath<Sprite>(LevelPath);
            Sprite playPanel =
                AssetDatabase.LoadAssetAtPath<Sprite>(PlayButtonPath);
            if (background == null || mascot == null ||
                levelBadge == null || playPanel == null)
            {
                Debug.LogError(
                    "[Foodie Sizzle] Thiếu sprite để dựng màn hình Home.");
                return;
            }

            TextMeshProUGUI referenceText = Object
                .FindObjectsByType<TextMeshProUGUI>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .FirstOrDefault(text => text.name == "TimerText");
            TMP_FontAsset font =
                referenceText != null ? referenceText.font : null;
            Material material = CreateTextMaterial(font);

            GameObject root = CreateUIObject(
                RootName,
                canvasObject.transform,
                typeof(Image));
            SetRect(root.GetComponent<RectTransform>(), 0f, 0f, 1f, 1f);
            Image rootImage = root.GetComponent<Image>();
            rootImage.sprite = background;
            rootImage.color = new Color(1f, 0.94f, 0.78f, 1f);
            rootImage.type = Image.Type.Simple;
            rootImage.raycastTarget = true;

            // Hai dải màu tạo chiều sâu nhưng vẫn giữ đúng tông vàng–kem.
            CreateColorBand(
                root.transform,
                "TopGlow",
                new Color(1f, 0.79f, 0.35f, 0.45f),
                0f, 0.73f, 1f, 1f);
            CreateColorBand(
                root.transform,
                "BottomTable",
                new Color(0.67f, 0.39f, 0.18f, 0.22f),
                0f, 0f, 1f, 0.30f);

            TextMeshProUGUI foodie = CreateText(
                "FoodieTitle", root.transform, font, material,
                "FOODIE", 88f, new Color(0.20f, 0.86f, 0.10f));
            SetRect(foodie.rectTransform, 0.10f, 0.805f, 0.90f, 0.90f);

            TextMeshProUGUI sizzle = CreateText(
                "SizzleTitle", root.transform, font, material,
                "SIZZLE", 88f, new Color(1f, 0.50f, 0.08f));
            SetRect(sizzle.rectTransform, 0.10f, 0.725f, 0.90f, 0.825f);

            Image mascotImage = CreateImage(
                "HomeMascot", root.transform, mascot,
                0.13f, 0.34f, 0.87f, 0.73f);
            mascotImage.preserveAspect = true;
            mascotImage.raycastTarget = false;

            Image badgeImage = CreateImage(
                "LevelBadge", root.transform, levelBadge,
                0.35f, 0.245f, 0.65f, 0.41f);
            badgeImage.preserveAspect = true;
            badgeImage.raycastTarget = false;

            TextMeshProUGUI levelText = CreateText(
                "HomeLevelText", badgeImage.transform, font, material,
                "LEVEL 1", 43f, Color.white);
            SetRect(levelText.rectTransform, 0.08f, 0.29f, 0.92f, 0.72f);
            levelText.enableAutoSizing = true;
            levelText.fontSizeMin = 25f;
            levelText.fontSizeMax = 43f;
            levelText.textWrappingMode = TextWrappingModes.NoWrap;

            Button playButton = CreateButton(
                "HomePlayButton", root.transform, playPanel,
                0.20f, 0.10f, 0.80f, 0.205f);
            TextMeshProUGUI playText = CreateText(
                "PlayText", playButton.transform, font, material,
                "PLAY", 64f, Color.white);
            SetRect(playText.rectTransform, 0f, 0.03f, 1f, 1f);
            playText.raycastTarget = false;

            Button pauseHome = FindChildRecursive(
                canvasObject.transform, "HomeButton")
                ?.GetComponent<Button>();
            uiManager.ConfigureHome(
                root, playButton, levelText, pauseHome);

            root.transform.SetAsLastSibling();
            root.SetActive(true);
            EditorUtility.SetDirty(uiManager);
            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log(
                "[Foodie Sizzle] Đã dựng Home và nối nút Play/Home.");
        }

        private static Material CreateTextMaterial(TMP_FontAsset font)
        {
            if (font == null || font.material == null) return null;

            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(TextMaterialPath);
            if (material == null)
            {
                material = new Material(font.material)
                {
                    name = "HomeText_v1"
                };
                AssetDatabase.CreateAsset(material, TextMaterialPath);
            }

            material.shader = font.material.shader;
            material.CopyPropertiesFromMaterial(font.material);
            material.EnableKeyword("OUTLINE_ON");
            material.SetColor(ShaderUtilities.ID_FaceColor, Color.white);
            material.SetColor(ShaderUtilities.ID_OutlineColor,
                new Color(0.18f, 0.08f, 0.025f));
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.18f);
            material.SetFloat(ShaderUtilities.ID_OutlineSoftness, 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreateUIObject(
            string name, Transform parent, params System.Type[] components)
        {
            GameObject gameObject = new GameObject(
                name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            foreach (System.Type component in components)
            {
                gameObject.AddComponent(component);
            }
            return gameObject;
        }

        private static Image CreateImage(
            string name, Transform parent, Sprite sprite,
            float minX, float minY, float maxX, float maxY)
        {
            GameObject gameObject = CreateUIObject(
                name, parent, typeof(Image));
            SetRect(gameObject.GetComponent<RectTransform>(),
                minX, minY, maxX, maxY);
            Image image = gameObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            return image;
        }

        private static void CreateColorBand(
            Transform parent, string name, Color color,
            float minX, float minY, float maxX, float maxY)
        {
            GameObject band = CreateUIObject(
                name, parent, typeof(Image));
            SetRect(band.GetComponent<RectTransform>(),
                minX, minY, maxX, maxY);
            Image image = band.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private static Button CreateButton(
            string name, Transform parent, Sprite sprite,
            float minX, float minY, float maxX, float maxY)
        {
            GameObject gameObject = CreateUIObject(
                name, parent, typeof(Image), typeof(Button));
            SetRect(gameObject.GetComponent<RectTransform>(),
                minX, minY, maxX, maxY);
            Image image = gameObject.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.color = Color.white;

            Button button = gameObject.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.94f);
            colors.pressedColor = new Color(0.86f, 0.94f, 0.82f, 1f);
            colors.fadeDuration = 0.06f;
            button.colors = colors;
            return button;
        }

        private static TextMeshProUGUI CreateText(
            string name, Transform parent, TMP_FontAsset font,
            Material material, string value, float size, Color color)
        {
            GameObject gameObject = CreateUIObject(
                name, parent, typeof(TextMeshProUGUI));
            TextMeshProUGUI text =
                gameObject.GetComponent<TextMeshProUGUI>();
            text.font = font;
            if (material != null) text.fontSharedMaterial = material;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }

        private static void SetRect(
            RectTransform rect,
            float minX, float minY, float maxX, float maxY)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static Transform FindChildRecursive(
            Transform root, string childName)
        {
            foreach (Transform child in root)
            {
                if (child.name == childName) return child;
                Transform nested =
                    FindChildRecursive(child, childName);
                if (nested != null) return nested;
            }
            return null;
        }

        private static void ImportAsSprite(string assetPath)
        {
            TextureImporter importer =
                AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;

            bool changed = importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Single;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            if (changed)
            {
                importer.SaveAndReimport();
            }
        }
    }
}
