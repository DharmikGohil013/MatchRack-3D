// Path: Assets/Scripts/UI/StarDisplay.cs
// Animates the 1/2/3 star reveal on the win screen.
// Each star scales up from zero with a staggered delay.

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MatchItems
{
    /// <summary>
    /// Manages 3 star Image components on the win screen. Animates them
    /// sequentially (scale from 0 → 1 with bounce) based on the player's star count.
    /// </summary>
    public class StarDisplay : MonoBehaviour
    {
        // ────────────────────────────────────────────
        // REFERENCES
        // ────────────────────────────────────────────

        [Header("Star Images (left to right)")]
        [Tooltip("Image components for the 3 stars. Index 0 = first star.")]
        [SerializeField] private Image[] _starImages = new Image[3];

        [Header("Colors")]
        [Tooltip("Color for an earned (lit) star.")]
        [SerializeField] private Color _earnedColor = Color.yellow;

        [Tooltip("Color for an unearned (dim) star.")]
        [SerializeField] private Color _unearnedColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        [Header("Animation")]
        [Tooltip("Delay between each star's reveal animation.")]
        [SerializeField] private float _staggerDelay = 0.3f;

        [Tooltip("Duration of each star's scale-up animation.")]
        [SerializeField] private float _scaleDuration = 0.3f;

        [Tooltip("Bounce strength for earned stars.")]
        [SerializeField] private float _bounceStrength = 0.4f;

        // ────────────────────────────────────────────
        // PUBLIC API
        // ────────────────────────────────────────────

        /// <summary>
        /// Reveals 0–3 stars with staggered scale-up animations.
        /// Unearned stars are shown dimmed at full scale.
        /// </summary>
        /// <param name="starCount">Number of stars earned (0, 1, 2, or 3).</param>
        public void ShowStars(int starCount)
        {
            starCount = Mathf.Clamp(starCount, 0, 3);

            // Reset all stars to zero scale first
            for (int i = 0; i < _starImages.Length; i++)
            {
                if (_starImages[i] == null) continue;
                _starImages[i].transform.localScale = Vector3.zero;
                _starImages[i].color = (i < starCount) ? _earnedColor : _unearnedColor;
            }

            // Start the staggered reveal
            StartCoroutine(RevealStarsRoutine(starCount));
        }

        /// <summary>
        /// Instantly resets all stars to hidden (zero scale). Called before re-showing.
        /// </summary>
        public void ResetStars()
        {
            for (int i = 0; i < _starImages.Length; i++)
            {
                if (_starImages[i] == null) continue;
                _starImages[i].transform.localScale = Vector3.zero;
            }
        }

        // ────────────────────────────────────────────
        // ANIMATION COROUTINE
        // ────────────────────────────────────────────

        /// <summary>
        /// Sequentially reveals each star with a delay. Earned stars get a bounce;
        /// unearned stars scale up without bounce.
        /// </summary>
        private IEnumerator RevealStarsRoutine(int earnedCount)
        {
            for (int i = 0; i < _starImages.Length; i++)
            {
                if (_starImages[i] == null) continue;

                // Wait before revealing this star
                yield return new WaitForSeconds(_staggerDelay);

                Transform starTransform = _starImages[i].transform;

                if (i < earnedCount)
                {
                    // Earned star: scale up with a bounce effect
                    TweenHelper.ScaleTo(starTransform, Vector3.one, _scaleDuration, () =>
                    {
                        TweenHelper.Bounce(starTransform, _bounceStrength, 0.2f);
                    });
                }
                else
                {
                    // Unearned star: simple scale up, no bounce
                    TweenHelper.ScaleTo(starTransform, Vector3.one * 0.6f, _scaleDuration);
                }
            }
        }
    }
}
