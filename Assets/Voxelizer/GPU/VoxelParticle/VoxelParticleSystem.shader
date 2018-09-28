Shader "Hidden/VoxelParticleSystem"
{
	Properties
	{
		_Color ("Color", Color) = (1,1,1,1)
		_Metallic ("Metallic", Range(0, 1)) = 0.0
		_Glossiness ("Smoothness", Range(0, 1)) = 0.5
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 200

		CGPROGRAM

		#pragma target 4.5
		#pragma surface surf Standard vertex:vert addshadow
		#pragma instancing_options procedural:setup

		#include "UnityCG.cginc"
		#include "./VoxelParticle.cginc"

		struct Input
		{
			float4 color;
		};

		float4 _Color;
		half _Metallic;
		half _Glossiness;

		#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
		StructuredBuffer<VoxelParticle> _ParticleBuffer;
		#endif

		void setup()
		{
		}

		void vert(inout appdata_full v, out Input data)
		{
			data.color = _Color;

			#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
			
			uint id = unity_InstanceID;
			VoxelParticle particle = _ParticleBuffer[id];

			float4x4 m = (float4x4)0;
			m._11_22_33_44 = float4(particle.scale, 1);
			m._14_24_34 = particle.position;

			v.vertex = mul(m, v.vertex);
			v.normal = normalize(mul(m, v.normal));

			#endif
		}

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			o.Albedo = IN.color;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
		}

		ENDCG
	}
}