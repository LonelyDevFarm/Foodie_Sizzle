using System.Collections;
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
        private TextMeshProUGUI playText;
        private bool isLoading;

        private void Awake()
        {
            // Cho phép bấm Play trực tiếp từ HomeScene trong Editor mà vẫn có
            // nhạc, cài đặt và AudioListener giống luồng đi qua BootScene.
            AppBootstrap.EnsureExists();
            ResolveMissingReferences();
            RefreshLevelText();
            Transform playLabel =
                FindChildRecursive(transform, "PlayText");
            if (playLabel != null)
                playText = playLabel.GetComponent<TextMeshProUGUI>();

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
            if (isLoading) return;
            StartCoroutine(LoadGameplay());
        }

        private IEnumerator LoadGameplay()
        {
            isLoading = true;
            if (playButton != null)
                playButton.interactable = false;
            if (playText != null)
                playText.text = "ĐANG TẢI...";

            // Cho Canvas render trạng thái tải trước khi Unity bắt đầu dựng
            // GameplayScene nặng hơn.
            Canvas.ForceUpdateCanvases();
            yield return null;

            AsyncOperation operation =
                AppSceneFlow.LoadGameplayAsync();
            if (operation == null)
            {
                isLoading = false;
                if (playButton != null)
                    playButton.interactable = true;
                if (playText != null)
                    playText.text = "PLAY";
                yield break;
            }

            while (!operation.isDone)
            {
                yield return null;
            }
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
