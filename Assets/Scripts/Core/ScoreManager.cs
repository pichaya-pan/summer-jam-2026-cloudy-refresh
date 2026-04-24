using UnityEngine;

// ScoreManager tracks the player's score by listening to tree recovery events.
// It lives on a Manager GameObject in the scene and is never directly called —
// it subscribes to events and reacts when trees broadcast state changes.
public class ScoreManager : MonoBehaviour
{
    // Auto-property: any script can READ Score, but only ScoreManager can WRITE it.
    // This protects the score from being accidentally changed by other systems.
    public int Score { get; private set; }

    [SerializeField] private ForestManager forestManager;
    // Local reference to every Tree in the scene, populated once at Start.
    private Trees[] trees;

    // Start() is called once by Unity just before the first frame.
    // We use it to find all trees and subscribe to their restoration events.
    private void Start()
    {

        // Subscribe to ForestManager so we get notified when trees are registered
        if (forestManager != null)
            forestManager.OnTreeRegistered += HandleTreeRegistered;
        
    }

    private void HandleTreeRegistered(Trees tree)
    {
        tree.OnTreeRestoredToState += HandleTreeRestored;
    }

    // This method is called automatically each time a tree recovers to a healthier state.
    // Parameters:
    //   tree      — the specific Tree that just recovered (we need it to check IsShaded)
    //   newState  — the WiltState the tree has transitioned INTO after receiving rain
    private void HandleTreeRestored(Trees tree, Trees.WiltState newState)
    {
        // Start with zero points; the switch below will assign a value based on state.
        int points = 0;

        // Award points based on which state the tree just recovered INTO.
        // Higher danger states that the tree was rescued from yield higher rewards.
        // Note: Lush gets a small reward (15 pts) as a bonus for a full recovery.
        switch (newState)
        {
            case Trees.WiltState.Dry:
                points = 10;
                break;
            case Trees.WiltState.Wilting:
                points = 25;
                break;
            case Trees.WiltState.Critical:
                points = 50;
                break;
            case Trees.WiltState.Lush:
                points = 15;
                break;

            // Dead state is intentionally omitted — dead trees cannot be recovered,
            // so there's no scoring scenario for that state.
        }

        // Shade bonus: if the cloud's shadow is covering this tree when rain heals it,
        // the player is rewarded for simultaneously protecting AND watering it.
        // Multiply points by 1.5× (50% bonus), then round to the nearest integer.
        // Mathf.RoundToInt() is needed because multiplying an int by a float (1.5f)
        // produces a float, which must be converted back to int for Score.
        
        /*
        if (tree.IsShaded)
        {
            points = Mathf.RoundToInt(points * 1.5f);
        }
        */

        // Add the final point value to the running total.

        Score += points;
    }


   
}
