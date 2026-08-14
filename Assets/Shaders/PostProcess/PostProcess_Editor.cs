using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
[PostProcess(typeof(ScreenEffectsLERenderer), PostProcessEvent.AfterStack, "Custom/ScreenEffectsLE")]
public sealed class ScreenEffectsLE : PostProcessEffectSettings
{
    public FloatParameter intensity = new FloatParameter { value = 1f };
    public FloatParameter fogStart = new FloatParameter { value = 0f };
    public FloatParameter fogEnd = new FloatParameter { value = 1f };
    public FloatParameter density = new FloatParameter { value = 1f };
    public ColorParameter color = new ColorParameter { value = Color.gray };

    public override bool IsEnabledAndSupported(PostProcessRenderContext context)
    {
        // required or _CameraDepthTexture will be garbage
        context.camera.depthTextureMode |= DepthTextureMode.Depth;
        return enabled.value && intensity.value > 0f;
    }
}

public sealed class ScreenEffectsLERenderer : PostProcessEffectRenderer<ScreenEffectsLE>
{
    Shader shader;

    public override void Init()
    {
        shader = Shader.Find("Hidden/Custom/ScreenEffectsLE");
    }

    public override void Render(PostProcessRenderContext context)
    {
        var sheet = context.propertySheets.Get(shader);
        sheet.properties.SetFloat("_Intensity", settings.intensity.value);
        sheet.properties.SetFloat("_FogStart", settings.fogStart.value);
        sheet.properties.SetFloat("_FogEnd", settings.fogEnd.value);
        sheet.properties.SetFloat("_Density", settings.density.value);
        sheet.properties.SetColor("_Color", settings.color.value);

        context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
    }
}