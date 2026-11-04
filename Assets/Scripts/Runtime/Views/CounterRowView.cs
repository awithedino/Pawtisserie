using UnityEngine;

namespace Paw.Runtime
{
    [DisallowMultipleComponent]
    public sealed class CounterRowView : MonoBehaviour
    {
        [SerializeField, Tooltip("Three counter box lanes.")] private BoxLaneView[] lanes;
        [SerializeField, Tooltip("Miniature queued box views.")] private BoxLaneView[] queue;
        public BoxLaneView[] Lanes => lanes;
        public BoxLaneView[] Queue => queue;
        public bool IsConfigured => lanes != null && lanes.Length == 3 && queue != null && queue.Length >= 12;
    }
}
