using UnityEngine;
using System;       // Required for Action (C# delegate type used for events)

public class HeatGaugeManager : MonoBehaviour
{
    // --- Tunable parameters (editable in the Unity Inspector) ---

    // The current heat value; starts at 0 each run.
    // [SerializeField] keeps this private to C# but visible in the Inspector.
    [SerializeField] private float currentHeat = 0f;

    // The ceiling value — heat is clamped to this max. 
    // Reaching it triggers the scorching event.
    [SerializeField] private float maxHeat = 100f;

    // How much heat ONE exposed (unshaded) tree adds per second.
    // e.g. 3 exposed trees × 3f × deltaTime heat per frame.
    [SerializeField] private float heatPerExposedTreePerSecond = 3f;

    // How much heat ONE shaded tree removes per second.
    // Cooling is weaker than heating by design — shade helps but can't fully cancel the sun.
    [SerializeField] private float coolPerShadedTreePerSecond = 2f;

    // --- Public read-only properties (other scripts can read, not write) ---

    // Raw heat value, 0..maxHeat
    public float CurrentHeat => currentHeat;

    // Heat as a 0.0–1.0 ratio, used to drive UI fill bars.
    // Guard against division-by-zero if maxHeat is accidentally set to 0.
    public float NormalizedHeat => maxHeat <= 0f ? 0f : currentHeat / maxHeat;

    // --- Events (Observer pattern) ---

    // Fires every frame with the normalized heat value (0..1).
    // The HUD subscribes to this to update the fill bar without polling.
    public event Action<float> OnHeatChanged;

    // Fires once when heat hits max — used to trigger the scorching lose condition.
    public event Action OnScorching;

    // --- Internal state ---

    // A latch flag: prevents OnScorching from firing every frame while at max heat.
    // Once set, it stays true until heat drops below max (the "reset" block below).
    private bool scorchingTriggered = false;

    // --- Core loop ---

    // Update() is called by Unity once per frame automatically.
    private void Update()
    {
        // Gather every living Tree instance currently in the scene.
        // FindObjectsByType is safe but costs a scan each frame — acceptable at jam scale.
        Trees[] trees = FindObjectsByType<Trees>(FindObjectsSortMode.None);

        int exposedCount = 0; // Trees in sun AND not shaded (worst case for heat)
        int shadedCount = 0; // Trees under the cloud shadow (cooling contribution)

        // Classify each living tree into exposed or shaded buckets.
        for (int i = 0; i < trees.Length; i++)
        {
            Trees tree = trees[i];

            // Skip destroyed references or trees that have already died.
            if (tree == null || tree.IsDead) continue;

            // A tree is "exposed" only if the sun reaches it AND the cloud isn't shading it.
            // A shaded-but-exposed tree counts for cooling, not heating.
            if (tree.IsExposedToSun && !tree.IsShaded)
                exposedCount++;

            // Any shaded tree (regardless of sun exposure) contributes to cooling.
            if (tree.IsShaded)
                shadedCount++;
        }

        // --- Heat equation (runs every frame, scaled by real elapsed time) ---

        // Heat rises proportionally to how many unprotected trees the sun is hitting.
        currentHeat += exposedCount * heatPerExposedTreePerSecond * Time.deltaTime;

        // Heat falls proportionally to how many trees the cloud is shading.
        // Time.deltaTime ensures this is frame-rate-independent (same result at 30fps or 120fps).
        currentHeat -= shadedCount * coolPerShadedTreePerSecond * Time.deltaTime;

        // Clamp: heat can never go below 0 or above maxHeat.
        currentHeat = Mathf.Clamp(currentHeat, 0f, maxHeat);

        // Notify subscribers (e.g. HeatGaugeUI) with the new normalized value.
        // The ?. means "only invoke if someone is actually subscribed" — avoids a null crash.
        OnHeatChanged?.Invoke(NormalizedHeat);

        // --- Scorching event (one-shot latch logic) ---

        // If heat just reached the ceiling and we haven't already fired the event this cycle...
        if (currentHeat >= maxHeat && !scorchingTriggered)
        {
            scorchingTriggered = true; // Lock the latch so this only fires once per peak.
            OnScorching?.Invoke();     // Tell anyone listening (e.g. GameStateManager) to react.
        }

        // Reset the latch as soon as heat drops below max, 
        // so scorching can trigger again if the player lets it climb back up.
        if (currentHeat < maxHeat)
        {
            scorchingTriggered = false;
        }
    }
}
