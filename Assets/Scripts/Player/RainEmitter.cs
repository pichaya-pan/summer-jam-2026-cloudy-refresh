using UnityEngine;

public class RainEmitter : MonoBehaviour
{
    [SerializeField] private ParticleSystem rainParticles;

    private void Awake()
    {
        if (rainParticles == null)
            rainParticles = GetComponentInChildren<ParticleSystem>();
    }

    private void Start()
    {
        if (rainParticles != null && !rainParticles.isPlaying)
            rainParticles.Play();
    }

}
