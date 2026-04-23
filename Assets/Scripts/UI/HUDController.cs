// HUDController.cs
// Responsibility: Read live game state from manager scripts each frame
// and push the values into the corresponding UI elements on the canvas.
// This script owns NO game logic — it is a pure display layer (View in MVC terms).

using UnityEngine;
using UnityEngine.UI;   // Needed for the Image component (fill bars)
using TMPro;            // Needed for TMP_Text (TextMeshPro text elements)

public class HUDController : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    // DATA SOURCES (injected via Inspector)
    // These are references to the manager scripts that own the actual game data.
    // HUDController reads from them — it never writes back.
    // [SerializeField] makes a private field visible and assignable in the Unity Inspector.
    // ─────────────────────────────────────────────────────────────────────────

    [SerializeField] private TimerManager timerManager;
    // Source of truth for the countdown clock. Exposes CurrentTime (float, in seconds).

    [SerializeField] private ScoreManager scoreManager;
    // Source of truth for the player's score. Exposes Score (int).

    [SerializeField] private ForestManager forestManager;
    // Source of truth for how many trees are alive vs dead.
    // Exposes ForestHealthPercent (float, 0–100).

    [SerializeField] private HeatGaugeManager heatGaugeManager;
    // Source of truth for the sun heat level.
    // Exposes NormalizedHeat (float, 0.0–1.0), already pre-scaled for fill bars.

    // ─────────────────────────────────────────────────────────────────────────
    // UI TARGETS (injected via Inspector)
    // These are the actual Unity UI objects that get updated each frame.
    // [Header("UI")] just draws a labelled separator in the Inspector for readability.
    // ─────────────────────────────────────────────────────────────────────────
    [Header("UI")]
    [SerializeField] private TMP_Text timerText;
    // The TextMeshPro text object in the top-center of the canvas (e.g. "01:30").

    [SerializeField] private TMP_Text scoreText;
    // The TextMeshPro text object showing the player's score (e.g. "Score: 250").

    [SerializeField] private Image forestHealthFill;
    // The fill Image inside ForestHealthBar. Its fillAmount (0.0–1.0) drives the bar width.
    // fillAmount = 1.0 means full bar (all trees alive); 0.0 means all trees dead.

    [SerializeField] private Image heatGaugeFill;
    // The fill Image inside HeatGaugeBar. fillAmount = 0.0 means cool; 1.0 means scorching.

    [SerializeField] private TMP_Text heatText;
    [SerializeField] private TMP_Text healthText;

    // ─────────────────────────────────────────────────────────────────────────
    // UPDATE — called by Unity once per frame (roughly 60×/sec at 60 fps)
    // This is the main polling loop. Each block reads one data source and
    // writes to its corresponding UI element. Null checks guard against
    // references that haven't been wired in the Inspector yet.
    // ─────────────────────────────────────────────────────────────────────────
    private void Update()

    {   // ── TIMER DISPLAY ────────────────────────────────────────────────────
        if (timerManager != null)
        {
            float time = timerManager.CurrentTime;
            // CurrentTime is a raw float in seconds (e.g. 73.4f).
            // We need to split it into whole minutes and whole seconds for "MM:SS" display.

            int minutes = Mathf.FloorToInt(time / 60f);
            // Mathf.FloorToInt rounds DOWN to the nearest integer.
            // Example: 73.4 / 60 = 1.22 → floor → 1 minute


            int seconds = Mathf.FloorToInt(time % 60f);
            // The % operator is modulo — it gives the REMAINDER after dividing by 60.
            // Example: 73.4 % 60 = 13.4 → floor → 13 seconds
            // Combined result: "01:13"

            timerText.text = $"{minutes:00}:{seconds:00}";
            // This is a C# interpolated string (the $ prefix).
            // {minutes:00} formats the integer with at least 2 digits, zero-padded.
            // So 1 minute → "01", 13 seconds → "13" → final string "01:13".
        }

        // ── SCORE DISPLAY ────────────────────────────────────────────────────
        if (scoreManager != null)
        {
            scoreText.text = $"Score: {scoreManager.Score}";
            // Straightforward: reads the integer Score property and formats it
            // as a plain label, e.g. "Score: 350".
        }

        // ── FOREST HEALTH BAR ────────────────────────────────────────────────
        if (forestManager != null && forestHealthFill != null)
        {
            forestHealthFill.fillAmount = forestManager.ForestHealthPercent / 100f;
            // ForestHealthPercent is on a 0–100 scale (percentage of alive trees).
            // Image.fillAmount expects a 0.0–1.0 range, so we divide by 100.
            // Example: 75% alive → fillAmount = 0.75 → bar is ¾ full.

            healthText.text = $"HP: {forestManager.ForestHealthPercent}";
        }

        // ── HEAT GAUGE BAR ───────────────────────────────────────────────────
        if (heatGaugeManager != null && heatGaugeFill != null)
        {
            heatGaugeFill.fillAmount = heatGaugeManager.NormalizedHeat;
            // NormalizedHeat is already 0.0–1.0 (currentHeat / maxHeat),
            // computed inside HeatGaugeManager, so no conversion needed here.
            // 0.0 = no heat pressure; 1.0 = scorching / lose condition.

            heatText.text = $"Heat: {Mathf.Round(heatGaugeManager.NormalizedHeat * 10000f) / 100f}";
        }
    }
}
