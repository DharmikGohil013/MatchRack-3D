// Path: Assets/Scripts/UI/UIManager.cs
// Drives all HUD elements: timer display, score counter, combo popup,
// win/lose screens, pause overlay, and undo button state.

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MatchItems
{
    /// <summary>
    /// Master UI controller. Subscribes to GameEvents and updates all on-screen
    /// elements accordingly. All text uses TextMeshPro.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        // ────────────────────────────────────────────
        // HUD REFERENCES
        // ────────────────────────────────────────────

        [Header("HUD — In-Game")]
        [Tooltip("Displays the countdown timer (MM:SS format).")]
        [SerializeField] private TextMeshProUGUI _timerText;

        [Tooltip("Displays the current score.")]
        [SerializeField] private TextMeshProUGUI _scoreText;

        [Tooltip("Displays the combo multiplier (e.g., 'x3'). Hidden when combo = 0.")]
        [SerializeField] private TextMeshProUGUI _comboText;

        [Tooltip("Level number label (e.g., 'Level 1').")]
        [SerializeField] private TextMeshProUGUI _levelText;

        // ────────────────────────────────────────────
        // BUTTONS
        // ────────────────────────────────────────────

        [Header("Buttons")]
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _undoButton;

        // ────────────────────────────────────────────
        // PANELS
        // ────────────────────────────────────────────

        [Header("Panels")]
        [Tooltip("Root HUD panel shown during gameplay.")]
        [SerializeField] private GameObject _hudPanel;

        [Tooltip("Pause overlay panel.")]
        [SerializeField] private GameObject _pausePanel;

        [Tooltip("Win screen panel.")]
        [SerializeField] private GameObject _winPanel;

        [Tooltip("Lose screen panel.")]
        [SerializeField] private GameObject _losePanel;

        // ────────────────────────────────────────────
        // WIN PANEL DETAILS
        // ────────────────────────────────────────────

        [Header("Win Panel")]
        [Tooltip("Final score text on the win screen.")]
        [SerializeField] private TextMeshProUGUI _winScoreText;

        [Tooltip("StarDisplay component on the win screen.")]
        [SerializeField] private StarDisplay _starDisplay;

        [SerializeField] private Button _nextLevelButton;
        [SerializeField] private Button _retryButtonWin;

        // ────────────────────────────────────────────
        // LOSE PANEL DETAILS
        // ────────────────────────────────────────────

        [Header("Lose Panel")]
        [Tooltip("Message text on the lose screen.")]
        [SerializeField] private TextMeshProUGUI _loseMessageText;

        [SerializeField] private Button _retryButtonLose;

        // ────────────────────────────────────────────
        // PAUSE PANEL DETAILS
        // ────────────────────────────────────────────

        [Header("Pause Panel")]
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _retryButtonPause;

        // ────────────────────────────────────────────
        // COMBO POPUP
        // ────────────────────────────────────────────

        [Header("Combo Popup")]
        [Tooltip("CanvasGroup on the combo text for fade effects.")]
        [SerializeField] private CanvasGroup _comboCanvasGroup;

        // ────────────────────────────────────────────
        // LIFECYCLE
        // ────────────────────────────────────────────

        private void OnEnable()
        {
            // Subscribe to game events
            GameEvents.OnGameStateChanged += HandleGameStateChanged;
            GameEvents.OnTimerUpdated += HandleTimerUpdated;
            GameEvents.OnScoreChanged += HandleScoreChanged;
            GameEvents.OnComboChanged += HandleComboChanged;
            GameEvents.OnComboReset += HandleComboReset;
            GameEvents.OnLevelLoaded += HandleLevelLoaded;
            GameEvents.OnUndoAvailabilityChanged += HandleUndoAvailability;

            // Wire up buttons
            if (_pauseButton != null) _pauseButton.onClick.AddListener(OnPauseClicked);
            if (_undoButton != null) _undoButton.onClick.AddListener(OnUndoClicked);
            if (_resumeButton != null) _resumeButton.onClick.AddListener(OnResumeClicked);
            if (_nextLevelButton != null) _nextLevelButton.onClick.AddListener(OnNextLevelClicked);
            if (_retryButtonWin != null) _retryButtonWin.onClick.AddListener(OnRetryClicked);
            if (_retryButtonLose != null) _retryButtonLose.onClick.AddListener(OnRetryClicked);
            if (_retryButtonPause != null) _retryButtonPause.onClick.AddListener(OnRetryClicked);
        }

        private void OnDisable()
        {
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;
            GameEvents.OnTimerUpdated -= HandleTimerUpdated;
            GameEvents.OnScoreChanged -= HandleScoreChanged;
            GameEvents.OnComboChanged -= HandleComboChanged;
            GameEvents.OnComboReset -= HandleComboReset;
            GameEvents.OnLevelLoaded -= HandleLevelLoaded;
            GameEvents.OnUndoAvailabilityChanged -= HandleUndoAvailability;

            // Unwire buttons
            if (_pauseButton != null) _pauseButton.onClick.RemoveListener(OnPauseClicked);
            if (_undoButton != null) _undoButton.onClick.RemoveListener(OnUndoClicked);
            if (_resumeButton != null) _resumeButton.onClick.RemoveListener(OnResumeClicked);
            if (_nextLevelButton != null) _nextLevelButton.onClick.RemoveListener(OnNextLevelClicked);
            if (_retryButtonWin != null) _retryButtonWin.onClick.RemoveListener(OnRetryClicked);
            if (_retryButtonLose != null) _retryButtonLose.onClick.RemoveListener(OnRetryClicked);
            if (_retryButtonPause != null) _retryButtonPause.onClick.RemoveListener(OnRetryClicked);
        }

        // ════════════════════════════════════════════
        // GAME STATE HANDLER
        // ════════════════════════════════════════════

        /// <summary>
        /// Shows/hides panels based on the current game state.
        /// </summary>
        private void HandleGameStateChanged(GameState state)
        {
            // Hide all overlays first
            SetPanel(_pausePanel, false);
            SetPanel(_winPanel, false);
            SetPanel(_losePanel, false);

            switch (state)
            {
                case GameState.Idle:
                case GameState.Loading:
                    SetPanel(_hudPanel, false);
                    break;

                case GameState.Playing:
                    SetPanel(_hudPanel, true);
                    break;

                case GameState.Paused:
                    SetPanel(_hudPanel, true);
                    SetPanel(_pausePanel, true);
                    break;

                case GameState.Win:
                    SetPanel(_hudPanel, true);
                    SetPanel(_winPanel, true);
                    ShowWinScreen();
                    break;

                case GameState.Lose:
                    SetPanel(_hudPanel, true);
                    SetPanel(_losePanel, true);
                    ShowLoseScreen();
                    break;
            }
        }

        // ════════════════════════════════════════════
        // EVENT HANDLERS
        // ════════════════════════════════════════════

        /// <summary>Updates the timer display in MM:SS format.</summary>
        private void HandleTimerUpdated(float remaining)
        {
            if (_timerText == null) return;

            int minutes = Mathf.FloorToInt(remaining / 60f);
            int seconds = Mathf.FloorToInt(remaining % 60f);
            _timerText.text = $"{minutes:00}:{seconds:00}";

            // Flash red when low time (under 30 seconds)
            _timerText.color = remaining < 30f ? Color.red : Color.white;
        }

        /// <summary>Updates the score display.</summary>
        private void HandleScoreChanged(int newScore, int pointsAdded)
        {
            if (_scoreText != null)
            {
                _scoreText.text = newScore.ToString();
            }

            // Bounce the score text for feedback
            if (_scoreText != null && pointsAdded > 0)
            {
                TweenHelper.Bounce(_scoreText.transform, 0.15f, 0.2f);
            }
        }

        /// <summary>Shows/updates the combo popup text.</summary>
        private void HandleComboChanged(int comboCount)
        {
            if (comboCount >= 2)
            {
                // Show combo text
                if (_comboText != null)
                {
                    _comboText.gameObject.SetActive(true);
                    _comboText.text = $"x{Mathf.Min(comboCount, 5)}";
                    TweenHelper.Bounce(_comboText.transform, 0.3f, 0.3f);
                }

                // Fade in combo canvas group
                if (_comboCanvasGroup != null)
                {
                    _comboCanvasGroup.alpha = 0f;
                    TweenHelper.FadeCanvasGroup(_comboCanvasGroup, 1f, 0.2f);
                }
            }
        }

        /// <summary>Hides the combo popup when combo resets.</summary>
        private void HandleComboReset()
        {
            if (_comboText != null)
            {
                if (_comboCanvasGroup != null)
                {
                    TweenHelper.FadeCanvasGroup(_comboCanvasGroup, 0f, 0.3f, () =>
                    {
                        _comboText.gameObject.SetActive(false);
                    });
                }
                else
                {
                    _comboText.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>Called when a new level is loaded — updates the level label and resets HUD.</summary>
        private void HandleLevelLoaded()
        {
            if (_levelText != null && GameManager.Instance != null)
            {
                LevelData ld = GameManager.Instance.LevelManagerRef.CurrentLevelData;
                if (ld != null)
                {
                    _levelText.text = $"Level {ld.levelNumber}";
                }
            }

            // Reset HUD values
            if (_scoreText != null) _scoreText.text = "0";
            if (_comboText != null) _comboText.gameObject.SetActive(false);
        }

        /// <summary>Enables or disables the undo button.</summary>
        private void HandleUndoAvailability(bool available)
        {
            if (_undoButton != null)
            {
                _undoButton.interactable = available;
            }
        }

        // ════════════════════════════════════════════
        // WIN / LOSE SCREENS
        // ════════════════════════════════════════════

        /// <summary>Populates the win screen with final score and star rating.</summary>
        private void ShowWinScreen()
        {
            if (_winScoreText != null && ScoreManager.Instance != null)
            {
                _winScoreText.text = $"Score: {ScoreManager.Instance.CurrentScore}";
            }

            if (_starDisplay != null && ScoreManager.Instance != null)
            {
                int stars = ScoreManager.Instance.CalculateStars();
                _starDisplay.ShowStars(stars);
            }
        }

        /// <summary>Sets the lose screen message based on the reason.</summary>
        private void ShowLoseScreen()
        {
            if (_loseMessageText != null)
            {
                _loseMessageText.text = "Game Over!";
            }
        }

        // ════════════════════════════════════════════
        // BUTTON CALLBACKS
        // ════════════════════════════════════════════

        private void OnPauseClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            GameEvents.FirePauseRequested();
        }

        private void OnUndoClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnUndoPressed();
            }
        }

        private void OnResumeClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            GameEvents.FireResumeRequested();
        }

        private void OnNextLevelClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            GameEvents.FireNextLevelRequested();
        }

        private void OnRetryClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            GameEvents.FireRetryRequested();
        }

        // ════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════

        /// <summary>Safely activates/deactivates a panel if it exists.</summary>
        private void SetPanel(GameObject panel, bool active)
        {
            if (panel != null) panel.SetActive(active);
        }
    }
}
