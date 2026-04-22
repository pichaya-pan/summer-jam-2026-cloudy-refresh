using UnityEngine;

// Attach this to SunEntity.
// Moves the object left and right between two X boundaries — stub only.
// No heat logic yet; that comes in Day 2 (SunController full logic).
public class SunController : MonoBehaviour
{
    // --- Inspector-exposed fields ---
    // SerializeField makes a private field visible in the Unity Inspector,
    // so you can tweak values without touching the code.

    [SerializeField] private float speed = 2f;  // Units per second
    [SerializeField] private float leftX = -7f; // Left reversal boundary (world X)
    [SerializeField] private float rightX = 7f; // Right reversal boundary (world X)

    // --- Internal state ---

    // Encodes travel direction as a sign multiplier: +1 = right, -1 = left.
    // Using int (not bool) means you can multiply it directly into a float
    // without a branch: pos.x += direction * speed * Time.deltaTime
    private int direction = 1;

    // --- Unity lifecycle ---

    // Update() is called once per frame by the Unity engine.
    // All position mutation happens here because this object has no physics —
    // it is a pure transform-driven entity (no Rigidbody2D).
    private void Update()
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
        pos.x += direction * speed * Time.deltaTime;

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


}
