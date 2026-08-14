Shader "Hidden/Custom/ScreenEffectsLE"
{
    HLSLINCLUDE
    #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"

    TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
    float _Intensity;
    float _FogStart, _FogEnd;
    float _Density;
    float4 _Color;
    TEXTURE2D_SAMPLER2D(_CameraDepthTexture, sampler_CameraDepthTexture);

    float4 Frag(VaryingsDefault i) : SV_Target
    {
        float4 screenColor = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, i.texcoord, 0);
        float rawDepth = SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture,sampler_CameraDepthTexture,float4(i.texcoord,0,0),0 );
        
        float linearDepth = Linear01Depth(rawDepth);
        
        
        
        float fog = smoothstep(_FogStart,_FogEnd,linearDepth);
        
        float4 finalColor = lerp(screenColor, _Color , fog * _Density );
        

        return finalColor;
    }
    ENDHLSL
    
    SubShader
    {
    Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment Frag
            ENDHLSL
        }
    }
}