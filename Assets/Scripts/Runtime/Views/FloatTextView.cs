using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Paw.Runtime
{
    [DisallowMultipleComponent]
    public sealed class FloatTextView : MonoBehaviour
    {
        [SerializeField, Tooltip("Floating text label.")] private Text label;
        [SerializeField, Tooltip("Text opacity group.")] private CanvasGroup group;
        [SerializeField, Tooltip("Shuffle feedback.")] private string shuffle = "SHUFFLED!";
        [SerializeField, Tooltip("Stash feedback.")] private string stash = "STASHED!";
        [SerializeField, Tooltip("Call box feedback.")] private string called = "CALLED!";
        [SerializeField, Tooltip("Queue feedback.")] private string next = "NEXT UP!";
        [SerializeField, Tooltip("Express feedback.")] private string express = "EXPRESS BOX!";
        [SerializeField, Tooltip("Tray feedback position.")] private Vector2 trayPosition = new Vector2(0, 80);
        [SerializeField, Tooltip("Shelf feedback position.")] private Vector2 shelfPosition = new Vector2(100, 165);
        [SerializeField, Tooltip("Box feedback position.")] private Vector2 boxPosition = new Vector2(0, 275);
        [SerializeField, Tooltip("Queue feedback position.")] private Vector2 queuePosition = new Vector2(0, 220);
        [SerializeField, Tooltip("Floating distance."), Min(0)] private float rise = 30;
        [SerializeField, Tooltip("Visible duration in seconds."), Min(0)] private float duration = .7f;
        private RectTransform rect;
        public bool IsConfigured => label != null && group != null;
        private void Awake() { rect = (RectTransform)transform; }
        private void OnDisable() { if (rect != null) rect.DOKill(); if (group != null) group.DOKill(); }
        public void Clear() { if (rect != null) rect.DOKill(); if (group != null) { group.DOKill(); group.alpha = 0; } }
        public void ShowShuffle() => Show(shuffle, trayPosition);
        public void ShowStash() => Show(stash, shelfPosition);
        public void ShowCalled() => Show(called, boxPosition);
        public void ShowNext() => Show(next, queuePosition);
        public void ShowExpress() => Show(express, shelfPosition);
        private void Show(string message, Vector2 position)
        {
            if (!IsConfigured) throw new InvalidOperationException("FloatTextView needs a label and canvas group.");
            if (rect == null) rect = (RectTransform)transform;
            Clear(); label.text = message; rect.anchoredPosition = position; group.alpha = 1;
            DOTween.To(() => rect.anchoredPosition, value => rect.anchoredPosition = value, position + new Vector2(0, rise), duration).SetTarget(rect).SetUpdate(true);
            DOTween.To(() => group.alpha, value => group.alpha = value, 0, duration).SetTarget(group).SetUpdate(true);
        }
    }
}
