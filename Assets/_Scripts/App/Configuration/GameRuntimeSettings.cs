using UnityEngine;

namespace FoodieSizzle
{
    [CreateAssetMenu(
        fileName = "GameRuntimeSettings",
        menuName = "Foodie Sizzle/Settings/Game Runtime Settings",
        order = 1)]
    public sealed class GameRuntimeSettings : ScriptableObject
    {
        [SerializeField, Range(30, 120)]
        private int targetFrameRate = 60;

        [SerializeField] private bool useVSync;

        [SerializeField, Range(1, 4)]
        private int vSyncCount = 1;

        [SerializeField] private bool runInBackground;

        public int TargetFrameRate => targetFrameRate;
        public bool UseVSync => useVSync;
        public int VSyncCount => vSyncCount;
        public bool RunInBackground => runInBackground;

        private void OnValidate()
        {
            targetFrameRate = Mathf.Clamp(targetFrameRate, 30, 120);
            vSyncCount = Mathf.Clamp(vSyncCount, 1, 4);
        }
    }
}
