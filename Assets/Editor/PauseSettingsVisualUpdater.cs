using FoodieSizzle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FoodieSizzle.EditorTools
{
    /// <summary>
    /// Thay bộ sprite tạm bằng bộ công tắc được thiết kế riêng cho Pause.
    /// </summary>
    [InitializeOnLoad]
    public static class PauseSettingsVisualUpdater
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const string MarkerName = "_PauseSettingsVisual_v10";
        private const string OnPath =
            "Assets/BaseGame/UsedSprites/pause_toggle_on.png";
        private const string OffPath =
            "Assets/BaseGame/UsedSprites/pause_toggle_off.png";
        private const string KnobPath =
            "Assets/BaseGame/UsedSprites/pause_toggle_knob.png";
        private const string MusicIconPath =
            "Assets/BaseGame/UsedSprites/pause_icon_music.png";
        private const string SoundIconPath =
            "Assets/BaseGame/UsedSprites/pause_icon_sound.png";
        private const string VibrationIconPath =
            "Assets/BaseGame/UsedSprites/pause_icon_vibration.png";

        static PauseSettingsVisualUpdater()
        {
            EditorApplication.update += UpdateOnce;
        }

        private static void UpdateOnce()
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

            ImportAsSprite(OnPath);
            ImportAsSprite(OffPath);
            ImportAsSprite(KnobPath);
            ImportAsSprite(MusicIconPath);
            ImportAsSprite(SoundIconPath);
            ImportAsSprite(VibrationIconPath);

            GameObject pausePopup = FindSceneObject("PausePopup");
            Transform card = pausePopup == null
                ? null
                : FindChild(pausePopup.transform, "Card");
            if (card == null || FindChild(card, MarkerName) != null)
            {
                EditorApplication.update -= UpdateOnce;
                return;
            }

            Transform music = FindChild(card, "MusicSetting");
            Transform sound = FindChild(card, "SoundSetting");
            Transform vibration = FindChild(card, "VibrationSetting");
            if (music == null || sound == null || vibration == null)
            {
                return;
            }

            Sprite onSprite = AssetDatabase.LoadAssetAtPath<Sprite>(OnPath);
            Sprite offSprite = AssetDatabase.LoadAssetAtPath<Sprite>(OffPath);
            Sprite knobSprite = AssetDatabase.LoadAssetAtPath<Sprite>(KnobPath);
            Sprite musicIcon =
                AssetDatabase.LoadAssetAtPath<Sprite>(MusicIconPath);
            Sprite soundIcon =
                AssetDatabase.LoadAssetAtPath<Sprite>(SoundIconPath);
            Sprite vibrationIcon =
                AssetDatabase.LoadAssetAtPath<Sprite>(VibrationIconPath);
            if (onSprite == null || offSprite == null || knobSprite == null ||
                musicIcon == null || soundIcon == null ||
                vibrationIcon == null)
            {
                return;
            }

            UpdateToggle(music, onSprite, offSprite, knobSprite);
            UpdateToggle(sound, onSprite, offSprite, knobSprite);
            UpdateToggle(vibration, onSprite, offSprite, knobSprite);
            UpdateRow(
                music,
                new Vector2(0.14f, 0.61f),
                new Vector2(0.86f, 0.715f),
                card,
                musicIcon);
            UpdateRow(
                sound,
                new Vector2(0.14f, 0.50f),
                new Vector2(0.86f, 0.605f),
                card,
                soundIcon);
            UpdateRow(
                vibration,
                new Vector2(0.14f, 0.39f),
                new Vector2(0.86f, 0.495f),
                card,
                vibrationIcon);
            FinishPausePanel(card);

            GameObject marker = new GameObject(MarkerName);
            marker.transform.SetParent(card, false);
            marker.hideFlags = HideFlags.HideInHierarchy;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            EditorApplication.update -= UpdateOnce;
            Debug.Log(
                "[Foodie Sizzle] Đã thay ba công tắc Pause bằng bộ sprite mới.");
        }

        private static void UpdateToggle(
            Transform row,
            Sprite onSprite,
            Sprite offSprite,
            Sprite knobSprite)
        {
            Transform toggleTransform = FindChild(row, "Toggle");
            Transform knobTransform = FindChild(row, "Knob");
            if (toggleTransform == null || knobTransform == null)
            {
                return;
            }

            Toggle toggle = toggleTransform.GetComponent<Toggle>();
            Image track = toggleTransform.GetComponent<Image>();
            Image knobImage = knobTransform.GetComponent<Image>();
            SettingsToggleVisual visual =
                toggleTransform.GetComponent<SettingsToggleVisual>();

            track.sprite = toggle != null && toggle.isOn
                ? onSprite
                : offSprite;
            track.color = Color.white;
            track.preserveAspect = false;

            knobImage.sprite = knobSprite;
            knobImage.color = Color.white;
            knobImage.preserveAspect = true;

            RectTransform knobRect =
                knobTransform.GetComponent<RectTransform>();
            knobRect.anchorMin = new Vector2(0.5f, 0.5f);
            knobRect.anchorMax = new Vector2(0.5f, 0.5f);

            RectTransform toggleRect =
                toggleTransform.GetComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(0.63f, 0.03f);
            toggleRect.anchorMax = new Vector2(1f, 0.99f);
            toggleRect.offsetMin = Vector2.zero;
            toggleRect.offsetMax = Vector2.zero;

            visual.Configure(
                track,
                knobRect,
                onSprite,
                offSprite,
                0f,
                8f);
        }

        private static void UpdateRow(
            Transform row,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Transform card,
            Sprite iconSprite)
        {
            RectTransform rowRect = row.GetComponent<RectTransform>();
            SetAnchors(rowRect, anchorMin, anchorMax);

            Transform labelTransform = FindChild(row, "Label");
            Transform titleTransform = FindChild(card, "PauseTitle");
            if (labelTransform == null || titleTransform == null)
            {
                return;
            }

            RectTransform labelRect =
                labelTransform.GetComponent<RectTransform>();
            SetAnchors(
                labelRect,
                new Vector2(0.13f, 0f),
                new Vector2(0.60f, 1f));

            TMPro.TextMeshProUGUI label =
                labelTransform.GetComponent<TMPro.TextMeshProUGUI>();
            TMPro.TextMeshProUGUI title =
                titleTransform.GetComponent<TMPro.TextMeshProUGUI>();
            label.font = title.font;
            label.fontSharedMaterial = title.fontSharedMaterial;
            label.fontSize = 58f;
            label.color = Color.white;
            label.alignment = TMPro.TextAlignmentOptions.MidlineRight;
            label.margin = new Vector4(0f, 0f, 4f, 0f);
            label.UpdateMeshPadding();
            EditorUtility.SetDirty(label);

            Transform iconTransform = FindChild(row, "SettingIcon");
            if (iconTransform == null)
            {
                GameObject iconObject = new GameObject(
                    "SettingIcon",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                iconObject.transform.SetParent(row, false);
                iconTransform = iconObject.transform;
            }

            RectTransform iconRect =
                iconTransform.GetComponent<RectTransform>();
            if (row.name == "MusicSetting")
            {
                // Nốt nhạc hẹp nhưng sprite gốc đã khá cao:
                // thu chiều rộng khung để diện tích nhìn không lấn chữ.
                SetAnchors(
                    iconRect,
                    new Vector2(0.031f, 0.10f),
                    new Vector2(0.109f, 0.90f));
            }
            else if (row.name == "SoundSetting")
            {
                // Loa rộng tự nhiên nên khung lớn hơn nốt nhạc một chút.
                SetAnchors(
                    iconRect,
                    new Vector2(0.014f, 0.15f),
                    new Vector2(0.126f, 0.85f));
            }
            else
            {
                // Canvas trong suốt của icon rung đã được cắt gọn.
                SetAnchors(
                    iconRect,
                    new Vector2(0.005f, 0.13f),
                    new Vector2(0.135f, 0.87f));
            }

            Image icon = iconTransform.GetComponent<Image>();
            icon.sprite = iconSprite;
            icon.color = Color.white;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            iconTransform.SetSiblingIndex(0);
        }

        private static void FinishPausePanel(Transform card)
        {
            Transform titleTransform = FindChild(card, "PauseTitle");
            if (titleTransform != null)
            {
                TMPro.TextMeshProUGUI title =
                    titleTransform.GetComponent<TMPro.TextMeshProUGUI>();
                title.fontSize = 80f;
                title.UpdateMeshPadding();
                EditorUtility.SetDirty(title);
            }

            SetButtonWidth(card, "PauseRestartButton", 0.245f, 0.755f);
            SetButtonWidth(card, "HomeButton", 0.245f, 0.755f);

            Transform homeTransform = FindChild(card, "HomeButton");
            if (homeTransform == null) return;

            Button homeButton = homeTransform.GetComponent<Button>();
            if (homeButton != null)
            {
                homeButton.interactable = false;
            }

            foreach (Image image in
                     homeTransform.GetComponentsInChildren<Image>(true))
            {
                Color color = image.color;
                color.a = 0.65f;
                image.color = color;
                EditorUtility.SetDirty(image);
            }
        }

        private static void SetButtonWidth(
            Transform card,
            string buttonName,
            float minimumX,
            float maximumX)
        {
            Transform buttonTransform = FindChild(card, buttonName);
            if (buttonTransform == null) return;

            RectTransform rect =
                buttonTransform.GetComponent<RectTransform>();
            Vector2 anchorMin = rect.anchorMin;
            Vector2 anchorMax = rect.anchorMax;
            anchorMin.x = minimumX;
            anchorMax.x = maximumX;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = new Vector2(
                0f,
                rect.anchoredPosition.y);
            rect.sizeDelta = new Vector2(
                0f,
                rect.sizeDelta.y);
            EditorUtility.SetDirty(rect);
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

        private static void ImportAsSprite(string path)
        {
            TextureImporter importer =
                AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            if (importer.textureType == TextureImporterType.Sprite &&
                importer.spriteImportMode == SpriteImportMode.Single &&
                importer.alphaIsTransparency)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression =
                TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
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
