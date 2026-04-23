using System.Collections.Generic;   // Required for HashSet<T> — not in UnityEngine, so it needs its own using directive
using UnityEngine;

public class RainZone : MonoBehaviour
{
    // [SerializeField] makes this private field visible in the Unity Inspector,
    // so you can tune the healing rate without editing code.
    // rainPerSecond = 20 means a tree gains 20 health points per second while in range.
    [SerializeField] private float rainPerSecond = 20f;

    // HashSet stores a collection of Tree references with NO duplicates and O(1) add/remove.
    // We use HashSet instead of List because:
    //   - A tree can only be in range once (no double-healing)
    //   - Add/Remove are faster than List when order doesn't matter
    // 'readonly' means the variable itself can't be reassigned, but its contents can change.
    // 'new()' is C# shorthand for 'new HashSet<Tree>()' — the type is inferred from the declaration.
    private readonly HashSet<Trees> treesInZone = new();

    // Unity calls this automatically when another 2D collider ENTERS this trigger collider.
    // 'other' is the collider that just entered.
    private void OnTriggerEnter2D(Collider2D other)
    {
        // GetComponent<Tree>() looks for a Tree script on the same GameObject as 'other'.
        // If the collider belongs to something that is NOT a tree (e.g. the sun, a wall),
        // this returns null and we skip it entirely.
        Trees tree = other.GetComponent<Trees>();
        
        if (tree != null)  
        {
            treesInZone.Add(tree);  // Register this tree as being inside the rain zone
            Debug.Log($"RainZone ENTER: {tree.name}");  
        }
        
    }

    // Unity calls this automatically when a 2D collider EXITS this trigger collider.
    private void OnTriggerExit2D(Collider2D other)
    {
        Trees tree = other.GetComponent<Trees>();
        
        if (tree != null)   
        {
            treesInZone.Remove(tree);   // Unregister — this tree is no longer being rained on
            Debug.Log($"RainZone EXIT: {tree.name}");
        }
    }

    // Update() runs once per frame.
    // This is where healing is actually delivered.
    private void Update()
    {
        // Iterate over every tree currently inside the rain zone
        foreach (Trees tree in treesInZone)
        {
            // Null check guards against the case where a tree GameObject was destroyed
            // mid-game but not yet removed from the set.
            // IsDead check prevents wasting healing on trees that are already gone.
            if (tree != null && !tree.IsDead)
            {
                // Time.deltaTime is the time elapsed since the last frame (in seconds).
                // Multiplying by deltaTime converts "per second" rates into
                // "per frame" amounts, keeping healing speed consistent regardless
                // of the game's frame rate (60 FPS vs 30 FPS behaves the same).
                tree.ReceiveRain(rainPerSecond * Time.deltaTime);
            }

        }
    }

}
