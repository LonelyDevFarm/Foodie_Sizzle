using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FoodieSizzle
{
    public class GameplayManager : MonoBehaviour
    {
        [Header("Grid Setup")]
        public Grill[] grills = new Grill[12]; // Drag the 12 grills in hierarchy here (3x4 grid)
        public List<FoodItemData> possibleFoodItems;

        [Header("Level Goals")]
        public int skewersTarget = 18; // Must clear 18 skewers (6 sets of 3)
        public float timeRemaining = 300f; // 5 minutes

        [Header("Select Effect")]
        public float liftOffset = 0.5f;

        private Queue<FoodItemData> levelSkewersPool = new Queue<FoodItemData>();
        private Grill selectedGrill = null;
        private bool isBoardLocked = false;
        private int skewersClearedCount = 0;
        private bool isGameActive = false;

        private void Start()
        {
            StartNewLevel();
        }

        private void Update()
        {
            if (isGameActive)
            {
                UpdateTimer();
            }
        }

        private void UpdateTimer()
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                if (timeRemaining <= 0)
                {
                    timeRemaining = 0;
                    GameOver(false); // Time out
                }
            }
        }

        public void SetBoardLocked(bool locked)
        {
            isBoardLocked = locked;
        }

        public bool IsBoardLocked()
        {
            return isBoardLocked;
        }

        public void StartNewLevel()
        {
            isGameActive = true;
            isBoardLocked = false;
            skewersClearedCount = 0;
            selectedGrill = null;

            GenerateSkewersPool();
            DistributeSkewersToGrills();
            
            Debug.Log($"Foodie Sizzle level started! Target: {skewersTarget} skewers.");
        }

        // Generate a pool of skewers that matches in sets of 3 to guarantee they can all be cleared
        private void GenerateSkewersPool()
        {
            levelSkewersPool.Clear();
            List<FoodItemData> tempPool = new List<FoodItemData>();

            int setsCount = Mathf.CeilToInt(skewersTarget / 3f);
            for (int i = 0; i < setsCount; i++)
            {
                // Select a random food item data
                FoodItemData randomItem = possibleFoodItems[Random.Range(0, possibleFoodItems.Count)];
                
                // Add 3 copies of this item
                tempPool.Add(randomItem);
                tempPool.Add(randomItem);
                tempPool.Add(randomItem);
            }

            // Shuffle the pool
            for (int i = 0; i < tempPool.Count; i++)
            {
                int randIdx = Random.Range(i, tempPool.Count);
                FoodItemData temp = tempPool[i];
                tempPool[i] = tempPool[randIdx];
                tempPool[randIdx] = temp;
            }

            // Enqueue all elements
            foreach (var item in tempPool)
            {
                levelSkewersPool.Enqueue(item);
            }
        }

        private void DistributeSkewersToGrills()
        {
            // For each of the 9 grills:
            // Spawn 1 or 2 active skewers on the grill, and 1 or 2 waiting skewers on the plate
            for (int i = 0; i < grills.Length; i++)
            {
                if (grills[i] == null) continue;

                List<FoodItemData> activeList = new List<FoodItemData>();
                List<FoodItemData> waitingList = new List<FoodItemData>();

                // Distribute from pool: 1 to 2 active skewers
                int activeCount = Random.Range(1, 3);
                for (int a = 0; a < activeCount; a++)
                {
                    if (levelSkewersPool.Count > 0)
                    {
                        activeList.Add(levelSkewersPool.Dequeue());
                    }
                }

                // Distribute from pool: 1 to 2 waiting skewers
                int waitingCount = Random.Range(1, 3);
                for (int w = 0; w < waitingCount; w++)
                {
                    if (levelSkewersPool.Count > 0)
                    {
                        waitingList.Add(levelSkewersPool.Dequeue());
                    }
                }

                grills[i].Initialize(activeList, waitingList, this);
            }
        }

        // Called by a Grill when it becomes empty and its plate is empty
        // Returns next waiting skewers from the pool
        public List<FoodItemData> RequestReplacementWaitingSkewers()
        {
            List<FoodItemData> newWaiting = new List<FoodItemData>();
            
            // Spawn up to 2 new waiting skewers from the pool
            int spawnCount = Random.Range(1, 3);
            for (int i = 0; i < spawnCount; i++)
            {
                if (levelSkewersPool.Count > 0)
                {
                    newWaiting.Add(levelSkewersPool.Dequeue());
                }
            }

            return newWaiting;
        }

        // Tap input handling
        public void OnGrillClicked(Grill grill)
        {
            if (isBoardLocked || !isGameActive) return;

            if (selectedGrill == null)
            {
                // Select grill if it has skewers on it
                if (grill.activeSkewers.Count > 0)
                {
                    selectedGrill = grill;
                    selectedGrill.LiftTopSkewer(true, liftOffset);
                }
            }
            else
            {
                // Target is clicked
                if (grill == selectedGrill)
                {
                    // Deselect
                    selectedGrill.LiftTopSkewer(false, liftOffset);
                    selectedGrill = null;
                }
                else
                {
                    // Check if we can move the top skewer from selected to target grill
                    SkewerVisual poppedSkewer = selectedGrill.activeSkewers[selectedGrill.activeSkewers.Count - 1];
                    
                    if (grill.CanPush(poppedSkewer.GetData()))
                    {
                        // Execute move
                        selectedGrill.Pop();
                        selectedGrill.LiftTopSkewer(false, liftOffset); // Put down selection state

                        grill.Push(poppedSkewer);
                        
                        // Check if selected grill became empty (needs waiting skewers to slide up)
                        if (selectedGrill.activeSkewers.Count == 0 && selectedGrill.waitingSkewers.Count > 0)
                        {
                            selectedGrill.CheckAndClear();
                        }

                        // Check target grill for completions
                        grill.CheckAndClear();

                        selectedGrill = null;
                    }
                    else
                    {
                        // Target grill cannot accept it.
                        // Automatically switch selection to target grill if target has skewers,
                        // otherwise just deselect.
                        selectedGrill.LiftTopSkewer(false, liftOffset);
                        
                        if (grill.activeSkewers.Count > 0)
                        {
                            selectedGrill = grill;
                            selectedGrill.LiftTopSkewer(true, liftOffset);
                        }
                        else
                        {
                            selectedGrill = null;
                        }
                    }
                }
            }
        }

        public void OnSkewerCleared(FoodItemData skewerData)
        {
            skewersClearedCount++;
            Debug.Log($"Skewer cleared! Total: {skewersClearedCount}/{skewersTarget}");
        }

        public void CheckGameStatus()
        {
            if (skewersClearedCount >= skewersTarget)
            {
                GameOver(true); // Win
            }
            else if (IsDeadlock())
            {
                Debug.LogWarning("Deadlock detected! Shuffling board...");
                StartCoroutine(ShuffleAllGrillsCoroutine());
            }
        }

        // Check if there are no possible moves left on the entire board
        private bool IsDeadlock()
        {
            // If any grill is empty, player can move any top skewer there (no deadlock)
            for (int i = 0; i < grills.Length; i++)
            {
                if (grills[i] != null && grills[i].activeSkewers.Count == 0)
                {
                    return false;
                }
            }

            // Check if any top skewer can be placed on any other grill
            for (int i = 0; i < grills.Length; i++)
            {
                if (grills[i] == null || grills[i].activeSkewers.Count == 0) continue;

                FoodItemData topItem = grills[i].activeSkewers[grills[i].activeSkewers.Count - 1].GetData();

                for (int j = 0; j < grills.Length; j++)
                {
                    if (i == j || grills[j] == null) continue;
                    if (grills[j].CanPush(topItem))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private IEnumerator ShuffleAllGrillsCoroutine()
        {
            isBoardLocked = true;
            yield return new WaitForSeconds(0.5f);

            // Shuffling algorithm for Skewer Sort:
            // 1. Gather all active items on the grills.
            // 2. Shuffle them.
            // 3. Redistribute them.
            // (In a real game, we would do this with check loops to ensure a valid move exists.
            // Here, we do a basic shuffle and check, repeating if still deadlocked).
            
            bool hasValidMove = false;
            int attempts = 0;

            while (!hasValidMove && attempts < 50)
            {
                attempts++;
                
                List<FoodItemData> allActiveData = new List<FoodItemData>();
                for (int i = 0; i < grills.Length; i++)
                {
                    if (grills[i] == null) continue;
                    foreach (var skewer in grills[i].activeSkewers)
                    {
                        allActiveData.Add(skewer.GetData());
                    }
                }

                // Shuffle list
                for (int i = 0; i < allActiveData.Count; i++)
                {
                    int rand = Random.Range(i, allActiveData.Count);
                    FoodItemData temp = allActiveData[i];
                    allActiveData[i] = allActiveData[rand];
                    allActiveData[rand] = temp;
                }

                // Redistribute visually and logic-wise
                int idx = 0;
                for (int i = 0; i < grills.Length; i++)
                {
                    if (grills[i] == null) continue;

                    // Keep same size but distribute new shuffled items
                    int currentCount = grills[i].activeSkewers.Count;
                    for (int k = 0; k < currentCount; k++)
                    {
                        if (idx < allActiveData.Count)
                        {
                            grills[i].activeSkewers[k].SetData(allActiveData[idx]);
                            grills[i].activeSkewers[k].MoveTo(grills[i].activeSlots[k].position, 0.3f);
                            idx++;
                        }
                    }
                }

                yield return new WaitForSeconds(0.35f);

                // Re-evaluate deadlock
                if (!IsDeadlock())
                {
                    hasValidMove = true;
                }
            }

            isBoardLocked = false;
            Debug.Log($"Shuffled board successfully after {attempts} attempts.");
        }

        private void GameOver(bool isWin)
        {
            isGameActive = false;
            if (isWin)
            {
                Debug.Log("LEVEL COMPLETED! You won!");
            }
            else
            {
                Debug.Log("GAME OVER! Time ran out.");
            }
        }

        public string GetFormattedTime()
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);
            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }

        public string GetProgressString()
        {
            return $"{skewersClearedCount}/{skewersTarget}";
        }
    }
}
