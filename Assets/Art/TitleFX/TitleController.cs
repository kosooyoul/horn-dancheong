using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TitleController : MonoBehaviour
{
    [Header("Target UI")]
    [SerializeField] private RawImage rawImage;

    [Header("Material")]
    [SerializeField] private Material sourceMaterial;

    [Header("Animation")]
    [SerializeField] private float duration = 2f;

    private Material runtimeMaterial;
    private Coroutine lerpCoroutine;

    private static readonly int LerpAlphaID = Shader.PropertyToID("_LerpAlpha");

    private void Awake()
    {
        InitMaterial();
        SetAlpha(0f);
    }

    public void Play()
    {
        Play(duration);
    }

    public void Play(float customDuration)
    {
        if (!InitMaterial())
        {
            Debug.LogError("[TitleController] RawImage 머터리얼 초기화 실패");
            return;
        }

        if (lerpCoroutine != null)
        {
            StopCoroutine(lerpCoroutine);
        }

        Debug.Log($"[TitleController] Play 시작 / duration={customDuration}");

        lerpCoroutine = StartCoroutine(LerpAlphaCoroutine(customDuration));
    }

    private bool InitMaterial()
    {
        if (runtimeMaterial != null)
            return true;

        if (rawImage == null)
        {
            Debug.LogError("[TitleController] rawImage가 비어있음");
            return false;
        }

        if (sourceMaterial == null)
        {
            Debug.LogError("[TitleController] sourceMaterial이 비어있음");
            return false;
        }

        runtimeMaterial = Instantiate(sourceMaterial);
        rawImage.material = runtimeMaterial;

        if (!runtimeMaterial.HasProperty(LerpAlphaID))
        {
            Debug.LogError($"[TitleController] {runtimeMaterial.name}에 _LerpAlpha 프로퍼티가 없음");
            return false;
        }

        Debug.Log($"[TitleController] RawImage Material 초기화 완료: {runtimeMaterial.name}");

        return true;
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

        Debug.Log("[TitleController] 애니메이션 완료");

        lerpCoroutine = null;
    }

    private void SetAlpha(float value)
    {
        if (runtimeMaterial == null)
            return;

        runtimeMaterial.SetFloat(LerpAlphaID, value);
    }
}