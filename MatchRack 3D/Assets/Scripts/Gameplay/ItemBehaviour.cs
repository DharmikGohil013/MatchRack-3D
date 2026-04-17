// Path: Assets/Scripts/Gameplay/ItemBehaviour.cs
// Attached to every food item prefab. Stores the item's type identity,
// tracks movement state, and provides tween-based movement helpers.

using System;
using UnityEngine;

namespace MatchItems
{
    /// <summary>
    /// Component on each spawned item. Knows its TypeID (index into ItemRegistry),
    /// which CellSlot it originally belongs to, and whether it's currently animating.
    /// </summary>
    public class ItemBehaviour : MonoBehaviour
    {
        // ────────────────────────────────────────────
        // PUBLIC FIELDS
        // ────────────────────────────────────────────

        [Header("Item Identity")]
        [Tooltip("Index into ItemRegistry.itemPrefabs — determines which food type this is.")]
        public int TypeID;

        [Header("Runtime State")]
        [Tooltip("True while the item is tweening (moving/scaling). Used to block input.")]
        public bool IsMoving;

        [Tooltip("The shelf cell this item belongs to (set at spawn time).")]
        public CellSlot HomeCell;

        // ────────────────────────────────────────────
        // MOVEMENT HELPERS
        // ────────────────────────────────────────────

        /// <summary>
        /// Tweens this item's position to the target over the specified duration.
        /// Sets IsMoving = true during the tween and false when complete.
        /// </summary>
        /// <param name="target">World-space destination.</param>
        /// <param name="duration">Tween time in seconds.</param>
        /// <param name="onComplete">Optional callback when finished.</param>
        public void MoveToPosition(Vector3 target, float duration, Action onComplete = null)
        {
            IsMoving = true;
            TweenHelper.MoveTo(transform, target, duration, () =>
            {
                IsMoving = false;
                onComplete?.Invoke();
            });
        }

        /// <summary>
        /// Tweens this item's scale to the target over the specified duration.
        /// Sets IsMoving = true during the tween and false when complete.
        /// </summary>
        /// <param name="targetScale">Desired local scale.</param>
        /// <param name="duration">Tween time in seconds.</param>
        /// <param name="onComplete">Optional callback when finished.</param>
        public void ScaleToSize(Vector3 targetScale, float duration, Action onComplete = null)
        {
            IsMoving = true;
            TweenHelper.ScaleTo(transform, targetScale, duration, () =>
            {
                IsMoving = false;
                onComplete?.Invoke();
            });
        }

        /// <summary>
        /// Plays a scale-bounce feedback effect on this item.
        /// </summary>
        /// <param name="strength">Overshoot amount (e.g., 0.3 = 130% peak).</param>
        /// <param name="duration">Total bounce time.</param>
        public void PlayBounce(float strength = 0.3f, float duration = 0.25f)
        {
            TweenHelper.Bounce(transform, strength, duration);
        }
    }
}
