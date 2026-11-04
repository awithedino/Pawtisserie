using System;
using Paw.Presentation;
using Paw.Sim;
using DG.Tweening;
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
        [SerializeField, Tooltip("Pre-coloured open box backgrounds by pastry type.")] private Sprite[] boxArtwork;
        [SerializeField, Tooltip("Pre-coloured closed boxes by pastry type.")] private Sprite[] closedArtwork;
        [SerializeField, Tooltip("Simple lid for partially resized boxes.")] private Sprite partialLidSprite;
        [SerializeField, Tooltip("Scale of the simple lid for partially resized boxes.")] private Vector3 partialLidScale = new Vector3(43, 4, 1);
        [SerializeField, Tooltip("Position of the simple lid for partially resized boxes.")] private Vector3 partialLidPosition = new Vector3(0, 28, 0);
        [SerializeField, Tooltip("Queue box artwork by pastry type.")] private Sprite[] queueArtwork;
        [SerializeField, Tooltip("Empty capacity markers by pastry type.")] private Sprite[] ghostArtwork;
        [SerializeField, Tooltip("Filled capacity markers by pastry type.")] private Sprite[] filledArtwork;
        [SerializeField, Tooltip("Use small queue artwork instead of a full lane box.")] private bool miniature;
        [SerializeField, Tooltip("Rotation of the capsule type marker in degrees.")] private float macaronRotation = 90;
        [SerializeField, Tooltip("Three capacity markers.")] private SpriteRenderer[] spots;
        [SerializeField, Tooltip("Gift tag.")] private GameObject giftTag;
        [SerializeField, Tooltip("Pre-coloured gift tag renderer.")] private SpriteRenderer giftArtwork;
        [SerializeField, Tooltip("Gift tag sprites in card ID order.")] private Sprite[] giftSprites;
        [SerializeField, Tooltip("Closing lid renderer, with authored size.")] private SpriteRenderer lid;
        [SerializeField, Tooltip("Arrival offset in logical units.")] private Vector3 arrivalOffset = new Vector3(0, 110, 0);
        [SerializeField, Tooltip("Departure offset in logical units.")] private Vector3 departureOffset = new Vector3(0, 140, 0);
        [SerializeField, Tooltip("Filled slot tint.")] private Color filledColor = new Color(.36f, .22f, .18f);
        [SerializeField, Tooltip("Queue pulse size."), Min(0)] private float pulseScale = .15f;
        [SerializeField, Tooltip("Queue pulse duration in seconds."), Min(0)] private float pulseDuration = .2f;
        private FlightPath motion;
        private bool moving;
        private int reserved;
        private int filled;
        private int need;
        private bool artwork;
        private Vector3 lidSize;
        private Vector3 lidPosition;
        private Vector3 closingLidSize;
        private Color lidTint;
        private bool lidSizeCached;
        private Vector3 visualScale;
        private bool visualScaleCached;
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
            if (!visualScaleCached) { visualScale = visual.localScale; visualScaleCached = true; }
            visual.DOKill(); visual.localScale = visualScale;
            if (!lidSizeCached) { lidSize = lid.transform.localScale; lidPosition = lid.transform.localPosition; lidSizeCached = true; }
            closing = false; lid.enabled = false; lid.transform.localScale = lidSize; lid.transform.localPosition = lidPosition;
            Id = box.Id; Type = box.Type; need = box.Need; filled = box.Filled; reserved = box.Reserved;
            artwork = boxArtwork != null && boxArtwork.Length == 4 && boxArtwork[Type] != null;
            body.sprite = artwork ? miniature && queueArtwork != null && queueArtwork.Length == 4 ? queueArtwork[Type] : boxArtwork[Type] : body.sprite;
            body.color = artwork ? Color.white : Color.Lerp(config.PastryColor(Type), Color.white, .62f);
            lidTint = config.PastryColor(Type);
            marker.sprite = typeSprites[Type]; marker.color = config.PastryColor(Type); marker.enabled = !artwork;
            Vector2 bounds = marker.sprite.bounds.size; marker.transform.localScale = Vector3.one * (19 / Mathf.Max(bounds.x, bounds.y));
            marker.transform.localRotation = Quaternion.Euler(0, 0, Type == 1 ? macaronRotation : 0);
            SetGift(box.Gift); visual.gameObject.SetActive(true); Paint();
        }
        public void Hide() { Id = 0; moving = false; visual.DOKill(); if (visualScaleCached) visual.localScale = visualScale; visual.gameObject.SetActive(false); }
        public void Pulse(bool reducedMotion)
        {
            if (reducedMotion || pulseDuration <= 0 || visual == null) return;
            if (!visualScaleCached) { visualScale = visual.localScale; visualScaleCached = true; }
            visual.DOKill(); visual.localScale = visualScale;
            visual.DOPunchScale(Vector3.one * pulseScale, pulseDuration, 6, .5f).SetUpdate(true);
        }
        private void OnDisable() { if (visual != null) { visual.DOKill(); if (visualScaleCached) visual.localScale = visualScale; } }
        public Vector3 ReservePosition(long time)
        {
            int slot = Mathf.Min(reserved++, spots.Length - 1);
            return transform.localPosition + VisualPositionAt(time) + spots[slot].transform.localPosition;
        }
        public void Land() { filled++; Paint(); }
        public void SetNeed(int value) { need = value; Paint(); }
        public void SetGift(CardKind? value)
        {
            giftTag.SetActive(value.HasValue);
            if (value.HasValue && giftArtwork != null && giftSprites != null && giftSprites.Length == 4)
            { giftArtwork.sprite = giftSprites[(int)value.Value]; giftArtwork.color = Color.white; }
        }
        public void Close(long start, long end)
        {
            closeStart = start; closeEnd = end; closing = true; closingLidSize = lidSize;
            if (artwork && need < Rules.BoxCapacity && partialLidSprite != null)
            { lid.sprite = partialLidSprite; lid.transform.localPosition = partialLidPosition; closingLidSize = partialLidScale; lid.color = lidTint; }
            else if (artwork && closedArtwork != null && closedArtwork.Length == 4)
            { lid.sprite = closedArtwork[Type]; lid.color = Color.white; }
            lid.enabled = !miniature;
        }
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
                lid.transform.localScale = new Vector3(closingLidSize.x, closingLidSize.y * t, closingLidSize.z);
                if (t >= 1) closing = false;
            }
        }
        private void Paint()
        {
            for (int i = 0; i < spots.Length; i++)
            {
                spots[i].enabled = !miniature && i < need;
                if (artwork && ghostArtwork != null && ghostArtwork.Length == 4 && filledArtwork != null && filledArtwork.Length == 4)
                { spots[i].sprite = i < filled ? filledArtwork[Type] : ghostArtwork[Type]; spots[i].color = Color.white; }
                else spots[i].color = i < filled ? filledColor : new Color(1, 1, 1, .6f);
            }
        }
    }
}
