using System;
using Paw.Presentation;
using Paw.Sim;
using UnityEngine;

namespace Paw.Runtime
{
    [DisallowMultipleComponent]
    public sealed class BoosterBarView : MonoBehaviour
    {
        [SerializeField, Tooltip("Buttons ordered Shuffle, Call box, Stash.")] private BoosterButtonView[] buttons;
        public event Action<BoosterKind> Pressed;
        public bool IsConfigured => buttons != null && buttons.Length == 3 && buttons[0] != null && buttons[1] != null && buttons[2] != null && buttons[0].IsConfigured && buttons[1].IsConfigured && buttons[2].IsConfigured;
        private void OnEnable() { if (buttons != null) foreach (var button in buttons) if (button != null) button.Pressed += OnPressed; }
        private void OnDisable() { if (buttons != null) foreach (var button in buttons) if (button != null) button.Pressed -= OnPressed; }
        private void OnPressed(BoosterKind kind) => Pressed?.Invoke(kind);
        public void Render(Simulator sim, bool panelVisible, bool adsEnabled)
        {
            if (!IsConfigured) throw new InvalidOperationException("BoosterBarView needs three configured buttons.");
            for (int i = 0; i < buttons.Length; i++)
                buttons[i].Render(new BoosterButtonState(sim.GetInventory((BoosterKind)i), sim.CurrentOutcome, sim.IsSettled, sim.IsTutorial, panelVisible, adsEnabled));
        }
        public void Refuse(BoosterKind kind, bool reducedMotion) { int i = (int)kind; if (i >= 0 && i < buttons.Length) buttons[i].Refuse(reducedMotion); }
    }
}
