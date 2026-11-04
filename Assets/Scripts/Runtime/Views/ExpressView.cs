using System;
using UnityEngine;

namespace Paw.Runtime
{
    [DisallowMultipleComponent]
    public sealed class ExpressView : MonoBehaviour
    {
        [SerializeField, Tooltip("Moving collector artwork.")] private Transform visual;
        [SerializeField, Tooltip("Landing positions for collected pastries.")] private Transform[] slots;
        [SerializeField, Tooltip("Collector entrance offset.")] private Vector3 entranceOffset = new Vector3(-300, 0, 0);
        [SerializeField, Tooltip("Collector departure offset.")] private Vector3 departureOffset = new Vector3(400, 0, 0);
        [SerializeField, Tooltip("Entrance animation duration in seconds."), Min(0)] private float entranceDuration = .35f;
        [SerializeField, Tooltip("Departure animation duration in seconds."), Min(0)] private float departureDuration = .45f;
        private long start;
        private bool departing;
        private bool visible;
        public bool IsConfigured => visual != null && slots != null && slots.Length > 0;
        public Vector3 Slot(int index) => slots[Math.Min(index, slots.Length - 1)].position;
        public void Clear() { visible = false; departing = false; if (visual != null) visual.gameObject.SetActive(false); }
        public void Arrive(long time) { if (!IsConfigured) throw new InvalidOperationException("ExpressView needs artwork and slots."); start = time; departing = false; visible = true; visual.gameObject.SetActive(true); visual.localPosition = entranceOffset; }
        public void Depart(long time) { start = time; departing = true; }
        public void Tick(long time)
        {
            if (!visible) return;
            float duration = departing ? departureDuration : entranceDuration;
            float t = duration <= 0 ? 1 : Mathf.Clamp01((time - start) / (duration * 1000000f));
            float eased = departing ? t * t : 1 - (1 - t) * (1 - t);
            visual.localPosition = departing ? Vector3.Lerp(Vector3.zero, departureOffset, eased) : Vector3.Lerp(entranceOffset, Vector3.zero, eased);
            if (departing && t >= 1) Clear();
        }
    }
}
