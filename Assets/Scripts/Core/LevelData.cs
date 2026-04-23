using UnityEngine;  // Gives access to Unity types: ScriptableObject, Header, etc.

// ──────────────────────────────────────────────
// ENUMS
// Enums define a fixed set of named choices.
// Under the hood each name maps to an int (0, 1, 2…),
// but you always refer to them by name for readability.
// ──────────────────────────────────────────────

/// <summary>
/// Defines what the player must accomplish to win this level.
/// LevelDataLoader passes this to the win-condition checker.
/// </summary>
public enum GoalType
{
    RestoreCount,           // Win by restoring N specific trees (default)
    SurviveTimer,           // Win by keeping the forest alive until time runs out
    ForestHealthThreshold   // Win when overall forest health reaches a target %
}

/// <summary>
/// Controls how the sun moves horizontally across the screen each level.
/// SunController reads this and selects the matching movement formula.
/// </summary>
public enum SunPathStyle
{
    Simple,      // Flat left-right sweep at constant Y
    Arcing,      // Parabolic dip — lower in the middle, higher at edges
    Sinusoidal,  // Sine wave — Y oscillates up and down smoothly over time
    Erratic      // Perlin-noise driven — unpredictable, used in hard levels
}

// ──────────────────────────────────────────────
// SCRIPTABLE OBJECT
// ──────────────────────────────────────────────

/// <summary>
/// Per-level configuration asset.
/// One LevelData asset exists per level (LevelData_Lv1, LevelData_Lv2, etc.).
/// LevelDataLoader reads this at scene Start and pushes values into all live managers.
/// Nothing here runs any code — it is purely a data bag.
/// </summary>
[CreateAssetMenu(
    fileName = "LevelData_",                // Default filename when created via right-click
    menuName = "Cloudy Refresh/LevelData"   // Path shown in the Assets > Create menu
)]

public class LevelData : ScriptableObject
{
    // ── BASIC ────────────────────────────────────────────────────────────────
    // Core identity and win-condition for this level.
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Basic")]
    public string levelName = "Level 1";    // Display name shown in UI ("Level 1 – The Meadow")
    public int levelIndex = 1;              // Numeric index; used to load the correct asset programmatically
    public int treeCount = 8;               // How many trees are spawned (ForestManager reads this)
    public float timerSeconds = 90f;        // Countdown duration in seconds; passed to TimerManager
    public GoalType goalType = GoalType.RestoreCount;   // Which win condition applies (see GoalType enum above)
    public int goalValue = 5;               // The numeric target for the chosen goal
                                            // e.g. if goalType == RestoreCount, player must restore 5 trees


    // ── TREE / FOREST ─────────────────────────────────────────────────────────
    // Controls how quickly trees deteriorate this level.
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Tree / Forest")]
    public float wiltSpeedMultiplier = 1f;  // Scales the base wilt rate on all trees.
                                            // 1 = normal, 2 = double speed, 0.5 = half speed.
                                            // Applied inside Tree.cs tick logic.
    public int maxDeadTreesAllowed = 5;     // Lose condition: if this many trees die, game over.
                                            // Checked by the win/lose manager each frame.


    // ── WIND ──────────────────────────────────────────────────────────────────
    // Wind gusts push the cloud, making it harder to keep shade on trees.
    // Introduced in Level 3. All values are ignored when hasWind == false.
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Wind")]
    public bool hasWind = false;        // Master toggle — WindManager checks this first before doing anything
    public float windStrength = 0f;     // Force magnitude applied to the cloud's Rigidbody2D (units/sec²)
    public float windInterval = 0f;     // Seconds between the start of each gust cycle
    public float windDuration = 0f;     // How many seconds each individual gust lasts


    // ── DROUGHT PULSE ─────────────────────────────────────────────────────────
    // A periodic event that damages random unshaded trees.
    // Key mechanic of Level 4: shaded trees are immune.
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Drought Pulse")]
    public bool hasDroughtPulse = false;       // Master toggle for the drought system
    public float droughtPulseInterval = 0f;    // Seconds between each pulse event
    public int droughtPulseTargetCount = 0;    // How many trees are targeted per pulse
    public float droughtWarningDuration = 3f;  // Warning display time (seconds) before the pulse hits.
                                               // Gives the player time to reposition the cloud.

    // ── SUN ───────────────────────────────────────────────────────────────────
    // Controls the sun's movement and heat behaviour.
    // These values are passed to SunController.Configure() by LevelDataLoader.
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Sun")]
    public float sunSpeed = 1f;             // Horizontal sweep speed (world units/sec)
    public float sunHeatRadius = 2f;        // Radius (world units) within which the sun damages trees
    public float sunHeatMultiplier = 1f;    // Scales heat damage rate (1 = normal, 2 = brutal)
    public SunPathStyle sunPathStyle = SunPathStyle.Simple; // Which movement formula SunController uses (see enum)


    // ── SOLAR FLARE ───────────────────────────────────────────────────────────
    // An intensified sun event introduced in Level 6 and Endless Mode.
    // During a flare, shade becomes the ONLY way to protect trees.
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Solar Flare")]
    public bool hasSolarFlare = false;      // Enables solar flare events for this level
    public float solarFlareInterval = 0f;   // Seconds between each solar flare trigger


    // ── ALTITUDE ──────────────────────────────────────────────────────────────
    // When enabled, the player can move the cloud up/down (Q/E keys).
    // Higher altitude = smaller rain/shadow radius + faster movement.
    // Lower altitude = larger rain/shadow radius + slower movement.
    // CloudController reads this flag and adjusts its physics accordingly.
    // ─────────────────────────────────────────────────────────────────────────


    [Header("Altitude")]
    public bool enableAltitude = false; // Off by default; introduced in later levels


    // ── SPAWN LAYOUT ──────────────────────────────────────────────────────────
    // A reference to a separate ScriptableObject that stores the 2D positions
    // of all trees for this level. ForestManager reads it to instantiate trees.
    // Keeping layout in its own asset means you can share or swap layouts
    // independently from the rest of the level settings.
    // ─────────────────────────────────────────────────────────────────────────

    //[Header("Spawn Layout")]
    public TreeSpawnLayout treeSpawnLayout;  // Drag the matching TreeSpawnLayout_LvN asset here in Inspector
}
