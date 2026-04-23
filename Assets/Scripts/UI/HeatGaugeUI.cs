using UnityEngine;
using UnityEngine.UI;

// HeatGaugeUI — a pure display script.
// Runs every frame, reads one float from HeatGaugeManager,
// and maps it to one of four colour states on a UI Image.
// No game logic lives here — this class only translates data into visuals.
public class HeatGaugeUI : MonoBehaviour
{
    // ── References ────────────────────────────────────────────────────────────
    // [SerializeField] makes a private field visible in the Unity Inspector
    // so you can drag-and-drop the objects without making the fields public.
    // Think of it as a wiring panel: you declare what sockets exist here,
    // and you plug in the actual objects in the Inspector.

    [SerializeField] private HeatGaugeManager heatGaugeManager; // The "brain" that owns the real heat value
    [SerializeField] private Image fillImage;                   // The UI bar whose colour we will change

    // ── Colour definitions ────────────────────────────────────────────────────
    // Unity colours use RGBA floats in the range 0.0–1.0 (not 0–255).
    // new Color(R, G, B) — alpha defaults to 1.0 (fully opaque).
    //
    // These are the four visual zones the design spec requires:
    //   pale yellow → orange → red → pulsing red

    [SerializeField] private Color coolColor = new Color(1f, 0.95f, 0.6f);      // Zone 1: pale yellow  (0–30%)
    [SerializeField] private Color warmColor = new Color(1f, 0.7f, 0.3f);       // Zone 2: orange       (31–60%)
    [SerializeField] private Color hotColor = new Color(1f, 0.35f, 0.2f);       // Zone 3: red          (61–85%)
    [SerializeField] private Color scorchingColor = new Color(1f, 0.15f, 0.15f);    // Zone 4: deep red     (86–100%, pulsing)

    private void Update()
    {
        // Guard clause: if either reference is missing (not wired up in the
        // Inspector), bail out immediately so we don't get a NullReferenceException.
        // "return" here exits the method early — nothing below this line runs.
        if (heatGaugeManager == null || fillImage == null) return;

        // NormalizedHeat is a property on HeatGaugeManager that returns the
        // current heat as a value between 0.0 (no heat) and 1.0 (max heat / scorching).
        // Normalized means it has been scaled to the 0–1 range regardless of the
        // internal max-heat value, which makes comparisons straightforward.
        float heat = heatGaugeManager.NormalizedHeat;

        // ── Zone selection ────────────────────────────────────────────────────
        // We check zones from coolest to hottest.
        // The first condition that matches wins; the rest are skipped (else if).
        // This is a simple threshold ladder — no interpolation in zones 1–3,
        // the colour snaps instantly as heat crosses each boundary.

        if (heat <= 0.30f)
        {
            // Zone 1 — Cool (0 % – 30 %)
            // Pale yellow. Forest is safe; sun pressure is low.
            fillImage.color = coolColor;
        }
        else if (heat <= 0.60f)
        {
            // Zone 2 — Warm (31 % – 60 %)
            // Orange. Player should start thinking about shading trees.
            fillImage.color = warmColor;
        }
        else if (heat <= 0.85f)
        {
            // Zone 3 — Hot (61 % – 85 %)
            // Red. Danger zone; the player needs to act quickly.
            fillImage.color = hotColor;
        }
        else
        {
            // Zone 4 — Scorching (86 % – 100 %)
            // The gauge pulses between hotColor and scorchingColor to signal
            // that a game-over lose condition is imminent.

            // Mathf.Sin(x) produces a smooth wave that oscillates between -1 and +1.
            // Time.time is Unity's running clock in seconds since the game started.
            // Multiplying by 6 controls the pulse speed:
            //   higher number = faster flashing (6 ≈ 3 full pulses per second).
            // Mathf.Abs() flips the negative half of the sine wave to positive,
            // giving us a value that bounces between 0.0 and 1.0 — never negative.
            float pulse = Mathf.Abs(Mathf.Sin(Time.time * 6f));

            // Color.Lerp(a, b, t) linearly interpolates between colours a and b.
            // When t = 0.0 → returns a (hotColor)
            // When t = 0.5 → returns the midpoint colour
            // When t = 1.0 → returns b (scorchingColor)
            // Because pulse oscillates 0→1→0 smoothly, the bar visibly breathes
            // between the two red shades, creating the pulsing danger effect.
            fillImage.color = Color.Lerp(hotColor, scorchingColor, pulse);

        }
    }
}
