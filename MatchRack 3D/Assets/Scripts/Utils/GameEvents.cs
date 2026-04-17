// Path: Assets/Scripts/Utils/GameEvents.cs
// Static event system for decoupled communication between game systems.
// All events are declared as static Action delegates so any script can subscribe/unsubscribe
// without needing a direct reference to the publisher.

using System;
using UnityEngine;

namespace MatchItems
{
    /// <summary>
    /// Centralized static event hub. Systems fire events here; listeners subscribe in OnEnable
    /// and unsubscribe in OnDisable to avoid memory leaks and null-reference callbacks.
    /// </summary>
    public static class GameEvents
    {
        // ────────────────────────────────────────────
        // GAME STATE EVENTS
        // ────────────────────────────────────────────

        /// <summary>Fired when the game state changes (Idle, Loading, Playing, Paused, Win, Lose).</summary>
        public static event Action<GameState> OnGameStateChanged;

        /// <summary>Fired when a level has finished loading and is ready to play.</summary>
        public static event Action OnLevelLoaded;

        /// <summary>Fired when the player wins the current level.</summary>
        public static event Action OnLevelWin;

        /// <summary>Fired when the player loses (rack full or timer expired).</summary>
        public static event Action OnLevelLose;

        // ────────────────────────────────────────────
        // ITEM / RACK EVENTS
        // ────────────────────────────────────────────

        /// <summary>Fired when a player taps a valid item on the shelf. Param: the tapped ItemBehaviour.</summary>
        public static event Action<ItemBehaviour> OnItemTapped;

        /// <summary>Fired when an item finishes moving to the rack.</summary>
        public static event Action<ItemBehaviour> OnItemPlacedInRack;

        /// <summary>Fired when 3 matching items are found in the rack. Param: the typeID that matched.</summary>
        public static event Action<int> OnItemsMatched;

        /// <summary>Fired after the merge animation completes and matched items are destroyed.</summary>
        public static event Action OnMatchAnimationComplete;

        /// <summary>Fired when the rack becomes full (7/7) with no match — triggers lose.</summary>
        public static event Action OnRackFull;

        /// <summary>Fired when all items have been cleared from the shelf grid — triggers win.</summary>
        public static event Action OnAllItemsCleared;

        // ────────────────────────────────────────────
        // SCORE / COMBO EVENTS
        // ────────────────────────────────────────────

        /// <summary>Fired when the score changes. Params: newScore, pointsAdded.</summary>
        public static event Action<int, int> OnScoreChanged;

        /// <summary>Fired when the combo multiplier changes. Param: new combo count.</summary>
        public static event Action<int> OnComboChanged;

        /// <summary>Fired when combo resets back to 0.</summary>
        public static event Action OnComboReset;

        // ────────────────────────────────────────────
        // TIMER EVENTS
        // ────────────────────────────────────────────

        /// <summary>Fired every frame while timer is running. Param: remaining seconds.</summary>
        public static event Action<float> OnTimerUpdated;

        /// <summary>Fired when the countdown timer reaches zero.</summary>
        public static event Action OnTimerExpired;

        // ────────────────────────────────────────────
        // UNDO EVENTS
        // ────────────────────────────────────────────

        /// <summary>Fired when an undo action is performed.</summary>
        public static event Action OnUndoPerformed;

        /// <summary>Fired when the undo availability changes. Param: true = undo available.</summary>
        public static event Action<bool> OnUndoAvailabilityChanged;

        // ────────────────────────────────────────────
        // UI EVENTS
        // ────────────────────────────────────────────

        /// <summary>Fired when the pause button is pressed.</summary>
        public static event Action OnPauseRequested;

        /// <summary>Fired when the resume button is pressed.</summary>
        public static event Action OnResumeRequested;

        /// <summary>Fired when the retry button is pressed.</summary>
        public static event Action OnRetryRequested;

        /// <summary>Fired when the next-level button is pressed.</summary>
        public static event Action OnNextLevelRequested;

        // ════════════════════════════════════════════
        // INVOCATION HELPERS — only called by the system that owns the event
        // ════════════════════════════════════════════

        public static void FireGameStateChanged(GameState state) => OnGameStateChanged?.Invoke(state);
        public static void FireLevelLoaded() => OnLevelLoaded?.Invoke();
        public static void FireLevelWin() => OnLevelWin?.Invoke();
        public static void FireLevelLose() => OnLevelLose?.Invoke();

        public static void FireItemTapped(ItemBehaviour item) => OnItemTapped?.Invoke(item);
        public static void FireItemPlacedInRack(ItemBehaviour item) => OnItemPlacedInRack?.Invoke(item);
        public static void FireItemsMatched(int typeID) => OnItemsMatched?.Invoke(typeID);
        public static void FireMatchAnimationComplete() => OnMatchAnimationComplete?.Invoke();
        public static void FireRackFull() => OnRackFull?.Invoke();
        public static void FireAllItemsCleared() => OnAllItemsCleared?.Invoke();

        public static void FireScoreChanged(int newScore, int pointsAdded) => OnScoreChanged?.Invoke(newScore, pointsAdded);
        public static void FireComboChanged(int comboCount) => OnComboChanged?.Invoke(comboCount);
        public static void FireComboReset() => OnComboReset?.Invoke();

        public static void FireTimerUpdated(float remaining) => OnTimerUpdated?.Invoke(remaining);
        public static void FireTimerExpired() => OnTimerExpired?.Invoke();

        public static void FireUndoPerformed() => OnUndoPerformed?.Invoke();
        public static void FireUndoAvailabilityChanged(bool available) => OnUndoAvailabilityChanged?.Invoke(available);

        public static void FirePauseRequested() => OnPauseRequested?.Invoke();
        public static void FireResumeRequested() => OnResumeRequested?.Invoke();
        public static void FireRetryRequested() => OnRetryRequested?.Invoke();
        public static void FireNextLevelRequested() => OnNextLevelRequested?.Invoke();
    }

    /// <summary>
    /// Enumeration of all possible game states for the state machine.
    /// </summary>
    public enum GameState
    {
        Idle,
        Loading,
        Playing,
        Paused,
        Win,
        Lose
    }
}
