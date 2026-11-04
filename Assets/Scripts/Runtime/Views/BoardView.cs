using System;
using System.Collections.Generic;
using DG.Tweening;
using Paw.Presentation;
using Paw.Sim;
using UnityEngine;

namespace Paw.Runtime
{
    [DisallowMultipleComponent]
    public sealed class BoardView : MonoBehaviour
    {
        [SerializeField, Tooltip("Layer containing pastry, cell and badge instances.")] private Transform worldLayer;
        [SerializeField, Tooltip("Top-center tray anchor, authored on the prefab.")] private Transform trayAnchor;
        [SerializeField, Tooltip("Pastry/can view prefab.")] private PastryView pastryPrefab;
        [SerializeField, Tooltip("Tray cell renderer prefab.")] private SpriteRenderer cellPrefab;
        [SerializeField, Tooltip("Aim badge prefab.")] private BadgeView badgePrefab;
        [SerializeField, Tooltip("Optional nested counter and queue prefab.")] private CounterRowView counterRow;
        [SerializeField, Tooltip("Optional nested shelf and rack prefab.")] private ShelfRackView shelfRack;
        [SerializeField, Tooltip("Three counter box views.")] private BoxLaneView[] lanes;
        [SerializeField, Tooltip("Miniature queue views.")] private BoxLaneView[] queueViews;
        [SerializeField, Tooltip("Five authored plate anchors.")] private Transform[] plateAnchors;
        [SerializeField, Tooltip("Two authored rack anchors.")] private Transform[] rackAnchors;
        [SerializeField, Tooltip("Rack visual, open during a combo or while occupied.")] private GameObject rackVisual;
        [SerializeField, Tooltip("Stash visual and its anchor.")] private Transform stashAnchor;
        [SerializeField, Tooltip("Overflow anchor.")] private Transform overflowAnchor;
        [SerializeField, Tooltip("Tint of the cells the aimed move will hit.")] private Color blastColor = new Color(.96f, .77f, .33f, .85f);
        [SerializeField, Tooltip("Rotation in degrees when a tile is pressed without a card."), Min(0)] private float emptyTapWobble = 5;
        [SerializeField, Tooltip("Seconds for the empty-tile wobble."), Min(0)] private float emptyTapWobbleDuration = .2f;
        private readonly List<PastryView> pool = new List<PastryView>();
        private readonly List<SpriteRenderer> cells = new List<SpriteRenderer>();
        private readonly List<BadgeView> badges = new List<BadgeView>();
        private readonly Dictionary<int, PastryView> active = new Dictionary<int, PastryView>();
        private readonly Dictionary<int, BoxState> boxes = new Dictionary<int, BoxState>();
        private readonly Dictionary<int, int> boxTargets = new Dictionary<int, int>();
        private readonly Dictionary<int, long> arrival = new Dictionary<int, long>();
        private readonly HashSet<int> landed = new HashSet<int>();
        private readonly Dictionary<int, int> slots = new Dictionary<int, int>();
        private readonly List<int> plates = new List<int>();
        private readonly List<int> rack = new List<int>();
        private readonly List<int> stash = new List<int>();
        private readonly List<int> queue = new List<int>();
        private ViewConfig config;
        private BoardMapping mapping;
        private int used;
        private Color cellColor;
        public bool IsConfigured => worldLayer != null && trayAnchor != null && pastryPrefab != null && cellPrefab != null && badgePrefab != null &&
            (counterRow != null ? counterRow.IsConfigured : lanes != null && lanes.Length == 3 && queueViews != null && queueViews.Length >= 12) &&
            (shelfRack != null ? shelfRack.IsConfigured : plateAnchors != null && plateAnchors.Length == 5 && rackAnchors != null && rackAnchors.Length == 2 && rackVisual != null && stashAnchor != null && overflowAnchor != null);

        public void Initialize(Simulator sim, ViewConfig settings)
        {
            if (!IsConfigured) throw new InvalidOperationException("BoardView has missing prefab or anchor references.");
            if (counterRow != null) { lanes = counterRow.Lanes; queueViews = counterRow.Queue; }
            if (shelfRack != null) { plateAnchors = shelfRack.Plates; rackAnchors = shelfRack.Rack; rackVisual = shelfRack.RackVisual; stashAnchor = shelfRack.Stash; overflowAnchor = shelfRack.Overflow; }
            config = settings; Clear();
            while (pool.Count < config.PoolSize) { var view = Instantiate(pastryPrefab, worldLayer); view.gameObject.SetActive(false); pool.Add(view); }
            while (cells.Count < 49) { var cell = Instantiate(cellPrefab, worldLayer); cell.gameObject.SetActive(false); cells.Add(cell); }
            while (badges.Count < 49) { var badge = Instantiate(badgePrefab, worldLayer); badge.gameObject.SetActive(false); badges.Add(badge); }
            Vector3 anchor = Local(trayAnchor);
            mapping = new BoardMapping(new Point2(anchor.x - sim.Columns * config.CellSize * .5, anchor.y), config.CellSize, sim.Columns, sim.Rows);
            cellColor = cellPrefab.color;
            for (int i = 0; i < sim.Columns * sim.Rows; i++)
            {
                var cell = new Cell(i % sim.Columns, i / sim.Columns);
                cells[i].transform.localPosition = CellPosition(cell); cells[i].color = cellColor;
                cells[i].transform.localScale = new Vector3((config.CellSize - 3) / cells[i].sprite.bounds.size.x, (config.CellSize - 3) / cells[i].sprite.bounds.size.y, 1);
                cells[i].gameObject.SetActive(true);
                var item = sim.GetTrayCell(i);
                if (item.Exists)
                {
                    var view = Rent(item.Id); view.Configure(item.Id, item.Type, item.Cloche, CellPosition(cell), config);
                }
            }
            var snapshot = sim.State;
            foreach (var box in snapshot.Lanes) if (box != null) boxes.Add(box.Id, box);
            foreach (var box in snapshot.Queue) { boxes.Add(box.Id, box); queue.Add(box.Id); }
            for (int i = 0; i < lanes.Length; i++) if (snapshot.Lanes[i] != null) lanes[i].Show(snapshot.Lanes[i], config); else lanes[i].Hide();
            RefreshQueue(); rackVisual.SetActive(false); stashAnchor.gameObject.SetActive(false);
        }

        public void Clear()
        {
            foreach (var view in pool) if (view != null) view.gameObject.SetActive(false);
            foreach (var cell in cells) if (cell != null) { cell.transform.DOKill(); cell.transform.localRotation = Quaternion.identity; cell.gameObject.SetActive(false); }
            foreach (var badge in badges) if (badge != null) badge.gameObject.SetActive(false);
            active.Clear(); boxes.Clear(); boxTargets.Clear(); arrival.Clear(); landed.Clear(); slots.Clear();
            plates.Clear(); rack.Clear(); stash.Clear(); queue.Clear(); used = 0;
        }

        private PastryView Rent(int id)
        {
            if (used >= pool.Count) throw new InvalidOperationException("Pastry view pool exhausted; increase ViewConfig pool size.");
            var view = pool[used++]; active.Add(id, view); return view;
        }
        private void Release(int id)
        {
            if (!active.TryGetValue(id, out PastryView view)) return;
            active.Remove(id); view.gameObject.SetActive(false);
            // Returned views are reused on the next level; total entities never exceed the authored tray.
        }
        private Vector3 Local(Transform anchor) => worldLayer.InverseTransformPoint(anchor.position);
        private Vector3 CellPosition(Cell cell) { var point = mapping.Center(cell); return new Vector3((float)point.X, (float)point.Y, 0); }
        public int Columns => mapping.Columns;
        public Vector3 CellWorld(Cell cell) => worldLayer.TransformPoint(CellPosition(cell));
        public bool TryCell(Vector3 world, out Cell cell)
        { var point = worldLayer.InverseTransformPoint(world); return mapping.TryCell(new Point2(point.x, point.y), out cell); }
        public void Wobble(Cell cell)
        {
            if (config == null || config.ReducedMotion || emptyTapWobbleDuration <= 0) return;
            int index = cell.Row * mapping.Columns + cell.Column;
            if (index < 0 || index >= cells.Count || cells[index] == null) return;
            var tile = cells[index].transform; tile.DOKill(); tile.localRotation = Quaternion.identity;
            tile.DOPunchRotation(new Vector3(0, 0, emptyTapWobble), emptyTapWobbleDuration).SetUpdate(true);
        }
        private BoxLaneView FindLane(int id)
        { foreach (var lane in lanes) if (lane.Id == id) return lane; return null; }

        public void Apply(SimEvent item)
        {
            active.TryGetValue(item.EntityId, out PastryView view);
            switch (item.Kind)
            {
                case SimEventKind.ClocheBroken: if (view != null) view.SetCloche(false); break;
                case SimEventKind.CanPrimed: if (view != null) view.Prime(); break;
                case SimEventKind.CanExploded: Release(item.EntityId); break;
                case SimEventKind.TrayFell: if (view != null) view.Move(CellPosition(item.Cell), item.Time, item.EndTime, 0); break;
                case SimEventKind.Routed: Route(item, view); break;
                case SimEventKind.Landed:
                    landed.Add(item.EntityId);
                    if ((Destination)item.Value == Destination.Box)
                    {
                        int boxId = boxTargets[item.EntityId]; boxes[boxId].Filled++;
                        var lane = FindLane(boxId); if (lane != null) lane.Land(); Release(item.EntityId);
                    }
                    else if ((Destination)item.Value == Destination.Express) Release(item.EntityId);
                    break;
                case SimEventKind.BoxClosing:
                    var closing = FindLane(item.EntityId); if (closing != null) closing.Close(item.Time, item.EndTime); break;
                case SimEventKind.BoxLeaving:
                    var leaving = FindLane(item.EntityId); if (leaving != null) leaving.Leave(item.Time, item.EndTime); break;
                case SimEventKind.BoxDeparted:
                    var departed = FindLane(item.EntityId); if (departed != null) departed.Hide(); break;
                case SimEventKind.BoxArriving:
                    queue.Remove(item.EntityId); lanes[item.Value].Show(boxes[item.EntityId], config);
                    lanes[item.Value].Arrive(item.Time, item.EndTime); RefreshQueue(); break;
                case SimEventKind.BoxResized:
                    boxes[item.EntityId].Need = item.Value;
                    var resized = FindLane(item.EntityId); if (resized != null) resized.SetNeed(item.Value); RefreshQueue(); break;
                case SimEventKind.BoxRemoved: queue.Remove(item.EntityId); RefreshQueue(); break;
                case SimEventKind.GiftTransferred:
                    boxes[item.Value].Gift = boxes[item.EntityId].Gift; boxes[item.EntityId].Gift = null;
                    RefreshGifts(); break;
                case SimEventKind.GiftEarned: boxes[item.EntityId].Gift = null; RefreshGifts(); break;
            }
        }

        private void Route(SimEvent item, PastryView view)
        {
            if (view == null) throw new InvalidOperationException("A route has no pastry view.");
            plates.Remove(item.EntityId); rack.Remove(item.EntityId); stash.Remove(item.EntityId); landed.Remove(item.EntityId); slots.Remove(item.EntityId);
            arrival[item.EntityId] = item.EndTime;
            var destination = (Destination)item.Value;
            Vector3 target;
            if (destination == Destination.Box)
            {
                var lane = FindLane(item.TargetId);
                if (lane == null) throw new InvalidOperationException("A reserved box view is missing.");
                target = worldLayer.InverseTransformPoint(lane.transform.parent.TransformPoint(lane.ReservePosition(item.Time)));
                boxTargets[item.EntityId] = item.TargetId;
            }
            else if (destination == Destination.Plate) { plates.Add(item.EntityId); slots[item.EntityId] = plates.Count - 1; target = Local(plateAnchors[plates.Count - 1]); }
            else if (destination == Destination.Rack) { rack.Add(item.EntityId); slots[item.EntityId] = rack.Count - 1; target = Local(rackAnchors[rack.Count - 1]); }
            else if (destination == Destination.Stash) { stash.Add(item.EntityId); target = Local(stashAnchor); }
            else target = Local(overflowAnchor);
            view.Move(target, item.Time, item.EndTime, config.ArcHeight);
            Compact(plates, plateAnchors, item.Time); Compact(rack, rackAnchors, item.Time);
        }

        private void Compact(List<int> storage, Transform[] anchors, long time)
        {
            for (int i = 0; i < storage.Count; i++)
            {
                int id = storage[i]; if (!active.TryGetValue(id, out PastryView view)) continue;
                // Only a pastry whose slot changed moves; re-issuing an unchanged flight would restart its arc.
                if (slots.TryGetValue(id, out int slot) && slot == i) continue;
                slots[id] = i;
                long end = landed.Contains(id) ? time + config.CompactionTime : arrival[id];
                view.Move(Local(anchors[i]), time, Math.Max(time, end), landed.Contains(id) ? 0 : config.ArcHeight);
            }
        }
        private void RefreshQueue()
        {
            for (int i = 0; i < queueViews.Length; i++)
                if (i < queue.Count) queueViews[i].Show(boxes[queue[i]], config); else queueViews[i].Hide();
        }
        private void RefreshGifts()
        {
            foreach (var lane in lanes) if (lane.Id != 0) lane.SetGift(boxes[lane.Id].Gift.HasValue);
            RefreshQueue();
        }
        public void Tick(long time, bool combo)
        {
            foreach (var pair in active) pair.Value.Evaluate(time);
            foreach (var lane in lanes) lane.Evaluate(time);
            rackVisual.SetActive(combo || rack.Count > 0); stashAnchor.gameObject.SetActive(stash.Count > 0);
        }
        public void Preview(PreviewSummary summary, CardKind card, Cell target)
        {
            ClearPreview();
            foreach (var cell in CardShapes.GetCells(card, target, mapping.Columns, mapping.Rows))
                cells[cell.Row * mapping.Columns + cell.Column].color = blastColor;
            for (int i = 0; i < summary.Badges.Count && i < badges.Count; i++)
            {
                var badge = summary.Badges[i]; badges[i].transform.localPosition = CellPosition(badge.Cell) + new Vector3(15, 14, 0);
                badges[i].Show(badge.Kind);
            }
        }
        public void ClearPreview()
        {
            foreach (var cell in cells) if (cell != null) cell.color = cellColor;
            foreach (var badge in badges) if (badge != null) badge.gameObject.SetActive(false);
        }
    }
}
