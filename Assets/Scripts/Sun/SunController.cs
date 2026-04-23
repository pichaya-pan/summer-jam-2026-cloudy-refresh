using UnityEngine;

// Upgraded SunController — Day 2.
// Responsibilities:
//   1. Move the sun object on a sinusoidal horizontal path (ping-pong + wave).
//   2. Each frame, scan every living tree and set IsExposedToSun based on distance.
//      TreeWiltTimer owns the actual damage; this script only sets the flag.
// Separation of concerns: this script LABELS trees; TreeWiltTimer DAMAGES them.
public class SunController : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector-exposed fields
    // -------------------------------------------------------------------------
    [Header("Movement")]
    [SerializeField] private float horizontalSpeed = 1.5f;  // Units/sec on X axis
    [SerializeField] private float leftX = -7f;             // Left reversal boundary
    [SerializeField] private float rightX = 7f;             // Right reversal boundary
    [SerializeField] private float waveAmplitude = 0.35f;   // Height of Y oscillation (units)
    [SerializeField] private float waveFrequency = 1.5f;    // Speed of Y oscillation (cycles/sec)

    [Header("Heat")]
    [SerializeField] private float heatRadius = 3.5f;       // Radius (units) within which trees are "exposed"

    // --- Internal state ---

    // Sign multiplier: +1 = right, -1 = left
    // Using int (not bool) means you can multiply it directly into a float
    private int direction = 1;

    // World position recorded at Start(); used as Y baseline
    private Vector3 startPosition;


    // -------------------------------------------------------------------------
    // Public read-only property
    // -------------------------------------------------------------------------

    // Expression-bodied property — exposes heatRadius to other scripts (e.g. a
    // debug visualizer or HeatGaugeManager) without allowing external writes.
    public float HeatRadius => heatRadius;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Start()
    {
        // Cache the spawn position. The Y wave is additive on top of this,
        // so the sun oscillates around where you placed it in the Inspector —
        // not around world origin.
        startPosition = transform.position;
    }

    private void Update()
    {
        // Split into two clearly named methods so each responsibility is testable
        // in isolation. Order matters: move first, then evaluate trees at the
        // new position so exposure is always current-frame-accurate.
        MoveSun();
        MarkTreesInSunRange();

    }

    // -------------------------------------------------------------------------
    // Movement
    // -------------------------------------------------------------------------
    private void MoveSun()
    {
        // Read the current world position into a local copy.
        // transform.position is a property; reading it once and writing back
        // once is cleaner and marginally cheaper than multiple property hits.
        Vector3 pos = transform.position;

        // Advance position along X.
        // Time.deltaTime = seconds elapsed since the last frame.
        // Multiplying by it makes movement frame-rate independent:
        // whether the game runs at 30 FPS or 120 FPS, the sun travels
        // the same distance per real-world second.
        pos.x += direction * horizontalSpeed * Time.deltaTime;

        // --- Vertical: sinusoidal oscillation around the starting Y ---
        // Mathf.Sin returns a value in [-1, 1].
        // Multiplying by waveAmplitude scales the range to [-amp, +amp].
        // Time.time is the total elapsed seconds since the scene loaded, so
        // the wave is always continuous and never resets mid-play.
        pos.y = startPosition.y + Mathf.Sin(Time.time * waveFrequency) * waveAmplitude;

        // Boundary check — clamp and reverse direction.
        // Note: position is clamped to the boundary value on the same frame
        // the overshoot is detected, so the sun never drifts past the edge.
        if (pos.x >= rightX)
        {
            pos.x = rightX; // Snap to boundary (prevent overshoot accumulation)
            direction = -1; // Reverse: now travelling left

        }
        else if (pos.x <= leftX)
        {
            pos.x = leftX;  // Snap to boundary
            direction = 1;  // Reverse: now travelling right
        }

        // Write the mutated position back to the transform.
        // Nothing moves until this line executes.
        transform.position = pos;

    }

    // -------------------------------------------------------------------------
    // Tree exposure tagging
    // -------------------------------------------------------------------------
    private void MarkTreesInSunRange()
    {
        // FindObjectsByType is the modern Unity 2022+ replacement for the
        // deprecated FindObjectsOfType. FindObjectsSortMode.None skips sorting,
        // which is faster — we don't care about order here.
        // NOTE: This is called every frame and allocates a new array each call.
        // Acceptable for a game-jam scope; for a larger project you would cache
        // the tree list and update it only when trees are added/removed.
        Trees[] trees = FindObjectsByType<Trees>(FindObjectsSortMode.None);

        for (int i = 0; i < trees.Length; i++)
        {
            Trees tree = trees[i];

            // Null guard (tree destroyed mid-frame) + skip dead trees.
            // Dead trees no longer participate in heat calculations.
            if (tree == null || tree.IsDead)
                continue;

            // Vector2.Distance intentionally discards the Z axis.
            // The game is top-down 2D; Z is render order, not gameplay space.
            // Using Vector2 (not Vector3) distance avoids any Z-depth error.
            float distance = Vector2.Distance(transform.position, tree.transform.position);

            // Set a boolean flag on the tree — no damage math here.
            // true  = tree is within the sun's heat radius this frame
            // false = tree is out of range and takes no sun damage
            tree.IsExposedToSun = distance <= heatRadius;
                 
        }
    }

}
