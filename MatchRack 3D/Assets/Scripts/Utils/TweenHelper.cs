// Path: Assets/Scripts/Utils/TweenHelper.cs
// Custom tweening utility — NO external packages (no DOTween, no LeanTween).
// Runs as a singleton MonoBehaviour that manages coroutine-based tweens.
// Provides static convenience methods so callers don't need a reference.

using System;
using System.Collections;
using UnityEngine;

namespace MatchItems
{
    /// <summary>
    /// Lightweight tween engine using coroutines and Mathf.SmoothStep for EaseInOut.
    /// Attach to a persistent GameObject or let the singleton auto-create one.
    /// </summary>
    public class TweenHelper : MonoBehaviour
    {
        // ────────────────────────────────────────────
        // SINGLETON
        // ────────────────────────────────────────────

        private static TweenHelper _instance;

        /// <summary>Returns the singleton instance, creating one if needed.</summary>
        public static TweenHelper Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Try to find an existing one in the scene
                    _instance = FindObjectOfType<TweenHelper>();

                    if (_instance == null)
                    {
                        // Auto-create a persistent GameObject
                        GameObject go = new GameObject("[TweenHelper]");
                        _instance = go.AddComponent<TweenHelper>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            // Standard singleton guard
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ════════════════════════════════════════════
        // PUBLIC STATIC API
        // ════════════════════════════════════════════

        /// <summary>
        /// Smoothly moves a Transform to a target world position over the given duration.
        /// Uses EaseInOut (Mathf.SmoothStep). Calls onComplete when finished.
        /// </summary>
        /// <param name="t">Transform to move.</param>
        /// <param name="target">Destination world position.</param>
        /// <param name="duration">Time in seconds (minimum 0.01).</param>
        /// <param name="onComplete">Optional callback invoked when the tween finishes.</param>
        /// <returns>The running Coroutine so callers can StopCoroutine if needed.</returns>
        public static Coroutine MoveTo(Transform t, Vector3 target, float duration, Action onComplete = null)
        {
            if (t == null) return null;
            return Instance.StartCoroutine(Instance.MoveToRoutine(t, target, duration, onComplete));
        }

        /// <summary>
        /// Smoothly scales a Transform to a target local scale over the given duration.
        /// </summary>
        public static Coroutine ScaleTo(Transform t, Vector3 targetScale, float duration, Action onComplete = null)
        {
            if (t == null) return null;
            return Instance.StartCoroutine(Instance.ScaleToRoutine(t, targetScale, duration, onComplete));
        }

        /// <summary>
        /// Fades a CanvasGroup's alpha to a target value over the given duration.
        /// </summary>
        public static Coroutine FadeCanvasGroup(CanvasGroup cg, float targetAlpha, float duration, Action onComplete = null)
        {
            if (cg == null) return null;
            return Instance.StartCoroutine(Instance.FadeCanvasGroupRoutine(cg, targetAlpha, duration, onComplete));
        }

        /// <summary>
        /// Performs a "bounce" effect: scales the Transform up by strength, then back to its
        /// original scale. Useful for collect/merge feedback.
        /// </summary>
        /// <param name="t">Transform to bounce.</param>
        /// <param name="strength">How much to scale up (e.g., 0.3 = 130% at peak).</param>
        /// <param name="duration">Total bounce time (up + down).</param>
        public static Coroutine Bounce(Transform t, float strength, float duration)
        {
            if (t == null) return null;
            return Instance.StartCoroutine(Instance.BounceRoutine(t, strength, duration));
        }

        // ════════════════════════════════════════════
        // COROUTINE IMPLEMENTATIONS
        // ════════════════════════════════════════════

        /// <summary>Coroutine: lerp position with EaseInOut.</summary>
        private IEnumerator MoveToRoutine(Transform t, Vector3 target, float duration, Action onComplete)
        {
            Vector3 start = t.position;
            float elapsed = 0f;
            duration = Mathf.Max(duration, 0.01f); // guard against zero

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);

                // EaseInOut via SmoothStep (Hermite interpolation)
                float eased = Mathf.SmoothStep(0f, 1f, progress);

                t.position = Vector3.Lerp(start, target, eased);
                yield return null;
            }

            // Snap to exact target to avoid floating-point drift
            t.position = target;
            onComplete?.Invoke();
        }

        /// <summary>Coroutine: lerp local scale with EaseInOut.</summary>
        private IEnumerator ScaleToRoutine(Transform t, Vector3 targetScale, float duration, Action onComplete)
        {
            Vector3 start = t.localScale;
            float elapsed = 0f;
            duration = Mathf.Max(duration, 0.01f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.SmoothStep(0f, 1f, progress);

                t.localScale = Vector3.Lerp(start, targetScale, eased);
                yield return null;
            }

            t.localScale = targetScale;
            onComplete?.Invoke();
        }

        /// <summary>Coroutine: fade a CanvasGroup alpha with EaseInOut.</summary>
        private IEnumerator FadeCanvasGroupRoutine(CanvasGroup cg, float targetAlpha, float duration, Action onComplete)
        {
            float startAlpha = cg.alpha;
            float elapsed = 0f;
            duration = Mathf.Max(duration, 0.01f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.SmoothStep(0f, 1f, progress);

                cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, eased);
                yield return null;
            }

            cg.alpha = targetAlpha;
            onComplete?.Invoke();
        }

        /// <summary>Coroutine: scale up then back down for a bounce effect.</summary>
        private IEnumerator BounceRoutine(Transform t, float strength, float duration)
        {
            Vector3 originalScale = t.localScale;
            Vector3 peakScale = originalScale * (1f + strength);
            float halfDuration = duration * 0.5f;

            // Phase 1: scale UP
            float elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / halfDuration);
                float eased = Mathf.SmoothStep(0f, 1f, progress);
                t.localScale = Vector3.Lerp(originalScale, peakScale, eased);
                yield return null;
            }

            // Phase 2: scale back DOWN to original
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / halfDuration);
                float eased = Mathf.SmoothStep(0f, 1f, progress);
                t.localScale = Vector3.Lerp(peakScale, originalScale, eased);
                yield return null;
            }

            t.localScale = originalScale;
        }
    }
}
