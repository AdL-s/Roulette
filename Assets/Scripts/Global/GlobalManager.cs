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
        Shader.SetGlobalFloat("_globalOverWriteTint",System.Convert.ToSingle(overWriteTint));
        Shader.SetGlobalFloat("_globalOverWriteSmoothness",System.Convert.ToSingle(overWriteSmoothness));
        
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!settingsWind && !materialSettings)
        {
            return;
        }
        
        materialSettings.SetGlobalMatSettings();
        settingsWind.SetGlobalWind();
    }
}
