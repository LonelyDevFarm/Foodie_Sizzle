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
    private const string RootName = "OrderUIRoot_v11";
    private const string BubblePath =
        "Assets/BaseGame/GeneratedOrderUI/Order_SpeechBubble_Cream.png";
    private const string CapyAtlasPath =
        "Assets/BaseGame/UsedSprites/sactx-0-4096x4096-Crunch-CapyLift-8132215d.png";
    private const string CapySpriteName =
        "sactx-0-4096x4096-Crunch-CapyLift-8132215d_21";
    private const string ClosePath = "Assets/Texture2D/icon_close.png";

    static OrderUIBuilder()
    {
        EditorApplication.delayCall += BuildOnce;
    }

    [MenuItem("Foodie Sizzle/Giao diện/Dựng lại bảng Order")]
    public static void BuildManually()
    {
        Build(true);
    }

    private static void BuildOnce()
    {
        Build(false);
    }

    private static void Build(bool force)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        GameObject canvasObject = GameObject.Find("GameCanvas");
        if (canvasObject == null) return;

        Transform oldRoot = canvasObject.transform.Find(RootName);
        if (oldRoot != null && !force) return;

        for (int index = canvasObject.transform.childCount - 1;
             index >= 0;
             index--)
        {
            Transform child = canvasObject.transform.GetChild(index);
            if (child.name.StartsWith("OrderUIRoot_"))
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        ImportAsSprite(BubblePath);
        ImportAsSprite(ClosePath);
        Sprite bubble =
            AssetDatabase.LoadAssetAtPath<Sprite>(BubblePath);
        Sprite close =
            AssetDatabase.LoadAssetAtPath<Sprite>(ClosePath);
        Sprite capy = AssetDatabase
            .LoadAllAssetsAtPath(CapyAtlasPath)
            .OfType<Sprite>()
            .FirstOrDefault(sprite => sprite.name == CapySpriteName);

        if (bubble == null || capy == null || close == null)
        {
            Debug.LogError(
                "Không tìm thấy bong bóng Order hoặc sprite capy _21.");
            return;
        }

        TextMeshProUGUI hudText = Object
            .FindObjectsByType<TextMeshProUGUI>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None)
            .FirstOrDefault(text => text.name == "TimerText");

        TMP_FontAsset font = hudText != null ? hudText.font : null;
        Material fontMaterial =
            hudText != null ? hudText.fontSharedMaterial : null;

        GameObject root = CreateUIObject(
            RootName,
            canvasObject.transform,
            typeof(OrderUIController));
        SetRect(
            root.GetComponent<RectTransform>(),
            0.10f, 0.745f, 0.90f, 0.855f);

        GameObject card = CreateUIObject("OrderCard", root.transform);
        SetRect(
            card.GetComponent<RectTransform>(),
            0f, 0f, 1f, 1f);

        GameObject capyCrop = CreateUIObject(
            "OrderCapyCrop",
            card.transform,
            typeof(RectMask2D));
        SetRect(
            capyCrop.GetComponent<RectTransform>(),
            -0.060f, 0.00f, 0.345f, 1.40f);

        Image capyImage = CreateImage(
            capyCrop.transform,
            "OrderCapybara",
            capy,
            Color.white,
            -0.04f, -0.22f, 1.04f, 0.99f);
        capyImage.preserveAspect = true;
        // Phóng trực tiếp ảnh (không chỉ nới mask) và quay mặt về bong bóng.
        capyImage.rectTransform.localScale =
            new Vector3(-0.82f, 0.82f, 1f);

        Image bubbleImage = CreateImage(
            card.transform,
            "OrderBubble",
            bubble,
            Color.white,
            0.27f, 0.075f, 1.065f, 1.015f);
        bubbleImage.preserveAspect = false;

        Sprite roundedSprite =
            AssetDatabase.GetBuiltinExtraResource<Sprite>(
                "UI/Skin/UISprite.psd");
        Image[] foodIcons = new Image[3];
        GameObject[] foodSlots = new GameObject[3];
        for (int index = 0; index < foodIcons.Length; index++)
        {
            float minX = 0.4925f + index * 0.197f;
            float maxX = minX + 0.175f;

            Image slot = CreateImage(
                card.transform,
                $"OrderSlot_{index}",
                roundedSprite,
                new Color(0.96f, 0.84f, 0.73f, 0.88f),
                minX, 0.395f, maxX, 0.855f);
            // Simple giữ vùng alpha ở bốn góc nên ô tròn rõ hơn Sliced.
            slot.type = Image.Type.Simple;
            foodSlots[index] = slot.gameObject;

            foodIcons[index] = CreateImage(
                slot.transform,
                $"OrderFood_{index}",
                null,
                Color.white,
                0.05f, 0.12f, 0.95f, 0.88f);
            foodIcons[index].preserveAspect = true;
            foodIcons[index].rectTransform.localRotation =
                Quaternion.Euler(0f, 0f, -13f);
        }

        GameObject sliderObject = CreateUIObject(
            "OrderTimeSlider",
            card.transform,
            typeof(Slider));
        SetRect(
            sliderObject.GetComponent<RectTransform>(),
            0.42f, 0.165f, 0.835f, 0.305f);

        Image timeBackground = CreateImage(
            sliderObject.transform,
            "Background",
            roundedSprite,
            new Color(0.69f, 0.61f, 0.51f, 0.72f),
            0f, 0f, 1f, 1f);
        timeBackground.type = Image.Type.Sliced;

        GameObject fillArea = CreateUIObject(
            "Fill Area",
            sliderObject.transform);
        SetRect(
            fillArea.GetComponent<RectTransform>(),
            0.025f, 0.18f, 0.975f, 0.82f);

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
        timeSlider.wholeNumbers = false;
        timeSlider.interactable = false;
        timeSlider.fillRect = timeFill.rectTransform;
        timeSlider.targetGraphic = timeBackground;
        timeSlider.direction = Slider.Direction.LeftToRight;

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

        OrderUIController controller =
            root.GetComponent<OrderUIController>();
        controller.Configure(
            card,
            foodSlots,
            foodIcons,
            timeSlider,
            timeText,
            skipButton);

        // Root phải luôn bật để Controller nhận biết lúc Order bắt đầu.
        // Chỉ Card được ẩn khi chưa có Order.
        card.SetActive(false);

        EditorUtility.SetDirty(root);
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(canvasObject.scene);
        EditorSceneManager.SaveScene(canvasObject.scene);
        Debug.Log("Đã dựng bảng Order với capy, ba ô món và đồng hồ.");
    }

    private static void ImportAsSprite(string path)
    {
        AssetDatabase.ImportAsset(
            path,
            ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer =
            AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression =
            TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
    }

    private static GameObject CreateUIObject(
        string name,
        Transform parent,
        params System.Type[] extraComponents)
    {
        System.Type[] components =
            new System.Type[extraComponents.Length + 1];
        components[0] = typeof(RectTransform);
        for (int index = 0; index < extraComponents.Length; index++)
        {
            components[index + 1] = extraComponents[index];
        }

        GameObject gameObject = new GameObject(name, components);
        gameObject.transform.SetParent(parent, false);
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
        SetRect(
            gameObject.GetComponent<RectTransform>(),
            minX, minY, maxX, maxY);

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
        SetRect(
            gameObject.GetComponent<RectTransform>(),
            minX, minY, maxX, maxY);

        TextMeshProUGUI text =
            gameObject.GetComponent<TextMeshProUGUI>();
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
