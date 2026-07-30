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

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

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
    }
}
