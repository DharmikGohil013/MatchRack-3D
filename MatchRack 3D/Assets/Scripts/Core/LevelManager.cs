// Path: Assets/Scripts/Core/LevelManager.cs
// Loads LevelData ScriptableObjects, spawns the shelf grid, initialises the rack,
// and manages level flow (loading, transitioning, retrying).

using UnityEngine;

namespace MatchItems
{
    /// <summary>
    /// Orchestrates level loading: reads LevelData, tells ShelfGrid to generate,
    /// initialises the CollectionRack, resets ScoreManager, and signals readiness.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        // ────────────────────────────────────────────
        // CONFIGURATION
        // ────────────────────────────────────────────

        [Header("Level Database")]
        [Tooltip("Array of all LevelData assets, in order (index 0 = level 1).")]
        [SerializeField] private LevelData[] _levels;

        [Header("Item Registry")]
        [Tooltip("Reference to the ItemRegistry ScriptableObject.")]
        [SerializeField] private ItemRegistry _itemRegistry;

        [Header("Scene References")]
        [Tooltip("The ShelfGrid component that manages the item grid.")]
        [SerializeField] private ShelfGrid _shelfGrid;

        [Tooltip("The CollectionRack component at the bottom of the screen.")]
        [SerializeField] private CollectionRack _collectionRack;

        // ────────────────────────────────────────────
        // STATE
        // ────────────────────────────────────────────

        /// <summary>Index of the currently loaded level (0-based).</summary>
        private int _currentLevelIndex = 0;

        /// <summary>The currently active LevelData configuration.</summary>
        public LevelData CurrentLevelData { get; private set; }

        /// <summary>Read-only access to the ShelfGrid.</summary>
        public ShelfGrid ShelfGrid => _shelfGrid;

        /// <summary>Read-only access to the CollectionRack.</summary>
        public CollectionRack CollectionRack => _collectionRack;

        // ────────────────────────────────────────────
        // LIFECYCLE
        // ────────────────────────────────────────────

        private void OnEnable()
        {
            GameEvents.OnRetryRequested += RetryCurrentLevel;
            GameEvents.OnNextLevelRequested += LoadNextLevel;
        }

        private void OnDisable()
        {
            GameEvents.OnRetryRequested -= RetryCurrentLevel;
            GameEvents.OnNextLevelRequested -= LoadNextLevel;
        }

        // ════════════════════════════════════════════
        // LEVEL LOADING
        // ════════════════════════════════════════════

        /// <summary>
        /// Loads the level at the given index. Called by GameManager during state transitions.
        /// </summary>
        /// <param name="levelIndex">0-based level index.</param>
        public void LoadLevel(int levelIndex)
        {
            // Clamp index to valid range
            if (_levels == null || _levels.Length == 0)
            {
                Debug.LogError("[LevelManager] No levels configured!");
                return;
            }

            _currentLevelIndex = Mathf.Clamp(levelIndex, 0, _levels.Length - 1);
            CurrentLevelData = _levels[_currentLevelIndex];

            Debug.Log($"[LevelManager] Loading Level {CurrentLevelData.levelNumber}: " +
                      $"{CurrentLevelData.rows}x{CurrentLevelData.columns} grid, " +
                      $"{CurrentLevelData.uniqueItemTypes} types, " +
                      $"{CurrentLevelData.TotalItems} total items, " +
                      $"Time={CurrentLevelData.timeLimit}s");

            // Validate registry has enough item types
            if (_itemRegistry == null)
            {
                Debug.LogError("[LevelManager] ItemRegistry is not assigned!");
                return;
            }
            if (_itemRegistry.Count < CurrentLevelData.uniqueItemTypes)
            {
                Debug.LogWarning($"[LevelManager] Registry has {_itemRegistry.Count} types but level needs " +
                                 $"{CurrentLevelData.uniqueItemTypes}. Some items may be null.");
            }

            // Generate the shelf grid
            if (_shelfGrid != null)
            {
                _shelfGrid.GenerateGrid(CurrentLevelData, _itemRegistry);
            }
            else
            {
                Debug.LogError("[LevelManager] ShelfGrid reference is missing!");
            }

            // Initialise the collection rack
            if (_collectionRack != null)
            {
                _collectionRack.Initialise();
            }
            else
            {
                Debug.LogError("[LevelManager] CollectionRack reference is missing!");
            }

            // Reset score/combo for this level
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.ResetForLevel(CurrentLevelData);
            }

            // Notify the system that the level is ready
            GameEvents.FireLevelLoaded();
        }

        /// <summary>
        /// Reloads the current level from scratch (retry).
        /// </summary>
        public void RetryCurrentLevel()
        {
            LoadLevel(_currentLevelIndex);
        }

        /// <summary>
        /// Advances to the next level. Wraps around if past the last level.
        /// </summary>
        public void LoadNextLevel()
        {
            int next = _currentLevelIndex + 1;
            if (next >= _levels.Length)
            {
                Debug.Log("[LevelManager] All levels completed! Wrapping to level 1.");
                next = 0;
            }
            LoadLevel(next);
        }

        // ════════════════════════════════════════════
        // QUERIES
        // ════════════════════════════════════════════

        /// <summary>Current 0-based level index.</summary>
        public int CurrentLevelIndex => _currentLevelIndex;

        /// <summary>Total number of configured levels.</summary>
        public int TotalLevels => _levels != null ? _levels.Length : 0;

        /// <summary>True if there is a next level available.</summary>
        public bool HasNextLevel => _currentLevelIndex + 1 < TotalLevels;
    }
}
