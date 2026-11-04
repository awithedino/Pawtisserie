using System;
using DG.Tweening;
using Paw.Presentation;
using Paw.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace Paw.Runtime
{
    [DisallowMultipleComponent]
    public sealed class BoosterButtonView : MonoBehaviour
    {
        [SerializeField, Tooltip("Booster represented by this button.")] private BoosterKind kind;
        [SerializeField, Tooltip("Clickable button.")] private Button button;
        [SerializeField, Tooltip("Visual wrapper animated independently of the layout slot.")] private RectTransform visual;
        [SerializeField, Tooltip("Count or AD label.")] private Text badgeText;
        [SerializeField, Tooltip("Count badge background.")] private Image badge;
        [SerializeField, Tooltip("Badge colour for available inventory.")] private Color countColor = new Color(1, .83f, .34f);
        [SerializeField, Tooltip("Badge colour for a simulated ad.")] private Color adColor = new Color(.25f, .7f, .5f);
        [SerializeField, Tooltip("Shake distance on a refused action."), Min(0)] private float shakeDistance = 6;
        [SerializeField, Tooltip("Shake duration in seconds."), Min(0)] private float shakeDuration = .2f;
        private Vector3 origin;
        public event Action<BoosterKind> Pressed;
        public bool IsConfigured => button != null && visual != null && badgeText != null && badge != null;
        private void Awake() { if (visual != null) origin = visual.localPosition; }
        private void OnEnable() { if (button != null) button.onClick.AddListener(OnClick); }
        private void OnDisable() { if (button != null) button.onClick.RemoveListener(OnClick); if (visual != null) { visual.DOKill(); visual.localPosition = origin; } }
        private void OnClick() => Pressed?.Invoke(kind);
        public void Render(BoosterButtonState state)
        {
            if (!IsConfigured) throw new InvalidOperationException("BoosterButtonView needs its button and badge references.");
            button.interactable = state.Interactable;
            badgeText.text = state.ShowsAd ? "AD" : state.Count.ToString();
            badge.color = state.ShowsAd ? adColor : countColor;
        }
        public void Refuse(bool reducedMotion)
        {
            if (reducedMotion || shakeDuration <= 0) return;
            visual.DOKill(); visual.localPosition = origin;
            visual.DOPunchPosition(new Vector3(shakeDistance, 0, 0), shakeDuration, 8, .5f).SetUpdate(true);
        }
    }
}
