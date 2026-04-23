using UnityEngine;


/// <summary>
/// LevelDataLoader acts as the "configuration dispatcher" for a level.
/// 
/// Architecture role:
///   LevelData (ScriptableObject / data asset)
///       └─► LevelDataLoader  ← you are here
///               ├─► TimerManager
///               ├─► SunController
///               ├─► WindManager
///               ├─► DroughtPulseManager
///               ├─► CloudController
///               └─► ForestManager
///
/// The key idea: instead of hardcoding numbers directly on each manager,
/// all level-specific values live in one LevelData asset. This loader
/// reads that asset once (on Start) and pushes the correct values into
/// every live system. Swapping a level is then just swapping the asset.
/// </summary>
public class LevelDataLoader : MonoBehaviour
{
    [SerializeField] private LevelData levelData;
    [SerializeField] private ForestManager forestManager;
    [SerializeField] private TimerManager timerManager;
    [SerializeField] private SunController sunController;
    [SerializeField] private WindManager windManager;
    [SerializeField] private DroughtPulseManager droughtPulseManager;
    [SerializeField] private CloudController cloudController;

    public LevelData CurrentLevelData => levelData;

    private void Start()
    {
        if (levelData == null)
        {
            Debug.LogError("LevelDataLoader: no LevelData assigned.");
            return;
        }

        ApplyLevelData();
    }

    public void ApplyLevelData()
    {
        if (timerManager != null)
            timerManager.SetTime(levelData.timerSeconds);

        if (sunController != null)
            sunController.Configure(
                levelData.sunSpeed,
                levelData.sunHeatRadius,
                levelData.sunHeatMultiplier,
                levelData.sunPathStyle,
                levelData.hasSolarFlare,
                levelData.solarFlareInterval

            );


        // ── WIND ────────────────────────────────────────────────────
        // Wind is introduced in Level 3. For Levels 1–2, hasWind = false
        // in the data asset, so WindManager will know to stay dormant
        // even though it exists in the scene.
        if (windManager != null)
            windManager.Configure(
                levelData.hasWind,       // master on/off switch
                levelData.windStrength,  // how hard the gust pushes the cloud (units/sec)
                levelData.windInterval,  // seconds between gusts
                levelData.windDuration   // how long each gust lasts
            );

        // ── DROUGHT PULSE ───────────────────────────────────────────
        // Drought pulses are introduced in Level 4. Same pattern:
        // hasDroughtPulse = false in early levels disables the system
        // without removing it from the scene.
        if (droughtPulseManager != null)
            droughtPulseManager.Configure(
                levelData.hasDroughtPulse,          // master on/off switch
                levelData.droughtPulseInterval,     // seconds between pulses
                levelData.droughtPulseTargetCount,  // how many trees get hit per pulse
                levelData.droughtWarningDuration    // seconds of warning before pulse hits
            );

        if (forestManager != null)
            forestManager.BuildForest(levelData);

    }


    
}
