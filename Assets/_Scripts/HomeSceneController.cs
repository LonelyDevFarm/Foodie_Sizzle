using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieSizzle
{
    /// <summary>
    /// Điều khiển Home độc lập với gameplay. Các nút meta sau này có thể
    /// được bổ sung tại đây mà không làm nặng GameplayScene.
    /// </summary>
    public class HomeSceneController : MonoBehaviour
    {
        [SerializeField] private Button playButton;
        [SerializeField] private TextMeshProUGUI levelText;

        private void Awake()
        {
            ResolveMissingReferences();
            RefreshLevelText();

            if (playButton != null)
            {
                playButton.onClick.RemoveListener(Play);
                playButton.onClick.AddListener(Play);
            }
        }

        public void Configure(
            Button play,
            TextMeshProUGUI currentLevelText)
        {
            playButton = play;
            levelText = currentLevelText;
        }

        private void Play()
        {
            AppSceneFlow.LoadGameplay();
        }

        private void RefreshLevelText()
        {
            if (levelText == null) return;

            int level = Mathf.Max(
                1,
                PlayerPrefs.GetInt(
                    AppSceneFlow.CurrentLevelPrefKey,
                    1));
            levelText.text = $"LEVEL {level}";
        }

        private void ResolveMissingReferences()
        {
            if (playButton == null)
            {
                Transform play =
                    FindChildRecursive(transform, "HomePlayButton");
                if (play != null)
                    playButton = play.GetComponent<Button>();
            }

            if (levelText == null)
            {
                Transform label =
                    FindChildRecursive(transform, "HomeLevelText");
                if (label != null)
                    levelText = label.GetComponent<TextMeshProUGUI>();
            }
        }

        private static Transform FindChildRecursive(
            Transform root,
            string childName)
        {
            foreach (Transform child in root)
            {
                if (child.name == childName) return child;
                Transform nested =
                    FindChildRecursive(child, childName);
                if (nested != null) return nested;
            }
            return null;
        }
    }
}
