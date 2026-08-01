using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FoodieSizzle
{
    /// <summary>
    /// Điểm khởi động rất nhẹ. Object này sống xuyên scene để những hệ thống
    /// dùng chung sau này (save, quảng cáo, IAP...) có một nơi ổn định.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public sealed class AppBootstrap : MonoBehaviour
    {
        private static AppBootstrap instance;

        [Header("App services")]
        [SerializeField] private FrameRateManager frameRateManager;
        [SerializeField] private GameSettingsManager settingsManager;
        [SerializeField] private AppMusicPlayer musicPlayer;

        public static void EnsureExists()
        {
            if (instance != null) return;

            Debug.LogError(
                "Không tìm thấy AppRoot. Hãy chạy game từ BootScene " +
                "hoặc dùng công cụ Refactor/Migrate App Root.");
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
            ValidateDependencies();
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
            instance = null;
        }

        private void ValidateDependencies()
        {
            if (frameRateManager == null ||
                settingsManager == null ||
                musicPlayer == null)
            {
                Debug.LogError(
                    "AppRoot chưa được nối đủ service. Chạy " +
                    "Foodie Sizzle/Refactor/Migrate App Root.",
                    this);
            }
        }
    }
}
