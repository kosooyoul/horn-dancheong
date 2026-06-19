using UnityEngine;

[System.Serializable]
public class SafetyVisualData
{
    [Header("Base")]
    public Color BaseColor = Color.white;

    [Header("Emission")]
    public Color TextureColor = Color.white;

    public float Intensity = 1.0f;

    [Header("Pattern")]
    public float OneHotMaskIndex = 0.0f;
}