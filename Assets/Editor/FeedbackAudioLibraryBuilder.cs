using FoodieSizzle;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class FeedbackAudioLibraryBuilder
{
    private const string LibraryPath =
        "Assets/Resources/FeedbackAudioLibrary.asset";

    static FeedbackAudioLibraryBuilder()
    {
        EditorApplication.delayCall += BuildOrUpdate;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    [MenuItem("Foodie Sizzle/Audio/Đồng bộ thư viện phản hồi")]
    public static void BuildOrUpdate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }
        if (EditorApplication.isCompiling)
        {
            EditorApplication.delayCall += BuildOrUpdate;
            return;
        }

        EnsureResourcesFolder();

        FeedbackAudioLibrary library =
            AssetDatabase.LoadAssetAtPath<FeedbackAudioLibrary>(
                LibraryPath);
        if (library == null)
        {
            library = ScriptableObject.CreateInstance<
                FeedbackAudioLibrary>();
            AssetDatabase.CreateAsset(library, LibraryPath);
        }

        library.gameplayMusic = Load("ingame.ogg");
        library.uiButton = Load("sfx_ui_button_click.ogg");
        library.selectSkewer = Load("Pickup object from grill.ogg");
        library.validDrop = Load("dat manh dung vi tri.ogg");
        library.invalidDrop = Load("dat manh sai vi tri.ogg");
        library.matchingSets = new[]
        {
            Load("Finish combo.ogg"),
            Load("Finish combo 2.ogg"),
            Load("Finish combo 3.ogg"),
            Load("Finish combo 4.ogg")
        };

        library.orderAppears = Load("Shipper Order.ogg");
        library.orderCompleted = Load("Order done.ogg");
        library.orderWarning = Load("Shipper time almost up.ogg");
        library.orderFailed = Load("sfx_fail.ogg");

        library.boxBooster = Load("click vao hom.ogg");
        library.refreshBooster = Load("sfx_rocket.ogg");
        library.timeBooster = Load("danut.ogg");
        library.plusBooster = Load("kholua.ogg");

        library.win = Load("Win game.ogg");
        library.lose = Load("Lose game.ogg");

        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        Debug.Log("Đã đồng bộ thư viện âm thanh phản hồi.");
    }

    private static void OnPlayModeChanged(
        PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            BuildOrUpdate();
        }
    }

    private static AudioClip Load(string fileName)
    {
        string path = $"Assets/AudioClip/{fileName}";
        AudioClip clip =
            AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clip == null)
        {
            Debug.LogWarning($"Không tìm thấy AudioClip: {path}");
        }
        return clip;
    }

    private static void EnsureResourcesFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
    }
}
