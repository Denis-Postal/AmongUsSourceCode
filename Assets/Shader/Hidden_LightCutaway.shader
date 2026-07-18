Shader "Hidden/LightCutaway" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		_LightRadius ("Light Radius", Float) = 0.2
		_VignetteStart ("Vignette Start", Float) = 0.55
		_ShadowPixelSize ("Shadow Pixel Size", Float) = 0
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
				float4 pos : SV_POSITION;
			};

			Vertex_Stage_Output vert(Vertex_Stage_Input input)
			{
				Vertex_Stage_Output output;
				output.uv = (input.uv.xy * _MainTex_ST.xy) + _MainTex_ST.zw;
				output.pos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, input.pos));
				return output;
			}

			Texture2D<float4> _MainTex;
			SamplerState sampler_MainTex;
			float _LightRadius;
			float _VignetteStart;
			float _ShadowPixelSize;

			struct Fragment_Stage_Input
			{
				float2 uv : TEXCOORD0;
			};

			float4 frag(Fragment_Stage_Input input) : SV_TARGET
			{
				if (_ShadowPixelSize > 1.0)
				{
					input.uv.xy = (floor(input.uv.xy * _ShadowPixelSize) + 0.5) / _ShadowPixelSize;
				}
				float radius = max(_LightRadius, 0.001);
				float dist = length(input.uv.xy) / radius;
				float light = 1.0 - smoothstep(_VignetteStart, 1.0, dist);
				return _MainTex.Sample(sampler_MainTex, input.uv.xy) * light;
			}

			ENDHLSL
		}
	}
}
