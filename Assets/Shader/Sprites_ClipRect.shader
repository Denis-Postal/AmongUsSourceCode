Shader "Sprites/ClipRect"
{
	Properties
	{
		_MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		_BackColor ("Shadow Color", Color) = (1,0,0,1)
		_BodyColor ("Body Color", Color) = (1,1,0,1)
		_VisorColor ("Visor Color", Color) = (0,1,1,1)
		_UsePlayerColors ("Use Player Colors", Float) = 0
		_UseClipRect ("Use Clip Rect", Float) = 0
		_ClipRect ("Clip Rect", Vector) = (-32767,-32767,32767,32767)
	}
	SubShader
	{
		Tags
		{
			"Queue"="Transparent"
			"IgnoreProjector"="True"
			"RenderType"="Transparent"
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
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
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float4 color : COLOR;
				float2 uv : TEXCOORD0;
				float3 worldPos : TEXCOORD1;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed4 _Color;
			fixed4 _BackColor;
			fixed4 _BodyColor;
			fixed4 _VisorColor;
			float _UsePlayerColors;
			float _UseClipRect;
			float4 _ClipRect;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.color = v.color * _Color;
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				if (_UseClipRect > 0.5)
				{
					float2 insideMin = i.worldPos.xy - _ClipRect.xy;
					float2 insideMax = _ClipRect.zw - i.worldPos.xy;
					clip(min(min(insideMin.x, insideMin.y), min(insideMax.x, insideMax.y)));
				}
				fixed4 tex = tex2D(_MainTex, i.uv);
				if (_UsePlayerColors > 0.5)
				{
					fixed mx = max(tex.r, max(tex.g, tex.b));
					fixed mn = min(tex.r, min(tex.g, tex.b));
					fixed4 playerColor = fixed4(saturate(_BodyColor.rgb * tex.r + _VisorColor.rgb * tex.g + _BackColor.rgb * tex.b), tex.a);
					if (mx < 0.001 || abs(1 - mn / mx) < .45)
					{
						playerColor.rgb = tex.rgb;
					}
					return playerColor * i.color;
				}
				return tex * i.color;
			}
			ENDCG
		}
	}
}
