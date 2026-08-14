Shader "Custom/Blur"
{
    Properties
    {
        [PerRendererData]  _MainTex ("Texture", 2D) = "white" {}
        _Size("Blur Radius", Range(0,8)) = 3
    }
    SubShader
    {
        Tags { 
            "RenderType"="Transparent" 
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        GrabPass{
            Tags{"LightMode"="Always"}
        }

        Pass
        {
            // horizontal pass
            Tags{"LightMode"="Always"}

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma fragmentoption ARB_precision_hint_fastest

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 uvgrab : TEXCOORD0;    
                float4 vertex : POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                #if UNITY_UV_STARTS_AT_TOP
                float scale = -1.0;
                #else
                float scale = 1.0;
                #endif
                o.uvgrab.xy = (float2(o.vertex.x, o.vertex.y * scale) + o.vertex.w) * 0.5;
                o.uvgrab.zw = o.vertex.zw;
                return o;
            }

            sampler2D _GrabTexture;
            float4 _GrabTexture_TexelSize;
            float _Size;

            fixed4 frag (v2f i) : Color
            {
                half4 sum = half4(0,0,0,0);
                #define BLURPIXEL(weight,kernelx) tex2Dproj(_GrabTexture, UNITY_PROJ_COORD(float4(i.uvgrab.x + _GrabTexture_TexelSize.x * kernelx * _Size, i.uvgrab.y, i.uvgrab.z, i.uvgrab.w))) * weight;
                sum += BLURPIXEL(0.05, -4.0);
                sum += BLURPIXEL(0.09, -3.0);
                sum += BLURPIXEL(0.12, -2.0);
                sum += BLURPIXEL(0.15, -1.0);
                sum += BLURPIXEL(0.18, 0.0);         
                sum += BLURPIXEL(0.15, +1.0);
                sum += BLURPIXEL(0.12, +2.0);
                sum += BLURPIXEL(0.09, +3.0);
                sum += BLURPIXEL(0.05, +4.0);
                return sum;
            }
            ENDCG
        }
           GrabPass{
            Tags{"LightMode"="Always"}
        }

         Pass
        {
            // vertical pass
            Tags{"LightMode"="Always"}

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma fragmentoption ARB_precision_hint_fastest

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 uvgrab : TEXCOORD0;    
                float4 vertex : POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                #if UNITY_UV_STARTS_AT_TOP
                float scale = -1.0;
                #else
                float scale = 1.0;
                #endif
                o.uvgrab.xy = (float2(o.vertex.x, o.vertex.y * scale) + o.vertex.w) * 0.5;
                o.uvgrab.zw = o.vertex.zw;
                return o;
            }

            sampler2D _GrabTexture;
            float4 _GrabTexture_TexelSize;
            float _Size;

            fixed4 frag (v2f i) : Color
            {
                half4 sum = half4(0,0,0,0);
                #define BLURPIXEL(weight,kernely) tex2Dproj(_GrabTexture, UNITY_PROJ_COORD(float4(i.uvgrab.x, i.uvgrab.y  + _GrabTexture_TexelSize.y * kernely * _Size, i.uvgrab.z, i.uvgrab.w))) * weight;
                sum += BLURPIXEL(0.05, -4.0);
                sum += BLURPIXEL(0.09, -3.0);
                sum += BLURPIXEL(0.12, -2.0);
                sum += BLURPIXEL(0.15, -1.0);
                sum += BLURPIXEL(0.18, 0.0);         
                sum += BLURPIXEL(0.15, +1.0);
                sum += BLURPIXEL(0.12, +2.0);
                sum += BLURPIXEL(0.09, +3.0);
                sum += BLURPIXEL(0.05, +4.0);
                return sum;
            }
            ENDCG
        }
    }
}