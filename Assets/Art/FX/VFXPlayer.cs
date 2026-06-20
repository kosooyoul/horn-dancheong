using UnityEngine;
using UnityEngine.VFX;

public class VFXPlayer : MonoBehaviour
{
    [SerializeField] private VisualEffect vfx;

    public void PlayVFX()
    {
        vfx.Play();
    }

    public void StopVFX()
    {
        vfx.Stop();
    }

    public void ReinitVFX()
    {
        vfx.Reinit();
    }
}