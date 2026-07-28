using TMPro;
using UnityEngine;

namespace FoodieSizzle
{
    public class HudTextOutlineMirror : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI source;
        [SerializeField] private TextMeshProUGUI[] outlineCopies;

        public void Configure(
            TextMeshProUGUI sourceText, TextMeshProUGUI[] copies)
        {
            source = sourceText;
            outlineCopies = copies;
            SyncText();
        }

        private void LateUpdate()
        {
            SyncText();
        }

        private void SyncText()
        {
            if (source == null || outlineCopies == null) return;

            foreach (TextMeshProUGUI copy in outlineCopies)
            {
                if (copy != null && copy.text != source.text)
                {
                    copy.text = source.text;
                }
            }
        }
    }
}
