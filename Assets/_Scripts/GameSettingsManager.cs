using System;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieSizzle
{
    /// <summary>
    /// Lưu và áp dụng các thiết lập cơ bản của người chơi.
    /// </summary>
    [DefaultExecutionOrder(-9500)]
    [DisallowMultipleComponent]
    public class GameSettingsManager : MonoBehaviour
    {
        private const string MusicKey = "Settings_Music";
        private const string SoundKey = "Settings_Sound";
        private const string VibrationKey = "Settings_Vibration";

        [SerializeField] private Toggle musicToggle;
        [SerializeField] private Toggle soundToggle;
        [SerializeField] private Toggle vibrationToggle;

        public static bool MusicEnabled { get; private set; } = true;
        public static bool SoundEnabled { get; private set; } = true;
        public static bool VibrationEnabled { get; private set; } = true;
        public static event Action<bool> MusicEnabledChanged;
        private static bool settingsLoaded;
        private static GameSettingsManager instance;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetRuntimeState()
        {
            // Hỗ trợ Editor tắt Domain Reload: mỗi lần bấm Play vẫn phải
            // đọc lại PlayerPrefs như một lần mở ứng dụng mới.
            settingsLoaded = false;
            MusicEnabledChanged = null;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                // GameplayScene giữ các reference Toggle trong Inspector.
                // Chuyển chúng sang manager bền vững ở AppRoot rồi bỏ component
                // tạm, vì vậy runtime chỉ còn đúng một manager.
                instance.Configure(
                    musicToggle,
                    soundToggle,
                    vibrationToggle);
                musicToggle = null;
                soundToggle = null;
                vibrationToggle = null;
                Destroy(this);
                return;
            }

            instance = this;
            // AppRoot nạp dữ liệu một lần. Component trong GameplayScene chỉ
            // nối các Toggle với cùng trạng thái, không đọc/ghi đè lần nữa.
            if (!settingsLoaded)
            {
                LoadSettings();
                settingsLoaded = true;
            }
            RefreshToggles();
            WireToggles();
        }

        private void OnDestroy()
        {
            if (musicToggle != null)
                musicToggle.onValueChanged.RemoveListener(SetMusicEnabled);
            if (soundToggle != null)
                soundToggle.onValueChanged.RemoveListener(SetSoundEnabled);
            if (vibrationToggle != null)
            {
                vibrationToggle.onValueChanged.RemoveListener(
                    SetVibrationEnabled);
            }

            if (instance == this) instance = null;
        }

        public void Configure(
            Toggle music,
            Toggle sound,
            Toggle vibration)
        {
            UnwireToggles();
            musicToggle = music;
            soundToggle = sound;
            vibrationToggle = vibration;
            if (Application.isPlaying)
            {
                RefreshToggles();
                WireToggles();
            }
        }

        private void UnwireToggles()
        {
            if (musicToggle != null)
                musicToggle.onValueChanged.RemoveListener(SetMusicEnabled);
            if (soundToggle != null)
                soundToggle.onValueChanged.RemoveListener(SetSoundEnabled);
            if (vibrationToggle != null)
            {
                vibrationToggle.onValueChanged.RemoveListener(
                    SetVibrationEnabled);
            }
        }

        public static void Vibrate(int durationMilliseconds = 25)
        {
            if (!VibrationEnabled) return;

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (AndroidJavaClass unityPlayer =
                       new AndroidJavaClass(
                           "com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity =
                       unityPlayer.GetStatic<AndroidJavaObject>(
                           "currentActivity"))
                using (AndroidJavaObject vibrator =
                       activity.Call<AndroidJavaObject>(
                           "getSystemService",
                           "vibrator"))
                {
                    int duration = Mathf.Clamp(
                        durationMilliseconds,
                        10,
                        100);
                    using (AndroidJavaClass version =
                           new AndroidJavaClass(
                               "android.os.Build$VERSION"))
                    {
                        int sdk = version.GetStatic<int>("SDK_INT");
                        if (sdk >= 26)
                        {
                            using (AndroidJavaClass effectClass =
                                   new AndroidJavaClass(
                                       "android.os.VibrationEffect"))
                            using (AndroidJavaObject effect =
                                   effectClass.CallStatic<
                                       AndroidJavaObject>(
                                       "createOneShot",
                                       (long)duration,
                                       -1))
                            {
                                vibrator.Call("vibrate", effect);
                            }
                        }
                        else
                        {
                            vibrator.Call(
                                "vibrate",
                                (long)duration);
                        }
                    }
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning(
                    $"Không thể rung thiết bị: {exception.Message}");
            }
#elif UNITY_IOS && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

        private void LoadSettings()
        {
            MusicEnabled = PlayerPrefs.GetInt(MusicKey, 1) == 1;
            SoundEnabled = PlayerPrefs.GetInt(SoundKey, 1) == 1;
            VibrationEnabled = PlayerPrefs.GetInt(VibrationKey, 1) == 1;
        }

        private void RefreshToggles()
        {
            if (musicToggle != null)
                musicToggle.isOn = MusicEnabled;
            if (soundToggle != null)
                soundToggle.isOn = SoundEnabled;
            if (vibrationToggle != null)
                vibrationToggle.isOn = VibrationEnabled;
        }

        private void WireToggles()
        {
            if (musicToggle != null)
            {
                musicToggle.onValueChanged.RemoveListener(SetMusicEnabled);
                musicToggle.onValueChanged.AddListener(SetMusicEnabled);
            }
            if (soundToggle != null)
            {
                soundToggle.onValueChanged.RemoveListener(SetSoundEnabled);
                soundToggle.onValueChanged.AddListener(SetSoundEnabled);
            }
            if (vibrationToggle != null)
            {
                vibrationToggle.onValueChanged.RemoveListener(
                    SetVibrationEnabled);
                vibrationToggle.onValueChanged.AddListener(
                    SetVibrationEnabled);
            }
        }

        private void SetMusicEnabled(bool enabled)
        {
            MusicEnabled = enabled;
            SaveSetting(MusicKey, enabled);
            MusicEnabledChanged?.Invoke(enabled);
        }

        private void SetSoundEnabled(bool enabled)
        {
            SoundEnabled = enabled;
            SaveSetting(SoundKey, enabled);
        }

        private void SetVibrationEnabled(bool enabled)
        {
            VibrationEnabled = enabled;
            SaveSetting(VibrationKey, enabled);
        }

        private static void SaveSetting(string key, bool enabled)
        {
            PlayerPrefs.SetInt(key, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

    }
}
