using UnityEngine;
using System;   // Required for Action<> delegate types

/* The reference says Tree should hold:
    * health 0–100
    * WiltState enum
    * isShaded
    * ReceiveRain(amount)
    * ReceiveSunHeat(rate)

*/

/// <summary>
/// Core data model and state machine for a single tree in the forest.
/// Tracks health (0–100) and maps it to a WiltState enum.
/// Broadcasts events on state transitions so other systems (ForestManager,
/// ScoreManager, TreeHealthDisplay) can react without being coupled here.
/// Does NOT handle rendering or timing — those live in TreeHealthDisplay
/// and TreeWiltTimer respectively.
/// </summary>

public class Trees : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Enum: WiltState
    // Ordered from best (0) to worst (4). The integer ordering matters because
    // RefreshWiltState() uses > / < comparisons between states.
    // -------------------------------------------------------------------------
    public enum WiltState
    {
        Lush,       // 0 — health 80–100: fully healthy
        Dry,        // 1 — health 50–79: starting to suffer
        Wilting,    // 2 — health 20–49: visibly stressed
        Critical,   // 3 — health 1–19:  near death
        Dead        // 4 — health = 0:   permanently gone
    }

    // -------------------------------------------------------------------------
    // Serialized backing fields
    // [SerializeField] makes private fields visible in the Unity Inspector,
    // allowing designers to tweak starting values without modifying code.
    // -------------------------------------------------------------------------
    [Header("State")]
    [SerializeField] private float health = 100f;   // Current health, 0–100
    [SerializeField] private bool isDead = false;   // Latched true once Dead; never resets

    // -------------------------------------------------------------------------
    // Public read-only surface
    // External scripts (ShadowZone, SunController) can SET IsShaded and
    // IsExposedToSun each frame. All other state is read-only from outside.
    // -------------------------------------------------------------------------
    public bool IsShaded { get; set; }          // Set by ShadowZone each frame
    public bool IsExposedToSun { get; set; }    // Set by SunController each frame

    public float Health => health;              // Read-only view of health field
    public bool IsDead => isDead;               // Read-only view of isDead field

    /// <summary>Current wilt category. Writable only from within this class.</summary>
    public WiltState CurrentWiltState { get; private set; } = WiltState.Lush;

    // -------------------------------------------------------------------------
    // Events (Observer pattern)
    // Other scripts subscribe to these rather than polling Tree every frame.
    // The ?. (null-conditional) operator in Invoke calls guards against
    // NullReferenceException when no listeners are attached.
    // -------------------------------------------------------------------------

    /// <summary>Fires once when health hits 0 and the tree enters the Dead state.</summary>
    public event Action<Trees> OnTreeDied;

    /// <summary>Fires whenever WiltState changes in either direction.</summary>
    public event Action<Trees, WiltState> OnWiltStateChanged;

    /// <summary> Fires when WiltState transitions to a numerically higher (worse) state.</summary>
    public event Action<Trees, WiltState> OnTreeRestoredToState;

    // Tracks the previous WiltState so transitions can be detected
    private WiltState previousWiltState;

    // -------------------------------------------------------------------------
    // Unity Lifecycle: Awake
    // Called once when the GameObject activates, before any Start().
    // Snapshots the initial state and validates consistency in case the prefab
    // was saved with a non-default health value in the Inspector.
    // -------------------------------------------------------------------------
    private void Awake()
    {
        previousWiltState = CurrentWiltState;
        RefreshWiltState(); // Ensure state matches whatever health starts at
    }

 
    // -------------------------------------------------------------------------
    // Public API: ReceiveRain
    // Called by RainZone each frame while a tree is inside the rain collider.
    // Adds health, clamped to [0, 100], then re-evaluates the WiltState.
    // -------------------------------------------------------------------------
    public void ReceiveRain(float amount)
    {
        if (isDead) return; // Dead trees are frozen — early exit guard

        // Mathf.Clamp prevents health from going above 100 due to rain overflow
        health = Mathf.Clamp(health + amount, 0f, 100f);
        RefreshWiltState();
    }

    // -------------------------------------------------------------------------
    // Public API: ReceiveSunHeat
    // Called by TreeWiltTimer each frame when the tree is in the sun's heat radius.
    // Subtracts health, clamped to [0, 100], then re-evaluates the WiltState.
    // -------------------------------------------------------------------------
    public void ReceiveSunHeat(float amount)
    {
        if (isDead) return; // Dead trees don't take further damage

        // Mathf.Clamp prevents health from going below 0
        health = Mathf.Clamp(health - amount, 0f, 100f);
        RefreshWiltState();
    }

    // -------------------------------------------------------------------------
    // Private: RefreshWiltState
    // Determines the correct WiltState bucket for the current health value,
    // then fires transition events ONLY if the state has actually changed.
    // This guard is critical — without it events would fire ~60 times/second.
    //
    // Health-to-state thresholds:
    //   > 79  → Lush
    //   50–79 → Dry
    //   20–49 → Wilting
    //   1–19  → Critical
    //   = 0   → Dead
    // -------------------------------------------------------------------------

    // Restores the tree to its initial healthy state.
    // Called by ForestManager.BuildForest() each time a level loads or reloads.
    // Ensures no stale health, state, or flags carry over from a previous run.
    public void ResetTree()
    {
        // Restore health to full.
        health = 100f;

        // Clear the dead flag — this tree is alive again.
        isDead = false;

        // Reset both the current and previous wilt state to Lush.
        // previousWiltState is used by RefreshWiltState() to detect direction of change,
        // so it must also be reset to avoid a false "restored" event firing on the first frame.
        CurrentWiltState = WiltState.Lush;
        previousWiltState = WiltState.Lush;

        // Clear the per-frame flags set by ShadowZone and SunController each frame.
        // Without this, a tree spawned mid-level could inherit the previous tree's shade/exposure state
        // for one frame before those systems have a chance to re-evaluate it.
        IsShaded = false;
        IsExposedToSun = false;
    }

    public void ReceiveDroughtPulseDamage()
    {
        if (isDead) return;              // Guard: don't damage a dead tree
        health = Mathf.Clamp(health - 20f, 0f, 100f); // Deal 20 HP, keep in bounds
        RefreshWiltState();             // Update WiltState enum + fire events
    }

    private void RefreshWiltState()
    {
        // Determine the new state from health. Checked worst-first so the
        // cascade of else-if short-circuits as soon as a match is found.
        WiltState newState;

        if (health <= 0f) newState = WiltState.Dead;
        else if (health <= 19f) newState = WiltState.Critical;
        else if (health <= 49f) newState = WiltState.Wilting;
        else if (health <= 79f) newState = WiltState.Dry;
        else newState = WiltState.Lush;

        // Only act if the state has actually changed — avoids event spam
        if (newState != CurrentWiltState)
        {
            previousWiltState = CurrentWiltState;   // Archive the old state
            CurrentWiltState = newState;            // Commit the new state

            // Notify all listeners (e.g. TreeHealthDisplay, ScoreManager)
            // The ?. operator safely skips the call if no one has subscribed
            OnWiltStateChanged?.Invoke(this, CurrentWiltState);

            // Handle the Dead terminal state — latches isDead so it can never
            // be unset, then fires the one-shot OnTreeDied event
            if (CurrentWiltState == WiltState.Dead && !isDead)
            {
                isDead = true;
                OnTreeDied?.Invoke(this);
            }

            // Fire restoration event when moving to a numerically higher state.
            if (!isDead && CurrentWiltState < previousWiltState)
            {
                OnTreeRestoredToState?.Invoke(this, CurrentWiltState);
            }
        }

    }

    

}
