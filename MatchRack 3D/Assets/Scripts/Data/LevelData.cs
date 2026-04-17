// Path: Assets/Scripts/Data/LevelData.cs
// ScriptableObject that defines all configuration for a single level.
// Create instances via Assets → Create → Game → LevelData.

using UnityEngine;

namespace MatchItems
{
    /// <summary>
    /// Data-driven level configuration. Each level asset stores grid dimensions,
    /// item variety count, time limit, and star score thresholds.
    /// Total items spawned = uniqueItemTypes * 3 (each type appears exactly 3 times).
    /// </summary>
    [CreateAssetMenu(menuName = "Game/LevelData", fileName = "Level_New")]
    public class LevelData : ScriptableObject
    {
        [Header("Level Identity")]
        [Tooltip("Sequential level number (1, 2, 3 …)")]
        public int levelNumber = 1;

        [Header("Grid Dimensions")]
        [Tooltip("Number of rows (vertical) in the shelf grid.")]
        public int rows = 3;

        [Tooltip("Number of columns (horizontal) in the shelf grid.")]
        public int columns = 4;

        [Header("Item Configuration")]
        [Tooltip("How many different item/food types this level uses. Total items = this × 3.")]
        public int uniqueItemTypes = 10;

        [Header("Timer")]
        [Tooltip("Countdown timer in seconds. Reaching 0 triggers a loss.")]
        public float timeLimit = 300f;

        [Header("Star Thresholds")]
        [Tooltip("Score needed for 1, 2, and 3 stars respectively. Array length must be 3.")]
        public int[] starThresholds = new int[] { 400, 800, 1200 };

        // ────────────────────────────────────────────
        // CONVENIENCE PROPERTIES
        // ────────────────────────────────────────────

        /// <summary>Total number of item instances in this level (each type × 3).</summary>
        public int TotalItems => uniqueItemTypes * 3;

        /// <summary>Total number of cells in the grid.</summary>
        public int TotalCells => rows * columns;

        /// <summary>Average items per cell (for debug / balancing info).</summary>
        public float AverageItemsPerCell => (float)TotalItems / TotalCells;
    }
}
