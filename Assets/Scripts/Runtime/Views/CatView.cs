using System;
using Paw.Sim;
using UnityEngine;

namespace Paw.Runtime
{
    [DisallowMultipleComponent]
    public sealed class CatView : MonoBehaviour
    {
        [SerializeField, Tooltip("Board used to locate marker targets.")] private BoardView board;
        [SerializeField, Tooltip("World-space parent for markers.")] private Transform markerLayer;
        [SerializeField, Tooltip("Marker prefab for the four moves.")] private CatMarker markerPrefab;
        [SerializeField, Tooltip("Maximum simultaneous marker views."), Min(1)] private int poolSize = 4;
        private CatMarker[] pool;
        private ViewConfig config;
        private long impactDelay;
        public bool IsConfigured => board != null && markerLayer != null && markerPrefab != null && markerPrefab.IsConfigured && poolSize > 0;
        public void Initialize(ViewConfig settings)
        {
            if (!IsConfigured) throw new InvalidOperationException("CatView: board, marker layer and configured marker prefab are required.");
            config = settings; impactDelay = settings.CreateSimulationConfig().ImpactDelay;
            if (pool == null)
            {
                pool = new CatMarker[poolSize];
                for (int i = 0; i < pool.Length; i++) { pool[i] = Instantiate(markerPrefab, markerLayer); pool[i].ResetView(); }
            }
            else Clear();
        }
        public void Apply(SimEvent item)
        {
            if (item.Kind != SimEventKind.CardPlayed || pool == null) return;
            CatMarker marker = null;
            foreach (var candidate in pool) if (candidate != null && !candidate.gameObject.activeSelf) { marker = candidate; break; }
            if (marker == null) throw new InvalidOperationException("CatView: marker pool exhausted.");
            var kind = (CardKind)item.EntityId;
            Vector3 end = markerLayer.InverseTransformPoint(board.CellWorld(item.Cell));
            Vector3 start = end;
            if (kind == CardKind.Drill)
                start = markerLayer.InverseTransformPoint(board.CellWorld(new Cell(item.Cell.Column, 0)));
            if (kind == CardKind.Sweep)
            {
                start = markerLayer.InverseTransformPoint(board.CellWorld(new Cell(0, item.Cell.Row)));
                end = markerLayer.InverseTransformPoint(board.CellWorld(new Cell(board.Columns - 1, item.Cell.Row)));
            }
            marker.Begin(kind, start, end, item.Time, item.EndTime - impactDelay, config.ReducedMotion);
        }
        public void Tick(long time) { if (pool != null) foreach (var marker in pool) if (marker != null) marker.Evaluate(time); }
        public void Clear() { if (pool != null) foreach (var marker in pool) if (marker != null) marker.ResetView(); }
        private void OnDisable() { Clear(); }
    }
}
