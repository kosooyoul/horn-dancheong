using System.Collections;
using UnityEngine;

public class TitleController : MonoBehaviour
{
    [Header("Materials")]
    [SerializeField] private Material materialA;
    [SerializeField] private Material materialB;

    [Header("Animation")]
    [SerializeField] private float duration = 2f;

    private Coroutine lerpCoroutine;

    private static readonly int LerpAlphaID = Shader.PropertyToID("LerpAlpha");

    private void Awake()
    {
        SetAlpha(0f);
    }

    /// <summary>
    /// 기본 duration 사용
    /// </summary>
    public void Play()
    {
        Play(duration);
    }

    /// <summary>
    /// 원하는 시간으로 재생
    /// </summary>
    public void Play(float customDuration)
    {
        if (lerpCoroutine != null)
        {
            StopCoroutine(lerpCoroutine);
        }

        lerpCoroutine = StartCoroutine(LerpAlphaCoroutine(customDuration));
    }

    private IEnumerator LerpAlphaCoroutine(float animDuration)
    {
        float time = 0f;

        SetAlpha(0f);

        while (time < animDuration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / animDuration);

            SetAlpha(t);

            yield return null;
        }

        SetAlpha(1f);
        lerpCoroutine = null;
    }

    private void SetAlpha(float value)
    {
        if (materialA != null)
            materialA.SetFloat(LerpAlphaID, value);

        if (materialB != null)
            materialB.SetFloat(LerpAlphaID, value);
    }
}