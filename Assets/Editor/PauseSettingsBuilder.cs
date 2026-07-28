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
    /// Bổ sung ba công tắc vào bảng Pause hiện có mà không dựng lại bố cục.
    /// </summary>
    [InitializeOnLoad]
    public static class PauseSettingsBuilder
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const string MarkerName = "_PauseSettings_v1";
        private const string OnPath =
            "Assets/BaseGame/UsedSprites/pause_toggle_on.png";
        private const string OffPath =
            "Assets/BaseGame/UsedSprites/pause_toggle_off.png";
        private const string KnobPath =
            "Assets/BaseGame/UsedSprites/pause_toggle_knob.png";

        static PauseSettingsBuilder()
        {
            EditorApplication.update += BuildOnce;
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
            {
                return;
            }

            GameObject pausePopup = FindSceneObject("PausePopup");
            Transform card = pausePopup == null
                ? null
                : FindChild(pausePopup.transform, "Card");
            if (card == null || FindChild(card, MarkerName) != null)
            {
                EditorApplication.update -= BuildOnce;
                return;
            }

            Sprite onSprite = AssetDatabase.LoadAssetAtPath<Sprite>(OnPath);
            Sprite offSprite = AssetDatabase.LoadAssetAtPath<Sprite>(OffPath);
            Sprite knobSprite = AssetDatabase.LoadAssetAtPath<Sprite>(KnobPath);
            TextMeshProUGUI title = FindChild(card, "PauseTitle")
                ?.GetComponent<TextMeshProUGUI>();

            if (onSprite == null || offSprite == null ||
                knobSprite == null || title == null)
            {
                return;
            }

            Toggle music = CreateSettingRow(
                card, "MusicSetting", "Âm nhạc",
                new Vector2(0.16f, 0.62f),
                new Vector2(0.84f, 0.72f),
                title, onSprite, offSprite, knobSprite);
            Toggle sound = CreateSettingRow(
                card, "SoundSetting", "Âm thanh",
                new Vector2(0.16f, 0.50f),
                new Vector2(0.84f, 0.60f),
                title, onSprite, offSprite, knobSprite);
            Toggle vibration = CreateSettingRow(
                card, "VibrationSetting", "Rung",
                new Vector2(0.16f, 0.38f),
                new Vector2(0.84f, 0.48f),
                title, onSprite, offSprite, knobSprite);

            GameplayManager gameplay =
                Object.FindFirstObjectByType<GameplayManager>();
            GameSettingsManager settings =
                gameplay.GetComponent<GameSettingsManager>();
            if (settings == null)
            {
                settings = gameplay.gameObject.AddComponent<GameSettingsManager>();
            }
            settings.Configure(music, sound, vibration);

            Transform homeTransform = FindChild(card, "HomeButton");
            if (homeTransform != null)
            {
                Button homeButton = homeTransform.GetComponent<Button>();
                if (homeButton != null)
                {
                    homeButton.interactable = false;
                }

                Image[] homeImages =
                    homeTransform.GetComponentsInChildren<Image>(true);
                foreach (Image image in homeImages)
                {
                    Color color = image.color;
                    color.a *= 0.52f;
                    image.color = color;
                }
            }

            GameObject marker = new GameObject(MarkerName);
            marker.transform.SetParent(card, false);
            marker.hideFlags = HideFlags.HideInHierarchy;

            EditorUtility.SetDirty(settings);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            EditorApplication.update -= BuildOnce;
            Debug.Log(
                "[Foodie Sizzle] Đã thêm Âm nhạc, Âm thanh và Rung vào Pause.");
        }

        private static Toggle CreateSettingRow(
            Transform parent,
            string name,
            string labelValue,
            Vector2 anchorMin,
            Vector2 anchorMax,
            TextMeshProUGUI styleSource,
            Sprite onSprite,
            Sprite offSprite,
            Sprite knobSprite)
        {
            GameObject row = new GameObject(name, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            RectTransform rowRect = row.GetComponent<RectTransform>();
            SetAnchors(rowRect, anchorMin, anchorMax);

            GameObject labelObject = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(row.transform, false);
            RectTransform labelRect =
                labelObject.GetComponent<RectTransform>();
            SetAnchors(
                labelRect,
                new Vector2(0f, 0f),
                new Vector2(0.58f, 1f));

            TextMeshProUGUI label =
                labelObject.GetComponent<TextMeshProUGUI>();
            label.font = styleSource.font;
            label.fontSharedMaterial = styleSource.fontSharedMaterial;
            label.text = labelValue;
            label.fontSize = 34f;
            label.color = new Color(0.43f, 0.22f, 0.17f, 1f);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.raycastTarget = false;
            label.UpdateMeshPadding();

            GameObject toggleObject = new GameObject(
                "Toggle",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Toggle),
                typeof(SettingsToggleVisual));
            toggleObject.transform.SetParent(row.transform, false);
            RectTransform toggleRect =
                toggleObject.GetComponent<RectTransform>();
            SetAnchors(
                toggleRect,
                new Vector2(0.60f, 0.08f),
                new Vector2(1f, 0.92f));

            Image track = toggleObject.GetComponent<Image>();
            track.sprite = onSprite;
            track.preserveAspect = false;

            Toggle toggle = toggleObject.GetComponent<Toggle>();
            toggle.targetGraphic = track;
            toggle.graphic = null;
            toggle.isOn = true;

            GameObject knobObject = new GameObject(
                "Knob",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            knobObject.transform.SetParent(toggleObject.transform, false);
            RectTransform knobRect =
                knobObject.GetComponent<RectTransform>();
            knobRect.anchorMin = new Vector2(0.5f, 0.5f);
            knobRect.anchorMax = new Vector2(0.5f, 0.5f);
            knobRect.sizeDelta = new Vector2(62f, 62f);
            knobRect.anchoredPosition = new Vector2(25f, 0f);

            Image knobImage = knobObject.GetComponent<Image>();
            knobImage.sprite = knobSprite;
            knobImage.preserveAspect = true;
            knobImage.raycastTarget = false;

            SettingsToggleVisual visual =
                toggleObject.GetComponent<SettingsToggleVisual>();
            visual.Configure(
                track, knobRect, onSprite, offSprite);

            return toggle;
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

        private static Transform FindChild(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform result = FindChild(root.GetChild(i), name);
                if (result != null) return result;
            }
            return null;
        }

        private static GameObject FindSceneObject(string name)
        {
            foreach (GameObject candidate in
                     Resources.FindObjectsOfTypeAll<GameObject>())
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
