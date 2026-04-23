using System;          // Needed for the Action<> delegate type used by the event
using UnityEngine;     // Needed for MonoBehaviour, Vector2, Time, Random, etc.

public class WindManager : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    // A static property that holds the one-and-only instance of this script.
    // "static" means it belongs to the class itself, not any individual object.
    // "private set" means only this class can assign it — other scripts can only READ it.
    // Usage from other scripts: WindManager.Instance.CurrentWind
    public static WindManager Instance { get; private set; }

    // ─── Event ────────────────────────────────────────────────────────────────
    // An event is a C# pub/sub mechanism. Any script can "subscribe" to OnWindChanged
    // and it will automatically be called whenever wind changes.
    // Action<Vector2> means: "a function that takes a Vector2 and returns nothing."
    // CloudController will subscribe here and apply the force to the cloud.
    public event Action<Vector2> OnWindChanged;

    // ─── Configuration fields (set by Configure()) ────────────────────────────
    // These are populated from LevelData at runtime — NOT hardcoded in the Inspector.
    private bool enabledForLevel; // Is wind even active in the current level?
    private float windStrength;    // How fast the wind pushes the cloud (units/sec)
    private float windInterval;    // Time (seconds) to WAIT between gusts
    private float windDuration;    // Time (seconds) each gust LASTS

    // ─── Runtime state ────────────────────────────────────────────────────────
    // These tick down every frame inside Update().
    private float timer;           // Countdown: time remaining until NEXT gust starts
    private float activeTimer;     // Countdown: time remaining in the CURRENT gust

    // ─── Wind vector ──────────────────────────────────────────────────────────
    // The actual 2D force being applied this frame. Zero when idle.
    private Vector2 currentWind;

    // A public read-only shorthand property — "=>" is C# expression-body syntax.
    // Equivalent to: public Vector2 get CurrentWind() { return currentWind; }
    // Other scripts read this to know what the wind is doing RIGHT NOW.
    public Vector2 CurrentWind => currentWind;

    // ─── Unity lifecycle ──────────────────────────────────────────────────────
    // Awake() runs once when the GameObject is first created, before Start().
    // Here we just register this object as the global singleton instance.
    private void Awake()
    {
        Instance = this;
    }

    // ─── Public API ───────────────────────────────────────────────────────────
    // Called by LevelDataLoader at scene start to inject level-specific values.
    // This is the "configure before play" pattern — no level values live in WindManager itself.
    public void Configure(bool hasWind, float strength, float interval, float duration)
    {
        enabledForLevel = hasWind;    // If false, Update() exits immediately every frame
        windStrength = strength;   // e.g., Level 3 uses 2.5
        windInterval = interval;   // e.g., Level 3 waits 6 seconds between gusts
        windDuration = duration;   // e.g., Level 3 gusts last 2 seconds

        // Start the idle countdown at the full interval
        // (so the first gust doesn't fire immediately at level load)
        timer = windInterval;
        activeTimer = 0f;             // No gust is active at start

        // Push a zero-wind event so subscribers reset to no-wind state
        SetWind(Vector2.zero);
    }

    // ─── Frame loop ───────────────────────────────────────────────────────────
    // Update() is called by Unity every frame (typically 60+ times per second).
    // This is where the two-state machine runs.
    private void Update()
    {
        // Guard clause: if this level has no wind, do nothing at all
        if (!enabledForLevel)
            return;

        // ── State 1: A gust is currently active ───────────────────────────────
        if (activeTimer > 0f)
        {
            // Subtract how much real time passed since last frame
            // Time.deltaTime is always "seconds elapsed since the previous frame"
            activeTimer -= Time.deltaTime;

            // Check if the gust just expired
            if (activeTimer <= 0f)
            {
                // Gust is over — reset wind to zero and broadcast the change
                SetWind(Vector2.zero);

                // Reset the idle countdown so we wait a full interval before the next gust
                timer = windInterval;
            }

            // Important: return here so we don't also run the idle countdown below
            return;
        }

        // ── State 2: Idle — waiting for the next gust ─────────────────────────
        timer -= Time.deltaTime;  // Count down toward zero

        if (timer <= 0f)
        {
            // Time's up — fire a new gust

            // Random.insideUnitCircle returns a random 2D point INSIDE a circle of radius 1.
            // .normalized converts it to a DIRECTION (length exactly 1, angle random).
            // Multiplying by windStrength scales that direction into actual speed.
            // Example: dir = (0.6, -0.8), windStrength = 2.5 → wind = (1.5, -2.0)
            Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized;
            SetWind(dir * windStrength);

            // Start the active countdown — gust will last windDuration seconds
            activeTimer = windDuration;
        }
    }

    // ─── Internal helper ──────────────────────────────────────────────────────
    // All wind changes go through this one function so we never forget to fire the event.
    private void SetWind(Vector2 wind)
    {
        currentWind = wind;   // Store the new value

        // Fire the event. The "?." is the null-conditional operator:
        // if no scripts have subscribed yet, this safely does nothing instead of crashing.
        // Invoke() calls every subscribed function and passes currentWind to each of them.
        OnWindChanged?.Invoke(currentWind);
    }
}