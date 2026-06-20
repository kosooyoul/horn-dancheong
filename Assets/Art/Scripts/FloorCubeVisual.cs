using System.Collections;
using UnityEngine;

public class FloorCubeVisual : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private FloorCubeStater stater;

    [SerializeField]
    private Renderer targetRenderer;

    [Header("Transition")]
    [SerializeField]
    private float transitionTime = 0.3f;

    [Header("Safety Data")]
    [SerializeField]
    private SafetyVisualData safeData;

    [SerializeField]
    private SafetyVisualData dangerSData;

    [SerializeField]
    private SafetyVisualData dangerMData;

    [SerializeField]
    private SafetyVisualData dangerLData;

    [SerializeField]
    private SafetyVisualData dangerXLData;

    private MaterialPropertyBlock mpb;

    private Coroutine transitionCoroutine;

    // 체커보드용 오프셋 — GridTile.Init()에서 (x+y)%2 기준으로 설정
    private float _checkerAlphaOffset = 0f;

    public void SetCheckerAlphaOffset(float offset)
    {
        _checkerAlphaOffset = offset;
        targetRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(ColorSwitchAlphaID, offset);
        targetRenderer.SetPropertyBlock(mpb);
    }

    // ── 하이라이트 제어 ──────────────────────────────────────────────────
    private Color _originalBaseColor;
    private bool  _isHighlighted;

    // GridManager에서 호출 — 하이라이트 색 적용 및 체커 비활성화
    public void SetHighlight(Color highlightColor, float strength)
    {
        if (!_isHighlighted)
        {
            targetRenderer.GetPropertyBlock(mpb);
            Color current = mpb.GetColor(BaseColorID);
            if (current == Color.clear && targetRenderer.sharedMaterial != null)
                current = targetRenderer.sharedMaterial.GetColor(BaseColorID);
            _originalBaseColor = current;
            _isHighlighted = true;
        }

        targetRenderer.GetPropertyBlock(mpb);
        mpb.SetColor(BaseColorID, Color.Lerp(_originalBaseColor, highlightColor, strength));
        mpb.SetFloat(ColorSwitchAlphaID, 0f);
        targetRenderer.SetPropertyBlock(mpb);
    }

    // GridManager에서 호출 — BaseColor·ColorSwitchAlpha 복원
    public void ClearHighlight()
    {
        if (!_isHighlighted) return;
        _isHighlighted = false;

        targetRenderer.GetPropertyBlock(mpb);
        mpb.SetColor(BaseColorID, _originalBaseColor);
        mpb.SetFloat(ColorSwitchAlphaID, _checkerAlphaOffset);
        targetRenderer.SetPropertyBlock(mpb);
    }

    private static readonly int BaseColorID =
        Shader.PropertyToID("_BaseColor");

    private static readonly int SubColorID =
        Shader.PropertyToID("_SubColor");

    private static readonly int ColorSwitchAlphaID =
        Shader.PropertyToID("_ColorSwitchAlpha");

    private static readonly int TextureColorID =
        Shader.PropertyToID("_TextureColor");

    private static readonly int IntensityID =
        Shader.PropertyToID("_Intensity");

    private static readonly int MaskIndexID =
        Shader.PropertyToID("_OneHotMaskIndex");

    private static readonly int DissolveID =
        Shader.PropertyToID("_DissolveFloat");

    private void Awake()
    {
        mpb = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        stater.OnSafetyChanged += HandleSafetyChanged;
    }

    private void OnDisable()
    {
        stater.OnSafetyChanged -= HandleSafetyChanged;
    }

    private void Start()
    {
        ApplyData(
            GetVisualData(
                stater.CurrentSafety));
    }

    private void HandleSafetyChanged(
        SafetyType oldState,
        SafetyType newState)
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(
                transitionCoroutine);
        }

        transitionCoroutine =
            StartCoroutine(
                TransitionCoroutine(
                    GetVisualData(oldState),
                    GetVisualData(newState)));
    }

    private SafetyVisualData GetVisualData(
        SafetyType safetyType)
    {
        switch (safetyType)
        {
            case SafetyType.Safe:
                return safeData;

            case SafetyType.DangerS:
                return dangerSData;

            case SafetyType.DangerM:
                return dangerMData;

            case SafetyType.DangerL:
                return dangerLData;

            case SafetyType.DangerXL:
                return dangerXLData;
        }

        return safeData;
    }

    private IEnumerator TransitionCoroutine(
        SafetyVisualData from,
        SafetyVisualData to)
    {
        float elapsed = 0.0f;

        bool maskChanged =
            !Mathf.Approximately(
                from.OneHotMaskIndex,
                to.OneHotMaskIndex);

        bool swappedMask = false;

        while (elapsed < transitionTime)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / transitionTime);

            ApplyInterpolatedData(
                from,
                to,
                t);

            if (maskChanged &&
                !swappedMask &&
                t >= 0.5f)
            {
                SetMaskIndex(
                    to.OneHotMaskIndex);

                swappedMask = true;
            }

            yield return null;
        }

        ApplyData(to);

        transitionCoroutine = null;
    }

    private void ApplyInterpolatedData(
        SafetyVisualData from,
        SafetyVisualData to,
        float t)
    {
        targetRenderer.GetPropertyBlock(mpb);

        mpb.SetColor(
            BaseColorID,
            Color.Lerp(
                from.BaseColor,
                to.BaseColor,
                t));

        mpb.SetColor(
            SubColorID,
            Color.Lerp(
                from.SubColor,
                to.SubColor,
                t));

        mpb.SetFloat(
            ColorSwitchAlphaID,
            _checkerAlphaOffset);

        mpb.SetColor(
            TextureColorID,
            Color.Lerp(
                from.TextureColor,
                to.TextureColor,
                t));

        mpb.SetFloat(
            IntensityID,
            Mathf.Lerp(
                from.Intensity,
                to.Intensity,
                t));

        mpb.SetFloat(
            DissolveID,
            t);

        targetRenderer.SetPropertyBlock(mpb);
    }

    private void ApplyData(
        SafetyVisualData data)
    {
        targetRenderer.GetPropertyBlock(mpb);

        mpb.SetColor(
            BaseColorID,
            data.BaseColor);

        mpb.SetColor(
            SubColorID,
            data.SubColor);

        mpb.SetFloat(
            ColorSwitchAlphaID,
            _checkerAlphaOffset);

        mpb.SetColor(
            TextureColorID,
            data.TextureColor);

        mpb.SetFloat(
            IntensityID,
            data.Intensity);

        mpb.SetFloat(
            MaskIndexID,
            data.OneHotMaskIndex);

        targetRenderer.SetPropertyBlock(mpb);
    }

    private void SetMaskIndex(
        float value)
    {
        targetRenderer.GetPropertyBlock(mpb);

        mpb.SetFloat(
            MaskIndexID,
            value);

        targetRenderer.SetPropertyBlock(mpb);
    }

}