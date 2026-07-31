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
        [SerializeField] private RectTransform foodSlotsRoot;
        [SerializeField] private GameObject[] foodSlots = new GameObject[3];
        [SerializeField] private Image[] foodIcons = new Image[3];
        [SerializeField] private Slider timeSlider;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private Button skipButton;

        private GameplayManager gameplayManager;
        private int displayedRevision = -1;
        private bool isSuppressed;

        /// <summary>
        /// Tạm ẩn toàn bộ Order khi một popup quan trọng (Pause/Kết quả) đang mở.
        /// Order vẫn tiếp tục giữ nguyên dữ liệu và thời gian vì GameplayManager
        /// mới là nơi quyết định trạng thái tạm dừng.
        /// </summary>
        public void SetSuppressed(bool suppressed)
        {
            isSuppressed = suppressed;
            if (suppressed && orderCard != null)
            {
                orderCard.SetActive(false);
            }
        }

        public void Configure(
            GameObject card,
            RectTransform slotsRoot,
            GameObject[] slots,
            Image[] icons,
            Slider slider,
            TextMeshProUGUI timer,
            Button skip)
        {
            orderCard = card;
            foodSlotsRoot = slotsRoot;
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

            bool visible =
                !isSuppressed &&
                gameplayManager.HasActiveOrder();
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
            for (int index = 0; index < foodIcons.Length; index++)
            {
                bool showSlot = index < visibleCount;
                if (index < foodSlots.Length && foodSlots[index] != null)
                {
                    foodSlots[index].SetActive(showSlot);
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

            if (foodSlotsRoot != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(foodSlotsRoot);
            }
        }
    }
}
