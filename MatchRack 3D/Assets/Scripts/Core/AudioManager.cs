// Path: Assets/Scripts/Core/AudioManager.cs
// Simple singleton audio manager for playing one-shot SFX clips.
// Listens to GameEvents to play context-appropriate sounds.

using UnityEngine;

namespace MatchItems
{
    /// <summary>
    /// Lightweight audio manager. Plays one-shot SFX through an AudioSource.
    /// Assign clips in the Inspector for each game event.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        // ────────────────────────────────────────────
        // SINGLETON
        // ────────────────────────────────────────────

        private static AudioManager _instance;
        public static AudioManager Instance => _instance;

        // ────────────────────────────────────────────
        // AUDIO SOURCE
        // ────────────────────────────────────────────

        private AudioSource _audioSource;

        // ────────────────────────────────────────────
        // SFX CLIPS (assign in Inspector)
        // ────────────────────────────────────────────

        [Header("SFX Clips")]
        [Tooltip("Played when the player taps an item.")]
        [SerializeField] private AudioClip _tapClip;

        [Tooltip("Played when an item lands in a rack slot.")]
        [SerializeField] private AudioClip _placeClip;

        [Tooltip("Played when 3 items merge/match.")]
        [SerializeField] private AudioClip _matchClip;

        [Tooltip("Played on combo increments (x2, x3 …).")]
        [SerializeField] private AudioClip _comboClip;

        [Tooltip("Played on level win.")]
        [SerializeField] private AudioClip _winClip;

        [Tooltip("Played on level lose.")]
        [SerializeField] private AudioClip _loseClip;

        [Tooltip("Played when undo is used.")]
        [SerializeField] private AudioClip _undoClip;

        [Tooltip("Generic UI button click.")]
        [SerializeField] private AudioClip _buttonClip;

        // ────────────────────────────────────────────
        // LIFECYCLE
        // ────────────────────────────────────────────

        private void Awake()
        {
            // Singleton guard
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Ensure we have an AudioSource
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
            _audioSource.playOnAwake = false;
        }

        private void OnEnable()
        {
            // Auto-play sounds on key events
            GameEvents.OnItemTapped += OnItemTapped;
            GameEvents.OnItemPlacedInRack += OnItemPlaced;
            GameEvents.OnItemsMatched += OnItemsMatched;
            GameEvents.OnComboChanged += OnComboChanged;
            GameEvents.OnLevelWin += OnWin;
            GameEvents.OnLevelLose += OnLose;
            GameEvents.OnUndoPerformed += OnUndo;
        }

        private void OnDisable()
        {
            GameEvents.OnItemTapped -= OnItemTapped;
            GameEvents.OnItemPlacedInRack -= OnItemPlaced;
            GameEvents.OnItemsMatched -= OnItemsMatched;
            GameEvents.OnComboChanged -= OnComboChanged;
            GameEvents.OnLevelWin -= OnWin;
            GameEvents.OnLevelLose -= OnLose;
            GameEvents.OnUndoPerformed -= OnUndo;
        }

        // ────────────────────────────────────────────
        // PUBLIC API
        // ────────────────────────────────────────────

        /// <summary>
        /// Plays a one-shot SFX clip. Safe to call with null (no-op).
        /// </summary>
        /// <param name="clip">The AudioClip to play.</param>
        public void PlaySFX(AudioClip clip)
        {
            if (clip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }

        /// <summary>Plays the generic button click sound.</summary>
        public void PlayButtonClick()
        {
            PlaySFX(_buttonClip);
        }

        // ────────────────────────────────────────────
        // EVENT HANDLERS
        // ────────────────────────────────────────────

        private void OnItemTapped(ItemBehaviour item) => PlaySFX(_tapClip);
        private void OnItemPlaced(ItemBehaviour item) => PlaySFX(_placeClip);
        private void OnItemsMatched(int typeID) => PlaySFX(_matchClip);
        private void OnComboChanged(int comboCount)
        {
            if (comboCount >= 2) PlaySFX(_comboClip);
        }
        private void OnWin() => PlaySFX(_winClip);
        private void OnLose() => PlaySFX(_loseClip);
        private void OnUndo() => PlaySFX(_undoClip);
    }
}
