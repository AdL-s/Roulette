Shader "Custom/Items_Shader"
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
        _EmissionIntensity("Emission Intensity", Range(0,5)) = 0

        [Header(Dissolve)]
        [Toggle(DISSOLVE)]
        _UseDissolve("Enable Dissolve", Float) = 0

        _DissolveAmount("Dissolve Amount", Range(0,1)) = 0
        _SliceGuide("Dissolve Guide (R)", 2D) = "white" {}
        _BurnSize("Burn Size", Range(0.0, 0.2)) = 0.05
        _BurnRamp("Burn Ramp (RGB)", 2D) = "white" {}
        // Burn color is GLOBAL: GlobalMaterialSettings.dissolveColor (_globalDissolveColor)
        _BurnEmission("Burn Emission", Float) = 2.0
    }
    SubShader
    {
        Tags
        {
            "RenderType"="TransparentCutout" "Queue"="AlphaTest" "Group"="None"
        }
        Cull Off
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard addshadow fullforwardshadows
        #pragma shader_feature NORMAL_FIX
        #pragma shader_feature SMOOTHNESS_MAP
        #pragma shader_feature DISSOLVE
        #pragma target 4.0

        sampler2D _MainTex;
        half4 _Color;

        float _Smoothness;
        sampler2D _SmoothnessMap;

        float _Metallic;
        float _Cutoff;

        sampler2D _NormalMap;
        float _NormalStrength;

        float4 _Emission;
        float _EmissionIntensity;

        sampler2D _SliceGuide;
        sampler2D _BurnRamp;
        float _BurnSize;
        float _BurnEmission;
        half4 _globalDissolveColor;

        float _globalSmoothness;
        float _globalOverWriteSmoothness;

        half4 _globalTint;
        float _globalOverWriteTint;

        UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(float, _DissolveAmount)
        UNITY_INSTANCING_BUFFER_END(Props)

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_SliceGuide;
            float facing : VFACE;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            clip(c.a - _Cutoff);

            float3 emission = _Emission.rgb * _EmissionIntensity;

            #ifdef DISSOLVE
            float amount = UNITY_ACCESS_INSTANCED_PROP(Props, _DissolveAmount);
            float slice = amount * (1.0 + _BurnSize + 0.001);
            float d = tex2D(_SliceGuide, IN.uv_SliceGuide).r - slice;
            clip(d);

            float burnSize = max(_BurnSize, 1e-5);
            if (amount > 0 && d < burnSize)
            {
                emission += tex2D(_BurnRamp, float2(d / burnSize, 0)).rgb * _globalDissolveColor.rgb * _BurnEmission;
            }
            #endif

            o.Albedo = _globalOverWriteTint > 0.5 ? c.rgb * _globalTint.rgb : c.rgb;

            float3 n = UnpackNormal(tex2D(_NormalMap, IN.uv_MainTex));
            n.xy *= _NormalStrength;
            n = normalize(n);

            #ifdef NORMAL_FIX
            o.Normal = IN.facing > 0 ? n : -n;
            #else
            o.Normal = n;
            #endif

            float smooth = _Smoothness;
            #ifdef SMOOTHNESS_MAP
            smooth *= tex2D(_SmoothnessMap, IN.uv_MainTex).r;
            #endif
            o.Smoothness = _globalOverWriteSmoothness > 0.5 ? (_globalSmoothness + smooth) * 0.5 : smooth;

            o.Emission = emission;
            o.Metallic = _Metallic;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}