// Path: Assets/Scripts/Core/ScoreManager.cs
// Singleton that tracks score, combo multiplier, and star calculation.
// Listens to GameEvents for match / tap outcomes and updates accordingly.

using UnityEngine;

namespace MatchItems
{
    /// <summary>
    /// Central score tracker. Manages base score, combo multiplier (max x5),
    /// and calculates star rating against LevelData thresholds.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        // ────────────────────────────────────────────
        // SINGLETON
        // ────────────────────────────────────────────

        private static ScoreManager _instance;
        public static ScoreManager Instance => _instance;

        // ────────────────────────────────────────────
        // CONSTANTS
        // ────────────────────────────────────────────

        /// <summary>Base points awarded per successful 3-match.</summary>
        private const int BASE_SCORE_PER_MATCH = 100;

        /// <summary>Maximum combo multiplier cap.</summary>
        private const int MAX_COMBO = 5;

        // ────────────────────────────────────────────
        // STATE
        // ────────────────────────────────────────────

        /// <summary>Current accumulated score for this level.</summary>
        public int CurrentScore { get; private set; }

        /// <summary>Current combo streak count (0 = no active combo).</summary>
        public int ComboCount { get; private set; }

        /// <summary>Current multiplier derived from ComboCount (1× to 5×).</summary>
        public float ComboMultiplier => Mathf.Min(ComboCount, MAX_COMBO);

        /// <summary>Reference to the active LevelData (for star thresholds).</summary>
        private LevelData _currentLevel;

        // ────────────────────────────────────────────
        // LIFECYCLE
        // ────────────────────────────────────────────

        private void Awake()
        {
            // Singleton setup
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            // Subscribe to relevant events
            GameEvents.OnItemsMatched += HandleMatch;
            GameEvents.OnItemPlacedInRack += HandleItemPlaced;
        }

        private void OnDisable()
        {
            GameEvents.OnItemsMatched -= HandleMatch;
            GameEvents.OnItemPlacedInRack -= HandleItemPlaced;
        }

        // ────────────────────────────────────────────
        // PUBLIC API
        // ────────────────────────────────────────────

        /// <summary>
        /// Resets score and combo for a new level. Called by LevelManager.
        /// </summary>
        /// <param name="levelData">The level being loaded (for star thresholds).</param>
        public void ResetForLevel(LevelData levelData)
        {
            _currentLevel = levelData;
            CurrentScore = 0;
            ComboCount = 0;

            GameEvents.FireScoreChanged(CurrentScore, 0);
            GameEvents.FireComboChanged(ComboCount);
        }

        /// <summary>
        /// Manually adds score (for bonus scenarios). Fires the ScoreChanged event.
        /// </summary>
        public void AddScore(int points)
        {
            CurrentScore += points;
            GameEvents.FireScoreChanged(CurrentScore, points);
        }

        /// <summary>
        /// Calculates how many stars the player earned based on the current level's thresholds.
        /// </summary>
        /// <returns>0, 1, 2, or 3 stars.</returns>
        public int CalculateStars()
        {
            if (_currentLevel == null || _currentLevel.starThresholds == null)
                return 0;

            int stars = 0;
            int[] thresholds = _currentLevel.starThresholds;

            // Check each threshold from lowest to highest
            for (int i = 0; i < thresholds.Length; i++)
            {
                if (CurrentScore >= thresholds[i])
                    stars = i + 1;
            }

            return stars;
        }

        // ────────────────────────────────────────────
        // EVENT HANDLERS
        // ────────────────────────────────────────────

        /// <summary>
        /// Called when 3 items successfully match. Increments combo, calculates score, fires events.
        /// </summary>
        private void HandleMatch(int typeID)
        {
            // Increment combo streak
            ComboCount++;
            GameEvents.FireComboChanged(ComboCount);

            // Calculate score: base × min(combo, maxCombo)
            int multiplier = Mathf.Min(ComboCount, MAX_COMBO);
            int points = BASE_SCORE_PER_MATCH * multiplier;

            AddScore(points);

            Debug.Log($"[ScoreManager] Match! Type={typeID}, Combo={ComboCount}, " +
                      $"Multiplier=x{multiplier}, Points=+{points}, Total={CurrentScore}");
        }

        /// <summary>
        /// Called when an item is placed in the rack. We use the match-animation-complete
        /// event to determine if a match happened. If no match follows the placement,
        /// the combo resets. This is tracked by a flag set in HandleMatch.
        /// </summary>
        private bool _matchOccurredThisTurn;

        /// <summary>
        /// Called each time an item lands in the rack. We reset a flag so we can detect
        /// whether a match follows. The actual reset happens after the full sequence completes.
        /// </summary>
        private void HandleItemPlaced(ItemBehaviour item)
        {
            _matchOccurredThisTurn = false;
            // We temporarily subscribe to match-animation-complete to know if a match happened
            GameEvents.OnMatchAnimationComplete += OnMatchThisTurn;
        }

        /// <summary>Flag helper: a match did happen this turn.</summary>
        private void OnMatchThisTurn()
        {
            _matchOccurredThisTurn = true;
            GameEvents.OnMatchAnimationComplete -= OnMatchThisTurn;
        }

        /// <summary>
        /// Should be called by GameManager after the full place-check-compact sequence.
        /// If no match occurred, the combo resets.
        /// </summary>
        public void FinaliseRound()
        {
            // Clean up subscription just in case
            GameEvents.OnMatchAnimationComplete -= OnMatchThisTurn;

            if (!_matchOccurredThisTurn && ComboCount > 0)
            {
                ComboCount = 0;
                GameEvents.FireComboReset();
                GameEvents.FireComboChanged(ComboCount);
                Debug.Log("[ScoreManager] Combo reset — no match this round.");
            }

            _matchOccurredThisTurn = false;
        }
    }
}
