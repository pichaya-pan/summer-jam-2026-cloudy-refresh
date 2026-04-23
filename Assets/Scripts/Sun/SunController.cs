using UnityEngine;

// Day 3 SunController — full path-style system.
// Responsibilities:
//   1. Move the sun using one of four path styles, loaded from LevelData.
//   2. Apply heat damage directly to unshaded trees within heatRadius each frame.
//
// Key Day 3 change vs Day 2:
//   - Day 2: SunController SET a flag (IsExposedToSun); TreeWiltTimer READ it and dealt damage.
//   - Day 3: SunController calls tree.ReceiveSunHeat() DIRECTLY in BroadcastHeat(),
//     and the shade check moves here too. TreeWiltTimer handles passive/background wilt only.
//   This is a deliberate consolidation — heat from the sun is now owned end-to-end
//   by SunController, which makes the heatMultiplier per-level tuning easier to reason about.
public class SunController : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector-exposed fields
    // -------------------------------------------------------------------------

    [SerializeField] private float speed = 1f;                    // Horizontal travel speed (units/sec)
    [SerializeField] private float heatRadius = 2f;                    // Radius (units) to check for trees
    [SerializeField] private float heatMultiplier = 1f;                    // Scales damage passed to ReceiveSunHeat()
    [SerializeField] private SunPathStyle pathStyle = SunPathStyle.Simple; // Which Y-movement mode to use

    // --- Boundary and path shape parameters ---
    [SerializeField] private float leftX = -7f;  // Left reversal boundary (world X)
    [SerializeField] private float rightX = 7f;  // Right reversal boundary (world X)
    [SerializeField] private float topY = 4f;  // Baseline Y position for all path styles
    [SerializeField] private float arcHeight = 0.6f; // Arcing mode: how far Y dips at the edges (units)
    [SerializeField] private float sineAmplitude = 0.8f; // Sinusoidal mode: peak Y deviation from topY
    [SerializeField] private float sineFrequency = 1f;   // Sinusoidal mode: oscillation speed (cycles/sec)

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    // t is a continuously accumulating time value scaled by speed.
    // Used as input to Sin and PerlinNoise so path frequency scales with sun speed.
    // Declared as a field (not using Time.time directly) so it can be paused or
    // reset independently of Unity's global clock if needed later.
    private float t;

    private int direction = 1;     // Horizontal travel sign: +1 = right, -1 = left
    private bool hasSolarFlare;          // Set by Configure(); reserved for Day 4 flare logic
    private float solarFlareInterval;     // Seconds between flares; consumed by Day 4 SolarFlareManager

    // -------------------------------------------------------------------------
    // Public configuration method — called by LevelDataLoader at scene start
    // -------------------------------------------------------------------------

    // Configure() is the Day 3 equivalent of setting Inspector values at runtime.
    // LevelDataLoader calls this once in Start(), pushing the current LevelData
    // values in. This is what allows different levels to have different sun behavior
    // without touching the scene manually.
    public void Configure(
        float newSpeed,
        float newHeatRadius,
        float newHeatMultiplier,
        SunPathStyle newPathStyle,
        bool flareEnabled,
        float flareInterval)
    {
        speed = newSpeed;
        heatRadius = newHeatRadius;
        heatMultiplier = newHeatMultiplier;
        pathStyle = newPathStyle;
        hasSolarFlare = flareEnabled;
        solarFlareInterval = flareInterval;
    }

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Update()
    {
        // Advance the internal time counter, scaled by speed.
        // This means faster suns also oscillate faster on their Y path —
        // the two axes stay visually coupled and consistent across levels.
        t += Time.deltaTime * speed;

        Vector3 pos = transform.position;

        // --- Horizontal: same ping-pong logic as Day 1 & 2 ---
        pos.x += direction * speed * Time.deltaTime;

        if (pos.x >= rightX)
        {
            pos.x = rightX;
            direction = -1;
        }
        else if (pos.x <= leftX)
        {
            pos.x = leftX;
            direction = 1;
        }

        // --- Vertical: delegated to GetYPosition(), which switches on pathStyle ---
        // pos.x is passed in (not transform.position.x) because it has already been
        // clamped and updated this frame — we want the current-frame X for arc math.
        pos.y = GetYPosition(pos.x, t);

        transform.position = pos;

        // Apply heat to all eligible trees at the sun's new position.
        BroadcastHeat();
    }

    // -------------------------------------------------------------------------
    // Path calculation
    // -------------------------------------------------------------------------

    // Pure function: given the sun's current X and the time counter, return the
    // correct Y coordinate for the active path style. No side effects.
    private float GetYPosition(float x, float timeValue)
    {
        switch (pathStyle)
        {
            case SunPathStyle.Simple:
                // Flat horizontal line — sun stays at topY regardless of X or time.
                // Used for Level 1 (tutorial feel, predictable behavior).
                return topY;

            case SunPathStyle.Arcing:
                // Inverted parabola: sun dips DOWN at the edges, peaks at center.
                // This mimics a realistic solar arc across the sky.
                //
                // How it works:
                //   center    = midpoint between left and right boundary
                //   halfWidth = distance from center to either edge
                //   normalized = maps current X into the range [-1, 0, +1]
                //                (-1 = at leftX, 0 = at center, +1 = at rightX)
                //   normalized² is always in [0, 1] — it's 0 at center, 1 at edges.
                //   Subtracting it (scaled by arcHeight) from topY creates the dip.
                float center = (leftX + rightX) * 0.5f;
                float halfWidth = (rightX - leftX) * 0.5f;
                float normalized = (x - center) / halfWidth;    // [-1, +1]
                return topY - (normalized * normalized) * arcHeight;

            case SunPathStyle.Sinusoidal:
                // Smooth wave up and down around topY — same principle as Day 2's
                // waveAmplitude logic, but now driven by the speed-scaled t counter
                // instead of raw Time.time, so it respects level speed settings.
                return topY + Mathf.Sin(timeValue * sineFrequency) * sineAmplitude;

            case SunPathStyle.Erratic:
                // Perlin noise produces smooth but unpredictable values in [0, 1].
                // Multiplying by 1.2 expands the range to [0, 1.2].
                // Subtracting 0.6 re-centers it to [-0.6, +0.6] around topY.
                // The 0.8 time scale slows the noise so it doesn't feel jittery.
                // Used in later levels (5–6) to make the sun harder to anticipate.
                return topY + Mathf.PerlinNoise(timeValue * 0.8f, 0f) * 1.2f - 0.6f;

            default:
                return topY;
        }
    }

    // -------------------------------------------------------------------------
    // Heat application
    // -------------------------------------------------------------------------

    // Called every frame after movement. Scans all living trees, checks distance
    // AND shade status, then calls ReceiveSunHeat() on eligible trees.
    private void BroadcastHeat()
    {
        Trees[] trees = FindObjectsByType<Trees>(FindObjectsSortMode.None);

        foreach (Trees tree in trees)
        {
            if (tree == null || tree.IsDead)
                continue;

            float distance = Vector2.Distance(transform.position, tree.transform.position);

            // Two conditions must both be true to deal heat damage:
            //   1. Tree is within the sun's heat radius this frame.
            //   2. Tree is NOT shaded (ShadowZone sets tree.IsShaded each frame).
            // Shaded trees are fully immune to BroadcastHeat — they can still wilt
            // slowly via TreeWiltTimer's shadedWiltRate, but that's a separate path.
            if (distance <= heatRadius && !tree.IsShaded)
            {
                tree.IsExposedToSun = true;  // ← ADD THIS: tells HeatGaugeManager this tree counts
                tree.ReceiveSunHeat(heatMultiplier * Time.deltaTime);
            }
            else
            {
                tree.IsExposedToSun = false; // ← ADD THIS: clear flag for trees now out of range
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, heatRadius);
    }
}