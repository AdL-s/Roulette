using UnityEngine;

[ExecuteAlways]
public class GlobalManager : MonoBehaviour
{
    public GlobalSettingsWind settingsWind;
    public GlobalMaterialSettings materialSettings;

    public bool overWriteSmoothness;
    public bool overWriteTint;

    public void OnValidate()
    {
        Shader.SetGlobalFloat("_globalOverWriteTint", System.Convert.ToSingle(overWriteTint));
        Shader.SetGlobalFloat("_globalOverWriteSmoothness", System.Convert.ToSingle(overWriteSmoothness));
    }

    void Update()
    {
        if (materialSettings != null) materialSettings.SetGlobalMatSettings();
        if (settingsWind != null) settingsWind.SetGlobalWind();
    }
}