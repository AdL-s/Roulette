using System;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "GlobalMaterialSettings", menuName = "Scriptable Objects/GlobalMaterialSettings")]
public class GlobalMaterialSettings : ScriptableObject
{
    private static readonly int globalTint = Shader.PropertyToID("_globalTint");
    private static readonly int globalSmoothness = Shader.PropertyToID("_globalSmoothness");
    private static readonly int globalDissolveColor = Shader.PropertyToID("_globalDissolveColor");

    public Color tint;

    [Range(0f, 1f)] public float smoothness;

    [Tooltip(
        "Color of the burning edge on every dissolving material (Items_Shader). HDR, so values above 1 glow with bloom.")]
    [ColorUsage(false, true)]
    public Color dissolveColor = new Color(2f, 0.8f, 0.2f);

    [Button]
    public void SetGlobalMatSettings()
    {
        Shader.SetGlobalColor(globalTint, tint);
        Shader.SetGlobalFloat(globalSmoothness, smoothness);
        Shader.SetGlobalColor(globalDissolveColor, dissolveColor);
    }
}