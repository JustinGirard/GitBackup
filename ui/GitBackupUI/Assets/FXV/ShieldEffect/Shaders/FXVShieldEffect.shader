Shader "FXV/FXVShieldEffect" 
{
	Properties 
	{
		_RimTexture("Albedo (RGB)", 2D) = "white" {}

		_NormalCorrection("NormalCorrection", Range(0.0,1.0)) = 0
		_GlobalIntensity ("GlobalIntensity", Range(0.0,16)) = 1
		_GlobalAlphaCurve ("GlobalAlphaCurve", Range(0.01,4)) = 1
		_BackfacesIntensity ("BackfacesIntensity", Range(0.01,2)) = 1 


		_Color ("Color", Color) = (1,1,1,1)
		_ColorRimMin("ColorRimMin", Range(-2,1)) = 0.6
		_ColorRimMax("ColorRimMax", Range(-1,4)) = 1.0
		_ColorRimHitInfluence("ColorRimHitInfluence", Range(0,1)) = 1.0

		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_TextureColor("Color", Color) = (1,1,1,1)
		_TexturePower("TexturePower", Range(0.0,4)) = 1.0
		_TextureRimMin("TextureRimMin", Range(-2,1)) = 0.6
		_TextureRimMax("TextureRimMax", Range(0,4)) = 1.0
		_TextureScrollX("TextureScrollX", Range(-20,20)) = 0.0
		_TextureScrollY("TextureScrollY", Range(-20,20)) = 0.0
		_TextureDistortionInfluence("TextureDistortionInfluence", Range(-2,2)) = 1.0
		_TextureHitInfluence("TextureHitInfluence", Range(0,1)) = 1.0
		_TextureAnimationSpeed("TextureAnimationSpeed", Range(-50,50)) = 10.0
		_TextureAnimationFactor("TextureAnimationFactor", Range(-50,50)) = 10.0

		_DistortTex ("Albedo (RGB)", 2D) = "black" {}
		_DistortionFactor ("DistortionFactor", Range(0,5)) = 0.2
		_DistortionSpeedX ("DistortionSpeedX", Range(-20,20)) = 1.0
		_DistortionSpeedY ("DistortionSpeedY", Range(-20,20)) = 1.0
		_RimVariationScale ("RimVariationScale", Range(0,1)) = 0.0
		_RimVariationFrequency ("RimVariationFrequency", Range(0.01,10)) = 0.5
		_FadeScale("FadeScale", Range(0,1)) = 0.0
		_FadePow("FadePow", Range(0,10)) = 1.0

		_PatternTex("Albedo (RGB)", 2D) = "black" {}
		_PatternColor("Color", Color) = (1,1,1,1)
		_PatternPower("PatternPower", Range(0.0,4)) = 1.0
		_PatternRimMin("PatternRimMin", Range(-2,1)) = 0.6
		_PatternRimMax("PatternRimMax", Range(0,4)) = 1.0
		_PatternScrollX("PatternScrollX", Range(-20,20)) = 0.0
		_PatternScrollY("PatternScrollY", Range(-20,20)) = 0.0
		_PatternDistortionInfluence("PatternDistortionInfluence", Range(-2,2)) = 1.0
		_PatternHitInfluence("PatternHitInfluence", Range(0,1)) = 1.0
		_PatternAnimationSpeed("PatternAnimationSpeed", Range(-50,50)) = 10.0
		_PatternAnimationFactor("PatternAnimationFactor", Range(-50,50)) = 10.0

	    _OverlapRim("OverlapRim", Range(0.0,10.0)) = 0.1
		_OverlapRimPower("OverlapRimPower", Range(0.0,10.0)) = 1.0

		_DirectionVisibility("DirectionVisibility", Range(0.0,2.0)) = 1.0
		_DirVisActivationInfluence("DirectionVisibilityActivationInfluence", Range(0.0,1.0)) = 1.0

		_ActivationTime("ActivationTime", Range(0.0,1.0)) = 1.0
		_ActivationTime01("ActivationTime01", Range(0.0,1.0)) = 1.0
		_ActivationTex("Albedo (RGB)", 2D) = "black" {}
		_ActivationRim("ActivationRim", Range(0.0,1.0)) = 0.0
		_ActivationRimPower("ActivationRimPower", Range(0.0,10.0)) = 1.0
		_ActivationFadeOut("ActivationFadeOut", Range(0.0,1.0)) = 0.0
		_ActivationInluenceByMainTex("ActivationInluenceByMainTex", Range(0.0,1.0)) = 0.0
		_ActivationInluenceByPatternTex("ActivationInluenceByPatternTex", Range(0.0,1.0)) = 0.0

		_RefractionScale("RefractionScale", Range(0.0,1.0)) = 0.0
		_RefractionRimMin("RefractionRimMin", Range(-2,1)) = -2
		_RefractionRimMax("RefractionRimMax", Range(-1,4)) = 2
		_RefractionBackgroundExposure("RefractionBackgroundExposure", Range(-1,4)) = 0.0
		_RefractionColorRimInfluence("RefractionColorRimInfluence", Range(-4,4)) = 0.5
		_RefractionTextureRimInfluence("RefractionTextureRimInfluence", Range(-4,4)) = 1.0
		_RefractionPatternRimInfluence("RefractionPatternRimInfluence", Range(-4,4)) = 1.0

		_ShieldDirection("ShieldDirection", Vector) = (1,0,0,0)
		_ShieldBounds("ShieldBounds", Vector) = (1,1,1,0)

		_HitEffectValue("HitEffectValue", Range(0.0,1.0)) = 1.0
		_HitColorAffect("_HitColorAffect", Float) = 0.0
		_HitRippleDistortion("_HitRippleDistortion", Float) = 0.0
		_HitRefractionScale("_HitRefractionScale", Float) = 0.1
		_WorldScale("_WorldScale", Vector) = (1,1,1)

		_BlendSrcMode("BlendSrcMode", Int) = 0
		_BlendDstMode("BlendDstMode", Int) = 0
		_CullMode("CullMode", Int) = 0

		_Preview("Preview", Range(0.0,1.0)) = 0.0
	}
	SubShader 
	{
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "DisableBatching" = "True" }
		LOD 300

		BlendOp Add
		Blend [_BlendSrcMode] [_BlendDstMode]
		ZWrite Off
		Cull [_CullMode]
		Offset -0.1, -0.1

		//Default pass
		Pass
        {
			Name "Default"

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5

			#define FXV_SHIELD_BUILTIN 

			#pragma multi_compile_local ACTIVATION_EFFECT_ON __
			#pragma shader_feature_local ACTIVATION_TYPE_FINALCOLOR ACTIVATION_TYPE_FINALCOLOR_UVX ACTIVATION_TYPE_FINALCOLOR_UVY ACTIVATION_TYPE_CUSTOM_TEX ACTIVATION_TYPE_UVX ACTIVATION_TYPE_UVY ACTIVATION_TYPE_FINALCOLOR_POSX ACTIVATION_TYPE_FINALCOLOR_POSY ACTIVATION_TYPE_FINALCOLOR_POSZ
			#pragma shader_feature_local RIM_SOURCE_NORMAL RIM_SOURCE_TEXTURE
			#pragma multi_compile_local USE_PATTERN_TEXTURE __
			#pragma multi_compile_local USE_PATTERN_TEXTURE_ANIMATION __
			#pragma multi_compile_local USE_MAIN_TEXTURE __
			#pragma multi_compile_local USE_MAIN_TEXTURE_ANIMATION __
			#pragma shader_feature_local USE_DISTORTION_FOR_MAIN_TEXTURE __
			#pragma shader_feature_local USE_DEPTH_OVERLAP_RIM __
			#pragma shader_feature_local USE_COLOR_RIM __
			#pragma shader_feature_local USE_DIRECTION_VISIBILITY __
			#pragma shader_feature_local USE_REFRACTION __
			#pragma shader_feature_local USE_MATERIAL_PROPERTY_BLOCKS __
			#pragma shader_feature_local USE_TEXTURE_UV1 USE_GENERATED_UV __
			#pragma multi_compile_local HIT_EFFECT_ON __
			#pragma multi_compile_local HIT_EFFECT_SKINNED_MESH __
			#pragma multi_compile_local USE_HIT_RIPPLE __
			#pragma multi_compile_local USE_HIT_VARIATION __

			#pragma multi_compile_fog
            #pragma multi_compile_instancing
			#pragma editor_sync_compilation

#if defined(FXV_SHIELD_URP)
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#elif defined(FXV_SHIELD_HDRP) 
			#define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TANGENT
			#define ATTRIBUTES_NEED_TEXCOORD0
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VaryingMesh.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"
#else
            #include "UnityCG.cginc"
#endif

			#include "FXVShieldEffect.cginc"

			struct appdata
            {
				FXV_SHIELD_EFFECT_APPDATA
            };

            struct v2f
            {
				FXV_SHIELD_EFFECT_V2F_COORDS
            };

			#include "FXVShieldEffectFunctions.cginc"

#if defined(FXV_SHIELD_HDRP) 
			v2f vert (AttributesMesh v)
            {
				v2f o;

				FXV_SHIELD_EFFECT_VERTEX_DEFAULT(v, o);

                return o;
            }
#else
            v2f vert (appdata v)
            {
                v2f o;

				FXV_SHIELD_EFFECT_VERTEX_DEFAULT(v, o);

                return o;
            }
#endif

			struct fragOutput 
			{
				half4 color0 : SV_Target;
			};

            fragOutput frag(v2f i, float facing : VFACE)
			{
				FXV_SHIELD_EFFECT_FRAGMENT_DEFAULT(i);

				_FXV_CheckVisuallyActive();

#if defined(USE_GENERATED_UV)
					i.uv = _FXV_GenerateUV(i);
#endif

				half3 vdn = half3(0, 0, 0);

					_FXV_ShieldGetVDN(i, vdn);

				half4 distortCoord = half4(0,0,1,1);

					_FXV_DistortCoord(i, distortCoord);

				half2 hitAdditionalCoord = half2(0,0);
				half hitRippleFade = 1.0;
				half4 hitTex;

					_FXV_ShieldHitEffect(i, distortCoord, hitAdditionalCoord, hitRippleFade, hitTex);

				half depthVisibility = 1.0;
				half depthRim = 1.0;

					_FXV_ShieldDepthParams(i, depthVisibility, depthRim);

				half colorRim = 0.0;

					_FXV_ShieldColorRim(vdn, distortCoord, depthRim, colorRim);

				half4 tex = half4(0, 0, 0, 0);
				half texRim = 1.0;

					_FXV_ShieldMainTexture(i, vdn, depthRim, distortCoord, hitAdditionalCoord, tex, texRim);

				half4 pattern = half4(0, 0, 0, 0);
				half patternRim = 1.0;

					_FXV_ShieldPatternTexture(i, vdn, distortCoord, hitAdditionalCoord, pattern, patternRim);

				half dirVisibility = 1.0;

					_FXV_ShieldDirectionVisibility(i, dirVisibility);

				half activationRim = 0.0;
				half activationVisibility = 1.0;

					_FXV_ShieldActivationRim(i, colorRim, tex, texRim, pattern, patternRim, activationRim, activationVisibility);

				half4 basicColor = half4(0, 0, 0, 0);

					_FXV_ShieldBasicColor(colorRim, activationRim, basicColor);

				fragOutput o;

#if HIT_EFFECT_ON
					o.color0 = _FXV_GetFinalColor_Hit(i, tex, pattern, hitTex, hitRippleFade, depthVisibility, dirVisibility, activationVisibility);
#else
	#ifdef USE_REFRACTION
					o.color0 = _FXV_GetFinalColor_Refraction(i, vdn, basicColor, tex, texRim, pattern, patternRim, distortCoord.xy, depthVisibility, depthRim, activationRim, activationVisibility, dirVisibility);
	#else
					o.color0 = _FXV_GetFinalColor_Default(i, basicColor, tex, texRim, pattern, patternRim, depthVisibility, depthRim, activationRim, activationVisibility, dirVisibility, facing);
	#endif
#endif

                return o;
            }
            ENDHLSL
        }

		//Postprocess pass
		Pass
        {
			Tags { "LightMode" = "Postprocess" }
			Name "Postprocess"

			ZTest Off

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5

			#define FXV_SHIELD_BUILTIN

			#pragma multi_compile_local ACTIVATION_EFFECT_ON __
			#pragma shader_feature_local ACTIVATION_TYPE_FINALCOLOR ACTIVATION_TYPE_FINALCOLOR_UVX ACTIVATION_TYPE_FINALCOLOR_UVY ACTIVATION_TYPE_CUSTOM_TEX ACTIVATION_TYPE_UVX ACTIVATION_TYPE_UVY ACTIVATION_TYPE_FINALCOLOR_POSX ACTIVATION_TYPE_FINALCOLOR_POSY ACTIVATION_TYPE_FINALCOLOR_POSZ
			#pragma shader_feature_local RIM_SOURCE_NORMAL RIM_SOURCE_TEXTURE
			#pragma multi_compile_local USE_PATTERN_TEXTURE __
			#pragma multi_compile_local USE_PATTERN_TEXTURE_ANIMATION __
			#pragma multi_compile_local USE_MAIN_TEXTURE __
			#pragma multi_compile_local USE_MAIN_TEXTURE_ANIMATION __
			#pragma shader_feature_local USE_DISTORTION_FOR_MAIN_TEXTURE __
			#pragma shader_feature_local USE_DEPTH_OVERLAP_RIM __
			#pragma shader_feature_local USE_COLOR_RIM __
			#pragma shader_feature_local USE_DIRECTION_VISIBILITY __
			#pragma shader_feature_local USE_REFRACTION __
			#pragma shader_feature_local USE_MATERIAL_PROPERTY_BLOCKS __
			#pragma shader_feature_local USE_TEXTURE_UV1 USE_GENERATED_UV __
			#pragma multi_compile_local HIT_EFFECT_ON __
			#pragma multi_compile_local HIT_EFFECT_SKINNED_MESH __
			#pragma multi_compile_local USE_HIT_RIPPLE __
			#pragma multi_compile_local USE_HIT_VARIATION __

			#pragma multi_compile_fog
            #pragma multi_compile_instancing
			#pragma editor_sync_compilation

#if defined(FXV_SHIELD_URP)
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#elif defined(FXV_SHIELD_HDRP) 
			#define ATTRIBUTES_NEED_NORMAL
			#define ATTRIBUTES_NEED_TANGENT
			#define ATTRIBUTES_NEED_TEXCOORD0
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VaryingMesh.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"
#else
            #include "UnityCG.cginc"
#endif
			#include "FXVShieldEffect.cginc"

            struct appdata
            {
				FXV_SHIELD_EFFECT_APPDATA
            };

            struct v2f
            {
				FXV_SHIELD_EFFECT_V2F_COORDS
            };

			#include "FXVShieldEffectFunctions.cginc"

#if defined(FXV_SHIELD_HDRP) 
			v2f vert(AttributesMesh inputMesh)
			{
				v2f o;

				FXV_SHIELD_EFFECT_VERTEX_DEFAULT(inputMesh, o);

                return o;
			}
#else
            v2f vert (appdata v)
            {
                v2f o;

				FXV_SHIELD_EFFECT_VERTEX_DEFAULT(v, o);

                return o;
            }
#endif

			struct fragOutput 
			{
				half4 color0 : SV_Target0;
#if !defined(HIT_EFFECT_ON)
				half4 color1 : SV_Target1;
				half4 color2 : SV_Target2;
#endif
			};

            fragOutput frag (v2f i, float facing : VFACE)
			{
				FXV_SHIELD_EFFECT_FRAGMENT_DEFAULT(i);

				_FXV_CheckVisuallyActive();

#if defined(USE_GENERATED_UV)
					i.uv = _FXV_GenerateUV(i);
#endif

				half3 vdn = half3(0, 0, 0);

					_FXV_ShieldGetVDN(i, vdn);

				half4 distortCoord = half4(0,0,1,1);

					_FXV_DistortCoord(i, distortCoord);

				half2 hitAdditionalCoord = half2(0,0);
				half hitRippleFade = 1.0;
				half4 hitTex = half4(0, 0, 0, 0);

					_FXV_ShieldHitEffect(i, distortCoord, hitAdditionalCoord, hitRippleFade, hitTex);

				half depthVisibility = 1.0;
				half depthRim = 1.0;

					_FXV_ShieldDepthParamsPostprocess(i, depthVisibility, depthRim);

				half colorRim = 0.0;

					_FXV_ShieldColorRim(vdn, distortCoord, depthRim, colorRim);

				half4 tex = half4(0, 0, 0, 0);
				half texRim = 1.0;

					_FXV_ShieldMainTexture(i, vdn, depthRim, distortCoord, hitAdditionalCoord, tex, texRim);

				half4 pattern = half4(0, 0, 0, 0);
				half patternRim = 1.0;

					_FXV_ShieldPatternTexture(i, vdn, distortCoord, hitAdditionalCoord, pattern, patternRim);

				half dirVisibility = 1.0;

					_FXV_ShieldDirectionVisibility(i, dirVisibility);

				half activationRim = 0.0;
				half activationVisibility = 1.0;

					_FXV_ShieldActivationRim(i, colorRim, tex, texRim, pattern, patternRim, activationRim, activationVisibility);

				half4 basicColor = half4(0, 0, 0, 0);

					_FXV_ShieldBasicColor(colorRim, activationRim, basicColor);


				fragOutput o;

#if HIT_EFFECT_ON
					o.color0 = _FXV_GetFinalColor_Hit(i, tex, pattern, hitTex, hitRippleFade, depthVisibility, dirVisibility, activationVisibility);
#else
					o.color0 = _FXV_GetFinalColor_Default(i, basicColor, tex, texRim, pattern, patternRim, depthVisibility, depthRim, activationRim, activationVisibility, dirVisibility, facing);

					float s = step(0.0, facing);
					o.color1 = float4(_FXV_ObjectToWorldCamRelativePos(i.objectSpacePos) * s, depthVisibility * s);
					o.color2 = float4(_FXV_ObjectToWorldCamRelativePos(i.objectSpacePos) * (1.0 - s), depthVisibility * (1.0 - s));
#endif

                return o;
            }
            ENDHLSL
        }
	}

	CustomEditor "FXV.ShieldEditorUtils.fxvShieldMaterialEditor"

	FallBack "Diffuse"
}
