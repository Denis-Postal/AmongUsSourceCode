Shader "Unlit/HortGradient"
{
	Properties
	{
		_MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Gradient Color", Color) = (0.52, 0.52, 0.52, 1)
		_Alpha ("Alpha", Range(0, 1)) = 0.96
		_EdgeFade ("Edge Fade", Range(0.001, 1)) = 0.08
		_CoreWidth ("Core Width", Range(0.001, 1)) = 0.24
		_CoreBoost ("Core Boost", Range(0, 1)) = 0.18
		_Rad ("Radius", Float) = 1
		_HortRad ("Horizontal Radius", Float) = 1
		_VertRad ("Vertical Radius", Float) = 0
	}

	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"PreviewType" = "Plane"
			"CanUseSpriteAtlas" = "True"
		}

		Cull Off
		Lighting Off
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				fixed4 color : COLOR;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				fixed4 color : COLOR;
			};

			sampler2D _MainTex;
			fixed4 _Color;
			float _Alpha;
			float _EdgeFade;
			float _CoreWidth;
			float _CoreBoost;
			float _HortRad;
			float _VertRad;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.color = v.color;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 tex = tex2D(_MainTex, i.uv);
				float2 fromCenter = abs(i.uv - 0.5) * 2.0;
				float axis = fromCenter.y;
				float coreAxis = abs(i.uv.y - 0.5);
				float edgeMask = smoothstep(0.0, 0.08, i.uv.x) * smoothstep(0.0, 0.08, 1.0 - i.uv.x);
				float fade = 1.0 - smoothstep(_EdgeFade, 0.92, axis);
				float core = 1.0 - smoothstep(0.0, _CoreWidth, coreAxis);
				float shade = saturate(fade + core * _CoreBoost);
				fixed4 color = _Color * i.color;
				color.rgb *= shade;
				color.a *= tex.a * fade * edgeMask * _Alpha;
				return color;
			}
			ENDCG
		}
	}
}
