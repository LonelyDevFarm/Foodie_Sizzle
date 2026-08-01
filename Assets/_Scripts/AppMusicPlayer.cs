using UnityEngine;

namespace FoodieSizzle
{
    /// <summary>
    /// Phát một nguồn nhạc xuyên suốt Boot, Home và Gameplay.
    /// SFX vẫn thuộc GameplayScene vì chỉ cần tồn tại trong phiên chơi.
    /// </summary>
    [DefaultExecutionOrder(-8000)]
    [DisallowMultipleComponent]
    public sealed class AppMusicPlayer : MonoBehaviour
    {
        private static AppMusicPlayer instance;

        [SerializeField] private FeedbackAudioLibrary audioLibrary;
        [SerializeField] private AudioSource musicSource;

        public static bool IsAvailable =>
            instance != null && instance.musicSource != null;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }

            instance = this;
            if (audioLibrary == null || musicSource == null)
            {
                Debug.LogError(
                    "AppMusicPlayer thiếu AudioLibrary hoặc AudioSource.",
                    this);
                return;
            }

            GameSettingsManager.MusicEnabledChanged += HandleMusicEnabled;
            musicSource.clip = audioLibrary.gameplayMusic;
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;
            musicSource.volume = 0.45f;
            HandleMusicEnabled(GameSettingsManager.MusicEnabled);
            if (musicSource.clip != null)
            {
                musicSource.Play();
            }
        }

        private void OnDestroy()
        {
            GameSettingsManager.MusicEnabledChanged -= HandleMusicEnabled;
            if (instance == this) instance = null;
        }

        private void HandleMusicEnabled(bool enabled)
        {
            if (musicSource != null)
            {
                musicSource.mute = !enabled;
            }
        }
    }
}
