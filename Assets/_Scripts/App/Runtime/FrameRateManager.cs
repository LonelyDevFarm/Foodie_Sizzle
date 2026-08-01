using UnityEngine;

namespace FoodieSizzle
{
    [DefaultExecutionOrder(-9000)]
    [DisallowMultipleComponent]
    public sealed class FrameRateManager : MonoBehaviour
    {
        [SerializeField] private GameRuntimeSettings settings;

        public GameRuntimeSettings Settings => settings;

        private void Awake()
        {
            Apply();
        }

        public void Apply()
        {
            if (settings == null)
            {
                Debug.LogError(
                    "FrameRateManager thiếu GameRuntimeSettings.",
                    this);
                return;
            }

            Application.runInBackground = settings.RunInBackground;
            if (settings.UseVSync)
            {
                QualitySettings.vSyncCount = settings.VSyncCount;
                Application.targetFrameRate = -1;
                return;
            }

            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = settings.TargetFrameRate;
        }
    }
}
