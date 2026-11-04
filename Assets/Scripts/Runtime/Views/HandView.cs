using System;
using DG.Tweening;
using Paw.Presentation;
using UnityEngine;

namespace Paw.Runtime
{
    [DisallowMultipleComponent]
    public sealed class HandView : MonoBehaviour
    {
        [SerializeField, Tooltip("Card slots in hand order.")] private CardView[] cards;
        [SerializeField, Tooltip("Authored deck position for dealt cards.")] private RectTransform deckAnchor;
        [SerializeField, Tooltip("Horizontal distance between card centers."), Min(0)] private float cardStep = 58;
        [SerializeField, Tooltip("Maximum distance between the outer card centers."), Min(0)] private float availableWidth = 232;
        [SerializeField, Tooltip("Downward curve per squared slot offset."), Min(0)] private float fanDip = 2.6f;
        [SerializeField, Tooltip("Tilt in degrees per slot offset.")] private float fanTilt = 4.5f;
        [SerializeField, Tooltip("Seconds for cards to move to their slots."), Min(0)] private float layoutDuration = .22f;
        private int previousCount = -1;
        public bool IsConfigured => cards != null && cards.Length == 5 && deckAnchor != null;
        public void Deal(int index) { if (IsConfigured && index >= 0 && index < cards.Length && cards[index] != null) cards[index].DealFrom(deckAnchor.position); }
        public void Layout(int count, bool instant)
        {
            if (!IsConfigured) throw new InvalidOperationException("HandView: cards must contain five views.");
            if (count < 0 || count > cards.Length) throw new ArgumentOutOfRangeException(nameof(count));
            if (count == previousCount && !instant) return;
            previousCount = count;
            for (int i = 0; i < count; i++)
            {
                if (cards[i] == null) throw new InvalidOperationException("HandView: a card view is missing.");
                FanLayout.Slot(i, count, cardStep, availableWidth, fanDip, fanTilt, out Point2 position, out double angle);
                var slot = (RectTransform)cards[i].transform;
                slot.DOKill();
                Vector2 target = new Vector2((float)position.X, (float)position.Y);
                bool snap = instant || !cards[i].gameObject.activeSelf;
                if (snap || layoutDuration <= 0) slot.anchoredPosition = target;
                else DOTween.To(() => slot.anchoredPosition, value => slot.anchoredPosition = value, target, layoutDuration)
                    .SetTarget(slot).SetEase(Ease.OutBack).SetUpdate(true);
                cards[i].SetFanAngle((float)angle, snap);
            }
        }
        private void OnDisable()
        {
            if (cards != null) foreach (var card in cards) if (card != null) card.transform.DOKill();
            previousCount = -1;
        }
    }
}
