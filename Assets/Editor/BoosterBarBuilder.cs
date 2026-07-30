using FoodieSizzle;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FoodieSizzle.EditorTools
{
    [InitializeOnLoad]
    public static class BoosterBarBuilder
    {
        private const string ScenePath = "Assets/Scenes/GameplayScene.unity";
        private const string BarName = "BoosterBar_v3";
        private const string ButtonBackgroundPath =
            "Assets/BaseGame/Sprite/_NewUI/Booster Button.png";
        private const string SolidButtonMaterialPath =
            "Assets/Generated/BoosterButtonSolidAlpha.mat";
        private const string BoxPath =
            "Assets/BaseGame/UsedSprites/box.png";
        private const string RefreshPath =
            "Assets/BaseGame/UsedSprites/refresh.png";
        private const string TimePath =
            "Assets/BaseGame/UsedSprites/time.png";
        private const string PlusPath =
            "Assets/BaseGame/UsedSprites/plus.png";

        static BoosterBarBuilder()
        {
            EditorApplication.update += BuildOnce;
        }

        [MenuItem("Foodie Sizzle/UI/Dựng lại thanh 4 vật phẩm")]
        public static void RebuildFromMenu()
        {
            Build(true);
        }

        private static void BuildOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling ||
                EditorApplication.isUpdating)
            {
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                return;

            Build(false);
            EditorApplication.update -= BuildOnce;
        }

        private static void Build(bool force)
        {
            GameObject canvas = GameObject.Find("GameCanvas");
            GameplayManager gameplay =
                Object.FindFirstObjectByType<GameplayManager>();
            if (canvas == null || gameplay == null) return;

            GameUIManager ui = gameplay.GetComponent<GameUIManager>();
            if (ui == null) return;

            Transform currentBar = FindChild(canvas.transform, BarName);
            if (currentBar != null && !force) return;

            for (int i = canvas.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = canvas.transform.GetChild(i);
                if (child.name.StartsWith("BoosterBar_"))
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            Sprite buttonBackground =
                AssetDatabase.LoadAssetAtPath<Sprite>(ButtonBackgroundPath);
            Material solidButtonMaterial = GetOrCreateSolidButtonMaterial();
            Sprite box = AssetDatabase.LoadAssetAtPath<Sprite>(BoxPath);
            Sprite refresh =
                AssetDatabase.LoadAssetAtPath<Sprite>(RefreshPath);
            Sprite time = AssetDatabase.LoadAssetAtPath<Sprite>(TimePath);
            Sprite plus = AssetDatabase.LoadAssetAtPath<Sprite>(PlusPath);
            if (buttonBackground == null || solidButtonMaterial == null ||
                box == null || refresh == null || time == null || plus == null)
            {
                Debug.LogWarning(
                    "[Foodie Sizzle] Thiếu một trong bốn icon booster.");
                return;
            }

            TextMeshProUGUI sampleText =
                FindChild(canvas.transform, "LevelText")
                    ?.GetComponent<TextMeshProUGUI>();

            RectTransform bar = CreateRect(
                BarName,
                canvas.transform,
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 52f),
                new Vector2(0f, 156f));

            BoosterButton boxButton = CreateBooster(
                bar, "BoxBooster", buttonBackground, solidButtonMaterial,
                box, 0.20f, sampleText);
            BoosterButton refreshButton = CreateBooster(
                bar, "RefreshBooster", buttonBackground, solidButtonMaterial,
                refresh, 0.40f, sampleText);
            BoosterButton timeButton = CreateBooster(
                bar, "TimeBooster", buttonBackground, solidButtonMaterial,
                time, 0.60f, sampleText);
            BoosterButton plusButton = CreateBooster(
                bar, "PlusBooster", buttonBackground, solidButtonMaterial,
                plus, 0.80f, sampleText);

            ui.ConfigureBoosters(
                boxButton.button,
                boxButton.count,
                refreshButton.button,
                refreshButton.count,
                timeButton.button,
                timeButton.count,
                plusButton.button,
                plusButton.count);

            MoveBeforePopups(bar, canvas.transform);
            EditorUtility.SetDirty(ui);
            EditorUtility.SetDirty(canvas);
            Scene scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log(
                "[Foodie Sizzle] Đã dựng và nối thanh 4 vật phẩm.");
        }

        private static BoosterButton CreateBooster(
            Transform parent,
            string name,
            Sprite backgroundSprite,
            Material backgroundMaterial,
            Sprite iconSprite,
            float anchorX,
            TextMeshProUGUI sampleText)
        {
            RectTransform root = CreateRect(
                name,
                parent,
                new Vector2(anchorX, 0.5f),
                new Vector2(anchorX, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(138f, 138f));

            Image background = root.gameObject.AddComponent<Image>();
            background.sprite = backgroundSprite;
            background.material = backgroundMaterial;
            background.type = Image.Type.Simple;
            background.preserveAspect = true;
            background.color = new Color(1f, 0.91f, 0.70f, 0.96f);

            Outline outline = root.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.46f, 0.29f, 0.18f, 0.70f);
            outline.effectDistance = new Vector2(2.5f, -2.5f);
            outline.useGraphicAlpha = true;

            Shadow shadow = root.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.25f, 0.13f, 0.06f, 0.30f);
            shadow.effectDistance = new Vector2(0f, -6f);
            shadow.useGraphicAlpha = true;

            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.navigation = new Navigation
            {
                mode = Navigation.Mode.None
            };
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.86f, 0.86f, 0.86f, 1f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.42f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            RectTransform icon = CreateRect(
                "Icon",
                root,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(92f, 92f));
            Image iconImage = icon.gameObject.AddComponent<Image>();
            iconImage.sprite = iconSprite;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            RectTransform badge = CreateRect(
                "CountBadge",
                root,
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-4f, 5f),
                new Vector2(52f, 52f));
            Image badgeImage = badge.gameObject.AddComponent<Image>();
            badgeImage.sprite = backgroundSprite;
            badgeImage.material = backgroundMaterial;
            badgeImage.type = Image.Type.Simple;
            badgeImage.preserveAspect = true;
            badgeImage.color = new Color(0.38f, 0.75f, 0.20f, 1f);
            badgeImage.raycastTarget = false;

            RectTransform countRect = CreateRect(
                "CountText",
                badge,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            TextMeshProUGUI count =
                countRect.gameObject.AddComponent<TextMeshProUGUI>();
            count.text = "3";
            count.alignment = TextAlignmentOptions.Center;
            count.fontSize = 32f;
            count.fontStyle = FontStyles.Bold;
            count.color = Color.white;
            count.raycastTarget = false;
            if (sampleText != null)
            {
                count.font = sampleText.font;
            }

            return new BoosterButton
            {
                button = button,
                count = count
            };
        }

        private static Material GetOrCreateSolidButtonMaterial()
        {
            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(
                    SolidButtonMaterialPath);
            if (material != null) return material;

            Shader shader = Shader.Find(
                "FoodieSizzle/UI Solid Alpha Tint");
            if (shader == null) return null;

            EnsureFolder("Assets/Generated");
            material = new Material(shader)
            {
                name = "BoosterButtonSolidAlpha"
            };
            AssetDatabase.CreateAsset(material, SolidButtonMaterialPath);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }

        private static RectTransform CreateRect(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            GameObject go = new GameObject(
                name,
                typeof(RectTransform));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            rect.localScale = Vector3.one;
            return rect;
        }

        private static void MoveBeforePopups(
            Transform bar,
            Transform canvas)
        {
            int popupIndex = canvas.childCount;
            for (int i = 0; i < canvas.childCount; i++)
            {
                string childName = canvas.GetChild(i).name;
                if (childName.Contains("Pause") ||
                    childName.Contains("Result"))
                {
                    popupIndex = Mathf.Min(popupIndex, i);
                }
            }
            bar.SetSiblingIndex(Mathf.Clamp(popupIndex, 0, canvas.childCount - 1));
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform result = FindChild(root.GetChild(i), name);
                if (result != null) return result;
            }
            return null;
        }

        private struct BoosterButton
        {
            public Button button;
            public TextMeshProUGUI count;
        }
    }
}
