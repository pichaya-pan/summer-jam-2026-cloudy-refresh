// ============================================================
// DroughtPulseManager.cs
// Manages periodic "drought pulse" events that damage trees.
// Key mechanic: trees currently in cloud shadow take ZERO damage.
// This is a MonoBehaviour — attach it to a GameObject in the scene.
// ============================================================

using System.Collections;        // Required for IEnumerator (coroutines)
using System.Collections.Generic; // Required for List<T>
using UnityEngine;               // Required for MonoBehaviour, Coroutine, etc.

public class DroughtPulseManager : MonoBehaviour
{
    // [SerializeField] makes a private field visible (and assignable)
    // in the Unity Inspector, without making it public in code.
    // Drag the ForestManager GameObject here in the Inspector.
    [SerializeField] private ForestManager forestManager;

    // --- Configuration fields (set by Configure(), not hardcoded) ---
    private bool enabledForLevel;      // Is drought pulse active this level?
    private float pulseInterval;       // Total seconds between pulses (e.g. 20s)
    private int targetCount;           // How many trees to try to hit per pulse
    private float warningDuration;     // How many seconds the warning lasts before the pulse fires


    // ============================================================
    // Configure() — called by LevelManager when a level loads.
    // Acts like a constructor: sets all parameters and starts or
    // stops the pulse loop depending on the level config.
    // ============================================================
    public void Configure(bool hasPulse, float interval, int count, float warning)
    {
        enabledForLevel = hasPulse;
        pulseInterval = interval;
        targetCount = count;
        warningDuration = warning;

        // Stop any existing PulseLoop coroutine before starting a new one.
        // This prevents duplicate loops if Configure() is called more than once.
        StopAllCoroutines();

        if (enabledForLevel)
            StartCoroutine(PulseLoop()); // Begin the repeating pulse cycle
    }


    // ============================================================
    // PulseLoop() — a coroutine that runs as an infinite loop.
    //
    // In C#/Unity, a coroutine is a method that can "pause" execution
    // without freezing the game. The keyword 'yield return' means:
    // "pause here, wait for the condition, then resume."
    //
    // Timeline per cycle:
    //   |--- (pulseInterval - warningDuration) seconds wait ---|
    //   |--- warningDuration seconds (warning shown) ---|
    //   |--- FirePulse() executes ---|
    //   (repeat)
    // ============================================================
    private IEnumerator PulseLoop()
    {
        while (true) // Loops forever until StopAllCoroutines() is called
        {
            // Wait for most of the interval BEFORE showing the warning.
            // E.g. if interval=20s and warning=3s, wait 17s first.
            yield return new WaitForSeconds(pulseInterval - warningDuration);

            // TODO: trigger warning UI here (red vignette, on-screen text, SFX)
            Debug.Log("Drought Pulse Warning!");

            // Now wait for the warning duration — player has this window
            // to reposition the cloud and shade as many trees as possible.
            yield return new WaitForSeconds(warningDuration);

            // Warning window is over — fire the actual pulse
            FirePulse();
        }
    }


    // ============================================================
    // FirePulse() — the main damage event.
    //
    // Algorithm:
    //   1. Take a snapshot of all active trees from ForestManager.
    //   2. Shuffle them randomly (so the same trees aren't always hit).
    //   3. Try to damage up to `targetCount` trees.
    //   4. Skip any tree that is dead OR currently shaded.
    // ============================================================
    private void FirePulse()
    {
        // Create a local copy of the active tree list so we can shuffle it
        // without modifying the original list in ForestManager.
        List<Trees> trees = new List<Trees>(forestManager.ActiveTrees);

        Shuffle(trees); // Randomise order before selecting targets

        // Cap the number of targets at the actual tree count.
        // Mathf.Min prevents trying to access indices that don't exist.
        int hitCount = Mathf.Min(targetCount, trees.Count);

        for (int i = 0; i < hitCount; i++)
        {
            Trees tree = trees[i];

            // Safety check: skip if the reference is null (destroyed object)
            // or if the tree has already reached the Dead state.
            if (tree == null || tree.IsDead)
                continue; // 'continue' skips to the next loop iteration

            // Core mechanic: if this tree is inside the cloud's shadow zone,
            // the pulse does ZERO damage — shade = full immunity.
            if (tree.IsShaded)
            {
                Debug.Log($"Pulse blocked by shade: {tree.name}");
                continue;
            }

            // Tree is alive and exposed — apply pulse damage.
            tree.ReceiveDroughtPulseDamage();
        }
    }


    // ============================================================
    // Shuffle<T>() — a generic Fisher-Yates shuffle.
    //
    // Generic means it works on a List of any type T (here: List<Tree>).
    //
    // Algorithm:
    //   For each position i, pick a random index between i and the end,
    //   then swap the two elements. This produces an unbiased random order.
    //
    // The tuple swap syntax: (a, b) = (b, a)
    //   This is C# 7+ syntactic sugar. It swaps both values in one line
    //   without needing a temporary variable.
    // ============================================================
    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            // Random.Range(min, max) — max is EXCLUSIVE for int overloads,
            // so this picks from [i .. list.Count - 1], inclusive.
            int randomIndex = Random.Range(i, list.Count);

            // Tuple swap — equivalent to:
            //   T temp = list[i];
            //   list[i] = list[randomIndex];
            //   list[randomIndex] = temp;
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }
}