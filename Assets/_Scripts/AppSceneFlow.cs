using UnityEngine;
using UnityEngine.SceneManagement;

namespace FoodieSizzle
{
    /// <summary>
    /// Tên scene và đường đi chung của toàn bộ ứng dụng.
    /// Level gameplay không phải scene riêng; chúng vẫn được đọc từ LevelData.
    /// </summary>
    public static class AppSceneFlow
    {
        public const string BootSceneName = "BootScene";
        public const string HomeSceneName = "HomeScene";
        public const string GameplaySceneName = "GameplayScene";
        public const string CurrentLevelPrefKey =
            "FoodieSizzle.CurrentLevel";

        public static bool CanLoadHome()
        {
            return Application.CanStreamedLevelBeLoaded(HomeSceneName);
        }

        public static void LoadHome()
        {
            Load(HomeSceneName);
        }

        public static void LoadGameplay()
        {
            Load(GameplaySceneName);
        }

        public static AsyncOperation LoadGameplayAsync()
        {
            return LoadAsync(GameplaySceneName);
        }

        private static void Load(string sceneName)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError(
                    $"Scene '{sceneName}' chưa có trong Build Settings.");
                return;
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }

        private static AsyncOperation LoadAsync(string sceneName)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError(
                    $"Scene '{sceneName}' chưa có trong Build Settings.");
                return null;
            }

            Time.timeScale = 1f;
            return SceneManager.LoadSceneAsync(
                sceneName,
                LoadSceneMode.Single);
        }
    }
}
