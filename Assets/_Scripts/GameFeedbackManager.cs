using UnityEngine;

namespace FoodieSizzle
{
    public enum FeedbackCue
    {
        UiButton,
        SelectSkewer,
        ValidDrop,
        InvalidDrop,
        MatchingSet,
        OrderAppears,
        OrderCompleted,
        OrderWarning,
        OrderFailed,
        BoxBooster,
        RefreshBooster,
        TimeBooster,
        PlusBooster,
        Win,
        Lose
    }

    /// <summary>
    /// Một đầu mối duy nhất cho âm thanh và rung của gameplay.
    /// Clip có thể để trống; hệ thống vẫn chạy bình thường và chờ gắn asset sau.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameFeedbackManager : MonoBehaviour
    {
        [Header("Thao tác")]
        [SerializeField] private AudioClip uiButtonClip;
        [SerializeField] private AudioClip selectSkewerClip;
        [SerializeField] private AudioClip validDropClip;
        [SerializeField] private AudioClip invalidDropClip;
        [SerializeField] private AudioClip matchingSetClip;

        [Header("Order")]
        [SerializeField] private AudioClip orderAppearsClip;
        [SerializeField] private AudioClip orderCompletedClip;
        [SerializeField] private AudioClip orderWarningClip;
        [SerializeField] private AudioClip orderFailedClip;

        [Header("Vật phẩm")]
        [SerializeField] private AudioClip boxBoosterClip;
        [SerializeField] private AudioClip refreshBoosterClip;
        [SerializeField] private AudioClip timeBoosterClip;
        [SerializeField] private AudioClip plusBoosterClip;

        [Header("Kết quả")]
        [SerializeField] private AudioClip winClip;
        [SerializeField] private AudioClip loseClip;

        [Header("Tinh chỉnh")]
        [SerializeField] [Range(0f, 1f)] private float sfxVolume = 0.85f;
        [SerializeField] [Range(0f, 0.08f)]
        private float randomPitchRange = 0.025f;
        [SerializeField] private FeedbackAudioLibrary audioLibrary;

        private AudioSource sfxSource;
        private AudioSource musicSource;
        private float lastMatchingSetTime = -10f;
        private int matchingComboIndex;

        private void Awake()
        {
            if (audioLibrary == null)
            {
                audioLibrary = Resources.Load<FeedbackAudioLibrary>(
                    "FeedbackAudioLibrary");
            }
            EnsureAudioSource();
            StartBackgroundMusic();
        }

        public void Play(FeedbackCue cue)
        {
            int comboIndex = 0;
            if (cue == FeedbackCue.MatchingSet)
            {
                matchingComboIndex =
                    Time.unscaledTime - lastMatchingSetTime <= 2f
                        ? matchingComboIndex + 1
                        : 0;
                lastMatchingSetTime = Time.unscaledTime;
                comboIndex = matchingComboIndex;
            }

            AudioClip clip = GetClip(cue, comboIndex);
            if (GameSettingsManager.SoundEnabled && clip != null)
            {
                EnsureAudioSource();
                sfxSource.pitch = 1f + Random.Range(
                    -randomPitchRange,
                    randomPitchRange);
                sfxSource.PlayOneShot(clip, sfxVolume);
            }

            int vibrationDuration = GetVibrationDuration(cue);
            if (vibrationDuration > 0)
            {
                GameSettingsManager.Vibrate(vibrationDuration);
            }
        }

        private void EnsureAudioSource()
        {
            if (sfxSource != null) return;

            sfxSource = GetComponent<AudioSource>();
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
            }

            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f;
        }

        private void StartBackgroundMusic()
        {
            if (audioLibrary == null ||
                audioLibrary.gameplayMusic == null)
            {
                return;
            }

            GameObject musicObject =
                new GameObject("GameplayMusic");
            musicObject.transform.SetParent(transform, false);
            musicSource = musicObject.AddComponent<AudioSource>();
            musicSource.clip = audioLibrary.gameplayMusic;
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;
            musicSource.volume = 0.45f;
            musicSource.mute = !GameSettingsManager.MusicEnabled;
            musicSource.Play();
        }

        private AudioClip GetClip(FeedbackCue cue, int comboIndex)
        {
            AudioClip localClip;
            switch (cue)
            {
                case FeedbackCue.UiButton:
                    localClip = uiButtonClip;
                    break;
                case FeedbackCue.SelectSkewer:
                    localClip = selectSkewerClip;
                    break;
                case FeedbackCue.ValidDrop:
                    localClip = validDropClip;
                    break;
                case FeedbackCue.InvalidDrop:
                    localClip = invalidDropClip;
                    break;
                case FeedbackCue.MatchingSet:
                    localClip = matchingSetClip;
                    break;
                case FeedbackCue.OrderAppears:
                    localClip = orderAppearsClip;
                    break;
                case FeedbackCue.OrderCompleted:
                    localClip = orderCompletedClip;
                    break;
                case FeedbackCue.OrderWarning:
                    localClip = orderWarningClip;
                    break;
                case FeedbackCue.OrderFailed:
                    localClip = orderFailedClip;
                    break;
                case FeedbackCue.BoxBooster:
                    localClip = boxBoosterClip;
                    break;
                case FeedbackCue.RefreshBooster:
                    localClip = refreshBoosterClip;
                    break;
                case FeedbackCue.TimeBooster:
                    localClip = timeBoosterClip;
                    break;
                case FeedbackCue.PlusBooster:
                    localClip = plusBoosterClip;
                    break;
                case FeedbackCue.Win:
                    localClip = winClip;
                    break;
                case FeedbackCue.Lose:
                    localClip = loseClip;
                    break;
                default:
                    localClip = null;
                    break;
            }

            return localClip != null
                ? localClip
                : audioLibrary != null
                    ? audioLibrary.GetClip(cue, comboIndex)
                    : null;
        }

        private static int GetVibrationDuration(FeedbackCue cue)
        {
            switch (cue)
            {
                case FeedbackCue.InvalidDrop:
                    return 15;
                case FeedbackCue.MatchingSet:
                    return 25;
                case FeedbackCue.OrderCompleted:
                    return 30;
                case FeedbackCue.OrderFailed:
                    return 45;
                case FeedbackCue.BoxBooster:
                case FeedbackCue.RefreshBooster:
                case FeedbackCue.TimeBooster:
                case FeedbackCue.PlusBooster:
                    return 20;
                case FeedbackCue.Win:
                    return 40;
                case FeedbackCue.Lose:
                    return 55;
                default:
                    return 0;
            }
        }
    }
}
