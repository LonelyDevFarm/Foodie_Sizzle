using UnityEngine;
using UnityEngine.UI;

namespace FoodieSizzle
{
    /// <summary>
    /// Điều khiển hình nền và vị trí núm của công tắc cài đặt.
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    public class SettingsToggleVisual : MonoBehaviour
    {
        [SerializeField] private Image trackImage;
        [SerializeField] private RectTransform knob;
        [SerializeField] private Sprite onSprite;
        [SerializeField] private Sprite offSprite;
        [SerializeField] private float knobOffset = 25f;
        [SerializeField, Range(0.6f, 1f)]
        private float knobHeightRatio = 0.88f;
        [SerializeField, Min(0f)] private float horizontalPadding = 3f;
        [SerializeField] private float knobVerticalOffset = -4f;

        private Toggle toggle;

        public void Configure(
            Image track,
            RectTransform knobTransform,
            Sprite enabledSprite,
            Sprite disabledSprite,
            float horizontalOffset = 25f,
            float verticalOffset = 8f)
        {
            trackImage = track;
            knob = knobTransform;
            onSprite = enabledSprite;
            offSprite = disabledSprite;
            knobOffset = horizontalOffset;
            knobVerticalOffset = verticalOffset;
        }

        private void Awake()
        {
            toggle = GetComponent<Toggle>();
            toggle.onValueChanged.AddListener(Refresh);
            Refresh(toggle.isOn);
        }

        private void OnDestroy()
        {
            if (toggle != null)
            {
                toggle.onValueChanged.RemoveListener(Refresh);
            }
        }

        private void Refresh(bool enabled)
        {
            if (trackImage != null)
            {
                trackImage.sprite = enabled ? onSprite : offSprite;
            }

            if (knob != null)
            {
                RectTransform trackRect = trackImage != null
                    ? trackImage.rectTransform
                    : transform as RectTransform;
                float trackHeight = trackRect.rect.height;
                float trackWidth = trackRect.rect.width;

                if (trackHeight > 0f && trackWidth > 0f)
                {
                    float knobSize = trackHeight * knobHeightRatio;
                    knob.sizeDelta = new Vector2(knobSize, knobSize);
                    knobOffset = Mathf.Max(
                        0f,
                        (trackWidth - knobSize) * 0.5f -
                        horizontalPadding);
                }

                Vector2 position = knob.anchoredPosition;
                position.x = enabled ? knobOffset : -knobOffset;
                // Track có bóng đổ phía dưới nên cần bù quang học nhẹ.
                position.y = knobVerticalOffset;
                knob.anchoredPosition = position;
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            if (toggle != null)
            {
                Refresh(toggle.isOn);
            }
        }
    }
}
