// Path: Assets/Scripts/Core/GameManager.cs
// Singleton that owns the game state machine and orchestrates all gameplay systems.
// States: IDLE → LOADING → PLAYING → PAUSED → WIN → LOSE
// Handles item taps, undo, timer countdown, and state transitions.

using System;
using UnityEngine;

namespace MatchItems
{
    /// <summary>
    /// Central game controller. Runs the state machine, processes item taps,
    /// manages the undo system, ticks the countdown timer, and wires up
    /// win/lose conditions via GameEvents.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // ────────────────────────────────────────────
        // SINGLETON
        // ────────────────────────────────────────────

        private static GameManager _instance;
        public static GameManager Instance => _instance;

        // ────────────────────────────────────────────
        // INSPECTOR REFERENCES
        // ────────────────────────────────────────────

        [Header("Scene References")]
        [Tooltip("LevelManager that handles level loading and grid/rack creation.")]
        [SerializeField] private LevelManager _levelManager;

        /// <summary>Public read-only access so UIManager can query level data.</summary>
        public LevelManager LevelManagerRef => _levelManager;

        // ────────────────────────────────────────────
        // STATE MACHINE
        // ────────────────────────────────────────────

        /// <summary>Current state of the game.</summary>
        public GameState CurrentState { get; private set; } = GameState.Idle;

        /// <summary>
        /// True while any animation sequence is playing (item move, merge, compact).
        /// InputHandler checks this to block input.
        /// </summary>
        public bool IsAnimating { get; private set; }

        // ────────────────────────────────────────────
        // TIMER
        // ────────────────────────────────────────────

        /// <summary>Seconds remaining on the countdown timer.</summary>
        private float _timeRemaining;

        // ────────────────────────────────────────────
        // UNDO SYSTEM
        // ────────────────────────────────────────────

        /// <summary>Record of the last item placed in the rack (for undo).</summary>
        private UndoRecord? _lastUndoRecord;

        /// <summary>Whether the undo power is currently available.</summary>
        private bool _undoAvailable;

        /// <summary>Struct storing all info needed to reverse the last tap.</summary>
        private struct UndoRecord
        {
            public ItemBehaviour Item;
            public CellSlot OriginalCell;
        }

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

        private void Start()
        {
            // Begin loading the first level
            ChangeState(GameState.Loading);
        }

        private void OnEnable()
        {
            // Listen for end-condition events
            GameEvents.OnRackFull += HandleRackFull;
            GameEvents.OnAllItemsCleared += HandleAllItemsCleared;
            GameEvents.OnPauseRequested += OnPausePressed;
            GameEvents.OnResumeRequested += OnResumePressed;
            GameEvents.OnLevelLoaded += HandleLevelLoaded;
            GameEvents.OnRetryRequested += OnRetryPressed;
            GameEvents.OnNextLevelRequested += OnNextLevelPressed;
        }

        private void OnDisable()
        {
            GameEvents.OnRackFull -= HandleRackFull;
            GameEvents.OnAllItemsCleared -= HandleAllItemsCleared;
            GameEvents.OnPauseRequested -= OnPausePressed;
            GameEvents.OnResumeRequested -= OnResumePressed;
            GameEvents.OnLevelLoaded -= HandleLevelLoaded;
            GameEvents.OnRetryRequested -= OnRetryPressed;
            GameEvents.OnNextLevelRequested -= OnNextLevelPressed;
        }

        private void Update()
        {
            // Tick the timer only while playing
            if (CurrentState == GameState.Playing)
            {
                TickTimer();
            }
        }

        // ════════════════════════════════════════════
        // STATE MACHINE
        // ════════════════════════════════════════════

        /// <summary>
        /// Transitions to a new game state. Handles entry logic for each state.
        /// </summary>
        /// <param name="newState">The state to transition to.</param>
        private void ChangeState(GameState newState)
        {
            GameState previousState = CurrentState;
            CurrentState = newState;

            Debug.Log($"[GameManager] State: {previousState} → {newState}");

            switch (newState)
            {
                case GameState.Idle:
                    break;

                case GameState.Loading:
                    // Load the current level (LevelManager fires OnLevelLoaded when done)
                    IsAnimating = false;
                    _lastUndoRecord = null;
                    SetUndoAvailable(false);
                    _levelManager.LoadLevel(_levelManager.CurrentLevelIndex);
                    // LoadLevel triggers ChangeState(Playing) synchronously.
                    // Return early to avoid firing the Loading event AFTER Playing.
                    return;

                case GameState.Playing:
                    // Initialise the timer from level data
                    _timeRemaining = _levelManager.CurrentLevelData.timeLimit;
                    break;

                case GameState.Paused:
                    break;

                case GameState.Win:
                    GameEvents.FireLevelWin();
                    break;

                case GameState.Lose:
                    GameEvents.FireLevelLose();
                    break;
            }

            GameEvents.FireGameStateChanged(newState);
        }

        // ════════════════════════════════════════════
        // TIMER
        // ════════════════════════════════════════════

        /// <summary>
        /// Decrements the timer each frame. Fires TimerExpired when it hits zero.
        /// </summary>
        private void TickTimer()
        {
            _timeRemaining -= Time.deltaTime;

            if (_timeRemaining <= 0f)
            {
                _timeRemaining = 0f;
                GameEvents.FireTimerUpdated(_timeRemaining);
                GameEvents.FireTimerExpired();
                ChangeState(GameState.Lose);
                return;
            }

            GameEvents.FireTimerUpdated(_timeRemaining);
        }

        // ════════════════════════════════════════════
        // ITEM TAP HANDLING
        // ════════════════════════════════════════════

        /// <summary>
        /// Called by InputHandler when a valid item is tapped.
        /// Removes the item from the shelf, records undo, and places it in the rack.
        /// </summary>
        /// <param name="item">The tapped ItemBehaviour.</param>
        public void OnItemTapped(ItemBehaviour item)
        {
            if (CurrentState != GameState.Playing) return;
            if (IsAnimating) return;
            if (item == null || item.HomeCell == null) return;

            // Fire the tap event (audio, etc.)
            GameEvents.FireItemTapped(item);

            // Record undo before moving
            CellSlot originalCell = item.HomeCell;
            _lastUndoRecord = new UndoRecord
            {
                Item = item,
                OriginalCell = originalCell
            };
            SetUndoAvailable(true);

            // Remove item from the shelf grid
            ShelfGrid grid = _levelManager.ShelfGrid;
            grid.RemoveItemFromCell(originalCell);

            // Mark as animating to block further input
            IsAnimating = true;

            // Place item into the collection rack
            CollectionRack rack = _levelManager.CollectionRack;
            bool placed = rack.TryPlaceItem(item, (didMatch) =>
            {
                // After the full sequence (place → match check → compact) completes:
                IsAnimating = false;

                // Tell ScoreManager to finalise this round (resets combo if no match)
                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.FinaliseRound();
                }

                // If a match happened, the undo record is no longer valid
                // (the item may have been destroyed)
                if (didMatch)
                {
                    _lastUndoRecord = null;
                    SetUndoAvailable(false);

                    // Check win condition: all items cleared
                    if (grid.IsCleared)
                    {
                        ChangeState(GameState.Win);
                    }
                }
            });

            // If the rack was already full and couldn't place, the rack-full event
            // will fire from CollectionRack's match check, triggering Lose via HandleRackFull.
            if (!placed)
            {
                IsAnimating = false;
            }
        }

        // ════════════════════════════════════════════
        // UNDO
        // ════════════════════════════════════════════

        /// <summary>
        /// Undoes the last tap: removes the item from the rack, tweens it back to its cell,
        /// and re-inserts it into the shelf grid. Only 1 undo available per tap.
        /// </summary>
        public void OnUndoPressed()
        {
            if (CurrentState != GameState.Playing) return;
            if (IsAnimating) return;
            if (!_undoAvailable || _lastUndoRecord == null) return;

            UndoRecord record = _lastUndoRecord.Value;

            // Check if the item still exists (hasn't been merged)
            if (record.Item == null)
            {
                _lastUndoRecord = null;
                SetUndoAvailable(false);
                return;
            }

            IsAnimating = true;
            SetUndoAvailable(false);
            _lastUndoRecord = null;

            // Remove item from the rack
            CollectionRack rack = _levelManager.CollectionRack;
            rack.RemoveItem(record.Item);
            rack.CompactAfterUndo();

            // Detach from rack hierarchy
            record.Item.transform.SetParent(null);

            // Tween item back to its original cell position
            Vector3 targetPos = record.OriginalCell.transform.position;
            record.Item.MoveToPosition(targetPos, 0.3f, () =>
            {
                // Re-insert into the shelf grid
                ShelfGrid grid = _levelManager.ShelfGrid;
                grid.ReInsertItemToCell(record.OriginalCell, record.Item);

                IsAnimating = false;

                GameEvents.FireUndoPerformed();
            });
        }

        /// <summary>Updates the undo availability flag and fires the event.</summary>
        private void SetUndoAvailable(bool available)
        {
            _undoAvailable = available;
            GameEvents.FireUndoAvailabilityChanged(available);
        }

        // ════════════════════════════════════════════
        // PAUSE / RESUME
        // ════════════════════════════════════════════

        /// <summary>Pauses the game (freezes timer, blocks input).</summary>
        public void OnPausePressed()
        {
            if (CurrentState == GameState.Playing)
            {
                ChangeState(GameState.Paused);
            }
        }

        /// <summary>Resumes the game from pause.</summary>
        public void OnResumePressed()
        {
            if (CurrentState == GameState.Paused)
            {
                ChangeState(GameState.Playing);
            }
        }

        // ════════════════════════════════════════════
        // RETRY / NEXT LEVEL
        // ════════════════════════════════════════════

        /// <summary>Reloads the current level.</summary>
        public void OnRetryPressed()
        {
            ChangeState(GameState.Loading);
        }

        /// <summary>Advances to the next level.</summary>
        public void OnNextLevelPressed()
        {
            // LevelManager already listens to OnNextLevelRequested and increments index
            ChangeState(GameState.Loading);
        }

        // ════════════════════════════════════════════
        // EVENT HANDLERS
        // ════════════════════════════════════════════

        /// <summary>Triggered when the rack is full (7/7) with no match.</summary>
        private void HandleRackFull()
        {
            if (CurrentState == GameState.Playing)
            {
                ChangeState(GameState.Lose);
            }
        }

        /// <summary>Triggered when all items are cleared from the shelf.</summary>
        private void HandleAllItemsCleared()
        {
            // Win condition is also checked after match sequence in OnItemTapped,
            // but this event-based handler covers edge cases.
            if (CurrentState == GameState.Playing)
            {
                ChangeState(GameState.Win);
            }
        }

        /// <summary>Triggered when the level has finished loading.</summary>
        private void HandleLevelLoaded()
        {
            // Transition from Loading → Playing
            if (CurrentState == GameState.Loading)
            {
                ChangeState(GameState.Playing);
            }
        }
    }
}
