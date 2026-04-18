using UnityEngine;

// Attribute: automatically adds Rigidbody2D to the GameObject if missing,
// and prevents it from being removed. Declares a hard dependency.
[RequireComponent(typeof(Rigidbody2D))]
public class CloudController : MonoBehaviour
{

    // [Header] adds a bold section label in the Unity Inspector — editor-only,
    // no runtime effect.
    [Header("Movement")]

    // [SerializeField] exposes this private field in the Inspector so you can
    // tune it without making it public (and accessible to other scripts).
    [SerializeField] private float moveForce = 18f; // Newtons applied per frame
    [SerializeField] private float maxSpeed = 5f;   // Hard velocity ceiling (units/s)

    [Header("Bounds")]
    [SerializeField] private Vector2 minBounds = new Vector2(-8.2f, -4.5f); // Bottom-left play area limit
    [SerializeField] private Vector2 maxBounds = new Vector2(8.2f, 4.5f);   // Top-right play area limit

    private Rigidbody2D rb; // Cached reference to the physics component
    private Vector2 input;  // Stores the normalized direction vector this frame

    // Read-only property: lets other scripts (e.g. CloudVisual) read velocity
    // without being able to write to it.
    public Vector2 Velocity => rb.linearVelocity;

    // Awake() runs once when the object is first created — before any Start().
    // Best place to cache component references.
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update() runs every rendered frame.
    // Input polling lives here so no keypress is ever missed between frames.
    private void Update()
    {
        input = new Vector2(
                Input.GetAxisRaw("Horizontal"), // -1 (left), 0, or 1 (right)
                Input.GetAxisRaw("Vertical")    // -1 (down), 0, or 1 (up)
                ).normalized;
        // .normalized converts the vector to length 1.
        // Without this, diagonal movement (1,1) would be ~1.41× faster than
        // cardinal movement (1,0) — the normalization fixes that.
    }

    // FixedUpdate() runs on a fixed physics timestep (default: every 0.02s / 50Hz).
    // All Rigidbody2D writes go here to stay in sync with the physics engine.
    private void FixedUpdate()
    {
        // Apply a force in the input direction. ForceMode2D.Force applies it
        // as a continuous force (scaled by mass), giving smooth acceleration.
        rb.AddForce(input * moveForce, ForceMode2D.Force);

        // Speed cap: if physics has integrated velocity above maxSpeed,
        // clamp it back. This prevents indefinite acceleration.
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }

        // Boundary clamp: read current physics position, clamp x and y
        // independently within the play area, then write it back.
        // Mathf.Clamp(value, min, max) returns value constrained to [min, max].
        Vector2 clamped = rb.position;
        clamped.x = Mathf.Clamp(clamped.x, minBounds.x, maxBounds.x);
        clamped.y = Mathf.Clamp(clamped.y, minBounds.y, maxBounds.y);

        rb.position = clamped;  // Direct position write — bypasses physics for the clamp only
    }
}
