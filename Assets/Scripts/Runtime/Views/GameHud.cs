using System;
using Paw.Presentation;
using Paw.Sim;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Paw.Runtime
{
    [DisallowMultipleComponent]
    public sealed class GameHud : MonoBehaviour
    {
        [SerializeField, Tooltip("Five card views in hand order.")] private CardView[] cards;
        [SerializeField, Tooltip("Card fan and deal animations.")] private HandView hand;
        [SerializeField, Tooltip("Tray pointer input surface.")] private TrayHitArea tray;
        [SerializeField, Tooltip("Level label.")] private Text levelLabel;
        [SerializeField, Tooltip("Deck count label.")] private Text deckLabel;
        [SerializeField, Tooltip("Shelf occupancy label.")] private Text shelfLabel;
        [SerializeField, Tooltip("Aim status pill.")] private Text aimLabel;
        [SerializeField, Tooltip("Outcome panel canvas group, kept active for initialization.")] private CanvasGroup panel;
        [SerializeField, Tooltip("Outcome title.")] private Text panelTitle;
        [SerializeField, Tooltip("Retry button.")] private Button retry;
        [SerializeField, Tooltip("Next-level button.")] private Button next;
        [SerializeField, Tooltip("Top restart button.")] private Button restart;
        [SerializeField, Tooltip("Label ink color.")] private Color inkColor = new Color(.3f, .17f, .15f);
        [SerializeField, Tooltip("Shelf counter color at 4 or 5 plates (GDD §5.6).")] private Color shelfWarningColor = new Color(.75f, .15f, .19f);
        [SerializeField, Tooltip("Aim pill color for normal and rack moves.")] private Color aimColor = new Color(.3f, .25f, .25f);
        [SerializeField, Tooltip("Aim pill color when the move overflows.")] private Color aimOverflowColor = new Color(.8f, .1f, .17f);
        public event Action<int> CardSelected;
        public event Action<CardView, PointerEventData> CardDown;
        public event Action<CardView, PointerEventData> CardMove;
        public event Action<CardView, PointerEventData> CardUp;
        public event Action<PointerEventData> BoardDown;
        public event Action<PointerEventData> BoardUp;
        public event Action<PointerEventData> BoardDrag;
        public event Action<PointerEventData> BoardMove;
        public event Action<PointerEventData> BoardExit;
        public event Action Retry;
        public event Action Next;
        public bool PanelVisible { get; private set; }
        public bool IsConfigured => cards != null && cards.Length == 5 && hand != null && hand.IsConfigured && tray != null && levelLabel != null && deckLabel != null && shelfLabel != null && aimLabel != null && restart != null;
        public bool HasLegacyOutcome => panel != null && panelTitle != null && retry != null && next != null;
        private void OnEnable()
        {
            if (retry != null) retry.onClick.AddListener(OnRetry);
            if (next != null) next.onClick.AddListener(OnNext);
            if (restart != null) restart.onClick.AddListener(OnRetry);
            if (tray != null) { tray.Pressed += OnBoardDown; tray.Released += OnBoardUp; tray.Dragged += OnBoardDrag; tray.Moved += OnBoardMove; tray.Exited += OnBoardExit; }
            if (cards != null) foreach (var card in cards) if (card != null)
            { card.Selected += OnCard; card.PointerDown += OnCardDown; card.PointerMove += OnCardMove; card.PointerUp += OnCardUp; }
        }
        private void OnDisable()
        {
            if (retry != null) retry.onClick.RemoveListener(OnRetry);
            if (next != null) next.onClick.RemoveListener(OnNext);
            if (restart != null) restart.onClick.RemoveListener(OnRetry);
            if (tray != null) { tray.Pressed -= OnBoardDown; tray.Released -= OnBoardUp; tray.Dragged -= OnBoardDrag; tray.Moved -= OnBoardMove; tray.Exited -= OnBoardExit; }
            if (cards != null) foreach (var card in cards) if (card != null)
            { card.Selected -= OnCard; card.PointerDown -= OnCardDown; card.PointerMove -= OnCardMove; card.PointerUp -= OnCardUp; }
        }
        private void OnCard(int index) => CardSelected?.Invoke(index);
        private void OnCardDown(CardView card, PointerEventData e) => CardDown?.Invoke(card, e);
        private void OnCardMove(CardView card, PointerEventData e) => CardMove?.Invoke(card, e);
        private void OnCardUp(CardView card, PointerEventData e) => CardUp?.Invoke(card, e);
        private void OnBoardDown(PointerEventData e) => BoardDown?.Invoke(e);
        private void OnBoardUp(PointerEventData e) => BoardUp?.Invoke(e);
        private void OnBoardDrag(PointerEventData e) => BoardDrag?.Invoke(e);
        private void OnBoardMove(PointerEventData e) => BoardMove?.Invoke(e);
        private void OnBoardExit(PointerEventData e) => BoardExit?.Invoke(e);
        private void OnRetry() => Retry?.Invoke();
        private void OnNext() => Next?.Invoke();
        public void Deal(int index) { if (hand != null) hand.Deal(index); }
        public void Render(Simulator sim, int selected, ViewConfig config)
        {
            if (!IsConfigured) throw new InvalidOperationException("GameHud has unassigned labels, cards, panel or buttons.");
            levelLabel.text = "LEVEL " + sim.LevelId;
            deckLabel.text = "DECK\n" + sim.DeckCount;
            int shelf = sim.GetStorageCount(Destination.Plate);
            shelfLabel.text = "SHELF " + shelf + "/5";
            shelfLabel.color = shelf >= 4 ? shelfWarningColor : inkColor;
            bool enabled = sim.CurrentOutcome == Outcome.Playing && !PanelVisible;
            hand.Layout(sim.HandCount, config.ReducedMotion || !Application.isPlaying);
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] == null) throw new InvalidOperationException("GameHud contains an unassigned card view.");
                cards[i].gameObject.SetActive(i < sim.HandCount);
                if (i < sim.HandCount) cards[i].Render(i, sim.GetHandCard(i), i == selected, enabled, config);
            }
        }
        public void Aim(PreviewSummary summary)
        {
            aimLabel.gameObject.SetActive(summary != null);
            if (summary == null) return;
            aimLabel.text = summary.Risk == PreviewBadgeKind.Overflow ? "OVERFLOW!" :
                summary.Risk == PreviewBadgeKind.Rack ? "COOLING RACK" : "READY";
            aimLabel.color = summary.Risk == PreviewBadgeKind.Overflow ? aimOverflowColor : aimColor;
        }
        public void HideOutcome()
        { PanelVisible = false; if (panel != null) { panel.alpha = 0; panel.blocksRaycasts = false; panel.interactable = false; } }
        public void SetExternalPanelVisible(bool visible) { PanelVisible = visible; if (visible) Aim(null); }
        public void ShowOutcome(Outcome outcome, int level)
        {
            if (!HasLegacyOutcome) throw new InvalidOperationException("GameHud: legacy outcome panel fields are missing.");
            PanelVisible = true; panel.alpha = 1; panel.blocksRaycasts = true; panel.interactable = true;
            panelTitle.text = outcome == Outcome.Won ? "Orders boxed!" : outcome == Outcome.OutOfMoves ? "Out of moves" : "Shelf overflow";
            next.gameObject.SetActive(outcome == Outcome.Won && level < PrototypeLevels.Count - 1);
            Aim(null);
        }
    }
}
