using Paw.Presentation;
using UnityEngine;

namespace Paw.Runtime
{
    [DisallowMultipleComponent]
    public sealed class BadgeView : MonoBehaviour
    {
        [SerializeField, Tooltip("Badge glyph.")] private TextMesh label;
        [SerializeField, Tooltip("Goes into a box (GDD §11.3 green tick).")] private Color boxColor = new Color(.15f, .55f, .3f);
        [SerializeField, Tooltip("Goes onto a plate (gold arrow).")] private Color plateColor = new Color(.75f, .48f, .08f);
        [SerializeField, Tooltip("Goes onto the rack (steel arrow).")] private Color rackColor = new Color(.3f, .4f, .5f);
        [SerializeField, Tooltip("Overflow (red !).")] private Color overflowColor = new Color(.85f, .12f, .18f);
        [SerializeField, Tooltip("Cloche breaks, pastry stays (blue crack).")] private Color clocheColor = new Color(.3f, .55f, .85f);
        [SerializeField, Tooltip("Cream can is set off (pink dot).")] private Color canColor = new Color(.85f, .25f, .55f);
        public void Show(PreviewBadgeKind kind)
        {
            switch (kind)
            {
                case PreviewBadgeKind.Box: label.text = "✓"; label.color = boxColor; break;
                case PreviewBadgeKind.Plate: label.text = "→"; label.color = plateColor; break;
                case PreviewBadgeKind.Rack: label.text = "→"; label.color = rackColor; break;
                case PreviewBadgeKind.Overflow: label.text = "!"; label.color = overflowColor; break;
                case PreviewBadgeKind.Cloche: label.text = "×"; label.color = clocheColor; break;
                case PreviewBadgeKind.Can: label.text = "•"; label.color = canColor; break;
            }
            gameObject.SetActive(true);
        }
    }
}
