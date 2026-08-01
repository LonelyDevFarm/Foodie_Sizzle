using System.Collections.Generic;
using FoodieSizzle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AppRootMigrationTool
{
    private const string BootScenePath =
        "Assets/Scenes/BootScene.unity";
    private const string HomeScenePath =
        "Assets/Scenes/HomeScene.unity";
    private const string GameplayScenePath =
        "Assets/Scenes/GameplayScene.unity";
    private const string AppPrefabFolder = "Assets/Prefabs/App";
    private const string AppPrefabPath =
        AppPrefabFolder + "/AppRoot.prefab";
    private const string SettingsFolder = "Assets/Data/Settings";
    private const string SettingsPath =
        SettingsFolder + "/GameRuntimeSettings.asset";
    private const string AudioLibraryPath =
        "Assets/Resources/FeedbackAudioLibrary.asset";

    [MenuItem("Foodie Sizzle/Refactor/Migrate App Root")]
    public static void Migrate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "Migrate App Root",
                "Hãy thoát Play Mode trước khi migration.",
                "Đóng");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        string originalScenePath = SceneManager.GetActiveScene().path;
        EnsureFolder(AppPrefabFolder);
        EnsureFolder(SettingsFolder);

        GameRuntimeSettings runtimeSettings =
            LoadOrCreateRuntimeSettings();
        AssetDatabase.SaveAssets();

        FeedbackAudioLibrary audioLibrary =
            AssetDatabase.LoadAssetAtPath<FeedbackAudioLibrary>(
                AudioLibraryPath);
        if (audioLibrary == null)
        {
            EditorUtility.DisplayDialog(
                "Migrate App Root",
                $"Không tìm thấy {AudioLibraryPath}.",
                "Đóng");
            return;
        }

        Scene bootScene = EditorSceneManager.OpenScene(
            BootScenePath,
            OpenSceneMode.Single);
        GameObject appRoot = FindRootWith<AppBootstrap>(bootScene);
        if (appRoot == null)
        {
            appRoot = new GameObject("AppRoot");
        }

        AppBootstrap bootstrap = GetOrAdd<AppBootstrap>(appRoot);
        GameSettingsManager settings =
            GetOrAdd<GameSettingsManager>(appRoot);
        FrameRateManager frameRate = GetOrAdd<FrameRateManager>(appRoot);
        AppMusicPlayer musicPlayer = GetOrAdd<AppMusicPlayer>(appRoot);
        AudioSource musicSource = GetOrAdd<AudioSource>(appRoot);

        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.volume = 0.45f;

        AssignObjectReference(
            bootstrap,
            "frameRateManager",
            frameRate);
        AssignObjectReference(
            bootstrap,
            "settingsManager",
            settings);
        AssignObjectReference(
            bootstrap,
            "musicPlayer",
            musicPlayer);
        AssignObjectReference(frameRate, "settings", runtimeSettings);
        AssignObjectReference(
            musicPlayer,
            "audioLibrary",
            audioLibrary);
        AssignObjectReference(
            musicPlayer,
            "musicSource",
            musicSource);

        EnsureSingleAudioListener(bootScene);
        PrefabUtility.SaveAsPrefabAssetAndConnect(
            appRoot,
            AppPrefabPath,
            InteractionMode.UserAction);
        ConfigurePrefabAsset(runtimeSettings, audioLibrary);
        EditorSceneManager.MarkSceneDirty(bootScene);
        EditorSceneManager.SaveScene(bootScene);

        EnsureSceneAudioListener(HomeScenePath);
        EnsureSceneAudioListener(GameplayScenePath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!string.IsNullOrEmpty(originalScenePath))
        {
            EditorSceneManager.OpenScene(
                originalScenePath,
                OpenSceneMode.Single);
        }

        EditorUtility.DisplayDialog(
            "Migrate App Root",
            "Đã tạo AppRoot.prefab, nối app services, tạo runtime " +
            "settings và chuẩn hóa AudioListener cho ba scene.",
            "Hoàn tất");
    }

    private static GameRuntimeSettings LoadOrCreateRuntimeSettings()
    {
        GameRuntimeSettings settings =
            AssetDatabase.LoadAssetAtPath<GameRuntimeSettings>(
                SettingsPath);
        if (settings != null)
        {
            return settings;
        }

        settings = ScriptableObject.CreateInstance<GameRuntimeSettings>();
        AssetDatabase.CreateAsset(settings, SettingsPath);
        return settings;
    }

    private static void ConfigurePrefabAsset(
        GameRuntimeSettings runtimeSettings,
        FeedbackAudioLibrary audioLibrary)
    {
        GameObject prefabRoot =
            PrefabUtility.LoadPrefabContents(AppPrefabPath);

        try
        {
            AppBootstrap bootstrap =
                prefabRoot.GetComponent<AppBootstrap>();
            GameSettingsManager settings =
                prefabRoot.GetComponent<GameSettingsManager>();
            FrameRateManager frameRate =
                prefabRoot.GetComponent<FrameRateManager>();
            AppMusicPlayer musicPlayer =
                prefabRoot.GetComponent<AppMusicPlayer>();
            AudioSource musicSource =
                prefabRoot.GetComponent<AudioSource>();

            AssignObjectReference(
                bootstrap,
                "frameRateManager",
                frameRate);
            AssignObjectReference(
                bootstrap,
                "settingsManager",
                settings);
            AssignObjectReference(
                bootstrap,
                "musicPlayer",
                musicPlayer);
            AssignObjectReference(frameRate, "settings", runtimeSettings);
            AssignObjectReference(
                musicPlayer,
                "audioLibrary",
                audioLibrary);
            AssignObjectReference(
                musicPlayer,
                "musicSource",
                musicSource);

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, AppPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void EnsureSceneAudioListener(string scenePath)
    {
        Scene scene = EditorSceneManager.OpenScene(
            scenePath,
            OpenSceneMode.Single);
        if (EnsureSingleAudioListener(scene))
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }

    private static bool EnsureSingleAudioListener(Scene scene)
    {
        List<AudioListener> listeners = new List<AudioListener>();
        Camera preferredCamera = null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            listeners.AddRange(
                root.GetComponentsInChildren<AudioListener>(true));

            Camera[] cameras = root.GetComponentsInChildren<Camera>(true);
            foreach (Camera camera in cameras)
            {
                if (preferredCamera == null ||
                    camera.CompareTag("MainCamera"))
                {
                    preferredCamera = camera;
                }
            }
        }

        bool changed = false;
        AudioListener keeper = listeners.Count > 0 ? listeners[0] : null;
        if (keeper == null && preferredCamera != null)
        {
            keeper = Undo.AddComponent<AudioListener>(
                preferredCamera.gameObject);
            changed = true;
        }

        for (int index = 1; index < listeners.Count; index++)
        {
            Undo.DestroyObjectImmediate(listeners[index]);
            changed = true;
        }

        if (keeper == null)
        {
            Debug.LogWarning(
                $"Scene '{scene.path}' không có Camera để gắn AudioListener.");
        }

        return changed;
    }

    private static GameObject FindRootWith<T>(Scene scene)
        where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.GetComponent<T>() != null)
            {
                return root;
            }
        }

        return null;
    }

    private static T GetOrAdd<T>(GameObject target)
        where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null
            ? component
            : Undo.AddComponent<T>(target);
    }

    private static void AssignObjectReference(
        Object target,
        string propertyName,
        Object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property =
            serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            throw new System.InvalidOperationException(
                $"Không tìm thấy field '{propertyName}' trên " +
                target.GetType().Name);
        }

        property.objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string[] segments = folderPath.Split('/');
        string current = segments[0];
        for (int index = 1; index < segments.Length; index++)
        {
            string next = current + "/" + segments[index];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, segments[index]);
            }

            current = next;
        }
    }
}
