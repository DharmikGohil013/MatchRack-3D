// Path: Assets/Scripts/Gameplay/CollectionRack.cs
// Manages the 7-slot collection rack at the bottom of the screen.
// Handles item placement, match detection, merge animation, and slot compaction.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MatchItems
{
    /// <summary>
    /// The collection rack where tapped items land. When 3 items of the same type
    /// are present, they merge and disappear. If all 7 slots fill without a match,
    /// the game is lost.
    /// </summary>
    public class CollectionRack : MonoBehaviour
    {
        // ────────────────────────────────────────────
        // CONFIGURATION
        // ────────────────────────────────────────────

        [Header("Rack Layout")]
        [Tooltip("Number of slots in the rack.")]
        [SerializeField] private int _slotCount = 7;

        [Tooltip("Horizontal spacing between slot centres.")]
        [SerializeField] private float _slotSpacing = 1.0f;

        [Header("Prefab")]
        [Tooltip("Prefab for each RackSlot (needs RackSlot component).")]
        [SerializeField] private GameObject _rackSlotPrefab;

        [Header("Timing")]
        [Tooltip("Duration for an item to fly from the shelf to the rack.")]
        [SerializeField] private float _moveToRackDuration = 0.3f;

        [Tooltip("Duration for matched items to slide to the centre before disappearing.")]
        [SerializeField] private float _mergeMoveDuration = 0.2f;

        [Tooltip("Duration for the scale-to-zero merge animation.")]
        [SerializeField] private float _mergeScaleDuration = 0.2f;

        [Tooltip("Duration for remaining items to slide left to fill gaps.")]
        [SerializeField] private float _compactDuration = 0.2f;

        // ────────────────────────────────────────────
        // RUNTIME STATE
        // ────────────────────────────────────────────

        /// <summary>Ordered list of rack slots (left to right).</summary>
        private List<RackSlot> _slots = new List<RackSlot>();

        /// <summary>True while a merge/compact animation sequence is playing.</summary>
        public bool IsAnimating { get; private set; }

        // ────────────────────────────────────────────
        // INITIALISATION
        // ────────────────────────────────────────────

        /// <summary>
        /// Creates the rack slots. Called by LevelManager during level setup.
        /// </summary>
        public void Initialise()
        {
            ClearRack();

            // Centre the rack horizontally
            float totalWidth = (_slotCount - 1) * _slotSpacing;
            float startX = -totalWidth / 2f;

            for (int i = 0; i < _slotCount; i++)
            {
                Vector3 pos = new Vector3(startX + i * _slotSpacing, 0f, 0f) + transform.position;

                GameObject slotGO;
                if (_rackSlotPrefab != null)
                {
                    slotGO = Instantiate(_rackSlotPrefab, pos, Quaternion.identity, transform);
                }
                else
                {
                    slotGO = new GameObject($"RackSlot_{i}");
                    slotGO.transform.SetParent(transform);
                    slotGO.transform.position = pos;
                }

                slotGO.name = $"RackSlot_{i}";

                RackSlot slot = slotGO.GetComponent<RackSlot>();
                if (slot == null) slot = slotGO.AddComponent<RackSlot>();

                slot.SlotIndex = i;
                _slots.Add(slot);
            }
        }

        /// <summary>Destroys all slot GameObjects and resets state.</summary>
        public void ClearRack()
        {
            foreach (RackSlot slot in _slots)
            {
                if (slot != null) Destroy(slot.gameObject);
            }
            _slots.Clear();
            IsAnimating = false;
        }

        // ════════════════════════════════════════════
        // PLACE ITEM IN RACK
        // ════════════════════════════════════════════

        /// <summary>
        /// Attempts to place an item into the first free slot. Triggers match check after landing.
        /// </summary>
        /// <param name="item">The item to place.</param>
        /// <param name="onSequenceComplete">Called when the full place → match → compact sequence finishes.</param>
        /// <returns>True if a free slot was found; false if rack is full.</returns>
        public bool TryPlaceItem(ItemBehaviour item, Action<bool> onSequenceComplete)
        {
            RackSlot freeSlot = GetFirstFreeSlot();
            if (freeSlot == null)
            {
                // Rack is full — shouldn't normally be called when full, but safety check
                onSequenceComplete?.Invoke(false);
                return false;
            }

            IsAnimating = true;

            // Detach item from cell hierarchy
            item.transform.SetParent(null);

            // Tween item from its current position to the rack slot
            Vector3 target = freeSlot.GetWorldPosition();
            item.MoveToPosition(target, _moveToRackDuration, () =>
            {
                // Place into slot data
                freeSlot.PlaceItem(item);
                item.transform.localScale = Vector3.one;

                GameEvents.FireItemPlacedInRack(item);

                // Check for a match of 3
                StartCoroutine(CheckForMatchAndCompact(onSequenceComplete));
            });

            return true;
        }

        // ════════════════════════════════════════════
        // MATCH DETECTION & MERGE ANIMATION
        // ════════════════════════════════════════════

        /// <summary>
        /// Scans all slots for 3 items of the same type. If found, merges them.
        /// Then compacts remaining items to the left. Fires events throughout.
        /// </summary>
        private IEnumerator CheckForMatchAndCompact(Action<bool> onComplete)
        {
            // Find a matching type (3 of the same typeID)
            int matchTypeID = -1;
            List<RackSlot> matchSlots = new List<RackSlot>();

            // Count occurrences of each type
            Dictionary<int, List<RackSlot>> typeCounts = new Dictionary<int, List<RackSlot>>();
            foreach (RackSlot slot in _slots)
            {
                if (!slot.IsFree)
                {
                    int tid = slot.OccupiedItem.TypeID;
                    if (!typeCounts.ContainsKey(tid))
                        typeCounts[tid] = new List<RackSlot>();
                    typeCounts[tid].Add(slot);
                }
            }

            foreach (var kvp in typeCounts)
            {
                if (kvp.Value.Count >= 3)
                {
                    matchTypeID = kvp.Key;
                    matchSlots = kvp.Value.GetRange(0, 3); // take exactly 3
                    break;
                }
            }

            bool didMatch = matchTypeID >= 0;

            if (didMatch)
            {
                // ── MERGE ANIMATION ──

                GameEvents.FireItemsMatched(matchTypeID);

                // Calculate centre position of the 3 matching items
                Vector3 centre = Vector3.zero;
                foreach (RackSlot ms in matchSlots)
                {
                    centre += ms.GetWorldPosition();
                }
                centre /= matchSlots.Count;

                // 1. Slide matching items to centre
                int movesComplete = 0;
                int totalMoves = matchSlots.Count;

                foreach (RackSlot ms in matchSlots)
                {
                    ms.OccupiedItem.MoveToPosition(centre, _mergeMoveDuration, () =>
                    {
                        movesComplete++;
                    });
                }

                // Wait for all moves to finish
                while (movesComplete < totalMoves) yield return null;

                // 2. Scale to zero (merge disappear)
                int scalesComplete = 0;
                foreach (RackSlot ms in matchSlots)
                {
                    ms.OccupiedItem.ScaleToSize(Vector3.zero, _mergeScaleDuration, () =>
                    {
                        scalesComplete++;
                    });
                }

                while (scalesComplete < totalMoves) yield return null;

                // 3. Destroy matched items and free their slots
                foreach (RackSlot ms in matchSlots)
                {
                    ItemBehaviour item = ms.ClearSlot();
                    if (item != null) Destroy(item.gameObject);
                }

                // 4. Bounce nearby remaining items for visual feedback
                foreach (RackSlot slot in _slots)
                {
                    if (!slot.IsFree)
                    {
                        slot.OccupiedItem.PlayBounce(0.2f, 0.2f);
                    }
                }

                GameEvents.FireMatchAnimationComplete();

                // 5. Compact remaining items to fill gaps (slide left)
                yield return CompactSlots();
            }
            else
            {
                // No match — check if rack is now full
                if (GetOccupiedCount() >= _slotCount)
                {
                    GameEvents.FireRackFull();
                }
            }

            IsAnimating = false;
            onComplete?.Invoke(didMatch);
        }

        /// <summary>
        /// Slides all occupied items to the leftmost contiguous slots,
        /// filling any gaps left by merged items.
        /// </summary>
        private IEnumerator CompactSlots()
        {
            // Gather all items currently in the rack (in order)
            List<ItemBehaviour> items = new List<ItemBehaviour>();
            foreach (RackSlot slot in _slots)
            {
                if (!slot.IsFree)
                {
                    items.Add(slot.ClearSlot());
                }
            }

            // Re-assign items to slots starting from the left
            int movesRemaining = 0;
            for (int i = 0; i < items.Count; i++)
            {
                RackSlot targetSlot = _slots[i];
                targetSlot.PlaceItem(items[i]);

                // Tween to the slot's position
                Vector3 targetPos = targetSlot.GetWorldPosition();
                if (Vector3.Distance(items[i].transform.position, targetPos) > 0.01f)
                {
                    movesRemaining++;
                    items[i].MoveToPosition(targetPos, _compactDuration, () =>
                    {
                        movesRemaining--;
                    });
                }
            }

            // Wait for all compact slides to finish
            while (movesRemaining > 0) yield return null;
        }

        // ════════════════════════════════════════════
        // UNDO SUPPORT
        // ════════════════════════════════════════════

        /// <summary>
        /// Removes a specific item from the rack (for Undo). Returns true if found and removed.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        public bool RemoveItem(ItemBehaviour item)
        {
            foreach (RackSlot slot in _slots)
            {
                if (!slot.IsFree && slot.OccupiedItem == item)
                {
                    slot.ClearSlot();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Compacts the rack after an undo removal so there are no gaps.
        /// </summary>
        public void CompactAfterUndo()
        {
            StartCoroutine(CompactSlots());
        }

        // ════════════════════════════════════════════
        // QUERIES
        // ════════════════════════════════════════════

        /// <summary>Returns the first free slot (left to right), or null if rack is full.</summary>
        public RackSlot GetFirstFreeSlot()
        {
            foreach (RackSlot slot in _slots)
            {
                if (slot.IsFree) return slot;
            }
            return null;
        }

        /// <summary>Returns how many slots are currently occupied.</summary>
        public int GetOccupiedCount()
        {
            int count = 0;
            foreach (RackSlot slot in _slots)
            {
                if (!slot.IsFree) count++;
            }
            return count;
        }

        /// <summary>True if all 7 slots are filled.</summary>
        public bool IsFull => GetOccupiedCount() >= _slotCount;
    }
}
