using System;
using Paw.Presentation;
using Paw.Sim;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Paw.Runtime
{
    [DisallowMultipleComponent]
    public sealed class GameController : MonoBehaviour
    {
        [SerializeField, Tooltip("Prototype level index 0–3."), Range(0, 3)] private int levelIndex;
        [SerializeField, Tooltip("Shared read-only view configuration.")] private ViewConfig config;
        [SerializeField, Tooltip("World-space board and pools.")] private BoardView board;
        [SerializeField, Tooltip("Hand, labels and outcome panel.")] private GameHud hud;
        [SerializeField, Tooltip("Shared world/UI orthographic camera.")] private Camera gameCamera;
        [SerializeField, Tooltip("HUD canvas scaler; switched to height matching together with the camera on short screens.")] private CanvasScaler canvasScaler;
        private readonly AimState aim = new AimState();
        private readonly EventCursor cursor = new EventCursor();
        private readonly SimClock clock = new SimClock();
        private Simulator sim;
        private int attempt;
        private bool started;
        private long outcomeAt = -1;
        private Cell? previousAim;
        private int previousCard = -1;
        private int previewRevision = -1;
        private float cameraAspect;
        public Simulator Simulation => sim;
        public bool IsConfigured => config != null && config.IsValid && board != null && board.IsConfigured && hud != null && hud.IsConfigured && gameCamera != null && canvasScaler != null;

        private void OnEnable()
        {
            if (hud != null) { hud.CardSelected += Select; hud.Retry += Retry; hud.Next += Next; }
            if (started && Application.isPlaying) LoadLevel();
        }
        private void Start() { started = true; LoadLevel(); }
        private void OnDisable()
        {
            if (hud != null) { hud.CardSelected -= Select; hud.Retry -= Retry; hud.Next -= Next; }
            if (board != null) board.Clear(); sim = null; aim.Cancel(); ResetPresentationState(); clock.Reset();
        }
        private void ResetPresentationState() { previousAim = null; previousCard = -1; previewRevision = -1; outcomeAt = -1; }
        private void OnApplicationFocus(bool focused) { clock.Focus(focused); if (!focused) CancelAim(); }

        public void PreviewLevel(int index)
        {
            if (index < 0 || index >= PrototypeLevels.Count) throw new ArgumentOutOfRangeException(nameof(index));
            levelIndex = index; attempt = 0; LoadLevel();
        }
        private void LoadLevel()
        {
            if (!IsConfigured) throw new InvalidOperationException("GameController needs valid config, board, HUD and camera references.");
            FitCamera();
            var level = PrototypeLevels.Create(levelIndex);
            uint seed = unchecked((uint)(levelIndex * 73856093) ^ (uint)(attempt * 19349663));
            sim = new Simulator(level, config.CreateSimulationConfig(), seed, config.CreateInventory());
            clock.Reset(); aim.Cancel(); cursor.Reset(sim); ResetPresentationState();
            board.Initialize(sim, config); hud.HideOutcome(); hud.Aim(null); hud.Render(sim, -1, config);
        }
        public void Retry() { attempt++; LoadLevel(); }
        public void Next()
        {
            if (sim == null || sim.CurrentOutcome != Outcome.Won || levelIndex >= PrototypeLevels.Count - 1) return;
            levelIndex++; attempt = 0; LoadLevel();
        }
        private bool CanInput => sim != null && sim.IsSettled && sim.CurrentOutcome == Outcome.Playing && !hud.PanelVisible;
        private void Update()
        {
            if (sim == null) return;
            if (cameraAspect != gameCamera.aspect) FitCamera();
            long time = clock.Advance(Time.unscaledDeltaTime);
            bool wasBusy = !sim.IsSettled;
            if (wasBusy) sim.AdvanceTo(time);
            DrainEvents(); board.Tick(time, !sim.IsSettled);
            if (wasBusy && sim.IsSettled) hud.Render(sim, aim.SelectedIndex, config);
            if (outcomeAt >= 0 && sim.IsSettled && time >= outcomeAt + config.PanelDelay && !hud.PanelVisible)
            { hud.ShowOutcome(sim.CurrentOutcome, levelIndex); hud.Render(sim, -1, config); }
            if (!CanInput) return;
            if (Input.GetKeyDown(KeyCode.Escape)) { CancelAim(); return; }
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            Vector3 point = gameCamera.ScreenToWorldPoint(Input.mousePosition);
            if (board.TryCell(point, out Cell cell))
            {
                aim.Hover(cell); RefreshPreview();
                if (Input.GetMouseButtonDown(0) && aim.MouseClick(cell)) PlaySelected(cell);
            }
            else if (aim.Target.HasValue) { aim.Hover(null); board.ClearPreview(); hud.Aim(null); previousAim = null; }
        }
        private void FitCamera()
        {
            cameraAspect = gameCamera.aspect;
            // Camera and HUD must fit the same axis, or the world board and the canvas drift apart on short screens.
            bool fitHeight = config.BoardWidth / cameraAspect < config.BoardHeight;
            gameCamera.orthographicSize = fitHeight ? config.BoardHeight / 2 : config.BoardWidth / (2 * cameraAspect);
            canvasScaler.matchWidthOrHeight = fitHeight ? 1 : 0;
        }
        private void Select(int index)
        {
            if (!CanInput || index >= sim.HandCount) return;
            aim.Select(index); hud.Render(sim, aim.SelectedIndex, config);
            if (aim.SelectedIndex < 0) { board.ClearPreview(); hud.Aim(null); previousCard = -1; }
            else RefreshPreview();
        }
        public bool PlaySelected(Cell target)
        {
            if (!CanInput || aim.SelectedIndex < 0) return false;
            sim.AdvanceTo(clock.Time);
            if (!sim.TryPlay(aim.SelectedIndex, target, out _)) return false;
            CancelAim(); DrainEvents(); hud.Render(sim, -1, config); return true;
        }
        private void CancelAim()
        {
            aim.Cancel(); previousAim = null; previousCard = -1;
            if (board != null) board.ClearPreview(); if (hud != null) hud.Aim(null);
            if (sim != null && hud != null) hud.Render(sim, -1, config);
        }
        private void RefreshPreview()
        {
            if (!aim.Target.HasValue || aim.SelectedIndex < 0) return;
            Cell target = aim.Target.Value;
            if (previousAim.HasValue && previousAim.Value.Equals(target) && previousCard == aim.SelectedIndex && previewRevision == sim.EventCount) return;
            previousAim = target; previousCard = aim.SelectedIndex; previewRevision = sim.EventCount;
            var prediction = Predictor.Predict(sim, aim.SelectedIndex, target);
            if (!prediction.Accepted) return;
            var summary = new PreviewSummary(prediction); board.Preview(summary, sim.GetHandCard(aim.SelectedIndex), target); hud.Aim(summary);
        }
        private void DrainEvents()
        {
            bool changed = false;
            while (cursor.TryRead(out SimEvent item))
            {
                board.Apply(item); changed = true;
                if (item.Kind == SimEventKind.Outcome) { outcomeAt = item.Time; aim.Cancel(); board.ClearPreview(); }
            }
            if (changed) hud.Render(sim, aim.SelectedIndex, config);
        }
        public void PreviewAdvanceTo(long time)
        {
            if (sim == null) throw new InvalidOperationException("Load a preview level first.");
            sim.AdvanceTo(time); DrainEvents(); board.Tick(time, !sim.IsSettled);
        }
    }
}
