using UnityEngine;
using TMPro;

namespace FoodieSizzle
{
    public class GameUIManager : MonoBehaviour
    {
        [Header("UI Text References")]
        public TextMeshProUGUI timerText;
        public TextMeshProUGUI progressText;
        public TextMeshProUGUI levelText;

        [Header("Manager Reference")]
        public GameplayManager gameplayManager;

        public int currentLevelNumber = 2; // Default to Level 2 as shown in the screenshot

        private void Start()
        {
            if (levelText != null)
            {
                levelText.text = $"Lv. {currentLevelNumber}";
            }
        }

        private void Update()
        {
            if (gameplayManager == null) return;

            // Update timer text
            if (timerText != null)
            {
                timerText.text = gameplayManager.GetFormattedTime();
            }

            // Update progress text
            if (progressText != null)
            {
                progressText.text = gameplayManager.GetProgressString();
            }
        }
    }
}
