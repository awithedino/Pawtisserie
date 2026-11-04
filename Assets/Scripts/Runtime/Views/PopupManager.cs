using System.Collections.Generic;
using UnityEngine;

namespace Paw.Runtime
{
    [DisallowMultipleComponent]
    public sealed class PopupManager : MonoBehaviour
    {
        [SerializeField, Tooltip("Shared dim layer behind popups.")] private CanvasGroup dim;
        private readonly List<UiPopup> stack = new List<UiPopup>();
        public bool IsConfigured => dim != null;
        public bool IsOpen => stack.Count > 0;
        public void Open(UiPopup popup)
        {
            if (popup == null || !popup.IsConfigured || !IsConfigured) throw new System.InvalidOperationException("PopupManager needs a dim and configured popup.");
            if (stack.Contains(popup)) return;
            if (stack.Count > 0) stack[stack.Count - 1].SetInput(false);
            stack.Add(popup); popup.transform.SetAsLastSibling(); dim.alpha = 1; dim.blocksRaycasts = true; popup.Open();
        }
        public void Close(UiPopup popup)
        {
            if (popup == null || !stack.Remove(popup)) return;
            popup.Close(); if (stack.Count == 0) { if (dim != null) { dim.alpha = 0; dim.blocksRaycasts = false; } }
            else if (stack[stack.Count - 1] != null) stack[stack.Count - 1].SetInput(true);
        }
        public bool Back()
        {
            if (stack.Count == 0) return false;
            var top = stack[stack.Count - 1];
            if (top == null)
            {
                stack.RemoveAt(stack.Count - 1);
                if (stack.Count == 0 && dim != null) { dim.alpha = 0; dim.blocksRaycasts = false; }
                else if (stack.Count > 0 && stack[stack.Count - 1] != null) stack[stack.Count - 1].SetInput(true);
                return true;
            }
            if (top.DismissOnBack) Close(top); return true;
        }
        public void CloseAll()
        {
            while (stack.Count > 0)
            {
                int index = stack.Count - 1; var popup = stack[index]; stack.RemoveAt(index);
                if (popup != null) popup.Close();
            }
            if (dim != null) { dim.alpha = 0; dim.blocksRaycasts = false; }
        }
        private void OnDisable() { CloseAll(); }
    }
}
