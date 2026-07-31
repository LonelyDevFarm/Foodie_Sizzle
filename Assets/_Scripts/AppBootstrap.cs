using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FoodieSizzle
{
    /// <summary>
    /// Điểm khởi động rất nhẹ. Object này sống xuyên scene để những hệ thống
    /// dùng chung sau này (save, quảng cáo, IAP...) có một nơi ổn định.
    /// </summary>
    [DisallowMultipleComponent]
    public class AppBootstrap : MonoBehaviour
    {
        private static AppBootstrap instance;

        public static void EnsureExists()
        {
            if (instance != null) return;

            AppBootstrap existing =
                Object.FindFirstObjectByType<AppBootstrap>(
                    FindObjectsInactive.Include);
            if (existing != null)
            {
                instance = existing;
                return;
            }

            new GameObject(
                "AppRoot",
                typeof(AppBootstrap));
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += HandleSceneLoaded;
            EnsureAudioListener();
            RemoveGameplayUiOutsideGameplayScene(
                SceneManager.GetActiveScene());

            if (GetComponent<AppMusicPlayer>() == null)
            {
                gameObject.AddComponent<AppMusicPlayer>();
            }
            if (GetComponent<GameSettingsManager>() == null)
            {
                gameObject.AddComponent<GameSettingsManager>();
            }
        }

        private IEnumerator Start()
        {
            // Chừa một frame để màn Loading kịp render và các thiết lập được đọc.
            yield return null;

            if (SceneManager.GetActiveScene().name ==
                AppSceneFlow.BootSceneName)
            {
                AppSceneFlow.LoadHome();
            }
        }

        private void OnDestroy()
        {
            if (instance != this) return;

            SceneManager.sceneLoaded -= HandleSceneLoaded;
            instance = null;
        }

        private static void HandleSceneLoaded(
            Scene scene,
            LoadSceneMode mode)
        {
            EnsureAudioListener();
            RemoveGameplayUiOutsideGameplayScene(scene);
        }

        private static void RemoveGameplayUiOutsideGameplayScene(Scene scene)
        {
            if (scene.name == AppSceneFlow.GameplaySceneName) return;

            // Phòng trường hợp một Editor Builder từng chèn nhầm Order UI vào
            // Boot/Home: màn tải và trang chủ tuyệt đối không được hiện nó.
            OrderUIController[] orderControllers =
                Object.FindObjectsByType<OrderUIController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            foreach (OrderUIController controller in orderControllers)
            {
                if (controller != null &&
                    controller.gameObject.scene == scene)
                {
                    controller.gameObject.SetActive(false);
                    Destroy(controller.gameObject);
                }
            }
        }

        private static void EnsureAudioListener()
        {
            AudioListener[] listeners =
                Object.FindObjectsByType<AudioListener>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);
            if (listeners.Length > 0) return;

            Camera camera = Camera.main;
            if (camera == null)
            {
                camera = Object.FindFirstObjectByType<Camera>();
            }
            if (camera != null)
            {
                camera.gameObject.AddComponent<AudioListener>();
            }
        }
    }
}
