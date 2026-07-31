using System.Linq;
using FoodieSizzle;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class OrderUIBuilder
{
    private const string GameplayScenePath = "Assets/Scenes/GameplayScene.unity";
    private const string PrefabPath = "Assets/Prefabs/OrderPanel.prefab";
    private const string RootName = "OrderPanel";
    private const string LegacyRootPrefix = "OrderUIRoot_";
    private const string BubblePath =
        "Assets/BaseGame/GeneratedOrderUI/Order_SpeechBubble_Cream.png";
    private const string CapyAtlasPath =
        "Assets/BaseGame/UsedSprites/sactx-0-4096x4096-Crunch-CapyLift-8132215d.png";
    private const string CapySpriteName =
        "sactx-0-4096x4096-Crunch-CapyLift-8132215d_21";
    private const string ClosePath = "Assets/Texture2D/icon_close.png";

    static OrderUIBuilder()
    {
        EditorApplication.delayCall += SetupOnce;
    }

    [MenuItem("Foodie Sizzle/Giao diện/Đặt Order Panel prefab vào scene")]
    public static void PlacePrefabManually()
    {
        SetupScene(true);
    }

    [MenuItem("Foodie Sizzle/Giao diện/Mở Order Panel prefab")]
    public static void OpenPrefab()
    {
        GameObject prefab = EnsurePrefabExists();
        if (prefab != null) AssetDatabase.OpenAsset(prefab);
    }

    private static void SetupOnce()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path !=
            GameplayScenePath)
        {
            return;
        }

        SetupScene(false);
    }

    private static void SetupScene(bool replaceExistingInstance)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        GameObject canvasObject = GameObject.Find("GameCanvas");
        if (canvasObject == null)
        {
            Debug.LogWarning("Không tìm thấy GameCanvas để đặt Order Panel.");
            return;
        }

        GameObject prefab = EnsurePrefabExists();
        if (prefab == null) return;

        Transform existing = canvasObject.transform.Find(RootName);
        bool hasLegacyRoot = FindLegacyRoot(canvasObject.transform) != null;
        if (existing != null && !replaceExistingInstance && !hasLegacyRoot) return;

        RemoveOrderRoots(canvasObject.transform);

        GameObject instance = PrefabUtility.InstantiatePrefab(
            prefab,
            canvasObject.transform) as GameObject;
        if (instance == null) return;

        instance.name = RootName;
        SetRect(instance.GetComponent<RectTransform>(), 0.10f, 0.745f, 0.90f, 0.855f);

        EditorUtility.SetDirty(instance);
        EditorSceneManager.MarkSceneDirty(canvasObject.scene);
        EditorSceneManager.SaveScene(canvasObject.scene);
        Debug.Log("Đã đặt OrderPanel prefab vào GameCanvas. Chỉnh visual bằng Prefab Mode.");
    }

    private static GameObject EnsurePrefabExists()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (existing != null) return existing;

        ImportAsSprite(BubblePath);
        ImportAsSprite(ClosePath);

        Sprite bubble = AssetDatabase.LoadAssetAtPath<Sprite>(BubblePath);
        Sprite close = AssetDatabase.LoadAssetAtPath<Sprite>(ClosePath);
        Sprite capy = AssetDatabase.LoadAllAssetsAtPath(CapyAtlasPath)
            .OfType<Sprite>()
            .FirstOrDefault(sprite => sprite.name == CapySpriteName);

        if (bubble == null || capy == null || close == null)
        {
            Debug.LogError("Không tìm thấy sprite bubble, capy hoặc nút đóng của Order Panel.");
            return null;
        }

        TextMeshProUGUI hudText = Object.FindObjectsByType<TextMeshProUGUI>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .FirstOrDefault(text => text.name == "TimerText");
        TMP_FontAsset font = hudText != null ? hudText.font : null;
        Material fontMaterial = hudText != null ? hudText.fontSharedMaterial : null;

        GameObject root = BuildDefaultVisual(bubble, capy, close, font, fontMaterial);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        Debug.Log("Đã tạo Assets/Prefabs/OrderPanel.prefab.");
        return prefab;
    }

    private static GameObject BuildDefaultVisual(
        Sprite bubble,
        Sprite capy,
        Sprite close,
        TMP_FontAsset font,
        Material fontMaterial)
    {
        GameObject root = CreateUIObject(RootName, null, typeof(OrderUIController));
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = new Vector2(936f, 279f);

        GameObject card = CreateUIObject("OrderCard", root.transform);
        SetRect(card.GetComponent<RectTransform>(), 0f, 0f, 1f, 1f);

        GameObject characterArea = CreateUIObject("CharacterArea", card.transform);
        SetRect(characterArea.GetComponent<RectTransform>(), -0.060f, 0f, 0.345f, 1.40f);

        GameObject capyCrop = CreateUIObject(
            "OrderCapyCrop",
            characterArea.transform,
            typeof(RectMask2D));
        SetRect(capyCrop.GetComponent<RectTransform>(), 0f, 0f, 1f, 1f);

        Image capyImage = CreateImage(
            capyCrop.transform,
            "OrderCapybara",
            capy,
            Color.white,
            -0.04f, -0.22f, 1.04f, 0.99f);
        capyImage.preserveAspect = true;
        capyImage.rectTransform.localScale = new Vector3(-0.82f, 0.82f, 1f);

        Image bubbleImage = CreateImage(
            card.transform,
            "OrderBubble",
            bubble,
            Color.white,
            0.27f, 0.075f, 1.065f, 1.015f);
        bubbleImage.preserveAspect = false;

        Sprite roundedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(
            "UI/Skin/UISprite.psd");

        GameObject slotsRootObject = CreateUIObject(
            "FoodSlots",
            card.transform,
            typeof(HorizontalLayoutGroup));
        RectTransform slotsRoot = slotsRootObject.GetComponent<RectTransform>();
        SetRect(slotsRoot, 0.4055f, 0.395f, 0.9745f, 0.855f);

        HorizontalLayoutGroup layout = slotsRootObject.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 20f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        Image[] foodIcons = new Image[3];
        GameObject[] foodSlots = new GameObject[3];
        for (int index = 0; index < foodIcons.Length; index++)
        {
            Image slot = CreateImage(
                slotsRoot,
                $"OrderSlot_{index}",
                roundedSprite,
                new Color(0.96f, 0.84f, 0.73f, 0.88f),
                0.5f, 0.5f, 0.5f, 0.5f);
            slot.type = Image.Type.Simple;
            slot.rectTransform.sizeDelta = new Vector2(164f, 128f);

            LayoutElement layoutElement = slot.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 164f;
            layoutElement.preferredHeight = 128f;
            foodSlots[index] = slot.gameObject;

            foodIcons[index] = CreateImage(
                slot.transform,
                $"OrderFood_{index}",
                null,
                Color.white,
                0.05f, 0.12f, 0.95f, 0.88f);
            foodIcons[index].preserveAspect = true;
            foodIcons[index].rectTransform.localRotation = Quaternion.Euler(0f, 0f, -13f);
        }

        GameObject sliderObject = CreateUIObject(
            "OrderTimeSlider",
            card.transform,
            typeof(Slider));
        SetRect(sliderObject.GetComponent<RectTransform>(), 0.42f, 0.165f, 0.835f, 0.305f);

        Image timeBackground = CreateImage(
            sliderObject.transform,
            "Background",
            roundedSprite,
            new Color(0.69f, 0.61f, 0.51f, 0.72f),
            0f, 0f, 1f, 1f);
        timeBackground.type = Image.Type.Sliced;

        GameObject fillArea = CreateUIObject("Fill Area", sliderObject.transform);
        SetRect(fillArea.GetComponent<RectTransform>(), 0.025f, 0.18f, 0.975f, 0.82f);

        Image timeFill = CreateImage(
            fillArea.transform,
            "Fill",
            roundedSprite,
            new Color(0.25f, 0.92f, 0.25f, 1f),
            0f, 0f, 1f, 1f);
        timeFill.type = Image.Type.Sliced;

        Slider timeSlider = sliderObject.GetComponent<Slider>();
        timeSlider.minValue = 0f;
        timeSlider.maxValue = 1f;
        timeSlider.value = 1f;
        timeSlider.interactable = false;
        timeSlider.fillRect = timeFill.rectTransform;
        timeSlider.targetGraphic = timeBackground;

        TextMeshProUGUI timeText = CreateText(
            card.transform,
            "OrderTimeText",
            "00:00",
            font,
            fontMaterial,
            56f,
            Color.white,
            0.805f, 0.075f, 1.030f, 0.405f);
        timeText.enableAutoSizing = false;
        timeText.fontSize = 56f;
        timeText.fontStyle = FontStyles.Bold;
        timeText.outlineColor = Color.black;
        timeText.outlineWidth = 0.32f;

        Image closeImage = CreateImage(
            card.transform,
            "OrderSkipButton",
            close,
            Color.white,
            0.975f, 0.735f, 1.085f, 1.045f);
        closeImage.preserveAspect = true;
        closeImage.raycastTarget = true;
        Button skipButton = closeImage.gameObject.AddComponent<Button>();
        skipButton.targetGraphic = closeImage;

        root.GetComponent<OrderUIController>().Configure(
            card,
            slotsRoot,
            foodSlots,
            foodIcons,
            timeSlider,
            timeText,
            skipButton);

        card.SetActive(false);
        return root;
    }

    private static Transform FindLegacyRoot(Transform canvas)
    {
        for (int index = 0; index < canvas.childCount; index++)
        {
            Transform child = canvas.GetChild(index);
            if (child.name.StartsWith(LegacyRootPrefix)) return child;
        }
        return null;
    }

    private static void RemoveOrderRoots(Transform canvas)
    {
        for (int index = canvas.childCount - 1; index >= 0; index--)
        {
            Transform child = canvas.GetChild(index);
            if (child.name == RootName || child.name.StartsWith(LegacyRootPrefix))
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }
    }

    private static void ImportAsSprite(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
    }

    private static GameObject CreateUIObject(
        string name,
        Transform parent,
        params System.Type[] extraComponents)
    {
        System.Type[] components = new System.Type[extraComponents.Length + 1];
        components[0] = typeof(RectTransform);
        for (int index = 0; index < extraComponents.Length; index++)
        {
            components[index + 1] = extraComponents[index];
        }

        GameObject gameObject = new GameObject(name, components);
        if (parent != null) gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static Image CreateImage(
        Transform parent,
        string name,
        Sprite sprite,
        Color color,
        float minX, float minY,
        float maxX, float maxY)
    {
        GameObject gameObject = CreateUIObject(
            name,
            parent,
            typeof(CanvasRenderer),
            typeof(Image));
        SetRect(gameObject.GetComponent<RectTransform>(), minX, minY, maxX, maxY);

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
        GameObject gameObject = CreateUIObject(
            name,
            parent,
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        SetRect(gameObject.GetComponent<RectTransform>(), minX, minY, maxX, maxY);

        TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();
        text.text = content;
        text.font = font;
        text.fontSharedMaterial = material;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.enableAutoSizing = true;
        text.fontSizeMin = 24f;
        text.fontSizeMax = fontSize;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
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
}
