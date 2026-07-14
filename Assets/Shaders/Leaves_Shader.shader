Shader "Custom/Leaves_Shader"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        [HDR]_Color ("Color", Color) = (1,1,1,1)

        [Header(Smoothness)]
        [Toggle(SMOOTHNESS_MAP)]
        _SmoothnessVar("Smoothness Variable", Range(0,1)) = 0.5
        _SmoothnessMap("Smoothness Map", 2D) = "white" {}

        _Smoothness("Smoothness",Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.5
        //_LightCutoff("Light Cutoff", Range(0,1)) = 0.5
        _Cutoff("Texture Cutoff", Range(0.0,1.0)) = 0.5
        _Height("Height (y)", Float) = 1
        _Width("Width (x)", float )= 1
        _Length("Length (z)", float) = 1


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
        Tags
        {
            "RenderType"="TransparentCutOut" "Queue" = "AlphaTest" "Group" = "None"
        }
        Cull Off
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard addshadow  fullforwardshadows  vertex:vert
        #pragma shader_feature NORMAL_FIX
        #pragma shader_feature SMOOTHNESS_MAP
        #pragma target 4.0


        sampler2D _MainTex;
        half4 _Color;

        float _Smoothness;
        sampler2D _SmoothnessMap;

        float _Metallic;
        float _Cutoff;
        float _Height;
        float _Width;
        float _Length;

        float _NormalVariable;
        sampler2D _NormalMap;
        float _NormalStrength;

        float4 _Emission;

        float _globalWindStrength, _globalWindScale;
        sampler2D _globalWindTexture;

        float _globalWindStrength2, _globalWindScale2;
        sampler2D _globalWindTexture2;

        float2 _globalWindDirection;


        float _globalSmoothness;
        float _globalOverWriteSmoothness;


        half4 _globalTint;
        float _globalOverWriteTint;


        struct Input
        {
            float2 uv_MainTex;
            float3 vertCoords;
            float coeY;
            float facing : VFACE;
        };

        void vert(inout appdata_full v, out Input o)
        {
                UNITY_INITIALIZE_OUTPUT(Input, o);
            float3 wpos = mul(unity_ObjectToWorld, v.vertex).xyz;

            float coeX = saturate(abs(v.vertex.x) / (_Width * 0.5));
            float coeZ = saturate(abs(v.vertex.z) / (_Length * 0.5));
            float coeY = saturate(v.vertex.y / _Height);
            o.coeY = coeY;

            float flex = max(coeY, max(coeX, coeZ));

            float2 windDir = normalize(_globalWindDirection + float2(0.0001, 0.0));
            float phase = dot(wpos.xz, float2(0.13, 0.17));

          
            float2 uv1 = wpos.xz / max(_globalWindScale, 0.0001) - windDir * _Time.x * 0.5;
            float waveMask = tex2Dlod(_globalWindTexture, float4(uv1, 0, 0)).r; 
            float bigWave = sin(_Time.y * 1.0 + phase) * waveMask; 

         
            float2 uv2 = wpos.xz / max(_globalWindScale2, 0.0001) - windDir * _Time.x * 2.0;
            float constantWind = tex2Dlod(_globalWindTexture2, float4(uv2, 0, 0)).r * 2.0 - 1.0;

            float sway = bigWave * _globalWindStrength + constantWind * _globalWindStrength2;

            wpos.xz += windDir * sway * flex;
            wpos.y -= abs(sway) * flex * 0.15;

            v.vertex.xyz = mul(unity_WorldToObject, float4(wpos, 1.0)).xyz;
            o.vertCoords = v.vertex.xyz;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
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
                o.Normal = -n;
            }
            #else
            o.Normal = n;

            #endif

            //o.Emission = _Emission;
            #ifdef SMOOTHNESS_MAP
            float smoothnessMap = tex2D(_SmoothnessMap, IN.uv_MainTex);
            o.Smoothness = _globalOverWriteSmoothness > 0.5
                                                                            ? (_globalSmoothness + _Smoothness *
                                                                                smoothnessMap) / 2
                                                                            : _Smoothness * smoothnessMap;

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