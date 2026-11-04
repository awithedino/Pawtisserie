using System;
using Paw.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace Paw.Runtime
{
    [DisallowMultipleComponent]
    public sealed class OutcomePanelView : MonoBehaviour
    {
        [SerializeField, Tooltip("Canvas group for the outcome panel.")] private CanvasGroup panel;
        [SerializeField, Tooltip("Outcome title.")] private Text title;
        [SerializeField, Tooltip("Retry button.")] private Button retry;
        [SerializeField, Tooltip("Next-level button.")] private Button next;
        public event Action Retry;
        public event Action Next;
        public bool IsConfigured => panel != null && title != null && retry != null && next != null;
        private void OnEnable() { if (retry != null) retry.onClick.AddListener(OnRetry); if (next != null) next.onClick.AddListener(OnNext); }
        private void OnDisable() { if (retry != null) retry.onClick.RemoveListener(OnRetry); if (next != null) next.onClick.RemoveListener(OnNext); }
        private void OnRetry() => Retry?.Invoke();
        private void OnNext() => Next?.Invoke();
        public void Hide()
        {
            if (!IsConfigured) throw new InvalidOperationException("OutcomePanelView: panel, title, retry and next must be assigned.");
            panel.alpha = 0; panel.blocksRaycasts = false; panel.interactable = false;
        }
        public void Show(Outcome outcome, int level)
        {
            if (!IsConfigured) throw new InvalidOperationException("OutcomePanelView: panel, title, retry and next must be assigned.");
            panel.alpha = 1; panel.blocksRaycasts = true; panel.interactable = true;
            title.text = outcome == Outcome.Won ? "Orders boxed!" : outcome == Outcome.OutOfMoves ? "Out of moves" : "Shelf overflow";
            next.gameObject.SetActive(outcome == Outcome.Won && level < PrototypeLevels.Count - 1);
        }
    }
}
