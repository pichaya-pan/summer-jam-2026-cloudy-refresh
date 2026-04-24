using UnityEngine;

// AudioManager is a singleton that persists across scene loads.
// It owns three AudioSource channels (BGM, SFX, SunDrone) and exposes
// methods for other systems to trigger audio without needing a direct reference
// to any specific AudioSource.
public class AudioManager : MonoBehaviour
{
    // --- Singleton pattern ---
    // A static field holds the one authoritative instance.
    // "static" means it belongs to the class itself, not any individual object,
    // so any script can reach it via AudioManager.Instance without a reference.
    public static AudioManager Instance { get; private set; }
                                        // ^^ { get; private set; } = any code can READ Instance,
                                        //    but only code inside AudioManager can WRITE to it.

    // --- Serialized AudioSource references ---
    // [SerializeField] makes a private field visible in the Unity Inspector.
    // You drag the three AudioSource components into these slots manually.
    // Each AudioSource is an independent playback channel; Unity lets one
    // GameObject carry multiple AudioSources to separate concerns cleanly.
    private AudioSource bgmSource;         // looping background music
    [SerializeField] private AudioSource sfxSource;         // one-shot sound effects
    [SerializeField] private AudioSource sunDroneSource;    // heat-driven ambient drone

    // --- Lifecycle: Awake() ---
    // Awake() runs once when the GameObject is first initialised,
    // before any Start() calls on any other script in the scene.
    // It is the correct place to set up singletons.
    private void Awake()
    {
        // Duplicate guard: if an Instance already exists and it isn't THIS object,
        // a second AudioManager has been created (e.g. on scene reload).
        // Destroy the newcomer immediately to keep the original alive.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return; // early exit — none of the setup below should run on the duplicate
        }

        // No existing instance → register this object as the singleton.
        Instance = this;

        // DontDestroyOnLoad tells Unity not to destroy this GameObject when
        // a new scene loads. This lets music and the drone channel continue
        // playing seamlessly across scene transitions (e.g. MainMenu → GameScene).
        DontDestroyOnLoad(gameObject);

    }


    // --- PlayBGM(AudioClip clip) ---
    // Assigns a clip to the BGM channel and starts looping playback.
    // Caller passes in the clip; AudioManager does not store a clip library —
    // that responsibility stays with whoever calls this method.
    public void PlayBGM(AudioClip clip)
    {
        if (bgmSource == null || clip == null) return;  // null-guard: fail silently if not wired up
        
        bgmSource.clip = clip;  // assign the clip to the channel
        bgmSource.loop = true;  // BGM should loop indefinitely
        bgmSource.Play();       // begin playback from the start of the clip
    }

    public void StopBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
            bgmSource.Stop();
    }


    // --- PlaySFX(AudioClip clip) ---
    // Fires a one-shot sound effect.
    // PlayOneShot() overlaps with any currently playing clip on the same source,
    // so rapid calls (e.g. multiple tree restores in quick succession) won't
    // cut each other off. Volume is the AudioSource's default volume.
    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip);
    }

    // --- SetRainVolume(float volume) ---
    // Stub — reserved for Day 2+ when ForestManager drives rain audio intensity
    // based on forest health percentage. Intentionally empty on Day 1.
    public void SetRainVolume(float volume)
    {
        // Stub for later
        // TODO Day 2: sfxSource cross-fade between sfx_rain_light and sfx_rain_heavy
    }

    // --- SetSunDronePitch(float normalizedHeat) ---
    // Called every frame by HeatGaugeManager (Day 2+), passing a 0–1 float
    // representing current heat level (0 = cool, 1 = scorching).
    //
    // Mathf.Lerp(a, b, t) returns a value linearly interpolated between a and b
    // at position t. So:
    //   normalizedHeat 0.0 → volume 0.0,  pitch 0.8  (silent, low)
    //   normalizedHeat 0.5 → volume 0.3,  pitch 1.1  (audible, mid)
    //   normalizedHeat 1.0 → volume 0.6,  pitch 1.4  (prominent, high)
    //
    // This gives the player an audio pressure cue that mirrors the visual
    // Heat Gauge — the drone rises as the situation becomes critical.
    public void SetSunDronePitch(float normalizedHeat)
    {
        if (sunDroneSource == null) return;

        sunDroneSource.volume = Mathf.Lerp(0f, 0.6f, normalizedHeat);
        sunDroneSource.pitch = Mathf.Lerp(0.8f, 1.4f, normalizedHeat);
    }


}
