using UnityEngine;

[System.Serializable]
public class SafetyVisualData
{
    [Header("Albedo")]
    public Color BaseColor = Color.white;
    public Color SubColor = Color.white;
    public float ColorSwitchAlpha = 0.0f;

    [Header("Emission")]
    public Color TextureColor = Color.white;

    public float Intensity = 1.0f;

    [Header("Pattern")]
    public float OneHotMaskIndex = 0.0f;
}