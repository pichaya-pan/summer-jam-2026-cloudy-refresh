// These two lines import namespaces — think of them as "include libraries".
// System.Collections.Generic gives you access to List<T>, a resizable array.
// UnityEngine gives you access to all Unity-specific types like Vector2 and ScriptableObject.
using System.Collections.Generic;
using UnityEngine;

// This attribute tells Unity's Editor to add this class to the
// Assets → Create menu, so you can right-click in the Project window
// and create a new asset of this type directly.
// fileName: the default name when you create one ("TreeSpawnLayout_")
// menuName: the path shown in the Create menu
[CreateAssetMenu(
    fileName = "TreeSpawnLayout_",
    menuName = "Cloudy Refresh/Tree Spawn Layout"
)]

// ScriptableObject is a Unity base class for assets that hold data.
// Unlike MonoBehaviour (which must be attached to a GameObject in a scene),
// a ScriptableObject lives as a standalone .asset file in your Project folder.
// It has no Update() or Start() — it is purely a data container.
public class TreeSpawnLayout : ScriptableObject
{
    // A List<Vector2> is a dynamically-sized array of 2D coordinates.
    // Vector2 holds two floats: x and y — enough to describe a position
    // on a flat 2D plane, which matches your top-down game world.
    //
    // "= new List<Vector2>()" initialises the list immediately so it's
    // never null. Without this, accessing the list before adding anything
    // would throw a NullReferenceException.
    //
    // "public" means Unity's Inspector can see and edit this field directly,
    // and ForestManager can read it from outside this class.
    public List<Vector2> positions = new List<Vector2>();
}
