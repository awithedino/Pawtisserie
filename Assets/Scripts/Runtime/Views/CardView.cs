using System;
using DG.Tweening;
using Paw.Sim;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Paw.Runtime
{
    [DisallowMultipleComponent]
    public sealed class CardView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IInitializePotentialDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField, Tooltip("Card button.")] private Button button;
        [SerializeField, Tooltip("Card name label.")] private Text title;
        [SerializeField, Tooltip("Card band image.")] private Image band;
        [SerializeField, Tooltip("Twenty-five mini-grid cells, row-major.")] private Image[] footprint;
        [SerializeField, Tooltip("Card background.")] private Image background;
        [SerializeField, Tooltip("Inner visual moved independently from the hand slot.")] private RectTransform visual;
        [SerializeField, Tooltip("Visual opacity for the deal animation.")] private CanvasGroup visualGroup;
        [SerializeField, Tooltip("Background tint of an unselected card.")] private Color idleColor = new Color(1, .98f, .95f);
        [SerializeField, Tooltip("Background tint of the selected card.")] private Color selectedColor = new Color(1, .88f, .59f);
        [SerializeField, Tooltip("Mini-grid cells outside the card's shape.")] private Color emptyCellColor = new Color(.82f, .76f, .71f, .45f);
        [SerializeField, Tooltip("Mini-grid target cell.")] private Color targetCellColor = new Color(.3f, .17f, .15f);
        [SerializeField, Tooltip("Maximum visual tilt in degrees while dragging."), Min(0)] private float dragTiltLimit = 12f;
        [SerializeField, Tooltip("Tilt response to pointer movement in canvas units per second."), Min(0)] private float dragTiltResponse = .025f;
        [SerializeField, Tooltip("Time in seconds for the drag tilt to settle."), Min(.01f)] private float dragTiltSmoothTime = .12f;
        [SerializeField, Tooltip("How far a selected card rises while it remains in the hand."), Min(0)] private float selectionLift = 22f;
        [SerializeField, Tooltip("How far a hovered card rises while it remains in the hand."), Min(0)] private float hoverLift = 8f;
        [SerializeField, Tooltip("Seconds to settle into a selected, hovered or fan pose."), Min(0)] private float poseDuration = .22f;
        [SerializeField, Tooltip("Seconds for a dealt card to reach its slot."), Min(0)] private float dealDuration = .42f;
        [SerializeField, Tooltip("Starting scale of a dealt card."), Min(0)] private float dealScale = .6f;
        [SerializeField, Tooltip("Starting rotation of a dealt card in degrees.")] private float dealAngle = -18f;
        public event Action<int> Selected;
        public event Action<CardView, PointerEventData> PointerDown;
        public event Action<CardView, PointerEventData> PointerMove;
        public event Action<CardView, PointerEventData> PointerUp;
        private RectTransform dragRect;
        private Vector3 dragStartPosition;
        private Quaternion dragStartRotation;
        private Vector2 previousDragPosition;
        private float previousDragTime;
        private float tiltTarget;
        private float tiltAngle;
        private float tiltVelocity;
        private int lastDragFrame;
        private int dragSiblingIndex;
        private bool draggingVisual;
        private bool selectedState;
        private bool hovered;
        private float fanAngle;
        private bool reducedMotion;
        public int Index { get; private set; }
        public bool IsConfigured => button != null && title != null && band != null && background != null && visual != null && visualGroup != null && footprint != null && footprint.Length == 25;
        public void OnInitializePotentialDrag(PointerEventData eventData) { eventData.useDragThreshold = false; }
        public void OnPointerDown(PointerEventData eventData) { PointerDown?.Invoke(this, eventData); }
        public void OnDrag(PointerEventData eventData) { PointerMove?.Invoke(this, eventData); }
        public void OnPointerUp(PointerEventData eventData) { PointerUp?.Invoke(this, eventData); }
        public void OnPointerEnter(PointerEventData eventData) { if (eventData.pointerId < 0) { hovered = true; ApplyPose(false); } }
        public void OnPointerExit(PointerEventData eventData) { if (eventData.pointerId < 0) { hovered = false; ApplyPose(false); } }
        public void NotifySelected() { Selected?.Invoke(Index); }
        public void SetFanAngle(float angle, bool instant) { fanAngle = angle; ApplyPose(instant); }
        public void DealFrom(Vector3 deckWorldPosition)
        {
            if (visual == null || visualGroup == null || !Application.isPlaying || reducedMotion || dealDuration <= 0) return;
            visual.DOKill(); visualGroup.DOKill();
            visual.localPosition = transform.InverseTransformPoint(deckWorldPosition);
            visual.localRotation = Quaternion.Euler(0, 0, dealAngle); visual.localScale = Vector3.one * dealScale;
            visualGroup.alpha = 0;
            visual.DOLocalMoveY(0, dealDuration).SetEase(Ease.OutBack).SetUpdate(true);
            visual.DOLocalMoveX(0, dealDuration).SetEase(Ease.OutBack).SetUpdate(true);
            visual.DOLocalRotate(new Vector3(0, 0, fanAngle), dealDuration).SetEase(Ease.OutBack).SetUpdate(true);
            visual.DOScale(Vector3.one, dealDuration).SetEase(Ease.OutBack).SetUpdate(true);
            DOTween.To(() => visualGroup.alpha, value => visualGroup.alpha = value, 1, dealDuration).SetTarget(visualGroup).SetUpdate(true);
        }
        public void MoveDragVisual(Vector2 screenPosition, Camera eventCamera)
        {
            if (!draggingVisual)
            {
                dragRect = visual;
                if (dragRect == null || !(dragRect.parent is RectTransform)) return;
                dragRect.DOKill();
                dragStartPosition = dragRect.localPosition; dragStartRotation = dragRect.localRotation;
                dragSiblingIndex = transform.GetSiblingIndex(); previousDragTime = Time.unscaledTime;
                previousDragPosition = Vector2.zero; tiltTarget = 0; tiltAngle = 0; tiltVelocity = 0; lastDragFrame = Time.frameCount;
                transform.SetAsLastSibling(); draggingVisual = true;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)dragRect.parent, screenPosition, eventCamera, out Vector2 start))
                    previousDragPosition = start;
            }
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)dragRect.parent, screenPosition, eventCamera, out Vector2 position))
            {
                float elapsed = Time.unscaledTime - previousDragTime;
                if (elapsed > 0.0001f)
                    tiltTarget = Mathf.Clamp(-(position.x - previousDragPosition.x) / elapsed * dragTiltResponse, -dragTiltLimit, dragTiltLimit);
                previousDragPosition = position; previousDragTime = Time.unscaledTime; lastDragFrame = Time.frameCount;
                dragRect.localPosition = new Vector3(position.x, position.y, dragStartPosition.z);
            }
        }
        private void Update()
        {
            if (!draggingVisual || dragRect == null || reducedMotion) return;
            if (Time.frameCount - lastDragFrame > 1) tiltTarget = 0;
            tiltAngle = Mathf.SmoothDampAngle(tiltAngle, tiltTarget, ref tiltVelocity, Mathf.Max(.01f, dragTiltSmoothTime), Mathf.Infinity, Time.unscaledDeltaTime);
            dragRect.localRotation = dragStartRotation * Quaternion.Euler(0, 0, tiltAngle);
        }
        public void EndDragVisual()
        {
            if (!draggingVisual) return;
            if (dragRect != null) { dragRect.localPosition = dragStartPosition; dragRect.localRotation = dragStartRotation; transform.SetSiblingIndex(dragSiblingIndex); }
            draggingVisual = false; dragRect = null; ApplyPose(false);
        }
        private void OnDisable()
        {
            EndDragVisual();
            if (visual != null) { visual.DOKill(); visual.localPosition = Vector3.zero; visual.localRotation = Quaternion.identity; visual.localScale = Vector3.one; }
            if (visualGroup != null) { visualGroup.DOKill(); visualGroup.alpha = 1; }
            selectedState = false; hovered = false; fanAngle = 0;
        }
        private void ApplyPose(bool instant)
        {
            if (visual == null || draggingVisual) return;
            float y = selectedState ? selectionLift : hovered ? hoverLift : 0;
            float angle = selectedState ? 0 : fanAngle;
            visual.DOKill();
            if (instant || reducedMotion || poseDuration <= 0) { visual.localPosition = new Vector3(0, y, 0); visual.localRotation = Quaternion.Euler(0, 0, angle); }
            else
            {
                visual.DOLocalMoveY(y, poseDuration).SetEase(Ease.OutBack).SetUpdate(true);
                visual.DOLocalRotate(new Vector3(0, 0, angle), poseDuration).SetEase(Ease.OutBack).SetUpdate(true);
            }
        }
        public void Render(int index, CardKind kind, bool selected, bool enabled, ViewConfig config)
        {
            if (!IsConfigured) throw new InvalidOperationException("CardView needs its button, labels, band and 25 grid references.");
            Index = index; title.text = Name(kind); button.interactable = enabled;
            reducedMotion = config.ReducedMotion;
            if (selectedState != selected) { selectedState = selected; ApplyPose(!Application.isPlaying); }
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
