using System.Collections;
using UnityEngine;

public class DissolveController : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;

    private Material materialInstance;
    private Coroutine dissolveCoroutine;

    private static readonly int DissolveAlphaID =
        Shader.PropertyToID("_DissolveAlpha"); // 셰이더 프로퍼티명에 맞게 수정

    private void Awake()
    {
        materialInstance = targetRenderer.material;
    }

    public void PlayDissolve(float duration)
    {
        if (dissolveCoroutine != null)
        {
            StopCoroutine(dissolveCoroutine);
        }

        dissolveCoroutine = StartCoroutine(DissolveRoutine(0f, 1f, duration));
    }

    public void PlayAppear(float duration)
    {
        if (dissolveCoroutine != null)
        {
            StopCoroutine(dissolveCoroutine);
        }

        dissolveCoroutine = StartCoroutine(DissolveRoutine(1f, 0f, duration));
    }

    private IEnumerator DissolveRoutine(float start, float end, float duration)
    {
        float time = 0f;

        materialInstance.SetFloat(DissolveAlphaID, start);

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / duration);

            // Ease In Out
            t = Mathf.SmoothStep(0f, 1f, t);

            float value = Mathf.Lerp(start, end, t);

            materialInstance.SetFloat(DissolveAlphaID, value);

            yield return null;
        }

        materialInstance.SetFloat(DissolveAlphaID, end);

        dissolveCoroutine = null;
    }
}