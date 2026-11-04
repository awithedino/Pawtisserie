using System;
using Paw.Presentation;
using Paw.Sim;
using UnityEngine;

namespace Paw.Runtime
{
    [DisallowMultipleComponent]
    public sealed class BoxLaneView : MonoBehaviour
    {
        [SerializeField, Tooltip("Movable box visual container.")] private Transform visual;
        [SerializeField, Tooltip("Box body renderer.")] private SpriteRenderer body;
        [SerializeField, Tooltip("Pastry type marker renderer.")] private SpriteRenderer marker;
        [SerializeField, Tooltip("Type sprites matching pastry IDs.")] private Sprite[] typeSprites;
        [SerializeField, Tooltip("Rotation of the capsule type marker in degrees.")] private float macaronRotation = 90;
        [SerializeField, Tooltip("Three capacity markers.")] private SpriteRenderer[] spots;
        [SerializeField, Tooltip("Gift tag.")] private GameObject giftTag;
        [SerializeField, Tooltip("Closing lid renderer, with authored size.")] private SpriteRenderer lid;
        [SerializeField, Tooltip("Arrival offset in logical units.")] private Vector3 arrivalOffset = new Vector3(0, 110, 0);
        [SerializeField, Tooltip("Departure offset in logical units.")] private Vector3 departureOffset = new Vector3(0, 140, 0);
        [SerializeField, Tooltip("Filled slot tint.")] private Color filledColor = new Color(.36f, .22f, .18f);
        private FlightPath motion;
        private bool moving;
        private int reserved;
        private int filled;
        private int need;
        private Vector3 lidSize;
        private bool lidSizeCached;
        private long closeStart;
        private long closeEnd;
        private bool closing;
        public int Id { get; private set; }
        public int Type { get; private set; }
        public bool IsConfigured => visual != null && body != null && marker != null && lid != null && spots != null && spots.Length == 3 && giftTag != null && typeSprites != null && typeSprites.Length == 4;
        public void Show(BoxState box, ViewConfig config)
        {
            if (!IsConfigured) throw new InvalidOperationException("BoxLaneView: visual, markers, sprites and gift tag are required.");
            moving = false; visual.localPosition = Vector3.zero;
            if (!lidSizeCached) { lidSize = lid.transform.localScale; lidSizeCached = true; }
            closing = false; lid.enabled = false; lid.transform.localScale = lidSize;
            Id = box.Id; Type = box.Type; need = box.Need; filled = box.Filled; reserved = box.Reserved;
            body.color = Color.Lerp(config.PastryColor(Type), Color.white, .62f);
            marker.sprite = typeSprites[Type]; marker.color = config.PastryColor(Type);
            Vector2 bounds = marker.sprite.bounds.size; marker.transform.localScale = Vector3.one * (19 / Mathf.Max(bounds.x, bounds.y));
            marker.transform.localRotation = Quaternion.Euler(0, 0, Type == 1 ? macaronRotation : 0);
            giftTag.SetActive(box.Gift.HasValue); visual.gameObject.SetActive(true); Paint();
        }
        public void Hide() { Id = 0; moving = false; visual.gameObject.SetActive(false); }
        public Vector3 ReservePosition(long time)
        {
            int slot = Mathf.Min(reserved++, spots.Length - 1);
            return transform.localPosition + VisualPositionAt(time) + spots[slot].transform.localPosition;
        }
        public void Land() { filled++; Paint(); }
        public void SetNeed(int value) { need = value; Paint(); }
        public void SetGift(bool value) => giftTag.SetActive(value);
        public void Close(long start, long end) { closeStart = start; closeEnd = end; closing = true; lid.enabled = true; }
        public void Arrive(long start, long end)
        {
            visual.localPosition = arrivalOffset;
            motion = new FlightPath(new Point2(arrivalOffset.x, arrivalOffset.y), new Point2(0, 0), start, end, 0); moving = true;
        }
        public void Leave(long start, long end)
        {
            Vector3 from = VisualPositionAt(start);
            motion = new FlightPath(new Point2(from.x, from.y), new Point2(departureOffset.x, departureOffset.y), start, end, 0); moving = true;
        }
        private Vector3 VisualPositionAt(long time)
        {
            if (!moving) return visual.localPosition;
            var point = motion.At(time); return new Vector3((float)point.X, (float)point.Y, 0);
        }
        public void Evaluate(long time)
        {
            if (moving) { visual.localPosition = VisualPositionAt(time); if (time >= motion.EndTime) moving = false; }
            if (closing)
            {
                float t = closeEnd == closeStart ? 1 : Mathf.Clamp01((float)(time - closeStart) / (closeEnd - closeStart));
                lid.transform.localScale = new Vector3(lidSize.x, lidSize.y * t, lidSize.z);
                if (t >= 1) closing = false;
            }
        }
        private void Paint()
        {
            for (int i = 0; i < spots.Length; i++)
            { spots[i].enabled = i < need; spots[i].color = i < filled ? filledColor : new Color(1, 1, 1, .6f); }
        }
    }
}
