using System;
using Paw.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace Paw.Runtime
{
    [DisallowMultipleComponent]
    public sealed class CardView : MonoBehaviour
    {
        [SerializeField, Tooltip("Card button.")] private Button button;
        [SerializeField, Tooltip("Card name label.")] private Text title;
        [SerializeField, Tooltip("Card band image.")] private Image band;
        [SerializeField, Tooltip("Twenty-five mini-grid cells, row-major.")] private Image[] footprint;
        [SerializeField, Tooltip("Card background.")] private Image background;
        [SerializeField, Tooltip("Background tint of an unselected card.")] private Color idleColor = new Color(1, .98f, .95f);
        [SerializeField, Tooltip("Background tint of the selected card.")] private Color selectedColor = new Color(1, .88f, .59f);
        [SerializeField, Tooltip("Mini-grid cells outside the card's shape.")] private Color emptyCellColor = new Color(.82f, .76f, .71f, .45f);
        [SerializeField, Tooltip("Mini-grid target cell.")] private Color targetCellColor = new Color(.3f, .17f, .15f);
        public event Action<int> Selected;
        public int Index { get; private set; }
        public bool IsConfigured => button != null && title != null && band != null && background != null && footprint != null && footprint.Length == 25;
        private void OnEnable() { if (button != null) button.onClick.AddListener(Clicked); }
        private void OnDisable() { if (button != null) button.onClick.RemoveListener(Clicked); }
        private void Clicked() => Selected?.Invoke(Index);
        public void Render(int index, CardKind kind, bool selected, bool enabled, ViewConfig config)
        {
            if (!IsConfigured) throw new InvalidOperationException("CardView needs its button, labels, band and 25 grid references.");
            Index = index; title.text = Name(kind); button.interactable = enabled;
            band.color = config.CardColor(kind);
            background.color = selected ? selectedColor : idleColor;
            for (int i = 0; i < footprint.Length; i++) footprint[i].color = emptyCellColor;
            foreach (var cell in CardShapes.GetCells(kind, new Cell(2, 2), 5, 5)) footprint[cell.Row * 5 + cell.Column].color = band.color;
            footprint[12].color = targetCellColor;
        }
        private static string Name(CardKind card)
        {
            switch (card) { case CardKind.Pop: return "TAP"; case CardKind.Sweep: return "SWIPE"; case CardKind.Drill: return "POUNCE"; default: return "FLOP"; }
        }
    }
}
