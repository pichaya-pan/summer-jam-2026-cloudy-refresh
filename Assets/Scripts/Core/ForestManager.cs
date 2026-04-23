using UnityEngine;
using System;   // Required for the Action delegate type used in events

// ForestManager: Tracks the health of the entire forest each frame.
// Exposes forest health as a percentage and fires an event when the lose
// condition is met (too many dead trees).
// Other systems (GameStateManager, HUDController) read from this script.
public class ForestManager : MonoBehaviour
{
    // Internal snapshot of all Tree objects found in the scene.
    // Refreshed every frame so newly spawned or destroyed trees are captured.
    private Trees[] trees;

    // Captured once in Start(). Used as the denominator for health % calculation.
    // NOTE: If you later add/remove trees at runtime, you may need to recalculate this too.
    private int totalTrees;

    // Public read-only property. Any script can read this, but only ForestManager writes it.
    // Range: 0.0 (all dead) to 100.0 (all alive). Updated every frame in Update().
    public float ForestHealthPercent { get; private set; }

    // Event that fires when the dead-tree threshold is reached.
    // GameStateManager subscribes to this to trigger the Game Over state.
    // The '?' in Invoke() is a null guard — safe to call even if no one has subscribed.
    public event Action OnForestDead;

    // The number of dead trees that triggers the lose condition.
    // [SerializeField] keeps it private in code but visible in the Unity Inspector,
    // so you can tune it per level without touching the script.
    [SerializeField] private int deadTreeLoseThreshold = 5;

    // Start() runs once when the scene begins, before the first Update().
    private void Start()
    {
        // Take an initial census of all Tree components in the scene.
        // This sets totalTrees, which is the fixed denominator for health %.
        trees = FindObjectsByType<Trees>(FindObjectsSortMode.None);
        totalTrees = trees.Length;
    }

    // Update() runs every frame (~60 times per second at 60fps).
    private void Update()
    {
        // Re-scan every frame. This is a safe approach for a game jam —
        // it handles trees being destroyed at runtime without extra bookkeeping.
        // Performance note: FindObjectsByType is slow on large scenes.
        // For polish/Day 5, consider switching to an event-driven registry instead.
        trees = FindObjectsByType<Trees>(FindObjectsSortMode.None);

        int aliveCount = 0;
        int deadCount = 0;

        // Loop through every tree and sort it into alive or dead.
        // trees[i].IsDead is a property on the Tree script you built earlier.
        for (int i = 0; i < trees.Length; i++)
        {
            if (trees[i].IsDead) deadCount++;
            else aliveCount++;
        }

        // Compute forest health as a percentage of alive trees over the total
        // that existed at scene start.
        //
        // Guard against division by zero with the ternary: if totalTrees is 0,
        // health is 0 (avoids a DivideByZeroException crash).
        //
        // The (float) cast is critical: without it, C# does integer division
        // and aliveCount/totalTrees would always round down to 0 or 1.
        ForestHealthPercent = totalTrees == 0 ? 0f : (aliveCount / (float)totalTrees) * 100f;

        // Check lose condition: if dead trees have hit or exceeded the threshold,
        // fire the OnForestDead event. Any subscribed scripts (e.g. GameStateManager)
        // will receive this and handle the game-over flow.
        //
        // NOTE: This will fire every frame once the threshold is crossed, not just once.
        // GameStateManager guards against this by checking its own current state before acting.
        if (deadCount >= deadTreeLoseThreshold)
        {
            OnForestDead?.Invoke();
        }
    }
}
