using System.Collections.Generic;   // Needed for HashSet<T>
using UnityEngine;                  // Needed for MonoBehaviour, Collider2D, Debug

public class ShadowZone : MonoBehaviour
{
    // Internal set — only this script can add/remove trees.
    // Upgraded from HashSet<TreeStub> to HashSet<Tree> (the real Tree class).
    private readonly HashSet<Trees> treesInZone = new();

    // Public read-only view of the same HashSet.
    // The => syntax is a C# "expression-bodied property" — a compact getter.
    // Equivalent to: public IReadOnlyCollection<Tree> TreesInZone { get { return treesInZone; } }
    // Other scripts (e.g. SunController) can iterate this but cannot mutate it.
    public IReadOnlyCollection<Trees> TreesInZone => treesInZone;


    // Unity calls this automatically when another 2D collider ENTERS this trigger collider.
    // 'other' is the collider that just entered.
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Try to get a TreeStub component from the object that just entered.
        // If the entering object isn't a tree (e.g. the sun, a boundary),
        // GetComponent returns null and we bail out immediately.
        Trees tree = other.GetComponent<Trees>();

        // HashSet.Add() returns true only if this tree wasn't already in the
        // set — guards against a duplicate enter event firing twice.
        if (tree != null)
        {
            treesInZone.Add(tree);
            //Debug.Log($"ShadowZone ENTER: {tree.name}");
        }
    }

    // Unity calls this automatically when a 2D collider EXITS this trigger collider.
    // Mirror of OnTriggerEnter2D.
    private void OnTriggerExit2D(Collider2D other)
    {
        Trees tree = other.GetComponent<Trees>();
        if (tree == null) return;

        // HashSet.Remove() returns true only if the tree was actually in the
        // set — guards against spurious exit events for non-member objects.
        if (tree != null)
        {
            treesInZone.Remove(tree);
            //Debug.Log($"ShadowZone EXIT: {tree.name}");

            // Immediately clear the shaded flag when a tree leaves.
            // We can't wait for LateUpdate — the tree is no longer in the
            // set so LateUpdate's first pass (reset all) handles it,
            // but this is an explicit safety clear in case of edge cases.
            tree.IsShaded = false;
        }
    }

    // ─── LATE UPDATE: AUTHORITATIVE SHADE WRITE ───────────────────────────
    // Runs after all Update() calls have finished for this frame.
    // This is the single place in the codebase that writes IsShaded,
    // ensuring SunController always reads a fully resolved value.
    private void LateUpdate()
    {
        // PASS 1 — Reset every tree in the scene to unshaded.
        // FindObjectsByType is a scene-wide search (expensive, optimize later).
        // FindObjectsSortMode.None skips sorting for a small speed gain.
        Trees[] allTrees = FindObjectsByType<Trees>(FindObjectsSortMode.None);

        for (int i = 0; i < allTrees.Length; i++)
        {
            allTrees[i].IsShaded = false;
        }

        // PASS 2 — Mark only the trees currently inside the shadow collider.
        // treesInZone is already up-to-date because trigger callbacks
        // fired earlier this frame during the physics step.
        foreach (Trees tree in treesInZone)
        {
            if (tree != null && !tree.IsDead)
            {
                tree.IsShaded = true;
                //Debug.Log($"ShadowZone OVERLAP: {tree.name}");
            }

        }
    }

}
