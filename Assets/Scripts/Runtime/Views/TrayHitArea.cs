using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Paw.Runtime
{
    [DisallowMultipleComponent]
    public sealed class TrayHitArea : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IInitializePotentialDragHandler, IPointerMoveHandler, IPointerExitHandler
    {
        public event Action<PointerEventData> Pressed;
        public event Action<PointerEventData> Released;
        public event Action<PointerEventData> Dragged;
        public event Action<PointerEventData> Moved;
        public event Action<PointerEventData> Exited;
        public void OnInitializePotentialDrag(PointerEventData e) { e.useDragThreshold = false; }
        public void OnPointerDown(PointerEventData e) { Pressed?.Invoke(e); }
        public void OnPointerUp(PointerEventData e) { Released?.Invoke(e); }
        public void OnDrag(PointerEventData e) { Dragged?.Invoke(e); }
        public void OnPointerMove(PointerEventData e) { Moved?.Invoke(e); }
        public void OnPointerExit(PointerEventData e) { Exited?.Invoke(e); }
    }
}
