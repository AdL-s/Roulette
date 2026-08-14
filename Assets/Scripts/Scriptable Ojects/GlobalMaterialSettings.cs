using System;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "GlobalMaterialSettings", menuName = "Scriptable Objects/GlobalMaterialSettings")]
public class GlobalMaterialSettings : ScriptableObject
{
    private static readonly int globalTint = Shader.PropertyToID("_globalTint");
    
    private static readonly int globalSmoothness = Shader.PropertyToID("_globalSmoothness");

    public Color tint;

    [Range(0f, 1f)]
    public float smoothness;

    [Button]
    public void SetGlobalMatSettings()
    {
        Shader.SetGlobalColor(globalTint, tint);
        Shader.SetGlobalFloat(globalSmoothness, smoothness);
        
    }

}
