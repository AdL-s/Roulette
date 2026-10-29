using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "GlobalSettingsWind", menuName = "Scriptable Objects/GlobalSettingsWind")]
public class GlobalSettingsWind : ScriptableObject
{
    
    private static readonly int globalWindStrength = Shader.PropertyToID("_globalWindStrength");
    
    private static readonly int globalWindTexture = Shader.PropertyToID("_globalWindTexture");
    private static readonly int globalWindScale = Shader.PropertyToID("_globalWindScale");
    
    private static readonly int globalWindTexture2 = Shader.PropertyToID("_globalWindTexture2");
    private static readonly int globalWindScale2 = Shader.PropertyToID("_globalWindScale2");
    
    private static readonly int globalWindDirection = Shader.PropertyToID("_globalWindDirection");
    
   // private static readonly int globalSmoothness = Shader.PropertyToID("_globalSmoothness");
    
    
    public Texture windTexture;
    public Texture windTexture2;

    public float windScale;
    public float windScale2;
    
    [Range(0f, 360f)]
    public float windDirection;
    
   // public float smoothness;
    
    public float windStrength;

    public Transform wind;
    [Range(0f, 45f)] public float leanAmount = 15f;

    private void OnEnable()
    {
        FindParticleSystem();
    }
    
    [Button]
    public void SetGlobalWind()
    {
        
        float rad = windDirection * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        
        
        float tiltY = dir.x * leanAmount;
        float tiltZ = dir.y * leanAmount;

        wind.localRotation = Quaternion.Euler(90f, tiltY, tiltZ);
        
        
        Shader.SetGlobalFloat(globalWindStrength, windStrength);
        Shader.SetGlobalTexture(globalWindTexture, windTexture);
        Shader.SetGlobalTexture(globalWindTexture2, windTexture2);
        Shader.SetGlobalFloat(globalWindScale2, windScale2);
        Shader.SetGlobalFloat(globalWindScale, windScale);
        Shader.SetGlobalVector(globalWindDirection, dir);
        
        
    }
    
    
    [Button]
    private void FindParticleSystem()
    {
        var found = GameObject.Find("WindParticles");
        if (found == null) return;
        wind = found.transform;
        
    }
    
    /*[Button]
    public void SetGlobalSmoothness()
    {
        Shader.SetGlobalFloat(globalSmoothness, smoothness);
    }*/

}


