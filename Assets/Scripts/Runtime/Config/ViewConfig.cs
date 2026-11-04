using System;
using Paw.Sim;
using UnityEngine;

namespace Paw.Runtime
{
    [CreateAssetMenu(menuName = "Paw/Prototype/View Config")]
    public sealed class ViewConfig : ScriptableObject
    {
        [SerializeField, Tooltip("Logical board width."), Min(1)] private float boardWidth = 400;
        [SerializeField, Tooltip("Smallest logical height kept on screen; shorter screens fit this height instead of the width."), Min(1)] private float boardHeight = 800;
        [SerializeField, Tooltip("Logical tray cell spacing."), Min(1)] private float cellSize = 50;
        [SerializeField, Tooltip("Pastry diameter in logical units."), Min(1)] private float pastrySize = 34;
        [SerializeField, Tooltip("Height of a pastry flight arc."), Min(0)] private float arcHeight = 45;
        [SerializeField, Tooltip("Prewarmed pastry views including the tray."), Min(1)] private int poolSize = 60;
        [SerializeField, Tooltip("Paw landing time in seconds."), Min(0)] private float tapLanding = .32f;
        [SerializeField, Tooltip("Pounce landing time in seconds."), Min(0)] private float pounceLanding = .45f;
        [SerializeField, Tooltip("Flop landing time in seconds."), Min(0)] private float flopLanding = .36f;
        [SerializeField, Tooltip("Tray gravity travel time in seconds."), Min(0)] private float gravityTravel = .25f;
        [SerializeField, Tooltip("Seconds between the outcome and its panel (GDD §8.1: win panel 0.7 s)."), Min(0)] private float panelDelay = .7f;
        [SerializeField, Tooltip("Seconds for storage compaction."), Min(0)] private float compactionTime = .18f;
        [SerializeField, Tooltip("Starting Shuffle inventory for prototype runs."), Min(0)] private int startingShuffle = 2;
        [SerializeField, Tooltip("Starting Call box inventory for prototype runs."), Min(0)] private int startingCallBox = 2;
        [SerializeField, Tooltip("Starting Stash inventory for prototype runs."), Min(0)] private int startingStash = 1;
        [SerializeField, Tooltip("Pastry colors, in stable type ID order.")] private Color[] pastryColors = {
            new Color(1f, .494f, .69f), new Color(.416f, .659f, .941f), new Color(.31f, .78f, .604f), new Color(.949f, .663f, .231f) };
        [SerializeField, Tooltip("Card band colors in card ID order: TAP, SWIPE, POUNCE, FLOP.")] private Color[] cardColors = {
            new Color(1f, .494f, .69f), new Color(.949f, .651f, .353f), new Color(.31f, .722f, .788f), new Color(.965f, .769f, .325f) };
        public float BoardWidth => boardWidth;
        public float BoardHeight => boardHeight;
        public Color CardColor(CardKind card) => cardColors[(int)card];
        public float CellSize => cellSize;
        public float PastrySize => pastrySize;
        public float ArcHeight => arcHeight;
        public int PoolSize => poolSize;
        public long PanelDelay => Micros(panelDelay);
        public long CompactionTime => Micros(compactionTime);
        public Color PastryColor(int type) => pastryColors[type];
        public int[] CreateInventory() => new[] { startingShuffle, startingCallBox, startingStash };
        public SimulationConfig CreateSimulationConfig() => new SimulationConfig(Micros(tapLanding), Micros(pounceLanding), Micros(flopLanding), Micros(gravityTravel));
        public bool IsValid => boardWidth > 0 && boardHeight > 0 && cellSize > 0 && pastrySize > 0 && arcHeight >= 0 && poolSize >= 49 &&
            tapLanding >= 0 && pounceLanding >= 0 && flopLanding >= 0 && gravityTravel >= 0 && panelDelay >= 0 && compactionTime >= 0 &&
            startingShuffle >= 0 && startingCallBox >= 0 && startingStash >= 0 && pastryColors != null && pastryColors.Length == 4 && cardColors != null && cardColors.Length == 4;
        private static long Micros(float seconds) => (long)Math.Round((double)seconds * 1000000);
        private void OnValidate() { if (!IsValid) Debug.LogError("ViewConfig needs valid sizes, nonnegative timings, four pastry and four card colors, and at least 49 pooled views."); }
    }
}
