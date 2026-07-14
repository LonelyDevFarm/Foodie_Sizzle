using System.Collections;
using UnityEngine;

namespace FoodieSizzle
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class SkewerVisual : MonoBehaviour
    {
        private FoodItemData itemData;
        private SpriteRenderer mainRenderer;
        
        private void Awake()
        {
            mainRenderer = GetComponent<SpriteRenderer>();
        }

        public void SetData(FoodItemData data)
        {
            itemData = data;
            
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            if (itemData == null || itemData.itemSprite == null) return;

            if (itemData.needsStacking)
            {
                mainRenderer.enabled = false;

                float[] yOffsets = new float[] { -0.3f, 0.05f, 0.4f };
                
                for (int i = 0; i < 3; i++)
                {
                    GameObject itemGo = new GameObject($"Ingredient_{i}");
                    itemGo.transform.SetParent(transform);
                    itemGo.transform.localPosition = new Vector3(0, yOffsets[i], 0);
                    
                    SpriteRenderer sr = itemGo.AddComponent<SpriteRenderer>();
                    sr.sprite = itemData.itemSprite;
                    sr.sortingOrder = 5 + i; 
                }
            }
            else
            {
                mainRenderer.enabled = true;
                mainRenderer.sprite = itemData.itemSprite;
                mainRenderer.sortingOrder = 5;
            }
        }

        public FoodItemData GetData()
        {
            return itemData;
        }

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