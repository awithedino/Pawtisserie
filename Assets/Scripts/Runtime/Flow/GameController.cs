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
        [SerializeField, Tooltip("Move markers for the cat's actions.")] private CatView cat;
        [SerializeField, Tooltip("Hand, labels and outcome panel.")] private GameHud hud;
        [SerializeField, Tooltip("Optional separate outcome panel view.")] private OutcomePanelView outcomePanel;
        [SerializeField, Tooltip("Shared world/UI orthographic camera.")] private Camera gameCamera;
        [SerializeField, Tooltip("HUD canvas scaler; switched to height matching together with the camera on short screens.")] private CanvasScaler canvasScaler;
        [SerializeField, Tooltip("HUD content root fitted to the screen safe area.")] private RectTransform safeArea;
        [SerializeField, Tooltip("Reference-sized content inside the safe area.")] private RectTransform safeContent;
        [SerializeField, Tooltip("Optional popup canvas scaler, matched to the HUD.")] private CanvasScaler popupScaler;
        [SerializeField, Tooltip("Optional popup canvas safe area.")] private RectTransform popupSafeArea;
        [SerializeField, Tooltip("Optional reference-sized popup content.")] private RectTransform popupSafeContent;
        private readonly PointerGesture gesture = new PointerGesture();
        private readonly EventCursor cursor = new EventCursor();
        private readonly SimClock clock = new SimClock();
        private readonly IHapticCues haptics = new NullHapticCues();
        private Simulator sim;
        private int attempt;
        private bool started;
        private long outcomeAt = -1;
        private Cell? previousAim;
        private int previousCard = -1;
        private int previewRevision = -1;
        private float cameraAspect;
        private Rect lastSafeArea;
        private int lastScreenWidth;
        private int lastScreenHeight;
        private CardView draggedCard;
        private bool mouseOverBoardArea;
        private Vector2 lastMousePosition;
        public Simulator Simulation => sim;
        public bool IsConfigured => config != null && config.IsValid && board != null && board.IsConfigured && cat != null && cat.IsConfigured && hud != null && hud.IsConfigured &&
            (outcomePanel != null ? outcomePanel.IsConfigured : hud.HasLegacyOutcome) && gameCamera != null && canvasScaler != null && safeArea != null && safeContent != null &&
            ((popupScaler == null && popupSafeArea == null && popupSafeContent == null) || (popupScaler != null && popupSafeArea != null && popupSafeContent != null));

        private void OnEnable()
        {
            if (hud != null) { hud.CardSelected += Select; hud.CardDown += CardDown; hud.CardMove += CardMove; hud.CardUp += CardUp; hud.BoardDown += BoardDown; hud.BoardUp += BoardUp; hud.BoardDrag += BoardMove; hud.BoardMove += BoardHover; hud.BoardExit += BoardExit; hud.Retry += Retry; hud.Next += Next; }
            if (outcomePanel != null) { outcomePanel.Retry += Retry; outcomePanel.Next += Next; }
            if (started && Application.isPlaying) LoadLevel();
        }
        private void Start() { started = true; LoadLevel(); }
        private void OnDisable()
        {
            if (hud != null) { hud.CardSelected -= Select; hud.CardDown -= CardDown; hud.CardMove -= CardMove; hud.CardUp -= CardUp; hud.BoardDown -= BoardDown; hud.BoardUp -= BoardUp; hud.BoardDrag -= BoardMove; hud.BoardMove -= BoardHover; hud.BoardExit -= BoardExit; hud.Retry -= Retry; hud.Next -= Next; }
            if (outcomePanel != null) { outcomePanel.Retry -= Retry; outcomePanel.Next -= Next; }
            if (draggedCard != null) draggedCard.EndDragVisual(); draggedCard = null;
            if (board != null) board.Clear(); if (cat != null) cat.Clear(); sim = null; gesture.Cancel(); ResetPresentationState(); clock.Reset();
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
            if (draggedCard != null) draggedCard.EndDragVisual(); draggedCard = null;
            FitCamera();
            var level = PrototypeLevels.Create(levelIndex);
            uint seed = unchecked((uint)(levelIndex * 73856093) ^ (uint)(attempt * 19349663));
            sim = new Simulator(level, config.CreateSimulationConfig(), seed, config.CreateInventory());
            clock.Reset(); gesture.Cancel(); cursor.Reset(sim); ResetPresentationState();
            board.Initialize(sim, config); cat.Initialize(config); hud.HideOutcome(); if (outcomePanel != null) outcomePanel.Hide(); hud.Aim(null); hud.Render(sim, -1, config);
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
            if (cameraAspect != gameCamera.aspect || lastSafeArea != Screen.safeArea || lastScreenWidth != Screen.width || lastScreenHeight != Screen.height) FitCamera();
            long time = clock.Advance(Time.unscaledDeltaTime);
            bool wasBusy = !sim.IsSettled;
            if (wasBusy) sim.AdvanceTo(time);
            DrainEvents(); board.Tick(time, !sim.IsSettled); cat.Tick(time);
            if (wasBusy && sim.IsSettled)
            {
                hud.Render(sim, gesture.SelectedIndex, config);
                if (CanInput && mouseOverBoardArea && !gesture.HasCapture && gesture.SelectedIndex >= 0)
                    SetPreview(CellAt(lastMousePosition));
            }
            if (outcomeAt >= 0 && sim.IsSettled && time >= outcomeAt + config.PanelDelay && !hud.PanelVisible)
            { if (outcomePanel != null) { outcomePanel.Show(sim.CurrentOutcome, levelIndex); hud.SetExternalPanelVisible(true); } else hud.ShowOutcome(sim.CurrentOutcome, levelIndex); hud.Render(sim, -1, config); }
            if (Input.GetKeyDown(KeyCode.Escape)) { CancelAim(); return; }
        }
        private void FitCamera()
        {
            cameraAspect = gameCamera.aspect;
            lastScreenWidth = Screen.width; lastScreenHeight = Screen.height;
            if (lastScreenWidth <= 0 || lastScreenHeight <= 0) return;
            lastSafeArea = Screen.safeArea;
            if (lastSafeArea.width <= 0 || lastSafeArea.height <= 0) lastSafeArea = new Rect(0, 0, lastScreenWidth, lastScreenHeight);
            var fit = SafeAreaLayout.Calculate(lastScreenWidth, lastScreenHeight, lastSafeArea.x, lastSafeArea.y,
                lastSafeArea.width, lastSafeArea.height, config.BoardWidth, config.BoardHeight);
            FitCanvas(canvasScaler, safeArea, safeContent, fit);
            if (popupScaler != null) FitCanvas(popupScaler, popupSafeArea, popupSafeContent, fit);
            gameCamera.orthographicSize = (float)fit.CameraHalfHeight;
            var position = gameCamera.transform.localPosition;
            position.x = (float)fit.CameraX; position.y = (float)fit.CameraY;
            gameCamera.transform.localPosition = position;
        }
        private void FitCanvas(CanvasScaler scaler, RectTransform area, RectTransform content, SafeAreaLayout fit)
        {
            scaler.matchWidthOrHeight = fit.FitHeight ? 1 : 0;
            area.anchorMin = new Vector2(lastSafeArea.xMin / lastScreenWidth, lastSafeArea.yMin / lastScreenHeight);
            area.anchorMax = new Vector2(lastSafeArea.xMax / lastScreenWidth, lastSafeArea.yMax / lastScreenHeight);
            area.offsetMin = area.offsetMax = Vector2.zero;
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(config.BoardWidth, config.BoardHeight);
            content.localScale = Vector3.one * (float)fit.HudScale;
        }
        private void Select(int index)
        {
            if (sim == null || sim.CurrentOutcome != Outcome.Playing || hud.PanelVisible || index >= sim.HandCount) return;
            haptics.Play(HapticCue.Select);
            hud.Render(sim, gesture.SelectedIndex, config);
            if (gesture.SelectedIndex < 0) { board.ClearPreview(); hud.Aim(null); previousCard = -1; }
            else if (CanInput) RefreshPreview();
        }
        private Cell? PointerCell(PointerEventData e)
        {
            return CellAt(e.position);
        }
        private Cell? CellAt(Vector2 position) => board.TryCell(gameCamera.ScreenToWorldPoint(position), out Cell cell) ? cell : (Cell?)null;
        private void SetPreview(Cell? target)
        {
            gesture.Hover(target, CanInput);
            if (gesture.Target.HasValue) RefreshPreview();
            else { board.ClearPreview(); hud.Aim(null); previousAim = null; }
        }
        public void BoardHover(PointerEventData e)
        {
            if (e.pointerId >= 0) return;
            mouseOverBoardArea = true; lastMousePosition = e.position;
            if (!CanInput || gesture.HasCapture) return;
            SetPreview(PointerCell(e));
        }
        public void BoardExit(PointerEventData e)
        {
            if (e.pointerId >= 0) return;
            mouseOverBoardArea = false;
            if (!gesture.HasCapture && sim != null) SetPreview(null);
        }
        public void BoardDown(PointerEventData e)
        {
            if (!CanInput) return;
            Cell? target = PointerCell(e);
            if (!target.HasValue) return;
            if (gesture.SelectedIndex < 0) { board.Wobble(target.Value); return; }
            if (gesture.BoardDown(e.pointerId, target.Value, true)) SetPreview(target);
        }
        public void BoardMove(PointerEventData e)
        {
            if (!gesture.OwnsBoard(e.pointerId)) return;
            Cell? target = PointerCell(e);
            var oldTarget = gesture.Target;
            gesture.BoardMove(e.pointerId, target);
            if (target.HasValue && (!oldTarget.HasValue || !oldTarget.Value.Equals(target.Value))) haptics.Play(HapticCue.AimMove);
            if (CanInput) SetPreview(target);
        }
        public void BoardUp(PointerEventData e)
        {
            if (!gesture.OwnsBoard(e.pointerId)) return;
            if (gesture.BoardUp(e.pointerId, PointerCell(e), CanInput, out int index, out Cell target))
            {
                PlaySelected(target);
            }
            else if (!gesture.HasCapture) SetPreview(null);
        }
        private void CardDown(CardView card, PointerEventData e)
        {
            if (sim == null || sim.CurrentOutcome != Outcome.Playing || hud.PanelVisible) return;
            gesture.CardDown(e.pointerId, card.Index, e.position.x, e.position.y);
        }
        private void CardMove(CardView card, PointerEventData e)
        {
            float dpi = Screen.dpi > 0 ? Screen.dpi : 160;
            Cell? target = PointerCell(e);
            bool wasDragging = gesture.IsDragging;
            var oldTarget = gesture.Target;
            gesture.CardMove(e.pointerId, e.position.x, e.position.y, config.DragThresholdDp * dpi / 160f, target, CanInput);
            if (!gesture.IsDragging) return;
            if (!gesture.OwnsCard(e.pointerId, card.Index)) return;
            if (target.HasValue && (!oldTarget.HasValue || !oldTarget.Value.Equals(target.Value))) haptics.Play(HapticCue.AimMove);
            draggedCard = card;
            card.MoveDragVisual(e.position, e.pressEventCamera);
            if (!wasDragging) { haptics.Play(HapticCue.DragStart); hud.Render(sim, gesture.SelectedIndex, config); }
            if (gesture.Target.HasValue) RefreshPreview();
            else { board.ClearPreview(); hud.Aim(null); previousAim = null; }
        }
        private void CardUp(CardView card, PointerEventData e)
        {
            if (!gesture.OwnsCard(e.pointerId, card.Index)) return;
            bool wasDragging = gesture.IsDragging;
            if (draggedCard != null) draggedCard.EndDragVisual(); draggedCard = null;
            if (gesture.CardUp(e.pointerId, PointerCell(e), CanInput, out int index, out Cell target))
            {
                PlaySelected(target);
            }
            else if (!wasDragging && !gesture.HasCapture) card.NotifySelected();
            else if (wasDragging) CancelAim();
        }
        public bool PlaySelected(Cell target)
        {
            if (!CanInput || gesture.SelectedIndex < 0) return false;
            sim.AdvanceTo(clock.Time);
            if (!sim.TryPlay(gesture.SelectedIndex, target, out _)) return false;
            haptics.Play(HapticCue.Drop);
            CancelAim(); DrainEvents(); hud.Render(sim, -1, config); return true;
        }
        private void CancelAim()
        {
            if (draggedCard != null) draggedCard.EndDragVisual(); draggedCard = null;
            gesture.Cancel(); previousAim = null; previousCard = -1;
            if (board != null) board.ClearPreview(); if (hud != null) hud.Aim(null);
            if (sim != null && hud != null) hud.Render(sim, -1, config);
        }
        private void RefreshPreview()
        {
            if (!gesture.Target.HasValue || gesture.SelectedIndex < 0) return;
            Cell target = gesture.Target.Value;
            if (previousAim.HasValue && previousAim.Value.Equals(target) && previousCard == gesture.SelectedIndex && previewRevision == sim.EventCount) return;
            previousAim = target; previousCard = gesture.SelectedIndex; previewRevision = sim.EventCount;
            var prediction = Predictor.Predict(sim, gesture.SelectedIndex, target);
            if (!prediction.Accepted) return;
            var summary = new PreviewSummary(prediction); board.Preview(summary, sim.GetHandCard(gesture.SelectedIndex), target); hud.Aim(summary);
        }
        private void DrainEvents()
        {
            bool changed = false;
            bool dealt = false;
            while (cursor.TryRead(out SimEvent item))
            {
                board.Apply(item); cat.Apply(item); changed = true;
                if (item.Kind == SimEventKind.CanExploded) haptics.Play(HapticCue.Explosion);
                if (item.Kind == SimEventKind.CardDealt) dealt = true;
                if (item.Kind == SimEventKind.Outcome) { outcomeAt = item.Time; gesture.Cancel(); board.ClearPreview(); }
            }
            if (changed) hud.Render(sim, gesture.SelectedIndex, config);
            if (dealt && sim.HandCount > 0) hud.Deal(sim.HandCount - 1);
        }
        public void PreviewAdvanceTo(long time)
        {
            if (sim == null) throw new InvalidOperationException("Load a preview level first.");
            sim.AdvanceTo(time); DrainEvents(); board.Tick(time, !sim.IsSettled); cat.Tick(time);
        }
    }
}
