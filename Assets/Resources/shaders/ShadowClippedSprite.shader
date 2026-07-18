Shader "Unlit/ShadowClippedSprite"
{
	Properties
	{
		_MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		_PlayerShadowClipEnabled ("Player Shadow Clip Enabled", Float) = 0
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
			float _PlayerShadowClipEnabled;
			sampler2D _PlayerShadowClipTex;
			float4x4 _PlayerShadowClipWorldToLocal;

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
				return tex2D(_MainTex, i.uv) * i.color;
			}
			ENDCG
		}
	}
}
