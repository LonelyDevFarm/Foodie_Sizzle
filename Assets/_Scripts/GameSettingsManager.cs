using UnityEngine;
using UnityEngine.UI;

namespace FoodieSizzle
{
    /// <summary>
    /// Lưu và áp dụng các thiết lập cơ bản của người chơi.
    /// </summary>
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

        private void Awake()
        {
            LoadSettings();
            RefreshToggles();
            WireToggles();
            ApplyMusicState();
        }

        public void Configure(
            Toggle music,
            Toggle sound,
            Toggle vibration)
        {
            musicToggle = music;
            soundToggle = sound;
            vibrationToggle = vibration;
        }

        public static void Vibrate()
        {
            if (VibrationEnabled)
            {
                Handheld.Vibrate();
            }
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
            ApplyMusicState();
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

        private static void ApplyMusicState()
        {
            // Hiện dự án chưa có AudioManager riêng. Các AudioSource chạy lặp
            // được xem là nhạc nền; SFX sau này kiểm tra SoundEnabled trước khi phát.
            AudioSource[] sources =
                Object.FindObjectsByType<AudioSource>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            foreach (AudioSource source in sources)
            {
                if (source != null && source.loop)
                {
                    source.mute = !MusicEnabled;
                }
            }
        }
    }
}
