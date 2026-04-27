Shader "Custom/TMPOverlay"
{
    Properties
    {
        _FaceColor ("Face Color", Color) = (1,1,1,1)
        _MainTex ("Font Atlas", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        ZTest Always
        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float2 uv       : TEXCOORD0;
                float4 color    : COLOR;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float4 color    : COLOR;
            };

            sampler2D _MainTex;
            float4 _FaceColor;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _FaceColor;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                float sdf = tex2D(_MainTex, i.uv).a;

                float alpha = smoothstep(0.5 - 0.1, 0.5 + 0.1, sdf);

                float4 col = float4(i.color.rgb, 1.0);
                col.a *= alpha;
                col *= i.color;
                
                return col;;
            }

            ENDHLSL
        }
    }
}