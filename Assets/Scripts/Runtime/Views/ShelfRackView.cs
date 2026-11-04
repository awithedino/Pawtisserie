using UnityEngine;

namespace Paw.Runtime
{
    [DisallowMultipleComponent]
    public sealed class ShelfRackView : MonoBehaviour
    {
        [SerializeField, Tooltip("Five plate anchors.")] private Transform[] plates;
        [SerializeField, Tooltip("Two rack anchors.")] private Transform[] rack;
        [SerializeField, Tooltip("Rack artwork.")] private GameObject rackVisual;
        [SerializeField, Tooltip("Stash anchor and artwork.")] private Transform stash;
        [SerializeField, Tooltip("Overflow anchor.")] private Transform overflow;
        public Transform[] Plates => plates;
        public Transform[] Rack => rack;
        public GameObject RackVisual => rackVisual;
        public Transform Stash => stash;
        public Transform Overflow => overflow;
        public bool IsConfigured => plates != null && plates.Length == 5 && rack != null && rack.Length == 2 && rackVisual != null && stash != null && overflow != null;
    }
}
