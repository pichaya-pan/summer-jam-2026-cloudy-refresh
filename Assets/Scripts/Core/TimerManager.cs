using UnityEngine;
using System;   // Required for the Action delegate type used by the event

// TimerManager is a MonoBehaviour, so it lives on a GameObject in the scene.
// Attach it to the Managers object alongside ForestManager and GameStateManager.
public class TimerManager : MonoBehaviour
{
    // [SerializeField] makes a private field visible in the Unity Inspector.
    // This means you can change startingTime in the Inspector without touching code.
    // For Level 1, set this to 90 in the Inspector (or leave it at the default below).
    [SerializeField] private float startingTime = 90f;

    // A C# property: other scripts can READ CurrentTime, but only this class can WRITE it.
    // The "private set" enforces that — it's a read-only view from the outside.
    // HUDController will read this every frame to update the timer display.
    public float CurrentTime { get; private set; }

    // Another read-only property. Defaults to true so the timer starts immediately.
    // External scripts (e.g. GameStateManager) can check this to know if the clock
    // is still running, but cannot pause it directly — only TimerManager controls this.
    public bool IsRunning { get; private set; } = true;

    // C# event using the Action delegate (from System).
    // An event is a signal — other scripts "subscribe" to it and get called when it fires.
    // GameStateManager subscribes to this in its Start() to know when to call SetGameOver().
    // The "?" in OnTimerEnd?.Invoke() means: only fire if at least one listener is subscribed.
    public event Action OnTimerEnd;

    // Private guard flag. Prevents the end logic from running more than once,
    // even if somehow Update() keeps running after IsRunning is set to false.
    // This is defensive programming — cheap and safe.
    private bool hasEnded = false;

    // Unity lifecycle method. Runs once when the scene starts.
    // We set CurrentTime here (not in the field declaration) because startingTime
    // could have been overridden via the Inspector before this runs.
    private void Start()
    {
        CurrentTime = startingTime;
    }

    // Unity lifecycle method. Runs once per frame (~60 times/sec at 60fps).
    // This is the core countdown loop.
    private void Update()
    {
        // Early exit: if the timer is paused or has already ended, do nothing this frame.
        // This is an idiomatic C# pattern — return early to avoid deeply nested if-blocks.
        if (!IsRunning || hasEnded) return;

        // Time.deltaTime is the number of seconds since the last frame (e.g. ~0.0167s at 60fps).
        // Subtracting it each frame gives a real-time countdown that's frame-rate independent.
        // If you just subtracted 1 every frame, a 30fps machine would count down half as fast.
        CurrentTime -= Time.deltaTime;

        // Check if we've hit zero
        if (CurrentTime <= 0f)
        {
            CurrentTime = 0f;       // Clamp to zero — prevents negative display values on the HUD
            hasEnded = true;        // Lock the guard flag so this block never runs again
            IsRunning = false;      // Mark the timer as stopped

            // Fire the event. The "?." is a null-safe invoke:
            // if nothing is subscribed to OnTimerEnd, this does nothing instead of crashing.
            // GameStateManager is listening and will call SetGameOver() in response.
            OnTimerEnd?.Invoke();
        }
    }

    // Public method: lets other systems grant bonus time to the player.
    // Example: a power-up or restoring a Critical tree could call AddTime(10f).
    // No upper clamp is applied here — if you want a maximum time cap, add it later.
    public void AddTime(float seconds)
    {
        CurrentTime += seconds;
    }

}
