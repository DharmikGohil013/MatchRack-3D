// Path: Assets/Scripts/Core/InputHandler.cs
// Detects tap/click input, raycasts into the scene, and notifies GameManager
// when a valid item is tapped. Works with both mouse (Editor) and touch (mobile).

using UnityEngine;

namespace MatchItems
{
    /// <summary>
    /// Reads mouse/touch input each frame, performs a physics raycast against items
    /// on the "Items" layer, and forwards valid taps to GameManager.
    /// Input is blocked while animations are playing (GameManager.IsAnimating).
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        // ────────────────────────────────────────────
        // CONFIGURATION
        // ────────────────────────────────────────────

        [Header("Raycast Settings")]
        [Tooltip("LayerMask for items that can be tapped. Set to the 'Items' layer.")]
        [SerializeField] private LayerMask _itemLayerMask = ~0; // default: Everything

        [Tooltip("Maximum raycast distance.")]
        [SerializeField] private float _raycastDistance = 100f;

        // ────────────────────────────────────────────
        // CACHED REFERENCES
        // ────────────────────────────────────────────

        private Camera _mainCamera;

        // ────────────────────────────────────────────
        // LIFECYCLE
        // ────────────────────────────────────────────

        private void Start()
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                Debug.LogError("[InputHandler] No Camera tagged MainCamera found in scene.");
            }
        }

        /// <summary>
        /// Each frame, check for a tap/click. If detected, raycast and attempt to find an item.
        /// </summary>
        private void Update()
        {
            // Only process input while the game is in the Playing state
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.CurrentState != GameState.Playing) return;

            // Block input during animations
            if (GameManager.Instance.IsAnimating) return;

            // Detect input
            if (HasTapInput(out Vector3 screenPosition))
            {
                TryRaycastItem(screenPosition);
            }
        }

        // ────────────────────────────────────────────
        // INPUT DETECTION
        // ────────────────────────────────────────────

        /// <summary>
        /// Returns true if the player tapped/clicked this frame and outputs the screen position.
        /// Supports both mouse (Editor/standalone) and single-touch (mobile).
        /// </summary>
        private bool HasTapInput(out Vector3 screenPosition)
        {
            screenPosition = Vector3.zero;

            // Mouse input (works in Editor and standalone)
            if (Input.GetMouseButtonDown(0))
            {
                screenPosition = Input.mousePosition;
                return true;
            }

            // Touch input (mobile)
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    screenPosition = touch.position;
                    return true;
                }
            }

            return false;
        }

        // ────────────────────────────────────────────
        // RAYCAST
        // ────────────────────────────────────────────

        /// <summary>
        /// Casts a ray from the camera through the screen position.
        /// If it hits an object with an ItemBehaviour on the "Items" layer,
        /// notifies GameManager.
        /// </summary>
        private void TryRaycastItem(Vector3 screenPosition)
        {
            if (_mainCamera == null) return;

            Ray ray = _mainCamera.ScreenPointToRay(screenPosition);

            if (Physics.Raycast(ray, out RaycastHit hit, _raycastDistance, _itemLayerMask))
            {
                // Check if the hit object (or its parent) has an ItemBehaviour
                ItemBehaviour item = hit.collider.GetComponentInParent<ItemBehaviour>();

                if (item != null && !item.IsMoving)
                {
                    // Only allow tapping the top item in a cell
                    if (item.HomeCell != null && item.HomeCell.TopItem == item)
                    {
                        GameManager.Instance.OnItemTapped(item);
                    }
                }
            }
        }
    }
}
