Shader "Custom/Particles_shader"
{
    Properties
    {   [Header(Main)]
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Alphacutoff("Alpha Cutoff", Range(0,1)) = 0.5
        
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        [HDR]_EmissionColor("Emission Color",Color) = (1,1,1,1)
        _EmissionIntensity ("Emission",Range(0,5)) = 0.0
        
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _TintMask("Tint Mask",2D) = "white" {}
        _TintColor("Tint Color", Color) = (0,1,0)
        [Space]
        [Toggle(NORMAL_ON)]_NormalON("Normal ON", Float) = 0
        _NormalMap("Normal Map", 2D) = "white" {}
        
        
        
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        LOD 200

        CGPROGRAM
       #pragma surface surf Standard fullforwardshadows  addshadow
        #pragma shader_feature CANCEL_OVER_WRITE_COLORS
        #pragma shader_feature NORMAL_ON
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _NormalMap;
        sampler2D _TintMask;
     

        struct Input
        {
            float2 uv_MainTex;
        };
       
        
        float _Alphacutoff;
        half _Smoothness;
        half _Metallic;
        float _EmissionIntensity;
        half4 _EmissionColor;
       half4 _TintColor;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
            float3 normalMap = UnpackNormal(tex2D(_NormalMap,IN.uv_MainTex));
            
            o.Alpha = c.a;
            clip(c.a - _Alphacutoff);
            
 
            
            #ifdef NORMAL_ON
            o.Normal = normalMap;
            #endif
            
        
            float4 tintMask = tex2D(_TintMask,IN.uv_MainTex);
            _TintColor*= tintMask;
            c *= _TintColor;
            
            o.Albedo = c.rgb ;
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
            o.Emission = _EmissionIntensity * c.rgb * _EmissionColor;
        }
        ENDCG
    }
    FallBack "Diffuse"
    CustomEditor "EditorPart.Editor_Particles"
}
