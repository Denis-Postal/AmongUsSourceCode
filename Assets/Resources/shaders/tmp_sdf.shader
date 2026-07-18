Shader "TextMeshPro/Distance Field"
{
    Properties
    {
        [PerRendererData] _MainTex ("Font Atlas", 2D) = "white" {}
        _FaceColor ("Face Color", Color) = (1,1,1,1)
        _FaceDilate ("Face Dilate", Range(-1,1)) = 0
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Thickness", Range(0,1)) = 0
        _OutlineSoftness ("Outline Softness", Range(0,1)) = 0
        _WeightNormal ("Weight Normal", Float) = 0
        _WeightBold ("Weight Bold", Float) = 0.5
        _ScaleRatioA ("Scale RatioA", Float) = 1
        _VertexOffsetX ("Vertex OffsetX", Float) = 0
        _VertexOffsetY ("Vertex OffsetY", Float) = 0
        _GradientScale ("Gradient Scale", Float) = 5
        _Sharpness ("Sharpness", Float) = 0
        _CullMode ("Cull Mode", Float) = 0
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _ZWrite ("Depth Write", Float) = 0
        _UseClipRect ("Use Clip Rect", Float) = 0
        _ClipRect ("Clip Rect", Vector) = (-32767,-32767,32767,32767)
        _PlayerShadowClipEnabled ("Player Shadow Clip Enabled", Float) = 0
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull [_CullMode]
        Lighting Off
        ZWrite [_ZWrite]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            sampler2D _PlayerShadowClipTex;
            float4 _MainTex_ST;
            fixed4 _FaceColor;
            fixed4 _OutlineColor;
            fixed4 _RendererColor;
            float4x4 _PlayerShadowClipWorldToLocal;
            float _PlayerShadowClipEnabled;
            float4 _ClipRect;
            float _UseClipRect;
            float _FaceDilate;
            float _OutlineWidth;
            float _OutlineSoftness;
            float _ScaleRatioA;
            float _VertexOffsetX;
            float _VertexOffsetY;
            float _GradientScale;
            float _Sharpness;

            v2f vert(appdata_t v)
            {
                v2f o;
                v.vertex.x += _VertexOffsetX;
                v.vertex.y += _VertexOffsetY;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _FaceColor * _RendererColor;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                if (_PlayerShadowClipEnabled > 0.5)
                {
                    float3 shadowLocal = mul(_PlayerShadowClipWorldToLocal, float4(i.worldPos, 1)).xyz;
                    float2 shadowUv = shadowLocal.xy + 0.5;
                    if (shadowUv.x >= 0 && shadowUv.y >= 0 && shadowUv.x <= 1 && shadowUv.y <= 1)
                    {
                        fixed4 shadow = tex2D(_PlayerShadowClipTex, shadowUv);
                        clip(dot(shadow.rgb, fixed3(0.299, 0.587, 0.114)) - 0.05);
                    }
                }
                if (_UseClipRect > 0.5)
                {
                    float2 insideMin = i.worldPos.xy - _ClipRect.xy;
                    float2 insideMax = _ClipRect.zw - i.worldPos.xy;
                    clip(min(min(insideMin.x, insideMin.y), min(insideMax.x, insideMax.y)));
                }
                float sdf = tex2D(_MainTex, i.texcoord).a + (_FaceDilate * 0.1);
                float width = max(fwidth(sdf), 1.0 / max(_GradientScale, 1.0));
                width = max(width + (_OutlineSoftness * 0.05), 0.001);

                float face = smoothstep(0.5 - width, 0.5 + width, sdf);
                float outlineSize = max(_OutlineWidth * _ScaleRatioA * 0.5, 0.0);
                float outline = smoothstep(0.5 - outlineSize - width, 0.5 - outlineSize + width, sdf);

                fixed4 outlineColor = _OutlineColor;
                outlineColor.a *= i.color.a;

                fixed4 color = outlineSize > 0.001 ? lerp(outlineColor, i.color, face) : i.color;
                color.a = outlineSize > 0.001 ? max(outline * outlineColor.a, face * i.color.a) : face * i.color.a;
                return color;
            }
            ENDCG
        }
    }
}
