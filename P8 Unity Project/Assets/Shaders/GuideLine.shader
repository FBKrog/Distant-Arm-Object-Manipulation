Shader "Custom/GuideLine"
{
    Properties
    {
        [HDR] _LitColor     ("Lit Color",   Color)          = (0, 1, 1, 1)
        _UnlitColor         ("Unlit Color", Color)          = (0, 0.05, 0.05, 0.15)
        _FillProgress       ("Fill Progress", Range(0,1))   = 0
        _EdgeSoftness       ("Edge Softness", Range(0,0.2)) = 0.05
        _GlowIntensity      ("Glow Intensity", Float)       = 2.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            // Required for XR Single Pass Instanced rendering (OpenXR / Oculus)
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _LitColor;
                float4 _UnlitColor;
                float  _FillProgress;
                float  _EdgeSoftness;
                float  _GlowIntensity;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                // UV.x runs 0 (line start) → 1 (line end) in LineRenderer Stretch mode.
                // fillMask = 1 where the line is lit (behind the fill front), 0 where unlit.
                float fillMask = 1.0 - smoothstep(
                    _FillProgress - _EdgeSoftness,
                    _FillProgress + _EdgeSoftness,
                    IN.uv.x
                );

                float4 litFinal   = float4(_LitColor.rgb * _GlowIntensity, _LitColor.a);
                float4 finalColor = lerp(_UnlitColor, litFinal, fillMask);
                return finalColor;
            }
            ENDHLSL
        }
    }
}
