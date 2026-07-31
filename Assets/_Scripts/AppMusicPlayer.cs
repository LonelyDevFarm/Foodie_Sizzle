using UnityEngine;

namespace FoodieSizzle
{
    /// <summary>
    /// Phát một nguồn nhạc xuyên suốt Boot, Home và Gameplay.
    /// SFX vẫn thuộc GameplayScene vì chỉ cần tồn tại trong phiên chơi.
    /// </summary>
    [DisallowMultipleComponent]
    public class AppMusicPlayer : MonoBehaviour
    {
        private static AppMusicPlayer instance;
        private AudioSource musicSource;

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
            FeedbackAudioLibrary library =
                Resources.Load<FeedbackAudioLibrary>(
                    "FeedbackAudioLibrary");
            if (library == null || library.gameplayMusic == null)
            {
                return;
            }

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.clip = library.gameplayMusic;
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;
            musicSource.volume = 0.45f;
            musicSource.mute = !GameSettingsManager.MusicEnabled;
            musicSource.Play();
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
