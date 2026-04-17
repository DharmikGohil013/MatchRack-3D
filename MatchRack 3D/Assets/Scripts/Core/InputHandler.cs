// Path: Assets/Scripts/Core/InputHandler.cs
// Detects tap/click input, raycasts into the scene, and notifies GameManager
// when a valid item is tapped. Works with both mouse (Editor) and touch (mobile).

using UnityEngine;
using UnityEngine.InputSystem;

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
                Debug.Log($"[InputHandler] Tap detected at {screenPosition}");
                TryRaycastItem(screenPosition);
            }
        }

        // ────────────────────────────────────────────
        // INPUT DETECTION
        // ────────────────────────────────────────────

        /// <summary>
        /// Returns true if the player tapped/clicked this frame and outputs the screen position.
        /// Uses the new Input System package (UnityEngine.InputSystem).
        /// </summary>
        private bool HasTapInput(out Vector3 screenPosition)
        {
            screenPosition = Vector3.zero;

            // Mouse input
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPosition = Mouse.current.position.ReadValue();
                return true;
            }

            // Touch input (mobile)
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
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
                Debug.Log($"[InputHandler] Raycast hit: {hit.collider.gameObject.name} on layer {hit.collider.gameObject.layer}");

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
            else
            {
                Debug.Log($"[InputHandler] Raycast missed. Layer mask: {_itemLayerMask.value}");
            }
        }
    }
}
