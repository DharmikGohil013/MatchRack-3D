// Path: Assets/Scripts/Gameplay/RackSlot.cs
// Represents a single slot in the bottom collection rack.
// Can be occupied by one item or empty. Tracks its world position.

using UnityEngine;

namespace MatchItems
{
    /// <summary>
    /// One of 7 slots in the collection rack. Knows its position and
    /// whether it currently holds an item.
    /// </summary>
    public class RackSlot : MonoBehaviour
    {
        // ────────────────────────────────────────────
        // STATE
        // ────────────────────────────────────────────

        /// <summary>The item currently occupying this slot, or null if empty.</summary>
        public ItemBehaviour OccupiedItem { get; private set; }

        /// <summary>True if the slot has no item.</summary>
        public bool IsFree => OccupiedItem == null;

        /// <summary>Index of this slot in the rack (0–6).</summary>
        public int SlotIndex { get; set; }

        // ────────────────────────────────────────────
        // SLOT MANAGEMENT
        // ────────────────────────────────────────────

        /// <summary>
        /// Places an item into this slot. The item's parent is set to this slot's transform.
        /// </summary>
        /// <param name="item">Item to place.</param>
        public void PlaceItem(ItemBehaviour item)
        {
            OccupiedItem = item;
            if (item != null)
            {
                item.transform.SetParent(transform);
            }
        }

        /// <summary>
        /// Clears this slot, removing the item reference. Does NOT destroy the item.
        /// </summary>
        /// <returns>The item that was removed (may be null).</returns>
        public ItemBehaviour ClearSlot()
        {
            ItemBehaviour item = OccupiedItem;
            OccupiedItem = null;
            return item;
        }

        /// <summary>
        /// Returns the world-space position of this slot's centre.
        /// Used as the target for item movement tweens.
        /// </summary>
        public Vector3 GetWorldPosition()
        {
            return transform.position;
        }
    }
}
