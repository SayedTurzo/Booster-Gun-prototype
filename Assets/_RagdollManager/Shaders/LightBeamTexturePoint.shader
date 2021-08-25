// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/LightBeamTexturePoint" 
{
	Properties 
	{
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_FadeDist("Fade Distance", Float ) = 12
		_TimeXInc("Time movement x", Float) = 0.01
		__TimeYInc("Time movement y", Float) = 0.02
		_LerpStart("Lerp start", Float) = -0.5
		_LerpEnd("Lerp end", Float) = 2.5
	}
	SubShader 
	{
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		LOD 200

		Pass
		{
			Name "FORWARD" 
			Tags { "LightMode" = "ForwardBase" }
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off  


			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			fixed4 _Color;
			float _FadeDist;
			sampler2D _MainTex;
			float _TimeXInc;
			float _TimeYInc;
			float _LerpStart;
			float _LerpEnd;

			struct Input
			{
				float4 pos : POSITION;
				float3 normal : NORMAL;
				float2 uv : TEXCOORD0;
			};

			struct v2f 
			{
				float4 pos : SV_POSITION;
				float4 posWS : TEXCOORD0;
				float3 modelPos : TEXCOORD1;
				float3 normalWS : TEXCOORD2;
				float2 uv : TEXCOORD3;
			};

			v2f vert(Input In)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(In.pos);

				float4 posWS = mul( unity_ObjectToWorld, In.pos);
				o.posWS = posWS;

				float3 f;
				f.x = unity_ObjectToWorld[0].w;
				f.y = unity_ObjectToWorld[1].w;
				f.z = unity_ObjectToWorld[2].w;
				o.modelPos = f;

				o.normalWS = mul(unity_ObjectToWorld, float4( In.normal, 0));

				o.uv = In.uv;

				return o;
			}

			void frag(v2f In, out fixed4 OUT : SV_Target)
			{
				float2 uv = In.uv ;
				uv.x += _Time.y * _TimeXInc;
				uv.y -= _Time.y * _TimeYInc;

				fixed4 col = tex2D(_MainTex, uv) * _Color;

				float fadeStart = 0;
				float fadeEnd = _FadeDist;
				float d = length(In.posWS.xyz - In.modelPos); 
				float fade = 1 - saturate((d - fadeStart) / (fadeEnd - fadeStart));
				// exponential
				fade *= fade;

				float3 dir2Cam =  _WorldSpaceCameraPos.xyz - In.posWS.xyz;
				dir2Cam = normalize(dir2Cam) ;
				float3 normal = In.normalWS; 
				float dotVal = max(0.0, dot(normalize(normal), dir2Cam));
				fade *= max( 0.0f,lerp(_LerpStart, _LerpEnd, dotVal ));

				OUT = fixed4(_Color.rgb + col.rgb, fade);
			}

			ENDCG
		}
	}
	Fallback "Transparent/VertexLit"
}
