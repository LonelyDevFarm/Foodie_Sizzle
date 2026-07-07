using System.Collections;
using UnityEngine;

namespace FoodieSizzle
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class SkewerVisual : MonoBehaviour
    {
        private FoodItemData itemData;
        private SpriteRenderer spriteRenderer;
        
        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void SetData(FoodItemData data)
        {
            itemData = data;
            if (spriteRenderer != null && itemData != null)
            {
                spriteRenderer.sprite = itemData.itemSprite;
            }
        }

        public FoodItemData GetData()
        {
            return itemData;
        }

        // Smoothly animate the skewer to a new local or world position
        public void MoveTo(Vector3 targetPosition, float duration, bool isLocal = false)
        {
            StartCoroutine(MoveCoroutine(targetPosition, duration, isLocal));
        }

        private IEnumerator MoveCoroutine(Vector3 targetPosition, float duration, bool isLocal)
        {
            Vector3 startPosition = isLocal ? transform.localPosition : transform.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (isLocal)
                {
                    transform.localPosition = Vector3.Lerp(startPosition, targetPosition, elapsed / duration);
                }
                else
                {
                    transform.position = Vector3.Lerp(startPosition, targetPosition, elapsed / duration);
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (isLocal)
            {
                transform.localPosition = targetPosition;
            }
            else
            {
                transform.position = targetPosition;
            }
        }
    }
}
