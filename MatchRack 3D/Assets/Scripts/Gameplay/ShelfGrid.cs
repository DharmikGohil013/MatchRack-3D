// Path: Assets/Scripts/Gameplay/ShelfGrid.cs
// Manages the 2D grid of CellSlots. Handles level generation (shuffle & distribute)
// and provides an API for querying / removing items from the grid.

using System.Collections.Generic;
using UnityEngine;

namespace MatchItems
{
    /// <summary>
    /// Creates and manages the shelf grid of CellSlots based on LevelData configuration.
    /// Owns the generation algorithm: create item list → Fisher-Yates shuffle → round-robin distribute.
    /// </summary>
    public class ShelfGrid : MonoBehaviour
    {
        // ────────────────────────────────────────────
        // CONFIGURATION
        // ────────────────────────────────────────────

        [Header("Grid Settings")]
        [Tooltip("Horizontal spacing between cell centres (world units).")]
        [SerializeField] private float _cellSpacingX = 1.2f;

        [Tooltip("Vertical spacing between cell centres (world units).")]
        [SerializeField] private float _cellSpacingY = 1.2f;

        [Header("Prefab")]
        [Tooltip("Prefab for the CellSlot container (needs a CellSlot component).")]
        [SerializeField] private GameObject _cellSlotPrefab;

        // ────────────────────────────────────────────
        // RUNTIME STATE
        // ────────────────────────────────────────────

        /// <summary>Flat list of all cells in the grid (row-major order).</summary>
        private List<CellSlot> _cells = new List<CellSlot>();

        /// <summary>Total items remaining across all cells. Decremented as items are removed.</summary>
        private int _totalItemsRemaining;

        // ────────────────────────────────────────────
        // PUBLIC PROPERTIES
        // ────────────────────────────────────────────

        /// <summary>How many items are still on the grid (data, not just visual).</summary>
        public int TotalItemsRemaining => _totalItemsRemaining;

        /// <summary>True when all items have been cleared.</summary>
        public bool IsCleared => _totalItemsRemaining <= 0;

        /// <summary>Read-only access to all cells.</summary>
        public IReadOnlyList<CellSlot> Cells => _cells;

        // ════════════════════════════════════════════
        // GENERATION
        // ════════════════════════════════════════════

        /// <summary>
        /// Generates the shelf grid for the given level. This is the main entry point
        /// called by LevelManager after loading the LevelData asset.
        /// </summary>
        /// <param name="levelData">Configuration for this level.</param>
        /// <param name="registry">Item prefab registry.</param>
        public void GenerateGrid(LevelData levelData, ItemRegistry registry)
        {
            // Clean up any previous grid
            ClearGrid();

            int rows = levelData.rows;
            int cols = levelData.columns;
            int totalItems = levelData.TotalItems;
            _totalItemsRemaining = totalItems;

            // Step 1: Build the full item list (each type appears exactly 3 times)
            List<int> allItems = BuildItemList(levelData.uniqueItemTypes);

            // Step 2: Shuffle with Fisher-Yates
            ShuffleList(allItems);

            // Step 3: Distribute items into per-cell buckets (round-robin)
            List<List<int>> cellBuckets = DistributeItems(allItems, rows * cols);

            // Step 4: Calculate the grid origin so it's centred horizontally
            float gridWidth = (cols - 1) * _cellSpacingX;
            float startX = -gridWidth / 2f;
            float startY = 0f; // bottom row at Y=0

            // Step 5: Instantiate cells and assign items
            int bucketIndex = 0;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    // World position for this cell
                    float x = startX + c * _cellSpacingX;
                    float y = startY + r * _cellSpacingY;
                    Vector3 pos = new Vector3(x, y, 0f);

                    // Instantiate cell
                    GameObject cellGO;
                    if (_cellSlotPrefab != null)
                    {
                        cellGO = Instantiate(_cellSlotPrefab, pos, Quaternion.identity, transform);
                    }
                    else
                    {
                        // Fallback: create an empty GameObject
                        cellGO = new GameObject($"Cell_{r}_{c}");
                        cellGO.transform.SetParent(transform);
                        cellGO.transform.position = pos;
                    }

                    cellGO.name = $"Cell_{r}_{c}";

                    CellSlot cell = cellGO.GetComponent<CellSlot>();
                    if (cell == null)
                    {
                        cell = cellGO.AddComponent<CellSlot>();
                    }

                    cell.Row = r;
                    cell.Column = c;
                    cell.Initialise(cellBuckets[bucketIndex], registry);

                    _cells.Add(cell);
                    bucketIndex++;
                }
            }

            Debug.Log($"[ShelfGrid] Generated {rows}x{cols} grid with {totalItems} items ({levelData.uniqueItemTypes} types).");
        }

        /// <summary>
        /// Destroys all cells and their children. Called before regenerating or on cleanup.
        /// </summary>
        public void ClearGrid()
        {
            foreach (CellSlot cell in _cells)
            {
                if (cell != null)
                {
                    Destroy(cell.gameObject);
                }
            }
            _cells.Clear();
            _totalItemsRemaining = 0;
        }

        // ════════════════════════════════════════════
        // ITEM REMOVAL (called by GameManager when an item is tapped)
        // ════════════════════════════════════════════

        /// <summary>
        /// Removes the top item from the specified cell and decrements the total count.
        /// </summary>
        /// <param name="cell">The cell to remove from.</param>
        /// <returns>The removed ItemBehaviour.</returns>
        public ItemBehaviour RemoveItemFromCell(CellSlot cell)
        {
            if (cell == null || cell.IsEmpty) return null;

            ItemBehaviour item = cell.RemoveTopItem();
            if (item != null)
            {
                _totalItemsRemaining--;

                // Check if the entire grid is now clear
                if (_totalItemsRemaining <= 0)
                {
                    GameEvents.FireAllItemsCleared();
                }
            }
            return item;
        }

        /// <summary>
        /// Re-adds an item to a cell (used by Undo). Increments total count.
        /// </summary>
        /// <param name="cell">Target cell.</param>
        /// <param name="item">Item to re-insert.</param>
        public void ReInsertItemToCell(CellSlot cell, ItemBehaviour item)
        {
            if (cell == null || item == null) return;
            cell.ReInsertItem(item);
            _totalItemsRemaining++;
        }

        // ════════════════════════════════════════════
        // PRIVATE HELPERS
        // ════════════════════════════════════════════

        /// <summary>
        /// Builds a flat list where each type ID appears exactly 3 times.
        /// </summary>
        private List<int> BuildItemList(int uniqueTypes)
        {
            List<int> list = new List<int>(uniqueTypes * 3);
            for (int typeID = 0; typeID < uniqueTypes; typeID++)
            {
                list.Add(typeID);
                list.Add(typeID);
                list.Add(typeID);
            }
            return list;
        }

        /// <summary>
        /// Fisher-Yates in-place shuffle for a List of ints.
        /// </summary>
        private void ShuffleList(List<int> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                // Swap
                int temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        /// <summary>
        /// Distributes items round-robin into cellCount buckets.
        /// Each bucket becomes the stack for one CellSlot.
        /// </summary>
        private List<List<int>> DistributeItems(List<int> items, int cellCount)
        {
            List<List<int>> buckets = new List<List<int>>(cellCount);
            for (int i = 0; i < cellCount; i++)
            {
                buckets.Add(new List<int>());
            }

            for (int i = 0; i < items.Count; i++)
            {
                buckets[i % cellCount].Add(items[i]);
            }

            return buckets;
        }
    }
}
