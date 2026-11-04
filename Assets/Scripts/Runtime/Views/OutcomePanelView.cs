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
        [SerializeField, Tooltip("Revive offer button.")] private Button revive;
        [SerializeField, Tooltip("Revive offer label.")] private Text reviveLabel;
        [SerializeField, Tooltip("Short explanation of the outcome.")] private Text body;
        [SerializeField, Tooltip("Express revive button text.")] private string expressText = "Express box  AD";
        [SerializeField, Tooltip("Out-of-moves revive button text.")] private string movesText = "+2 FLOP  AD";
        [SerializeField, Tooltip("Win explanation.")] private string winBody = "Every order is ready!";
        [SerializeField, Tooltip("Overflow explanation.")] private string overflowBody = "The shelf is full.";
        [SerializeField, Tooltip("Rack failure explanation.")] private string rackBody = "The rack did not clear.";
        [SerializeField, Tooltip("Out-of-moves explanation.")] private string movesBody = "There are no cards left.";
        public event Action Retry;
        public event Action Next;
        public event Action Revive;
        public bool IsConfigured => panel != null && title != null && retry != null && next != null;
        private void OnEnable() { if (retry != null) retry.onClick.AddListener(OnRetry); if (next != null) next.onClick.AddListener(OnNext); if (revive != null) revive.onClick.AddListener(OnRevive); }
        private void OnDisable() { if (retry != null) retry.onClick.RemoveListener(OnRetry); if (next != null) next.onClick.RemoveListener(OnNext); if (revive != null) revive.onClick.RemoveListener(OnRevive); }
        private void OnRetry() => Retry?.Invoke();
        private void OnNext() => Next?.Invoke();
        private void OnRevive() => Revive?.Invoke();
        public void Hide()
        {
            if (!IsConfigured) throw new InvalidOperationException("OutcomePanelView: panel, title, retry and next must be assigned.");
            panel.alpha = 0; panel.blocksRaycasts = false; panel.interactable = false;
        }
        public void Show(Outcome outcome, int level, bool canRevive = true)
        {
            if (!IsConfigured) throw new InvalidOperationException("OutcomePanelView: panel, title, retry and next must be assigned.");
            panel.alpha = 1; panel.blocksRaycasts = true; panel.interactable = true;
            title.text = outcome == Outcome.Won ? "Orders boxed!" : outcome == Outcome.OutOfMoves ? "Out of moves" : "Shelf overflow";
            next.gameObject.SetActive(outcome == Outcome.Won && level < PrototypeLevels.Count - 1);
            if (revive != null) revive.gameObject.SetActive(outcome != Outcome.Won && canRevive);
            if (reviveLabel != null) reviveLabel.text = outcome == Outcome.OutOfMoves ? movesText : expressText;
            if (body != null) body.text = outcome == Outcome.Won ? winBody : outcome == Outcome.OutOfMoves ? movesBody : outcome == Outcome.RackNotCleared ? rackBody : overflowBody;
        }
    }
}
