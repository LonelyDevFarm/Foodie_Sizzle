using FoodieSizzle;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ResultPopupRebuilder
{
    private const string MarkerName = "_ResultPopupStyle_v5";
    private const string WinRibbonPath =
        "Assets/BaseGame/Sprite/_NewUI2/WinLose/Ribbon.png";
    private const string LoseRibbonPath =
        "Assets/BaseGame/Sprite/_NewUI2/WinLose/Ribbon2.png";
    private const string ContinueButtonPath =
        "Assets/BaseGame/UsedSprites/btn.png";
    private const string RetryButtonPath =
        "Assets/BaseGame/UsedSprites/pause_button_restart.png";
    private const string HomeButtonPath =
        "Assets/BaseGame/UsedSprites/pause_button_home.png";
    private const string ContinueIconPath =
        "Assets/BaseGame/UsedSprites/Continue.png";
    private const string RetryIconPath =
        "Assets/BaseGame/UsedSprites/Retry.png";
    private const string HomeIconPath =
        "Assets/BaseGame/UsedSprites/Home.png";
    private const string WinCharacterPath =
        "Assets/BaseGame/GeneratedCharacters/Chef_Win.png";
    private const string LoseCharacterPath =
        "Assets/BaseGame/GeneratedCharacters/Chef_Lose_HeartMinus1.png";

    static ResultPopupRebuilder()
    {
        EditorApplication.delayCall += RebuildOnce;
    }

    [MenuItem("Foodie Sizzle/Giao diện/Làm lại bảng Thắng Thua")]
    public static void RebuildManually()
    {
        Rebuild(true);
    }

    private static void RebuildOnce()
    {
        Rebuild(false);
    }

    private static void Rebuild(bool force)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        GameUIManager manager =
            Object.FindFirstObjectByType<GameUIManager>(
                FindObjectsInactive.Include);
        if (manager == null || !manager.gameObject.scene.IsValid())
        {
            return;
        }

        if (!force && manager.transform.Find(MarkerName) != null)
        {
            return;
        }

        SerializedObject managerData = new SerializedObject(manager);
        GameObject resultPopup =
            GetReference<GameObject>(managerData, "resultPopup");
        if (resultPopup == null)
        {
            Debug.LogError("Không tìm thấy ResultPopup để dựng lại.");
            return;
        }

        Transform card = resultPopup.transform.Find("Card");
        if (card == null)
        {
            Debug.LogError("ResultPopup không có Card.");
            return;
        }

        Image overlay = resultPopup.GetComponent<Image>();
        if (overlay != null)
        {
            overlay.color = new Color(0.01f, 0.006f, 0.002f, 0.94f);
        }

        Undo.RegisterFullObjectHierarchyUndo(
            resultPopup,
            "Làm lại bảng Thắng Thua");

        for (int index = card.childCount - 1; index >= 0; index--)
        {
            Object.DestroyImmediate(card.GetChild(index).gameObject);
        }

        RectTransform cardRect = card.GetComponent<RectTransform>();
        SetRect(cardRect, 0.04f, 0.17f, 0.96f, 0.83f);
        Canvas.ForceUpdateCanvases();

        Button pauseRestart =
            GetReference<Button>(managerData, "pauseRestartButton");
        GameObject pausePopup =
            GetReference<GameObject>(managerData, "pausePopup");
        RectTransform pauseRestartRect = pauseRestart != null
            ? pauseRestart.GetComponent<RectTransform>()
            : null;
        RectTransform pauseHomeRect = pausePopup != null
            ? FindChildRecursive(pausePopup.transform, "HomeButton")
                ?.GetComponent<RectTransform>()
            : null;

        Vector2 primaryButtonSize = pauseRestartRect != null
            ? pauseRestartRect.rect.size
            : new Vector2(428f, 225f);
        Vector2 homeButtonSize = pauseHomeRect != null
            ? pauseHomeRect.rect.size
            : new Vector2(428f, 232f);
        float pauseButtonCenterDistance =
            pauseRestartRect != null && pauseHomeRect != null
                ? Mathf.Abs(
                    GetCenterYInParent(pauseRestartRect) -
                    GetCenterYInParent(pauseHomeRect))
                : 217f;
        float resultCardHeight = Mathf.Max(1f, cardRect.rect.height);
        float homeButtonAnchorY = 0.15f;
        float primaryButtonAnchorY =
            homeButtonAnchorY +
            pauseButtonCenterDistance / resultCardHeight;

        Image cardImage = card.GetComponent<Image>();
        if (cardImage != null)
        {
            cardImage.color = Color.clear;
            cardImage.raycastTarget = false;
        }
        Outline oldOutline = card.GetComponent<Outline>();
        if (oldOutline != null)
        {
            Object.DestroyImmediate(oldOutline);
        }

        Sprite winRibbon =
            AssetDatabase.LoadAssetAtPath<Sprite>(WinRibbonPath);
        Sprite loseRibbon =
            AssetDatabase.LoadAssetAtPath<Sprite>(LoseRibbonPath);
        Sprite continueButton =
            AssetDatabase.LoadAssetAtPath<Sprite>(ContinueButtonPath);
        Sprite retryButton =
            AssetDatabase.LoadAssetAtPath<Sprite>(RetryButtonPath);
        Sprite homeButtonSprite =
            AssetDatabase.LoadAssetAtPath<Sprite>(HomeButtonPath);
        Sprite continueIcon =
            AssetDatabase.LoadAssetAtPath<Sprite>(ContinueIconPath);
        Sprite retryIcon =
            AssetDatabase.LoadAssetAtPath<Sprite>(RetryIconPath);
        Sprite homeIcon =
            AssetDatabase.LoadAssetAtPath<Sprite>(HomeIconPath);
        ImportAsSprite(WinCharacterPath);
        ImportAsSprite(LoseCharacterPath);
        Sprite winCharacter =
            AssetDatabase.LoadAssetAtPath<Sprite>(WinCharacterPath);
        Sprite loseCharacter =
            AssetDatabase.LoadAssetAtPath<Sprite>(LoseCharacterPath);

        TMP_FontAsset font = GetHudFont(managerData);
        Material titleMaterial = CreateOutlinedMaterial(font);

        Image ribbon = CreateImage(
            card,
            "ResultRibbon",
            winRibbon,
            new Color(1f, 1f, 1f, 1f),
            0.01f, 0.70f, 0.99f, 0.94f);
        ribbon.preserveAspect = true;

        TextMeshProUGUI title = CreateText(
            ribbon.transform,
            "ResultTitle",
            "CHIẾN THẮNG",
            font,
            titleMaterial,
            78f,
            Color.white,
            0.08f, 0.16f, 0.92f, 0.86f);
        title.fontStyle = FontStyles.Bold;
        title.enableAutoSizing = true;
        title.fontSizeMin = 45f;
        title.fontSizeMax = 78f;
        title.textWrappingMode = TextWrappingModes.NoWrap;

        Image character = CreateImage(
            card,
            "ResultCharacter",
            winCharacter,
            Color.white,
            0.16f, 0.34f, 0.84f, 0.70f);
        character.preserveAspect = true;

        Button primary = CreateButton(
            card,
            "ResultPrimaryButton",
            continueButton,
            0.245f, 0.22f, 0.755f, 0.345f);
        SetFixedRect(
            primary.GetComponent<RectTransform>(),
            new Vector2(0.5f, primaryButtonAnchorY),
            primaryButtonSize);
        Image primaryIcon = CreateImage(
            primary.transform,
            "PrimaryIcon",
            continueIcon,
            Color.white,
            0.39f, 0.16f, 0.61f, 0.84f);
        primaryIcon.preserveAspect = true;

        Button home = CreateButton(
            card,
            "ResultHomeButton",
            homeButtonSprite,
            0.245f, 0.085f, 0.755f, 0.21f);
        SetFixedRect(
            home.GetComponent<RectTransform>(),
            new Vector2(0.5f, homeButtonAnchorY),
            homeButtonSize);
        Image homeImage = CreateImage(
            home.transform,
            "HomeIcon",
            homeIcon,
            new Color(1f, 1f, 1f, 0.82f),
            0.39f, 0.16f, 0.61f, 0.84f);
        homeImage.preserveAspect = true;
        home.interactable = false;

        manager.ConfigureResultVisuals(
            title,
            null,
            ribbon,
            winRibbon,
            loseRibbon,
            character,
            winCharacter,
            loseCharacter,
            primary,
            primary.GetComponent<Image>(),
            primaryIcon,
            continueButton,
            retryButton,
            continueIcon,
            retryIcon,
            home);

        GameObject marker = new GameObject(MarkerName);
        marker.hideFlags = HideFlags.HideInHierarchy;
        marker.transform.SetParent(manager.transform, false);

        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(resultPopup);
        EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        EditorSceneManager.SaveScene(manager.gameObject.scene);
        Debug.Log(
            "Đã dựng lại bảng Thắng/Thua với banner, nền tối và hai nút chức năng.");
    }

    private static void ImportAsSprite(string assetPath)
    {
        AssetDatabase.ImportAsset(
            assetPath,
            ImportAssetOptions.ForceSynchronousImport);

        TextureImporter importer =
            AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        bool changed =
            importer.textureType != TextureImporterType.Sprite ||
            !importer.alphaIsTransparency;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;

        if (changed)
        {
            importer.SaveAndReimport();
        }
    }

    private static TMP_FontAsset GetHudFont(SerializedObject managerData)
    {
        TextMeshProUGUI timer =
            GetReference<TextMeshProUGUI>(managerData, "timerText");
        return timer != null ? timer.font : null;
    }

    private static Material CreateOutlinedMaterial(TMP_FontAsset font)
    {
        if (font == null) return null;

        string folder = "Assets/Generated";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder("Assets", "Generated");
        }

        string path = folder + "/ResultTitleOutline.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(font.material);
            material.name = "ResultTitleOutline";
            AssetDatabase.CreateAsset(material, path);
        }

        material.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.22f, 0.08f, 0.04f, 1f));
        material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.24f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Image CreateImage(
        Transform parent,
        string name,
        Sprite sprite,
        Color color,
        float minX, float minY,
        float maxX, float maxY)
    {
        GameObject gameObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        gameObject.transform.SetParent(parent, false);
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        SetRect(rect, minX, minY, maxX, maxY);

        Image image = gameObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TextMeshProUGUI CreateText(
        Transform parent,
        string name,
        string content,
        TMP_FontAsset font,
        Material material,
        float fontSize,
        Color color,
        float minX, float minY,
        float maxX, float maxY)
    {
        GameObject gameObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        gameObject.transform.SetParent(parent, false);
        SetRect(
            gameObject.GetComponent<RectTransform>(),
            minX, minY, maxX, maxY);

        TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();
        text.text = content;
        text.font = font;
        text.fontSharedMaterial = material;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(
        Transform parent,
        string name,
        Sprite sprite,
        float minX, float minY,
        float maxX, float maxY)
    {
        GameObject gameObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        gameObject.transform.SetParent(parent, false);
        SetRect(
            gameObject.GetComponent<RectTransform>(),
            minX, minY, maxX, maxY);

        Image image = gameObject.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.preserveAspect = false;

        Button button = gameObject.GetComponent<Button>();
        button.targetGraphic = image;
        return button;
    }

    private static void SetRect(
        RectTransform rect,
        float minX, float minY,
        float maxX, float maxY)
    {
        rect.anchorMin = new Vector2(minX, minY);
        rect.anchorMax = new Vector2(maxX, maxY);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void SetFixedRect(
        RectTransform rect,
        Vector2 anchor,
        Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static float GetCenterYInParent(RectTransform rect)
    {
        RectTransform parent = rect.parent as RectTransform;
        if (parent == null)
        {
            return rect.anchoredPosition.y;
        }

        float anchorY = (rect.anchorMin.y + rect.anchorMax.y) * 0.5f;
        return (anchorY - parent.pivot.y) * parent.rect.height +
            rect.anchoredPosition.y;
    }

    private static Transform FindChildRecursive(
        Transform parent,
        string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                return child;
            }

            Transform nested = FindChildRecursive(child, childName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private static T GetReference<T>(
        SerializedObject data,
        string propertyName)
        where T : Object
    {
        SerializedProperty property = data.FindProperty(propertyName);
        return property != null
            ? property.objectReferenceValue as T
            : null;
    }
}
