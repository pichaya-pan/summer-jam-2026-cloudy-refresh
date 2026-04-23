using UnityEngine;

// [RequireComponent] is a Unity attribute that enforces a dependency.
// It tells Unity: "This script cannot exist on a GameObject unless
// a Tree component is also attached." If you forget to add Tree,
// Unity will add it automatically. It also prevents accidentally
// removing Tree while TreeWiltTimer is still present.
[RequireComponent(typeof(Trees))] 
public class TreeWiltTimer : MonoBehaviour
{
    // [Header] creates a labelled section in the Unity Inspector.
    // Useful for organising fields when a script has many parameters.
    [Header("Sun Wilt Rates (HP/sec)")]

    // [SerializeField] exposes a private field in the Inspector.
    // Private keeps the field safe from other scripts reading/writing it
    // directly in code, while SerializeField still lets designers tune
    // the value without touching code.
    //
    // exposedWiltRate: HP drained per second when the tree is in full sun
    // with NO cloud shadow over it. Default = 4 HP/sec.
    [SerializeField] private float exposedWiltRate = 4f;

    // shadedWiltRate: HP drained per second when the tree is in sun range
    // BUT the cloud is casting shadow over it. Default = 1 HP/sec.
    // The cloud shadow slows damage but does not stop it — this is the
    // core design rule from the GDD: shade mitigates, not immunises.
    [SerializeField] private float shadedWiltRate = 1f;

    // Private reference to the sibling Tree component on the same GameObject.
    // Cached in Awake() so we don't call GetComponent<Tree>() every frame,
    // which would be slow at runtime.
    private Trees tree;

    // Awake() runs once when the GameObject is first initialised,
    // before any Update() calls. It is the standard place to resolve
    // component references on the same GameObject.
    private void Awake()
    {
        // GetComponent<Tree>() searches this GameObject for a Tree component
        // and stores the reference. Because [RequireComponent] guarantees
        // Tree exists, this will never return null.
        tree = GetComponent<Trees>();
    }

    // Update() is called by Unity once per frame, for every active
    // MonoBehaviour. This is where per-frame game logic lives.
    private void Update()
    {
        // Early-exit guard #1: dead trees take no further damage.
        // Returning early skips all remaining logic in this frame,
        // which is more efficient than wrapping everything in an if-block.
        if (tree.IsDead) return;

        // Early-exit guard #2: trees outside the sun's heat radius
        // receive no sun damage at all (the GDD rule: "if not in sun,
        // no passive damage for now"). IsExposedToSun is set each frame
        // by SunController based on distance from the sun entity.
        //if (!tree.IsExposedToSun) return;

        // At this point we know: the tree is alive AND in sun range.
        // Now determine which damage rate applies this frame.
        //
        // This is a ternary expression — shorthand for:
        //   if (tree.IsShaded) { rate = shadedWiltRate; }
        //   else               { rate = exposedWiltRate; }
        //
        // tree.IsShaded is set each frame by ShadowZone when the cloud's
        // shadow collider overlaps this tree's collider.
        float rate = tree.IsShaded ? shadedWiltRate : exposedWiltRate;

        // Apply damage to the tree this frame.
        //
        // Time.deltaTime is the elapsed time (in seconds) since the last frame.
        // Multiplying rate × Time.deltaTime converts "HP per second" into
        // "HP for this specific frame", so the damage is frame-rate independent.
        // Whether the game runs at 30 FPS or 120 FPS, the total HP lost
        // per real-world second stays consistent.
        //
        // ReceiveSunHeat() is defined on Tree — it clamps health to [0, 100]
        // and then calls RefreshWiltState() to update the tree's visual state.
        tree.ReceiveSunHeat(rate * Time.deltaTime);
    }
}
