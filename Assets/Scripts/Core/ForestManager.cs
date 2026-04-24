using System;
using System.Collections.Generic; // Required for List<T> and IReadOnlyList<T>
using UnityEngine;

// ForestManager (Day 3): Data-driven factory and registry for Tree objects.
// Replaces the Day 2 approach of scanning the scene every frame with FindObjectsByType.
//
// Responsibilities:
//   - Spawn trees from a LevelData asset when a level loads
//   - Maintain a live, accurate list of active Tree components
//   - Expose that list read-only to other systems (HeatGaugeManager, DroughtPulseManager, etc.)
//   - Clean up the previous forest when a new level loads
public class ForestManager : MonoBehaviour
{
    // The Tree prefab to clone for each spawn position.
    // Must include: SpriteRenderer, Collider2D, Tree, TreeWiltTimer, TreeHealthDisplay.
    // Assigned in the Inspector — drag Assets/Prefabs/Tree.prefab here.
    [SerializeField] private GameObject treePrefab;

    // The parent Transform that all spawned trees will be children of in the hierarchy.
    // Keeping trees under one parent makes ClearForest() simple and keeps the scene tidy.
    // Assign the TreeContainer object in the Inspector.
    [SerializeField] private Transform treeParent;

    // The authoritative list of Tree components currently alive in the scene.
    // readonly: this variable always points to the same list — only its contents change.
    // Other systems that need tree data should read ActiveTrees, not call FindObjectsByType.
    private readonly List<Trees> activeTrees = new();

    // Public read-only view of activeTrees.
    // The => syntax is a shorthand property getter — equivalent to { get { return activeTrees; } }
    // IReadOnlyList means callers can iterate and index, but cannot Add() or Clear().
    // This enforces that ForestManager is the sole owner of the list.
    public IReadOnlyList<Trees> ActiveTrees => activeTrees;

    // ── COMPATIBILITY BRIDGE ─────────────────────────────────────────────────
    // ForestHealthPercent: derived from activeTrees each frame.
    // Returns 0–100, matching the Day 2 contract that HUDController expects.
    public float ForestHealthPercent
    {
        get
        {
            if (activeTrees.Count == 0) return 0f;
            int aliveCount = 0;
            foreach (Trees t in activeTrees)
                if (t != null && !t.IsDead) aliveCount++;
            return (aliveCount / (float)activeTrees.Count) * 100f;
        }
    }

    // OnForestDead: fired when dead trees exceed the threshold.
    // GameStateManager subscribes to this to trigger the lose state.
    public event Action OnForestDead;

    [SerializeField] private int deadTreeLoseThreshold = 5;

    private bool deadEventFired = false; // guard: only fire once per level
    private Coroutine deadEventCoroutine;

    private void Update()
    {
        // Count dead trees and fire the event if threshold is crossed.
        int deadCount = 0;
        foreach (Trees t in activeTrees)
            if (t != null && t.IsDead) deadCount++;

        if (!deadEventFired && deadCount >= deadTreeLoseThreshold)
        {
            deadEventFired = true;
            //OnForestDead?.Invoke();
            // Start coroutine instead of firing immediately
            deadEventCoroutine = StartCoroutine(FireDeadEventDelayed());
        }
    }

    // Called by LevelDataLoader when a level starts (or restarts).
    // Wipes the existing forest and builds a fresh one from the level's spawn layout.
    public event Action<Trees> OnTreeRegistered;
    public void BuildForest(LevelData levelData)
    {
        if (deadEventCoroutine != null)
        {
            StopCoroutine(deadEventCoroutine);
            deadEventCoroutine = null;
        }

        // Always clear first — ensures no leftover trees from a previous level or test run.
        ClearForest();
        deadEventFired = false; // reset guard for new level


        deadTreeLoseThreshold = levelData.maxDeadTreesAllowed;

        // Guard: if no layout asset is assigned to this level, log an error and bail out.
        // Without this check, the foreach below would throw a NullReferenceException.
        if (levelData.treeSpawnLayout == null)
        {
            Debug.LogError("ForestManager: TreeSpawnLayout missing.");
            return;
        }

        // Iterate over every Vector2 position defined in the level's spawn layout asset.
        // Each position was set manually in the Inspector for that level's TreeSpawnLayout.
        foreach (Vector2 pos in levelData.treeSpawnLayout.positions)
        {
            // Clone the Tree prefab into the scene at this position.
            // Quaternion.identity = no rotation applied.
            // treeParent = this new GameObject becomes a child of TreeContainer.
            GameObject treeGO = Instantiate(treePrefab, pos, Quaternion.identity, treeParent);

            // Retrieve the Tree component from the newly spawned GameObject.
            // GetComponent returns null if Tree.cs isn't on the prefab — the guard below catches that.
            Trees tree = treeGO.GetComponent<Trees>();

            if (tree != null)
            {
                // Reset the tree to its initial healthy state (Lush, full health).
                // Important when reloading a level — without this, trees keep their previous state.
                tree.ResetTree();

                // Register this tree in the live registry.
                // From this point on, other systems read from ActiveTrees instead of scanning the scene.
                activeTrees.Add(tree);
                OnTreeRegistered?.Invoke(tree);
            }
        }
    }

    // Destroys all tree GameObjects under treeParent and clears the registry.
    // Called at the start of BuildForest() to ensure a clean slate.
    private void ClearForest()
    {
        // Iterate BACKWARDS through child indices.
        // If you iterate forward and destroy as you go, Unity shifts remaining children
        // down by one — causing every other child to be skipped.
        // Iterating backwards avoids this: removing the last child never affects earlier indices.
        for (int i = treeParent.childCount - 1; i >= 0; i--)
        {
            Destroy(treeParent.GetChild(i).gameObject);
        }

        // Wipe the registry to match. After this, activeTrees.Count == 0.
        activeTrees.Clear();
    }

    private System.Collections.IEnumerator FireDeadEventDelayed()
    {
        // Wait enough frames for the Dead animation to actually begin playing.
        // 2 frames is usually enough; 0.5s gives a visible moment of drama.
        yield return new WaitForSecondsRealtime(0.5f);
        OnForestDead?.Invoke();
    }
}