// Path: Assets/Scripts/Gameplay/ItemStackPreview.cs
// Visual helper that displays a count badge or indicator showing how many items
// remain in a CellSlot's stack beyond the top 2 visible ones.

using TMPro;
using UnityEngine;

namespace MatchItems
{
    /// <summary>
    /// Attach to each CellSlot (or its child) to display a count indicator
    /// showing how many items remain in the stack. Updates every frame based
    /// on the associated CellSlot's item count.
    /// </summary>
    public class ItemStackPreview : MonoBehaviour
    {
        // ────────────────────────────────────────────
        // REFERENCES
        // ────────────────────────────────────────────

        [Header("References")]
        [Tooltip("The CellSlot this preview is associated with. Auto-detected if on the same GameObject.")]
        [SerializeField] private CellSlot _cellSlot;

        [Tooltip("TextMeshPro label showing the remaining item count (e.g., '+5').")]
        [SerializeField] private TextMeshPro _countLabel;

        [Tooltip("Optional background sprite/object for the count badge.")]
        [SerializeField] private GameObject _badgeBackground;

        // ────────────────────────────────────────────
        // LIFECYCLE
        // ────────────────────────────────────────────

        private void Awake()
        {
            // Auto-detect cell slot if not assigned
            if (_cellSlot == null)
            {
                _cellSlot = GetComponentInParent<CellSlot>();
            }
        }

        /// <summary>
        /// Update is called once per frame. Refreshes the stack count display.
        /// Only shows the badge when there are more than 2 items (i.e., hidden items exist).
        /// </summary>
        private void Update()
        {
            if (_cellSlot == null) return;

            int totalItems = _cellSlot.ItemCount;
            int hiddenCount = Mathf.Max(0, totalItems - 2);

            bool showBadge = hiddenCount > 0;

            // Update the count text
            if (_countLabel != null)
            {
                _countLabel.gameObject.SetActive(showBadge);
                if (showBadge)
                {
                    _countLabel.text = $"+{hiddenCount}";
                }
            }

            // Toggle badge background
            if (_badgeBackground != null)
            {
                _badgeBackground.SetActive(showBadge);
            }
        }

        // ────────────────────────────────────────────
        // PUBLIC SETUP
        // ────────────────────────────────────────────

        /// <summary>
        /// Manually sets the associated cell slot (used when creating previews via code).
        /// </summary>
        /// <param name="cell">The cell slot to observe.</param>
        public void SetCellSlot(CellSlot cell)
        {
            _cellSlot = cell;
        }
    }
}
