using System;
using Paw.Presentation;
using DG.Tweening;
using UnityEngine;

namespace Paw.Runtime
{
    [DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
    public sealed class PastryView : MonoBehaviour
    {
        [SerializeField, Tooltip("Main pastry or can renderer.")] private SpriteRenderer body;
        [SerializeField, Tooltip("Cloche cover renderer.")] private SpriteRenderer cover;
        [SerializeField, Tooltip("Sprites for the four pastry IDs.")] private Sprite[] pastrySprites;
        [SerializeField, Tooltip("Cream can sprite.")] private Sprite canSprite;
        [SerializeField, Tooltip("Rotation of the capsule pastry sprite in degrees.")] private float macaronRotation = 90;
        [SerializeField, Tooltip("Cream can tint.")] private Color canColor = new Color(.92f, .91f, .87f);
        [SerializeField, Tooltip("Express landing bounce size."), Min(0)] private float expressBounce = .12f;
        [SerializeField, Tooltip("Express landing bounce duration in seconds."), Min(0)] private float expressBounceDuration = .18f;
        private FlightPath motion;
        private bool moving;
        private bool primed;
        private Color color;
        private Vector3 bodyScale;
        public int Id { get; private set; }
        public int Type { get; private set; }
        public bool IsConfigured => body != null && cover != null && canSprite != null && pastrySprites != null && pastrySprites.Length == 4;

        public void Configure(int id, int type, bool cloche, Vector3 position, ViewConfig config)
        {
            if (!IsConfigured) throw new InvalidOperationException("PastryView: body, cover and all sprite references are required.");
            Id = id; Type = type; moving = false; primed = false;
            body.sprite = type < 0 ? canSprite : pastrySprites[type];
            color = type < 0 ? canColor : config.PastryColor(type); body.color = color;
            transform.localPosition = position; transform.localScale = Vector3.one;
            Vector2 size = body.sprite.bounds.size;
            float largest = Mathf.Max(size.x, size.y);
            body.transform.localScale = Vector3.one * (config.PastrySize / largest);
            bodyScale = body.transform.localScale;
            body.transform.localRotation = Quaternion.Euler(0, 0, type == 1 ? macaronRotation : 0);
            cover.transform.localScale = new Vector3(largest * 1.15f / cover.sprite.bounds.size.x, largest * 1.15f / cover.sprite.bounds.size.y, 1);
            cover.enabled = cloche; body.sortingOrder = 10; gameObject.SetActive(true);
        }
        public void SetCloche(bool value) => cover.enabled = value;
        public void Prime() => primed = true;
        public Vector3 PositionAt(long time)
        {
            if (!moving) return transform.localPosition;
            var point = motion.At(time); return new Vector3((float)point.X, (float)point.Y, transform.localPosition.z);
        }
        public void Move(Vector3 target, long start, long end, float arc)
        {
            Vector3 from = PositionAt(start);
            motion = new FlightPath(new Point2(from.x, from.y), new Point2(target.x, target.y), start, end, arc);
            moving = true; body.sortingOrder = arc == 0 ? 10 : 30;
        }
        public void Evaluate(long time)
        {
            if (moving) { transform.localPosition = PositionAt(time); if (time >= motion.EndTime) moving = false; }
            if (primed) body.color = Color.Lerp(color, Color.white, .5f + .5f * Mathf.Sin(time * .00003f));
        }
        public void FinishExpress(long time, bool reducedMotion)
        {
            transform.localPosition = PositionAt(time); moving = false;
            if (reducedMotion || expressBounceDuration <= 0) { gameObject.SetActive(false); return; }
            body.transform.DOKill(); body.transform.localScale = bodyScale;
            body.transform.DOPunchScale(bodyScale * expressBounce, expressBounceDuration, 6, .5f).SetUpdate(true)
                .OnComplete(() => { if (this != null) gameObject.SetActive(false); });
        }
        private void OnDisable() { if (body != null) { body.transform.DOKill(); if (bodyScale != Vector3.zero) body.transform.localScale = bodyScale; } }
    }
}
