using UnityEngine;

namespace FoodieSizzle
{
    [CreateAssetMenu(
        fileName = "FeedbackAudioLibrary",
        menuName = "Foodie Sizzle/Feedback Audio Library",
        order = 3)]
    public class FeedbackAudioLibrary : ScriptableObject
    {
        [Header("Nhạc nền")]
        public AudioClip gameplayMusic;

        [Header("Thao tác")]
        public AudioClip uiButton;
        public AudioClip selectSkewer;
        public AudioClip validDrop;
        public AudioClip invalidDrop;
        public AudioClip[] matchingSets;

        [Header("Order")]
        public AudioClip orderAppears;
        public AudioClip orderCompleted;
        public AudioClip orderWarning;
        public AudioClip orderFailed;

        [Header("Vật phẩm")]
        public AudioClip boxBooster;
        public AudioClip refreshBooster;
        public AudioClip timeBooster;
        public AudioClip plusBooster;

        [Header("Kết quả")]
        public AudioClip win;
        public AudioClip lose;

        public AudioClip GetClip(FeedbackCue cue, int comboIndex = 0)
        {
            switch (cue)
            {
                case FeedbackCue.UiButton:
                    return uiButton;
                case FeedbackCue.SelectSkewer:
                    return selectSkewer;
                case FeedbackCue.ValidDrop:
                    return validDrop;
                case FeedbackCue.InvalidDrop:
                    return invalidDrop;
                case FeedbackCue.MatchingSet:
                    if (matchingSets == null || matchingSets.Length == 0)
                        return null;
                    return matchingSets[
                        Mathf.Abs(comboIndex) % matchingSets.Length];
                case FeedbackCue.OrderAppears:
                    return orderAppears;
                case FeedbackCue.OrderCompleted:
                    return orderCompleted;
                case FeedbackCue.OrderWarning:
                    return orderWarning;
                case FeedbackCue.OrderFailed:
                    return orderFailed;
                case FeedbackCue.BoxBooster:
                    return boxBooster;
                case FeedbackCue.RefreshBooster:
                    return refreshBooster;
                case FeedbackCue.TimeBooster:
                    return timeBooster;
                case FeedbackCue.PlusBooster:
                    return plusBooster;
                case FeedbackCue.Win:
                    return win;
                case FeedbackCue.Lose:
                    return lose;
                default:
                    return null;
            }
        }
    }
}
