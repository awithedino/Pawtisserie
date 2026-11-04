using System;
using Paw.Sim;
using UnityEngine;

namespace Paw.Runtime
{
    [DisallowMultipleComponent]
    public sealed class CatMarker : MonoBehaviour
    {
        [SerializeField, Tooltip("Marker sprites in card-kind order.")] private SpriteRenderer[] shapes;
        [SerializeField, Tooltip("Arm behind the TAP paw marker.")] private SpriteRenderer tapArm;
        [SerializeField, Tooltip("Focused cat sprite for POUNCE and FLOP.")] private Sprite focusedCat;
        [SerializeField, Tooltip("Happy cat sprite after POUNCE and FLOP land.")] private Sprite happyCat;
        [SerializeField, Tooltip("Start offsets for TAP, SWIPE, POUNCE and FLOP.")] private Vector3[] startOffsets = {
            new Vector3(0,130,0), Vector3.zero, new Vector3(0,120,0), new Vector3(0,190,0) };
        [SerializeField, Tooltip("Marker bounce-off offset.")] private Vector3 exitOffset = new Vector3(0,60,0);
        [SerializeField, Tooltip("Marker scale at landing before returning to its normal size.")] private Vector3 landingSquash = new Vector3(1.15f,.85f,1);
        [SerializeField, Tooltip("Seconds to recover from the landing squash."), Min(0)] private float squashDuration = .12f;
        [SerializeField, Tooltip("Seconds for the marker to fade after landing."), Min(0)] private float fadeDuration = .5f;
        private SpriteRenderer activeShape;
        private Color color;
        private Color armColor;
        private Vector3 start;
        private Vector3 target;
        private long startTime;
        private long landingTime;
        private long endTime;
        private bool reduced;
        private bool sweep;
        private bool catMove;
        private Vector3 baseScale;
        public bool IsConfigured => shapes != null && shapes.Length == 4 && tapArm != null && startOffsets != null && startOffsets.Length == 4;
        public void Begin(CardKind kind, Vector3 from, Vector3 to, long startAt, long landingAt, bool reducedMotion)
        {
            int index = (int)kind;
            if (!IsConfigured || index < 0 || index >= shapes.Length || shapes[index] == null) throw new InvalidOperationException("CatMarker: four marker shapes and offsets are required.");
            for (int i = 0; i < shapes.Length; i++) if (shapes[i] != null) shapes[i].gameObject.SetActive(i == index);
            activeShape = shapes[index]; color = activeShape.color; armColor = tapArm.color;
            catMove = kind == CardKind.Drill || kind == CardKind.Box;
            if (catMove && focusedCat != null) activeShape.sprite = focusedCat;
            baseScale = transform.localScale;
            start = from + startOffsets[index]; target = to; startTime = startAt; landingTime = Math.Max(startAt, landingAt);
            endTime = landingTime + (long)(fadeDuration * 1000000); reduced = reducedMotion; sweep = kind == CardKind.Sweep;
            gameObject.SetActive(true); Evaluate(startAt);
        }
        public void Evaluate(long time)
        {
            if (!gameObject.activeSelf || activeShape == null) return;
            if (time >= endTime) { ResetView(); return; }
            if (time < landingTime)
            {
                transform.localScale = baseScale;
                float t = landingTime == startTime ? 1 : Mathf.Clamp01((float)((double)(time - startTime) / (landingTime - startTime)));
                transform.localPosition = reduced ? target : Vector3.LerpUnclamped(start, target, sweep ? t : t * t);
            }
            else
            {
                if (catMove && happyCat != null) activeShape.sprite = happyCat;
                float t = endTime == landingTime ? 1 : Mathf.Clamp01((float)((double)(time - landingTime) / (endTime - landingTime)));
                transform.localPosition = reduced ? target : target + exitOffset * t;
                float recovery = squashDuration <= 0 ? 1 : Mathf.Clamp01((float)((time - landingTime) / 1000000d) / squashDuration);
                transform.localScale = reduced ? baseScale : Vector3.Scale(baseScale, Vector3.Lerp(landingSquash, Vector3.one, recovery));
                var tint = color; tint.a = color.a * (1 - t); activeShape.color = tint;
                if (activeShape == shapes[0]) { var armTint = armColor; armTint.a = armColor.a * (1 - t); tapArm.color = armTint; }
            }
        }
        public void ResetView()
        {
            if (activeShape != null) { activeShape.color = color; if (tapArm != null) tapArm.color = armColor; transform.localScale = baseScale; }
            activeShape = null; gameObject.SetActive(false);
        }
        private void OnDisable() { if (activeShape != null) { activeShape.color = color; if (tapArm != null) tapArm.color = armColor; transform.localScale = baseScale; } activeShape = null; }
    }
}
