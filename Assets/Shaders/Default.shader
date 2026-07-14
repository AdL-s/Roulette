Shader "Custom/Default"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        [HDR]_Color ("Color", Color) = (1,1,1,1)
                
        [Header(Smoothness)]
        [Toggle(SMOOTHNESS_MAP)]
        SmoothnessVar("Smoothness Variable", Range(0,1)) = 0.5
        _SmoothnessMap("Smoothness Map", 2D) = "white" {}
                
        _Smoothness("Smoothness",Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.5
        _Cutoff("Texture Cutoff", Range(0.0,1.0)) = 0.5
        
        [Header(Normals)]
        [Toggle(NORMAL_FIX)]
        _NormalVar("Normal Variable", Float) = 0.5
        _NormalMap("Normal Map", 2D) = "bump" {}
        _NormalStrength("Normal Strength",Range(0,1)) = 1
                
        [Header(Emission)]
        [HDR]_Emission("Emission", Color) = (0,0,0,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue" = "AlphaTest" "Group" = "None"}
        Cull Off
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard addshadow  fullforwardshadows  
        #pragma shader_feature NORMAL_FIX
        #pragma shader_feature SMOOTHNESS_MAP
        #pragma target 4.0

       sampler2D _MainTex;
        half4 _Color;
        
        float _Smoothness;
        sampler2D _SmoothnessMap;
        
        float _Metallic;
        float _Cutoff;
        
        float _NormalVariable;
        sampler2D _NormalMap;
        float _NormalStrength;
        
        float4 _Emission;
        
        float _globalSmoothness;
        float _globalOverWriteSmoothness;
        
        half4 _globalTint;
        float _globalOverWriteTint;

        struct Input
        {
            float2 uv_MainTex;
            float facing : VFACE;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            
            float3 n = UnpackNormal(tex2D(_NormalMap, IN.uv_MainTex));
            n.xy *= _NormalStrength;
            n = normalize(n);
            
           o.Albedo = _globalOverWriteTint > 0.5 ? c.rgb * _globalTint : c.rgb; 
            
           
            
            #ifdef  NORMAL_FIX
            if (IN.facing > 0)
            {
                o.Normal = n;
            }
            else
            {
                o.Normal = -n ;
            }
            #else
            o.Normal = n;
            
            #endif
            
            //o.Emission = _Emission;
            #ifdef SMOOTHNESS_MAP
                float smoothnessMap = tex2D(_SmoothnessMap, IN.uv_MainTex);
                o.Smoothness = _globalOverWriteSmoothness > 0.5 ? (_globalSmoothness + _Smoothness * smoothnessMap) / 2 : _Smoothness * smoothnessMap;
                
                #else 
                    
                o.Smoothness = _globalOverWriteSmoothness > 0.5 ? (_globalSmoothness + _Smoothness) / 2 : _Smoothness;
                
            #endif
        
           
            o.Metallic = _Metallic;
            o.Alpha = c.a;
            clip(c.a - _Cutoff);
            
        }
        ENDCG
    }
    FallBack "Diffuse"
}
