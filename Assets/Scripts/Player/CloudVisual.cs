using UnityEngine;


// RequireComponent ensures Unity automatically adds a SpriteRenderer if one
// doesn't exist on this GameObject. It also prevents you from removing the
// SpriteRenderer while this script is attached.
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]        // Add this when using animated sprites.
public class CloudVisual : MonoBehaviour
{
    // [SerializeField] exposes private fields in the Unity Inspector.
    // Private keeps them encapsulated from other scripts, but Inspector-editable
    // so you can wire them up without hardcoding references.
    [SerializeField] private CloudController cloudController;   // Source of velocity data
    [SerializeField] private float movingThreshold = 0.2f;      // Min speed to trigger swap
    [SerializeField] private Sprite idleSprite;                 // Sprite when stationary (can be removed if use animated sprites)
    [SerializeField] private Sprite movingSprite;               // Sprite when moving (can be removed if use animated sprites)

    // Cached reference — avoids calling GetComponent every frame
    private SpriteRenderer sr;  
    private Animator animator;
    //The Animator.StringToHash caches the parameter lookup — slightly better than passing the string every frame.
    private static readonly int IsMoving = Animator.StringToHash("isMoving");

    private void Awake()
    {
        // Awake() runs once when the object is first initialized, before Start().
        // Good place for caching and self-wiring.

        sr = GetComponent<SpriteRenderer>();    // Cache the SpriteRenderer on this object (can be removed if use animated sprites)
        animator = GetComponent<Animator>();    // Cache the Animator on this object

        // Null-check: if cloudController wasn't assigned in the Inspector,
        // try to find it on the same GameObject as a fallback.
        if (cloudController == null)
            cloudController = GetComponent<CloudController>();
    }

    private void Update()
    {
        // Update() runs every frame (~60/s). Keep it lean.

        // Guard clause: if any required reference is missing, bail out early.
        // This prevents NullReferenceExceptions during setup or if art isn't
        // assigned yet — safe to leave in until sprites are ready.
        if (cloudController == null) return;    // For non-animated sprites, use this => if (idleSprite == null || movingSprite == null || cloudController == null)

        // Velocity is a Vector2 (x, y components). .magnitude computes its
        // scalar length: sqrt(x² + y²). This gives speed regardless of direction.
        // If speed exceeds threshold → moving sprite; otherwise → idle sprite.
        bool moving = cloudController.Velocity.magnitude > movingThreshold;
        animator.SetBool(IsMoving, moving);

        // For non-animated sprites, use this
        /*
            sr.sprite = cloudController.Velocity.magnitude > movingThreshold
                ? movingSprite   // Ternary: condition is true
                : idleSprite;    // Ternary: condition is false

        */
    }

}
