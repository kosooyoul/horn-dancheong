using UnityEngine;
using UnityEngine.VFX;
using System.Collections;

public enum BuffFXType
{
    Buff,
    Heal,
    Debuff
}

public class BuffFXController : MonoBehaviour
{
    [SerializeField] private VisualEffect vfx;

    [Header("State")]
    [SerializeField]
    private BuffFXType currentType = BuffFXType.Buff;

    [SerializeField]
    [Min(0f)]
    private float colorIntensity = 1.5f;

    private Coroutine stopCoroutine;

    private void Awake()
    {
        ApplyCurrentType();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (vfx == null)
            vfx = GetComponent<VisualEffect>();

        ApplyCurrentType();
    }
#endif

    private void ApplyCurrentType()
    {
        if (vfx == null)
            return;

        switch (currentType)
        {
            case BuffFXType.Buff:
                vfx.SetFloat("Index", 0);
                vfx.SetVector4("Color", GetHDRColor("#8F1D21", colorIntensity));
                break;

            case BuffFXType.Heal:
                vfx.SetFloat("Index", 1);
                vfx.SetVector4("Color", GetHDRColor("#1B6B5A", colorIntensity));
                break;

            case BuffFXType.Debuff:
                vfx.SetFloat("Index", 2);
                vfx.SetVector4("Color", GetHDRColor("#243B7A", colorIntensity));
                break;
        }
    }

    private Color GetHDRColor(string hex, float intensity)
    {
        Color color;
        ColorUtility.TryParseHtmlString(hex, out color);

        color *= intensity;
        color.a = 1f;

        return color;
    }

    public void SetType(BuffFXType type)
    {
        currentType = type;
        ApplyCurrentType();
    }

    public void Play()
    {
        vfx.Play();
    }

    public void Play(float time)
    {
        vfx.Play();

        if (stopCoroutine != null)
            StopCoroutine(stopCoroutine);

        stopCoroutine = StartCoroutine(Co_StopAfterTime(time));
    }

    public void Stop()
    {
        vfx.Stop();

        if (stopCoroutine != null)
        {
            StopCoroutine(stopCoroutine);
            stopCoroutine = null;
        }
    }

    private IEnumerator Co_StopAfterTime(float time)
    {
        yield return new WaitForSeconds(time);

        vfx.Stop();
        stopCoroutine = null;
    }
}