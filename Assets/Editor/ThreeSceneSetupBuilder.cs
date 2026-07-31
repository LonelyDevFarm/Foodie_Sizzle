using System.Linq;
using FoodieSizzle;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FoodieSizzle.EditorTools
{
    /// <summary>
    /// Chuyển project một-scene sang Boot → Home → Gameplay.
    /// Chỉ tự dựng khi ba scene chưa tồn tại để không ghi đè chỉnh sửa về sau.
    /// </summary>
    [InitializeOnLoad]
    public static class ThreeSceneSetupBuilder
    {
        private const string SamplePath =
            "Assets/Scenes/SampleScene.unity";
        private const string BootPath =
            "Assets/Scenes/BootScene.unity";
        private const string HomePath =
            "Assets/Scenes/HomeScene.unity";
        private const string GameplayPath =
            "Assets/Scenes/GameplayScene.unity";
        private const string HomePrefabPath =
            "Assets/Prefabs/HomeScreen.prefab";
        private const string HomeRootName = "HomeScreen_v1";

        static ThreeSceneSetupBuilder()
        {
            EditorApplication.delayCall += BuildOnce;
        }

        [MenuItem("Foodie Sizzle/Hệ thống/Dựng lại cấu trúc 3 scene")]
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
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling ||
                EditorApplication.isUpdating)
            {
                return;
            }

            bool allScenesExist =
                AssetDatabase.LoadAssetAtPath<SceneAsset>(BootPath) != null &&
                AssetDatabase.LoadAssetAtPath<SceneAsset>(HomePath) != null &&
                AssetDatabase.LoadAssetAtPath<SceneAsset>(GameplayPath) != null;
            if (allScenesExist && !force)
            {
                ConfigureBuildSettings();
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Scene sourceScene =
                EditorSceneManager.OpenScene(
                    SamplePath,
                    OpenSceneMode.Single);
            GameObject sourceCanvas = GameObject.Find("GameCanvas");
            Transform sourceHome = sourceCanvas != null
                ? sourceCanvas.transform.Find(HomeRootName)
                : null;
            if (sourceHome == null)
            {
                Debug.LogError(
                    "[Foodie Sizzle] SampleScene chưa có HomeScreen_v1.");
                return;
            }

            EnsureFolder("Assets/Prefabs");
            PrefabUtility.SaveAsPrefabAsset(
                sourceHome.gameObject,
                HomePrefabPath);

            TMP_FontAsset referenceFont = Object
                .FindObjectsByType<TextMeshProUGUI>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .FirstOrDefault(text => text.name == "TimerText")
                ?.font;
            Material referenceMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Generated/HomeText_v1.mat");

            CreateGameplayScene(force);
            CreateHomeScene(referenceFont, force);
            CreateBootScene(referenceFont, referenceMaterial, force);
            ConfigureBuildSettings();

            EditorSceneManager.OpenScene(HomePath, OpenSceneMode.Single);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[Foodie Sizzle] Đã dựng BootScene → HomeScene → GameplayScene.");
        }

        private static void CreateGameplayScene(bool force)
        {
            bool exists =
                AssetDatabase.LoadAssetAtPath<SceneAsset>(GameplayPath) != null;
            if (!exists)
            {
                AssetDatabase.CopyAsset(SamplePath, GameplayPath);
                AssetDatabase.ImportAsset(GameplayPath);
            }
            else if (!force)
            {
                return;
            }

            Scene gameplay =
                EditorSceneManager.OpenScene(
                    GameplayPath,
                    OpenSceneMode.Single);
            GameObject canvas = GameObject.Find("GameCanvas");
            Transform home = canvas != null
                ? canvas.transform.Find(HomeRootName)
                : null;
            if (home != null)
            {
                Object.DestroyImmediate(home.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(gameplay);
            EditorSceneManager.SaveScene(gameplay, GameplayPath);
        }

        private static void CreateHomeScene(
            TMP_FontAsset referenceFont,
            bool force)
        {
            bool exists =
                AssetDatabase.LoadAssetAtPath<SceneAsset>(HomePath) != null;
            if (exists && !force) return;

            Scene homeScene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            CreateCamera(new Color(0.96f, 0.76f, 0.50f));

            GameObject canvasObject = CreateCanvas();
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(HomePrefabPath);
            GameObject homeRoot = PrefabUtility.InstantiatePrefab(
                prefab,
                homeScene) as GameObject;
            homeRoot.transform.SetParent(canvasObject.transform, false);
            SetFullStretch(
                homeRoot.GetComponent<RectTransform>());

            HomeSceneController controller =
                homeRoot.GetComponent<HomeSceneController>();
            if (controller == null)
            {
                controller =
                    homeRoot.AddComponent<HomeSceneController>();
            }

            Button play = FindChildRecursive(
                homeRoot.transform,
                "HomePlayButton")?.GetComponent<Button>();
            TextMeshProUGUI level = FindChildRecursive(
                homeRoot.transform,
                "HomeLevelText")?.GetComponent<TextMeshProUGUI>();
            controller.Configure(play, level);

            CreateEventSystem();
            EditorSceneManager.SaveScene(homeScene, HomePath);
        }

        private static void CreateBootScene(
            TMP_FontAsset referenceFont,
            Material referenceMaterial,
            bool force)
        {
            bool exists =
                AssetDatabase.LoadAssetAtPath<SceneAsset>(BootPath) != null;
            if (exists && !force) return;

            Scene bootScene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            CreateCamera(new Color(0.96f, 0.76f, 0.50f));

            GameObject appRoot = new GameObject(
                "AppRoot",
                typeof(AppBootstrap));

            GameObject canvasObject = CreateCanvas();
            GameObject background = new GameObject(
                "LoadingBackground",
                typeof(RectTransform),
                typeof(Image));
            background.transform.SetParent(canvasObject.transform, false);
            SetFullStretch(background.GetComponent<RectTransform>());
            background.GetComponent<Image>().color =
                new Color(0.96f, 0.77f, 0.53f, 1f);

            GameObject labelObject = new GameObject(
                "LoadingText",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(canvasObject.transform, false);
            RectTransform labelRect =
                labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.15f, 0.43f);
            labelRect.anchorMax = new Vector2(0.85f, 0.57f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label =
                labelObject.GetComponent<TextMeshProUGUI>();
            label.font = referenceFont;
            if (referenceMaterial != null)
                label.fontSharedMaterial = referenceMaterial;
            label.text = "ĐANG TẢI...";
            label.fontSize = 58f;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;

            appRoot.transform.SetAsFirstSibling();
            EditorSceneManager.SaveScene(bootScene, BootPath);
        }

        private static GameObject CreateCanvas()
        {
            GameObject canvasObject = new GameObject(
                "GameCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler =
                canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1170f, 2532f);
            scaler.screenMatchMode =
                CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvasObject;
        }

        private static void CreateEventSystem()
        {
            new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
        }

        private static void CreateCamera(Color background)
        {
            GameObject cameraObject =
                new GameObject(
                    "Main Camera",
                    typeof(Camera),
                    typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            camera.orthographic = true;
            camera.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootPath, true),
                new EditorBuildSettingsScene(HomePath, true),
                new EditorBuildSettingsScene(GameplayPath, true)
            };
        }

        private static Transform FindChildRecursive(
            Transform root,
            string childName)
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

        private static void SetFullStretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;

            int split = folderPath.LastIndexOf('/');
            string parent = folderPath.Substring(0, split);
            string name = folderPath.Substring(split + 1);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
