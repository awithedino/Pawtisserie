using System;
using DG.Tweening;
using UnityEngine;

namespace Paw.Runtime
{
    [DisallowMultipleComponent]
    public sealed class UiPopup : MonoBehaviour
    {
        [SerializeField, Tooltip("Canvas group controlled by the popup manager.")] private CanvasGroup group;
        [SerializeField, Tooltip("Whether back may dismiss this popup.")] private bool dismissOnBack;
        [SerializeField, Tooltip("Popup fade duration in unscaled seconds."), Min(0)] private float fadeDuration = .16f;
        public event Action Closed;
        public bool IsConfigured => group != null;
        public bool DismissOnBack => dismissOnBack;
        public void Open()
        {
            if (!IsConfigured) throw new InvalidOperationException("UiPopup needs a canvas group.");
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            group.DOKill(); group.alpha = 0; group.blocksRaycasts = true; group.interactable = true;
            DOTween.To(() => group.alpha, value => group.alpha = value, 1, fadeDuration).SetTarget(group).SetUpdate(true);
        }
        public void SetInput(bool enabled) { if (group != null) { group.blocksRaycasts = enabled; group.interactable = enabled; } }
        public void Close()
        {
            if (!IsConfigured) throw new InvalidOperationException("UiPopup needs a canvas group.");
            group.DOKill(); group.blocksRaycasts = false; group.interactable = false;
            DOTween.To(() => group.alpha, value => group.alpha = value, 0, fadeDuration).SetTarget(group).SetUpdate(true).OnComplete(() => Closed?.Invoke());
        }
        private void OnDisable() { if (group != null) group.DOKill(); }
    }
}
