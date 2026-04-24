using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    // An enum (enumeration) defines a fixed set of named integer constants.
    // This is cleaner than using raw numbers like 0, 1, 2, 3 — you get
    // human-readable names that prevent bugs from mismatched magic numbers.
    public enum GameState
    {
        Playing,       // Normal gameplay — timer counting, trees wilting
        Paused,        // Game frozen, probably showing a pause menu
        LevelComplete, // Player won — freeze time, show results
        GameOver       // Player lost — freeze time, show game over screen
    }

    // This is a C# property with a public getter and a private setter.
    // - Any other script CAN READ CurrentState (public get)
    // - But ONLY GameStateManager itself can CHANGE it (private set)
    // This enforces that state changes go through the proper methods below,
    // not by direct assignment from outside.
    // It starts as Playing because the game begins in active play.
    public GameState CurrentState { get; private set; } = GameState.Playing;

    // [SerializeField] exposes a private field in the Unity Inspector,
    // so you can drag-and-drop the reference without making the field public.
    // This is preferred over public fields — it keeps fields encapsulated
    // while still being wirable in the editor.
    [SerializeField] private TimerManager timerManager;
    [SerializeField] private ForestManager forestManager;
    [SerializeField] private GameOverUI gameOverUI;
    [SerializeField] private GameObject bgmSource;


    // Start() is a Unity lifecycle method called once, on the first frame
    // the script is active. This is where you do one-time setup/wiring.
    private void Start()
    {
        // The += operator subscribes to a C# event (similar to a callback/listener
        // pattern in other languages). When TimerManager fires OnTimerEnd,
        // our HandleTimerEnd() method will be called automatically.
        // The null check (timerManager != null) prevents a crash if the
        // reference wasn't wired up in the Inspector.
        if (timerManager != null)
        {
            timerManager.OnTimerEnd += HandleTimerEnd;
        }

        // Same pattern — subscribe to ForestManager's OnForestDead event.
        // ForestManager fires this when too many trees have died.
        if (forestManager != null)
        {
            forestManager.OnForestDead += HandleForestDead;
        }

        bgmSource?.SetActive(true);

    }

    // This is the event handler for when the countdown timer hits zero.
    // It's private because nothing outside this class needs to call it directly —
    // it only fires via the event subscription above.
    private void HandleTimerEnd()
    {
        SetGameOver("Timer ran out");  // Timer ran out → player loses
    }

    // Event handler for when ForestManager reports too many dead trees.
    private void HandleForestDead()
    {
        SetGameOver("Too many trees died");  // Forest died → player loses
    }

    // Public method — other scripts (e.g. LevelManager) call this
    // when all win conditions are satisfied.
    private void SetLevelComplete()
    {
        CurrentState = GameState.LevelComplete;

        // Time.timeScale controls how fast Unity's game loop runs.
        // Setting it to 0 effectively pauses all physics, animations,
        // and Update() calls that use Time.deltaTime.
        // This freezes the game world while showing the results screen.
        Time.timeScale = 0f;
    }

    // Public method for triggering the lose state.
    private void SetGameOver(string cause)
    {
        // Guard clause: if we're already in GameOver, do nothing.
        // This prevents the method from running twice if both
        // OnTimerEnd and OnForestDead fire at nearly the same time.
        if (CurrentState == GameState.GameOver) return;

        CurrentState = GameState.GameOver;

        // Same as SetLevelComplete — freeze the game world.
        Time.timeScale = 0f;
        bgmSource?.SetActive(false);
        gameOverUI.Show(cause);          // shows panel, stops BGM, pauses

    }

}
