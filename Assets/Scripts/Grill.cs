using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FoodieSizzle
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class Grill : MonoBehaviour
    {
        [Header("Slots Setup")]
        [Tooltip("3 elements: Left, Middle, Right slots on the grill")]
        public Transform[] activeSlots = new Transform[3];
        [Tooltip("3 elements: Left, Middle, Right slots on the plate")]
        public Transform[] waitingSlots = new Transform[3];

        [Header("State")]
        public List<SkewerVisual> activeSkewers = new List<SkewerVisual>();
        public List<SkewerVisual> waitingSkewers = new List<SkewerVisual>();

        private GameplayManager gameplayManager;
        private bool isAnimating = false;

        public void Initialize(List<FoodItemData> initialActive, List<FoodItemData> initialWaiting, GameplayManager manager)
        {
            gameplayManager = manager;
            activeSkewers.Clear();
            waitingSkewers.Clear();

            // Spawn active skewers on the grill
            for (int i = 0; i < initialActive.Count && i < 3; i++)
            {
                if (initialActive[i] == null) continue;
                SkewerVisual skewer = SpawnSkewer(initialActive[i]);
                skewer.transform.position = activeSlots[i].position;
                activeSkewers.Add(skewer);
            }

            // Spawn waiting skewers on the plate
            for (int i = 0; i < initialWaiting.Count && i < 3; i++)
            {
                if (initialWaiting[i] == null) continue;
                SkewerVisual skewer = SpawnSkewer(initialWaiting[i]);
                skewer.transform.position = waitingSlots[i].position;
                waitingSkewers.Add(skewer);
            }
        }

        private SkewerVisual SpawnSkewer(FoodItemData data)
        {
            GameObject skewerGo = new GameObject($"Skewer_{data.itemId}");
            skewerGo.transform.SetParent(transform);
            
            // Add SpriteRenderer and SkewerVisual components
            SpriteRenderer sr = skewerGo.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 5; // Render above the grill background
            
            SkewerVisual skewer = skewerGo.AddComponent<SkewerVisual>();
            skewer.SetData(data);
            
            return skewer;
        }

        // Check if a skewer can be added to this grill
        public bool CanPush(FoodItemData item)
        {
            if (isAnimating) return false;
            if (activeSkewers.Count >= 3) return false;

            // Can push if empty or if the rightmost skewer is of the same type
            if (activeSkewers.Count == 0) return true;

            SkewerVisual topSkewer = activeSkewers[activeSkewers.Count - 1];
            return topSkewer.GetData().itemId == item.itemId;
        }

        // Push a skewer onto the grill (animate to slot)
        public void Push(SkewerVisual skewer, float duration = 0.25f)
        {
            skewer.transform.SetParent(transform);
            activeSkewers.Add(skewer);
            
            Vector3 targetPosition = activeSlots[activeSkewers.Count - 1].position;
            skewer.MoveTo(targetPosition, duration);
        }

        // Remove and return the rightmost skewer from the grill
        public SkewerVisual Pop()
        {
            if (activeSkewers.Count == 0 || isAnimating) return null;

            SkewerVisual popped = activeSkewers[activeSkewers.Count - 1];
            activeSkewers.RemoveAt(activeSkewers.Count - 1);
            return popped;
        }

        // Hover effect helper: lift the rightmost skewer slightly when selected
        public void LiftTopSkewer(bool lift, float offset = 0.4f)
        {
            if (activeSkewers.Count == 0) return;
            
            SkewerVisual topSkewer = activeSkewers[activeSkewers.Count - 1];
            Vector3 targetPos = activeSlots[activeSkewers.Count - 1].position;
            if (lift)
            {
                targetPos.y += offset;
            }
            topSkewer.MoveTo(targetPos, 0.1f);
        }

        // Check if the grill has 3 identical skewers and should clear them
        public void CheckAndClear()
        {
            if (activeSkewers.Count == 3)
            {
                string firstId = activeSkewers[0].GetData().itemId;
                bool isMatch = true;
                
                for (int i = 1; i < 3; i++)
                {
                    if (activeSkewers[i].GetData().itemId != firstId)
                    {
                        isMatch = false;
                        break;
                    }
                }

                if (isMatch)
                {
                    StartCoroutine(ClearGrillCoroutine());
                }
            }
        }

        private IEnumerator ClearGrillCoroutine()
        {
            isAnimating = true;
            gameplayManager.SetBoardLocked(true);

            // Pop all 3 skewers to clear
            List<SkewerVisual> toClear = new List<SkewerVisual>(activeSkewers);
            activeSkewers.Clear();

            // Sizzle/shrink animation
            float elapsed = 0f;
            float duration = 0.3f;
            while (elapsed < duration)
            {
                float scale = Mathf.Lerp(1f, 0f, elapsed / duration);
                foreach (var skewer in toClear)
                {
                    if (skewer != null) skewer.transform.localScale = Vector3.one * scale;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Destroy objects and notify progress
            foreach (var skewer in toClear)
            {
                if (skewer != null)
                {
                    gameplayManager.OnSkewerCleared(skewer.GetData());
                    Destroy(skewer.gameObject);
                }
            }

            yield return new WaitForSeconds(0.1f);

            // If the grill is empty, slide waiting skewers up from the plate
            if (waitingSkewers.Count > 0)
            {
                yield return StartCoroutine(SlideUpWaitingSkewersCoroutine());
            }

            isAnimating = false;
            gameplayManager.SetBoardLocked(false);
            
            // Check if game has ended or needs further updates
            gameplayManager.CheckGameStatus();
        }

        private IEnumerator SlideUpWaitingSkewersCoroutine()
        {
            // Move references from waiting list to active list
            activeSkewers = new List<SkewerVisual>(waitingSkewers);
            waitingSkewers.Clear();

            // Animate each sliding up to its active slot
            float duration = 0.3f;
            for (int i = 0; i < activeSkewers.Count; i++)
            {
                Vector3 targetPos = activeSlots[i].position;
                activeSkewers[i].MoveTo(targetPos, duration);
            }

            yield return new WaitForSeconds(duration);

            // Request new waiting skewers if possible
            List<FoodItemData> newWaitingData = gameplayManager.RequestReplacementWaitingSkewers();
            for (int i = 0; i < newWaitingData.Count && i < 3; i++)
            {
                if (newWaitingData[i] == null) continue;
                SkewerVisual skewer = SpawnSkewer(newWaitingData[i]);
                skewer.transform.position = waitingSlots[i].position;
                waitingSkewers.Add(skewer);
                
                // Pop effect: scale up
                skewer.transform.localScale = Vector3.zero;
                skewer.MoveTo(waitingSlots[i].position, 0.2f);
                StartCoroutine(ScaleUpSkewerCoroutine(skewer, 0.2f));
            }

            if (newWaitingData.Count > 0)
            {
                yield return new WaitForSeconds(0.2f);
            }
        }

        private IEnumerator ScaleUpSkewerCoroutine(SkewerVisual skewer, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (skewer == null) yield break;
                skewer.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (skewer != null) skewer.transform.localScale = Vector3.one;
        }

        // On Mouse Down is called when the user clicks the collider of this grill
        private void OnMouseDown()
        {
            if (gameplayManager != null)
            {
                gameplayManager.OnGrillClicked(this);
            }
        }
    }
}
