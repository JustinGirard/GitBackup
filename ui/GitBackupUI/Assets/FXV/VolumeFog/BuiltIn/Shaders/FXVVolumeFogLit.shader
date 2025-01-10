Shader "FXV/FXVVolumeFogLit"
{
	Properties
	{
		[HideInInspector] _FogType("__fogtype", Float) = 0.0
		[HideInInspector] _FogFalloffType("__fogfallofftype", Float) = 0.0
		[HideInInspector] _FogFalloff("__fogfalloff", Float) = 1.0
		[HideInInspector][Toggle] _InAirEnabled("__inairenabled", Int) = 0
		[HideInInspector] _BlendMode("__mode", Float) = 0.0
		[HideInInspector] _SrcBlend("__src", Float) = 1.0
		[HideInInspector] _DstBlend("__dst", Float) = 0.0
		[HideInInspector] _ZWrite("__zw", Float) = 1.0
		[HideInInspector][Toggle] _AxisFadeEnabled("__axisfade", Float) = 0.0
	}
	
	Subshader
	{
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		LOD 300

		Blend[_SrcBlend][_DstBlend]
		ZWrite[_ZWrite]

		CGPROGRAM

		#pragma shader_feature_local FXV_FOGTYPE_VIEWALIGNED FXV_FOGTYPE_SPHERICALPOS FXV_FOGTYPE_SPHERICALDIST FXV_FOGTYPE_BOXPOS FXV_FOGTYPE_BOXDIST FXV_FOGTYPE_BOXEXPERIMENTAL FXV_FOGTYPE_HEIGHT FXV_FOGTYPE_HEIGHTXBOX FXV_FOGTYPE_INVERTEDSPHERICAL FXV_FOGTYPE_INVERTEDSPHERICALXHEIGHT FXV_FOGTYPE_HEIGHTXVIEW FXV_FOGTYPE_BOXXVIEW
		#pragma shader_feature_local FXV_IN_AIR_FOG __
		#pragma shader_feature_local FXV_LINEAR_FALLOFF FXV_SMOOTHED_FALLOFF FXV_EXP_FALLOFF FXV_EXP2_FALLOFF
		#pragma shader_feature_local FXV_FOG_CUSTOM_MESH __
		#pragma shader_feature_local FXV_FOG_CLIP_SKYBOX FXV_FOG_CLIP_BOUNDS __

		#pragma surface surf VolumeFog vertex:vert alpha:blend nolightmap noshadow noshadowmask 
		#pragma target 3.5

#define FXV_VOLUMEFOG_BUILTIN 

#if defined(FXV_VOLUMEFOG_BUILTIN)

        #include "UnityCG.cginc"
		#include "AutoLight.cginc"
		#include "UnityPBSLighting.cginc"

		#include "../../Shaders/FXVVolumeFog.cginc"

		struct appdata 
		{
			FXV_VOLUMETRIC_FOG_LIT_APPDATA
		};

		struct Input 
		{
			FXV_VOLUMETRIC_FOG_V2F_COORDS
			INTERNAL_DATA
		};

		float3 _worldPixelPosition;
		float3 _viewDirWS;
		float3 _viewDirOriginWS;
		fxvFogData _fxvFogData;

		half4 LightingVolumeFog(SurfaceOutputStandard s, half3 viewDir, UnityGI gi)
		{
			float4 c;

#if defined (POINT) || defined (POINT_COOKIE)
			float3 invLightRangeV = float3(unity_WorldToLight[0][0], unity_WorldToLight[1][0], unity_WorldToLight[2][0]);
			float invLightRangeSqr = dot(invLightRangeV, invLightRangeV);
#else
			float invLightRangeSqr = 1.0;
#endif

			half isOrtho = unity_OrthoParams.w;

			fxvLightingData fxvLightData = (fxvLightingData)0;
            fxvLightData.pixelPositionWS = _worldPixelPosition;
            fxvLightData.objectPositionWS = _FXV_ObjectToWorldPos(float3(0,0,0));
            fxvLightData.lightPositionWS = _WorldSpaceLightPos0;
            fxvLightData.lightRangeAttenuation = float2(invLightRangeSqr, 0.0);
			fxvLightData.lightDirectionWS = gi.light.dir;
			fxvLightData.lightColor = _LightColor0.rgb;
			if (isOrtho == 1)
			{
				fxvLightData.viewDirectionOriginWS = _viewDirOriginWS;
				fxvLightData.viewDirectionWS = -normalize(_viewDirWS);
			}
			else
			{
				fxvLightData.viewDirectionOriginWS = _WorldSpaceCameraPos.xyz;
				fxvLightData.viewDirectionWS = viewDir;	// this is the same as in ortho but we skip normalization for optimalization this way
			}
			fxvLightData.normalWS = s.Normal;
			fxvLightData.albedo = s.Albedo;
            fxvLightData.alpha = s.Alpha;


			c.rgb = _FXV_FogLightingFunction(fxvLightData, _fxvFogData);

			c.rgb += gi.indirect.diffuse * s.Albedo;
			c.a = s.Alpha;

			return c;
		}

		void LightingVolumeFog_GI (SurfaceOutputStandard s, UnityGIInput data, inout UnityGI gi)
		{
			s.Normal = float3(0,1,0);
			//    ResetUnityGI(gi);
			LightingStandard_GI(s, data, gi);
		}

		// we use appdata_tan here as it needs position var named 'vertex'
		void vert (inout appdata_tan v, out Input o) 
		{
			UNITY_INITIALIZE_OUTPUT(Input, o);

			FXV_VOLUMETRIC_FOG_VERTEX_DEFAULT_LIT(v, o);
        }

		void surf(Input i, inout SurfaceOutputStandard o)
		{
			_FXV_GetViewRayOriginAndDirWS_fromPositionOS(i.positionOS, _viewDirOriginWS, _viewDirWS);

			_fxvFogData = _FXV_CalcVolumetricFog(i.positionWS, _viewDirOriginWS, _viewDirWS, i.depth, i.screenPosition);

			float4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);

			_worldPixelPosition = i.positionWS; 
			o.Albedo = color;
			o.Alpha = color.a * _fxvFogData.fogT;
		}
#else
		struct appdata 
		{
			float3 positionOS : POSITION;
		};

		struct Input 
		{
			float4 vert : SV_POSITION;
			INTERNAL_DATA
		};

		half4 LightingVolumeFog(SurfaceOutputStandard s, half3 viewDir, UnityGI gi)
		{
			return half4(0,0,0,0);
		}

		void LightingVolumeFog_GI (SurfaceOutputStandard s, UnityGIInput data, inout UnityGI gi)
		{

		}

		void vert (inout appdata_tan v, out Input o) 
		{
			UNITY_INITIALIZE_OUTPUT(Input, o);
        }

		void surf(Input i, inout SurfaceOutputStandard o)
		{
			o.Albedo = float3(0,0,0);
			o.Alpha = 1.0;
		}
#endif

		ENDCG
	}
	CustomEditor "FXV.fxvVolumeFogShaderInspector"

	Fallback "VertexLit"
}
