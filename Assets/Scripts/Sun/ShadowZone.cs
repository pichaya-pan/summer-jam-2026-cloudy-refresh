using System.Collections.Generic;   // Needed for HashSet<T>
using UnityEngine;                  // Needed for MonoBehaviour, Collider2D, Debug

public class ShadowZone : MonoBehaviour
{
    // HashSet stores references to every TreeStub currently inside the
    // shadow collider. Using HashSet (not List) because:
    //   - duplicate entries are automatically rejected
    //   - Add/Remove/Contains are all O(1)
    // "readonly" means the HashSet itself can't be reassigned,
    // but its contents can still change freely at runtime.
    private readonly HashSet<TreeStub> overlappingTrees = new();

    // ─── UNITY PHYSICS CALLBACK ───────────────────────────────────────────
    // Unity calls this automatically the moment another collider
    // enters this trigger zone. Runs once per entering object, not every frame.
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Try to get a TreeStub component from the object that just entered.
        // If the entering object isn't a tree (e.g. the sun, a boundary),
        // GetComponent returns null and we bail out immediately.
        TreeStub tree = other.GetComponent<TreeStub>();
        if (tree == null) return;

        // HashSet.Add() returns true only if this tree wasn't already in the
        // set — guards against a duplicate enter event firing twice.
        if (overlappingTrees.Add(tree))
        {
            Debug.Log($"ShadowZone ENTER: {tree.name}");
        }
    }

    // ─── UNITY PHYSICS CALLBACK ───────────────────────────────────────────
    // Unity calls this automatically the moment a collider leaves the zone.
    // Mirror of OnTriggerEnter2D.
    private void OnTriggerExit2D(Collider2D other)
    {
        TreeStub tree = other.GetComponent<TreeStub>();
        if (tree == null) return;

        // HashSet.Remove() returns true only if the tree was actually in the
        // set — guards against spurious exit events for non-member objects.
        if (overlappingTrees.Remove(tree))
        {
            Debug.Log($"ShadowZone EXIT: {tree.name}");
        }
    }

    // ─── UNITY FRAME CALLBACK ─────────────────────────────────────────────
    // Runs every frame. In Day 1 this just spams the Console to prove
    // the set is being maintained correctly while the cloud moves.
    //
    // In Day 2 this becomes genuinely useful: other scripts (SunController,
    // DroughtPulseManager) will call into this set to ask "is this tree
    // currently shaded?" instead of doing their own overlap checks.
    private void Update()
    {
        foreach (var tree in overlappingTrees)
        {
            // Null-check guards against a tree being destroyed mid-frame
            // (won't happen Day 1, but good habit to build now).
            if (tree != null)
            {
                Debug.Log($"ShadowZone OVERLAP: {tree.name}");
            }

        }
    }

}
