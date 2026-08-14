Shader "Custom/Fish"
{
    Properties
    {
        [Header(Main)]
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1)
        _CutOff("Texture CutOff", Range(0,1)) = 0
        
        [Header(Smoothness)]
        [Toggle(SMOOTHNESS_MAP)] _SmoothnessVar("Smoothness Variable", Float) = 0.5
        _SmoothnessMap("Smoothness Map", 2D) = "white" {}
        _Smoothness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.5
        
        [Header(Normal)]
        [Toggle(NORMAL_FIX)]
        _NormalVar("NormalVariable", Float) = 0.5
        _NormalMap("Normal Map", 2D) = "bump" {}
        _NormalStrength("Normal Strength", Range(0,1)) = 1
        
        
        [Header(Swimming Settings)]
        _SwimSpeed("Swim Speed", Float) = 5
        _SwimStrength("Swim Strength",Float) = 5
        
        
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue" = "Geometry"}
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma shader_feature SMOOTHNESS_MAP
        #pragma shader_feature NORMAL_FIX
        #pragma target 4.0

        sampler2D _MainTex;
        sampler2D _SmoothnessMap;
        sampler2D _NormalMap;

        struct Input
        {
            float2 uv_MainTex;
            float facing : VFACE;
        };
        
        float _Smoothness;
        half _Metallic;
        fixed4 _Color;
        float _NormalStrength;
        float _SwimSpeed;
        float _SwimStrength;
        float _CutOff;
        
        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o)
            
             float3 originWS = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
            float instanceSeed  = frac(sin(dot(originWS.xz, float2(12.9898, 78.233))) * 43758.5453);
            float instancePhase = instanceSeed * 6.28318530718;

            float basePhase = _Time.y * _SwimSpeed + instancePhase;
    
            float swim = sin(basePhase) * _SwimStrength;

            float offsetSwim = swim * v.color.g; // v.color.g as painted per-vertex weight (e.g. tail moves more than head)

            v.vertex.x += offsetSwim;
            
        }


        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            
            float3 n = UnpackNormal(tex2D(_NormalMap, IN.uv_MainTex));
            n.xy *= _NormalStrength;
            n = normalize(n);
            
            #ifdef NORMAL_FIX
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
            
            #ifdef SMOOTHNESS_MAP
            float smoothnessMap = tex2D(_SmoothnessMap,IN.uv_MainTex);
            o.Smoothness = smoothnessMap * _Smoothness;
            
            
            #else
            
            o.Smoothness = _Smoothness;
            
            #endif
            
            o.Metallic = _Metallic;
            
            
            o.Alpha = c.a;
            clip(c.a - _CutOff);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
