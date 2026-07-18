Shader "Unlit/Stars" {
    Properties {
    }
    SubShader {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        LOD 200

        Pass {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct Vertex_Stage_Input {
                float4 pos : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Vertex_Stage_Output {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Vertex_Stage_Output vert(Vertex_Stage_Input input) {
                Vertex_Stage_Output output;
                output.pos = UnityObjectToClipPos(input.pos);
                output.uv = input.uv;
                return output;
            }

            float4 frag(Vertex_Stage_Output input) : SV_TARGET {
                float dist = length(input.uv);

                float core = smoothstep(0.55, 0.02, dist);
                float glow = smoothstep(1.0, 0.18, dist) * 0.72;
                float alpha = saturate(max(core, glow));

                clip(alpha - 0.01);
                return float4(1.0, 1.0, 1.0, alpha);
            }

            ENDHLSL
        }
    }
}
