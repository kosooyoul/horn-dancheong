using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticlePosterizeUpdate : MonoBehaviour
{
    [SerializeField] private float simulatedFps = 12f;
    [SerializeField] private bool useUnscaledTime = false;

    private ParticleSystem ps;
    private float accumulator;
    private float step;

    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        step = 1f / simulatedFps;

        ps.Play();
        ps.Pause();
    }

    private void Update()
    {
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        accumulator += dt;

        while (accumulator >= step)
        {
            ps.Simulate(step, true, false, false);
            accumulator -= step;
        }
    }

    public void SetPosterizeFPS(float newFps)
    {
        simulatedFps = Mathf.Max(1f, newFps);
        step = 1f / simulatedFps;
    }
}