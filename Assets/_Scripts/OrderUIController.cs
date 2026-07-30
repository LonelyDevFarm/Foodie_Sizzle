using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieSizzle
{
    /// <summary>
    /// Hiển thị Order hiện tại. Root luôn hoạt động để có thể tự bật Card
    /// khi Order được kích hoạt giữa màn chơi.
    /// </summary>
    public class OrderUIController : MonoBehaviour
    {
        [SerializeField] private GameObject orderCard;
        [SerializeField] private GameObject[] foodSlots = new GameObject[3];
        [SerializeField] private Image[] foodIcons = new Image[3];
        [SerializeField] private Slider timeSlider;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private Button skipButton;

        private GameplayManager gameplayManager;
        private int displayedRevision = -1;

        public void Configure(
            GameObject card,
            GameObject[] slots,
            Image[] icons,
            Slider slider,
            TextMeshProUGUI timer,
            Button skip)
        {
            orderCard = card;
            foodSlots = slots;
            foodIcons = icons;
            timeSlider = slider;
            timeText = timer;
            skipButton = skip;
            BindSkipButton();
        }

        private void Awake()
        {
            FindGameplayManager();
            BindSkipButton();
        }

        private void BindSkipButton()
        {
            if (skipButton == null) return;
            skipButton.onClick.RemoveListener(SkipOrder);
            skipButton.onClick.AddListener(SkipOrder);
        }

        private void SkipOrder()
        {
            if (gameplayManager == null) FindGameplayManager();
            gameplayManager?.TrySkipActiveOrder();
        }

        private void Update()
        {
            if (gameplayManager == null)
            {
                FindGameplayManager();
                if (gameplayManager == null) return;
            }

            bool visible = gameplayManager.HasActiveOrder();
            if (orderCard != null && orderCard.activeSelf != visible)
            {
                orderCard.SetActive(visible);
            }

            if (!visible) return;

            int revision = gameplayManager.GetOrderRevision();
            if (revision != displayedRevision)
            {
                displayedRevision = revision;
                RefreshFoodIcons();
            }

            if (timeSlider != null)
            {
                timeSlider.SetValueWithoutNotify(
                    gameplayManager.GetOrderTimeRatio());
            }

            if (timeText != null)
            {
                float remaining =
                    gameplayManager.GetOrderRemainingTime();
                int minutes = Mathf.FloorToInt(remaining / 60f);
                int seconds = Mathf.CeilToInt(remaining % 60f);
                if (seconds >= 60)
                {
                    minutes++;
                    seconds = 0;
                }
                timeText.text = $"{minutes:00}:{seconds:00}";
            }
        }

        private void FindGameplayManager()
        {
            gameplayManager =
                Object.FindFirstObjectByType<GameplayManager>();
        }

        private void RefreshFoodIcons()
        {
            IReadOnlyList<FoodItemData> items =
                gameplayManager.GetActiveOrderItems();
            int visibleCount = Mathf.Min(
                items.Count,
                Mathf.Min(foodSlots.Length, foodIcons.Length));

            // Chỉ hiện đúng số ô cần dùng và luôn căn cả nhóm vào giữa.
            const float slotWidth = 0.175f;
            const float gap = 0.022f;
            const float contentCenterX = 0.69f;
            float totalWidth =
                visibleCount * slotWidth +
                Mathf.Max(0, visibleCount - 1) * gap;
            float startX = contentCenterX - totalWidth * 0.5f;

            for (int index = 0; index < foodIcons.Length; index++)
            {
                bool showSlot = index < visibleCount;
                if (index < foodSlots.Length && foodSlots[index] != null)
                {
                    foodSlots[index].SetActive(showSlot);
                    if (showSlot)
                    {
                        RectTransform slotRect =
                            foodSlots[index].GetComponent<RectTransform>();
                        float minX = startX + index * (slotWidth + gap);
                        slotRect.anchorMin = new Vector2(minX, 0.395f);
                        slotRect.anchorMax =
                            new Vector2(minX + slotWidth, 0.855f);
                        slotRect.anchoredPosition = Vector2.zero;
                        slotRect.sizeDelta = Vector2.zero;
                    }
                }

                Image icon = foodIcons[index];
                if (icon == null) continue;

                bool hasItem =
                    showSlot &&
                    items[index] != null &&
                    items[index].itemSprite != null;
                icon.gameObject.SetActive(hasItem);
                if (!hasItem) continue;

                icon.sprite = items[index].itemSprite;
                icon.preserveAspect = true;
                icon.rectTransform.localRotation =
                    Quaternion.Euler(0f, 0f, -13f);
                icon.color = gameplayManager.IsOrderItemCompleted(index)
                    ? new Color(1f, 1f, 1f, 0.28f)
                    : Color.white;
            }
        }
    }
}
