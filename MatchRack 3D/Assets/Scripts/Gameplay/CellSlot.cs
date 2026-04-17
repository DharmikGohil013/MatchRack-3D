// Path: Assets/Scripts/Gameplay/CellSlot.cs
// Represents a single cell on the shelf grid. Each cell holds a stack of items
// where most are "virtual" (data-only) and only the top 2 have instantiated GameObjects.

using System.Collections.Generic;
using UnityEngine;

namespace MatchItems
{
    /// <summary>
    /// A single grid cell that holds a stack of item type IDs.
    /// Only the top 2 items are visually spawned; deeper items exist as data until they surface.
    /// </summary>
    public class CellSlot : MonoBehaviour
    {
        // ────────────────────────────────────────────
        // DATA
        // ────────────────────────────────────────────

        [Header("Stack Data")]
        [Tooltip("Full stack of item type IDs in this cell. Index 0 = bottom, last = top.")]
        [SerializeField] private List<int> _itemTypeIDs = new List<int>();

        [Header("Visual Instances")]
        [Tooltip("Currently spawned item GameObjects (max 2). Last = topmost visible.")]
        [SerializeField] private List<ItemBehaviour> _spawnedItems = new List<ItemBehaviour>();

        /// <summary>Reference to the ItemRegistry so we can spawn prefabs on demand.</summary>
        private ItemRegistry _registry;

        // ────────────────────────────────────────────
        // GRID POSITION (set by ShelfGrid during setup)
        // ────────────────────────────────────────────

        /// <summary>Row index in the grid.</summary>
        public int Row { get; set; }

        /// <summary>Column index in the grid.</summary>
        public int Column { get; set; }

        // ────────────────────────────────────────────
        // PUBLIC PROPERTIES
        // ────────────────────────────────────────────

        /// <summary>Total number of items remaining in this cell (data + visual).</summary>
        public int ItemCount => _itemTypeIDs.Count;

        /// <summary>True if the cell has no items left.</summary>
        public bool IsEmpty => _itemTypeIDs.Count == 0;

        /// <summary>Returns the topmost spawned ItemBehaviour, or null if empty.</summary>
        public ItemBehaviour TopItem => _spawnedItems.Count > 0 ? _spawnedItems[_spawnedItems.Count - 1] : null;

        // ────────────────────────────────────────────
        // INITIALISATION
        // ────────────────────────────────────────────

        /// <summary>
        /// Initialise this cell with a list of type IDs and the item registry.
        /// Called by ShelfGrid during level generation. Spawns the top 2 items visually.
        /// </summary>
        /// <param name="typeIDs">Ordered list of item type IDs (bottom → top).</param>
        /// <param name="registry">Registry used to look up prefabs by typeID.</param>
        public void Initialise(List<int> typeIDs, ItemRegistry registry)
        {
            _registry = registry;
            _itemTypeIDs = new List<int>(typeIDs);
            _spawnedItems.Clear();

            // Spawn up to the top 2 items as visible GameObjects
            SpawnVisibleItems();
        }

        // ────────────────────────────────────────────
        // REMOVE TOP ITEM
        // ────────────────────────────────────────────

        /// <summary>
        /// Removes the topmost item from this cell. The caller is responsible for
        /// tweening the item away; this method only updates data and spawns the
        /// next item if one exists deeper in the stack.
        /// </summary>
        /// <returns>The removed ItemBehaviour (still alive, not destroyed).</returns>
        public ItemBehaviour RemoveTopItem()
        {
            if (_itemTypeIDs.Count == 0 || _spawnedItems.Count == 0)
            {
                Debug.LogWarning($"[CellSlot ({Row},{Column})] Tried to remove from an empty cell.");
                return null;
            }

            // Remove the top type ID from data
            _itemTypeIDs.RemoveAt(_itemTypeIDs.Count - 1);

            // Remove the top visual item
            ItemBehaviour removed = _spawnedItems[_spawnedItems.Count - 1];
            _spawnedItems.RemoveAt(_spawnedItems.Count - 1);

            // If there are still items in data that need a visual, spawn the next one
            RefreshVisuals();

            return removed;
        }

        // ────────────────────────────────────────────
        // RE-INSERT ITEM (for Undo)
        // ────────────────────────────────────────────

        /// <summary>
        /// Re-inserts an item back on top of this cell's stack (used by the Undo system).
        /// </summary>
        /// <param name="item">The ItemBehaviour to put back.</param>
        public void ReInsertItem(ItemBehaviour item)
        {
            // Add type ID back on top
            _itemTypeIDs.Add(item.TypeID);

            // If we already have 2 spawned items, destroy the bottom one (it's now virtual)
            if (_spawnedItems.Count >= 2)
            {
                // The bottommost visible becomes virtual again
                ItemBehaviour bottomVisible = _spawnedItems[0];
                _spawnedItems.RemoveAt(0);
                Destroy(bottomVisible.gameObject);
            }

            // Add the returning item to the top of the spawned list
            _spawnedItems.Add(item);
            item.HomeCell = this;

            // Reposition visuals
            RepositionVisuals();
        }

        // ────────────────────────────────────────────
        // VISUAL MANAGEMENT
        // ────────────────────────────────────────────

        /// <summary>
        /// Spawns visible GameObjects for the top 2 items in the stack.
        /// Called during initialisation.
        /// </summary>
        private void SpawnVisibleItems()
        {
            int count = _itemTypeIDs.Count;
            // Determine how many to spawn (max 2)
            int spawnCount = Mathf.Min(count, 2);

            for (int i = 0; i < spawnCount; i++)
            {
                // Start from the top: index (count - spawnCount + i) gives bottom-to-top order
                int dataIndex = count - spawnCount + i;
                int typeID = _itemTypeIDs[dataIndex];

                ItemBehaviour item = SpawnSingleItem(typeID);
                _spawnedItems.Add(item);
            }

            // Position them correctly
            RepositionVisuals();
        }

        /// <summary>
        /// After the top item is removed, check if we need to promote a virtual item
        /// to a visible one, then reposition all visible items.
        /// </summary>
        private void RefreshVisuals()
        {
            int dataCount = _itemTypeIDs.Count;
            int visualCount = _spawnedItems.Count;

            // We want up to 2 visible items. If we have fewer than 2 and there's data beneath, spawn.
            if (visualCount < 2 && dataCount > visualCount)
            {
                // The next item to surface is at index (dataCount - visualCount - 1) from the bottom,
                // but we want the one just below the current visible items.
                int surfaceIndex = dataCount - visualCount - 1;
                if (surfaceIndex >= 0 && surfaceIndex < dataCount)
                {
                    int typeID = _itemTypeIDs[surfaceIndex];
                    ItemBehaviour newItem = SpawnSingleItem(typeID);
                    // Insert at the bottom of the spawned list (it's the second-from-top)
                    _spawnedItems.Insert(0, newItem);
                }
            }

            RepositionVisuals();
        }

        /// <summary>
        /// Instantiates a single item prefab at this cell's position.
        /// </summary>
        /// <param name="typeID">Which prefab to spawn.</param>
        /// <returns>The new ItemBehaviour.</returns>
        private ItemBehaviour SpawnSingleItem(int typeID)
        {
            GameObject prefab = _registry.GetPrefab(typeID);
            if (prefab == null)
            {
                Debug.LogError($"[CellSlot ({Row},{Column})] Null prefab for typeID {typeID}.");
                return null;
            }

            GameObject go = Instantiate(prefab, transform.position, Quaternion.identity, transform);

            // Ensure the item has a working collider for raycast detection.
            // MeshColliders on prefabs may have null mesh references, so add a
            // BoxCollider fitted to the renderer bounds as a reliable fallback.
            Collider existingCollider = go.GetComponentInChildren<Collider>();
            bool needsCollider = existingCollider == null;
            if (!needsCollider && existingCollider is MeshCollider mc && mc.sharedMesh == null)
            {
                needsCollider = true;
                Destroy(existingCollider);
            }
            if (needsCollider)
            {
                Renderer rend = go.GetComponentInChildren<Renderer>();
                if (rend != null)
                {
                    BoxCollider box = rend.gameObject.AddComponent<BoxCollider>();
                    // Fit the box to the renderer's local bounds
                    box.center = rend.localBounds.center;
                    box.size = rend.localBounds.size;
                }
                else
                {
                    go.AddComponent<BoxCollider>();
                }
            }

            ItemBehaviour item = go.GetComponent<ItemBehaviour>();
            if (item == null)
            {
                item = go.AddComponent<ItemBehaviour>();
            }

            item.TypeID = typeID;
            item.HomeCell = this;
            item.IsMoving = false;

            return item;
        }

        /// <summary>
        /// Positions spawned items so the top item sits at the cell's origin
        /// and the second item is slightly behind (Z+0.5) and smaller (scale 0.85).
        /// </summary>
        private void RepositionVisuals()
        {
            for (int i = 0; i < _spawnedItems.Count; i++)
            {
                if (_spawnedItems[i] == null) continue;

                bool isTop = (i == _spawnedItems.Count - 1);

                if (isTop)
                {
                    // Top item: exact cell position, full scale
                    _spawnedItems[i].transform.localPosition = Vector3.zero;
                    _spawnedItems[i].transform.localScale = Vector3.one;
                }
                else
                {
                    // Second item: slightly behind and smaller
                    _spawnedItems[i].transform.localPosition = new Vector3(0f, 0f, 0.5f);
                    _spawnedItems[i].transform.localScale = Vector3.one * 0.85f;
                }
            }
        }
    }
}
