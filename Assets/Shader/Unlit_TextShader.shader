Shader "Unlit/TextShader" {
	Properties {
		_MainTex ("Font Texture", 2D) = "white" {}
		_InputTex ("Input Texture", 2D) = "white" {}
		_ColorTex ("Input Colors", 2D) = "white" {}
		_Mask ("Mask", Float) = 0
		_UseClipRect ("Use Clip Rect", Float) = 0
		_ClipRect ("Clip Rect", Vector) = (-32767,-32767,32767,32767)
		_PlayerShadowClipEnabled ("Player Shadow Clip Enabled", Float) = 0
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4x4 unity_ObjectToWorld;
			float4x4 unity_MatrixVP;
			float4 _MainTex_ST;

			struct Vertex_Stage_Input
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct Vertex_Stage_Output
			{
				float2 uv : TEXCOORD0;
				float3 worldPos : TEXCOORD1;
				float4 pos : SV_POSITION;
			};

			Vertex_Stage_Output vert(Vertex_Stage_Input input)
			{
				Vertex_Stage_Output output;
				output.uv = (input.uv.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
				float4 worldPos = mul(unity_ObjectToWorld, input.pos);
				output.worldPos = worldPos.xyz;
				output.pos = mul(unity_MatrixVP, worldPos);
				return output;
			}

			Texture2D<float4> _MainTex;
			Texture2D<float4> _PlayerShadowClipTex;
			SamplerState sampler_MainTex;
			SamplerState sampler_PlayerShadowClipTex;
			float4x4 _PlayerShadowClipWorldToLocal;
			float _PlayerShadowClipEnabled;
			float4 _ClipRect;
			float _UseClipRect;

			struct Fragment_Stage_Input
			{
				float2 uv : TEXCOORD0;
				float3 worldPos : TEXCOORD1;
			};

			float4 frag(Fragment_Stage_Input input) : SV_TARGET
			{
				if (_PlayerShadowClipEnabled > 0.5)
				{
					float3 shadowLocal = mul(_PlayerShadowClipWorldToLocal, float4(input.worldPos, 1)).xyz;
					float2 shadowUv = shadowLocal.xy + 0.5;
					if (shadowUv.x >= 0 && shadowUv.y >= 0 && shadowUv.x <= 1 && shadowUv.y <= 1)
					{
						float4 shadow = _PlayerShadowClipTex.Sample(sampler_PlayerShadowClipTex, shadowUv);
						clip(dot(shadow.rgb, float3(0.299, 0.587, 0.114)) - 0.05);
					}
				}
				if (_UseClipRect > 0.5)
				{
					float2 insideMin = input.worldPos.xy - _ClipRect.xy;
					float2 insideMax = _ClipRect.zw - input.worldPos.xy;
					clip(min(min(insideMin.x, insideMin.y), min(insideMax.x, insideMax.y)));
				}
				return _MainTex.Sample(sampler_MainTex, input.uv.xy);
			}

			ENDHLSL
		}
	}
}
