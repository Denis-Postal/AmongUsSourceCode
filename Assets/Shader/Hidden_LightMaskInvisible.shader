Shader "Hidden/LightMaskInvisible" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		_LightRadius ("Light Radius", Float) = 0.2
	}
	SubShader {
		Tags {
			"Queue"="Geometry"
			"RenderType"="Opaque"
		}
		LOD 100

		Cull Off
		Lighting Off
		ZWrite Off
		ColorMask 0

		Pass {
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4x4 unity_ObjectToWorld;
			float4x4 unity_MatrixVP;

			struct Vertex_Stage_Input {
				float4 pos : POSITION;
			};

			struct Vertex_Stage_Output {
				float4 pos : SV_POSITION;
			};

			Vertex_Stage_Output vert(Vertex_Stage_Input input) {
				Vertex_Stage_Output output;
				output.pos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, input.pos));
				return output;
			}

			float4 frag(Vertex_Stage_Output input) : SV_TARGET {
				return 0;
			}
			ENDHLSL
		}
	}
}
