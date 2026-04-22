using UnityEngine;                  // Required for MonoBehaviour (Unity's base class for scripts)
using UnityEngine.SceneManagement;  // Required for SceneManager — Unity's built-in scene loading API

// MonoBehaviour is Unity's base class that lets this script attach to a GameObject
// and participate in Unity's lifecycle (Awake, Start, Update, etc.)
public class MainMenuUI : MonoBehaviour
{
    // Public method — Unity's UI Button can call this via its OnClick() event in the Inspector.
    // "public" is required here: the Button's OnClick system uses reflection to find
    // callable methods, and private methods are invisible to it.
    public void PlayGame()
    {
        // SceneManager.LoadScene() unloads the current scene and loads the target scene by name.
        // The string must exactly match the scene name in File > Build Settings > Scenes in Build.
        // Unity finds scenes by the name you gave the .unity file — not the full path.
        SceneManager.LoadScene("GameScene");
    }
}
