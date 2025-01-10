using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace FXV.ShieldEditorUtils
{
    public enum FXV_BLEND_MODE_OPTIONS
    {
        AlphaBlend = 0,
        AdditiveBlend = 1,
    }

    public enum FXV_RENDER_FACES
    {
        FrontAndBack = 0,
        Back = 1,
        Front = 2,
    }

    public enum FXV_RIM_SOURCE_OPTIONS
    {
        NormalVector = 0,
        Texture = 1,
    }

    public class fxvShieldMaterialEditor : ShaderGUI
    {
        public enum ACTIVATION_TYPE_OPTIONS
        {
            FinalColor = 0,
            FinalColor_and_UVx = 1,
            FinalColor_and_UVy = 2,
            CustomTexture = 3,
            UVx = 4,
            UVy = 5,
            FinalColor_and_PosY = 6,
            FinalColor_and_PosX = 7,
            FinalColor_and_PosZ = 8,
        }

        public enum UV_COORDS_OPTIONS
        {
            UV0 = 0,
            UV1 = 1,
            GeneratedUV = 2,
        }

        bool firstTimeApply = true;

        MaterialEditor materialEditor;

        MaterialProperty _RimTextureProperty = null;

        MaterialProperty _NormalCorrectionProperty = null;

        MaterialProperty _GlobalIntensityProperty = null;
        MaterialProperty _GlobalAlphaCurveProperty = null;
        MaterialProperty _BackfacesIntensityProperty = null;

        MaterialProperty _ColorProperty = null;
        MaterialProperty _ColorRimMinProperty = null;
        MaterialProperty _ColorRimMaxProperty = null;
        MaterialProperty _ColorRimHitInfluenceProperty = null;

        MaterialProperty _MainTexProperty = null;
        MaterialProperty _TextureColorProperty = null;
        MaterialProperty _TexturePowerProperty = null;
        MaterialProperty _TextureRimMinProperty = null;
        MaterialProperty _TextureRimMaxProperty = null;
        MaterialProperty _TextureScrollXProperty = null;
        MaterialProperty _TextureScrollYProperty = null;
        MaterialProperty _TextureDistortionInfluenceProperty = null;
        MaterialProperty _TextureHitInfluenceProperty = null;
        MaterialProperty _TextureAnimationSpeedProperty = null;
        MaterialProperty _TextureAnimationFactorProperty = null;

        MaterialProperty _DistortTexProperty = null;
        MaterialProperty _DistortionFactorProperty = null;
        MaterialProperty _DistortionSpeedXProperty = null;
        MaterialProperty _DistortionSpeedYProperty = null; 
        MaterialProperty _RimVariationScaleProperty = null;
        MaterialProperty _RimVariationFrequencyProperty = null;
        MaterialProperty _FadeScaleProperty = null;
        MaterialProperty _FadePowProperty = null;
        

        MaterialProperty _PatternTexProperty = null;
        MaterialProperty _PatternColorProperty = null;
        MaterialProperty _PatternPowerProperty = null;
        MaterialProperty _PatternRimMinProperty = null;
        MaterialProperty _PatternRimMaxProperty = null;
        MaterialProperty _PatternScrollXProperty = null;
        MaterialProperty _PatternScrollYProperty = null;
        MaterialProperty _PatternDistortionInfluenceProperty = null;
        MaterialProperty _PatternHitInfluenceProperty = null;
        MaterialProperty _PatternAnimationSpeedProperty = null;
        MaterialProperty _PatternAnimationFactorProperty = null;

        MaterialProperty _OverlapRimProperty = null;
        MaterialProperty _OverlapRimPowerProperty = null;

        MaterialProperty _RefractionScaleProperty = null;
        MaterialProperty _RefractionRimMinProperty = null;
        MaterialProperty _RefractionRimMaxProperty = null;
        MaterialProperty _RefractionBackgroundExposureProperty = null;
        MaterialProperty _RefractionColorRimInfluenceProperty = null;
        MaterialProperty _RefractionTextureRimInfluenceProperty = null;
        MaterialProperty _RefractionPatternRimInfluenceProperty = null;

        MaterialProperty _DirectionVisibilityProperty = null;
        MaterialProperty _DirVisActivationInfluenceProperty = null;

        MaterialProperty _ActivationTexProperty = null;
        MaterialProperty _ActivationRimProperty = null;
        MaterialProperty _ActivationRimPowerProperty = null;
        MaterialProperty _ActivationFadeOutProperty = null;
        MaterialProperty _ActivationInluenceByMainTexProperty = null;
        MaterialProperty _ActivationInluenceByPatternTexProperty = null;

        GUIStyle _groupStyle;
        GUIStyle _errorStyle;

        public void FindProperties(MaterialProperty[] props)
        {
            _RimTextureProperty = FindProperty("_RimTexture", props);

            _NormalCorrectionProperty = FindProperty("_NormalCorrection", props);

            _GlobalIntensityProperty = FindProperty("_GlobalIntensity", props);
            _GlobalAlphaCurveProperty = FindProperty("_GlobalAlphaCurve", props);
            _BackfacesIntensityProperty = FindProperty("_BackfacesIntensity", props);

            _ColorProperty = FindProperty("_Color", props);
            _ColorRimMinProperty = FindProperty("_ColorRimMin", props);
            _ColorRimMaxProperty = FindProperty("_ColorRimMax", props);
            _ColorRimHitInfluenceProperty = FindProperty("_ColorRimHitInfluence", props);

            _MainTexProperty = FindProperty("_MainTex", props);
            _TextureColorProperty = FindProperty("_TextureColor", props);
            _TexturePowerProperty = FindProperty("_TexturePower", props);
            _TextureRimMinProperty = FindProperty("_TextureRimMin", props);
            _TextureRimMaxProperty = FindProperty("_TextureRimMax", props);
            _TextureScrollXProperty = FindProperty("_TextureScrollX", props);
            _TextureScrollYProperty = FindProperty("_TextureScrollY", props);
            _TextureDistortionInfluenceProperty = FindProperty("_TextureDistortionInfluence", props);
            _TextureHitInfluenceProperty = FindProperty("_TextureHitInfluence", props);
            _TextureAnimationSpeedProperty = FindProperty("_TextureAnimationSpeed", props);
            _TextureAnimationFactorProperty = FindProperty("_TextureAnimationFactor", props);

            _DistortTexProperty = FindProperty("_DistortTex", props);
            _DistortionFactorProperty = FindProperty("_DistortionFactor", props);
            _DistortionSpeedXProperty = FindProperty("_DistortionSpeedX", props);
            _DistortionSpeedYProperty = FindProperty("_DistortionSpeedY", props);
            _RimVariationScaleProperty = FindProperty("_RimVariationScale", props);
            _RimVariationFrequencyProperty = FindProperty("_RimVariationFrequency", props);
            _FadeScaleProperty = FindProperty("_FadeScale", props);
            _FadePowProperty = FindProperty("_FadePow", props);

            _PatternTexProperty = FindProperty("_PatternTex", props);
            _PatternColorProperty = FindProperty("_PatternColor", props);
            _PatternPowerProperty = FindProperty("_PatternPower", props);
            _PatternRimMinProperty = FindProperty("_PatternRimMin", props);
            _PatternRimMaxProperty = FindProperty("_PatternRimMax", props);
            _PatternScrollXProperty = FindProperty("_PatternScrollX", props);
            _PatternScrollYProperty = FindProperty("_PatternScrollY", props);
            _PatternDistortionInfluenceProperty = FindProperty("_PatternDistortionInfluence", props);
            _PatternHitInfluenceProperty = FindProperty("_PatternHitInfluence", props);
            _PatternAnimationSpeedProperty = FindProperty("_PatternAnimationSpeed", props);
            _PatternAnimationFactorProperty = FindProperty("_PatternAnimationFactor", props);

            _OverlapRimProperty = FindProperty("_OverlapRim", props, false);
            _OverlapRimPowerProperty = FindProperty("_OverlapRimPower", props, false);

            _RefractionScaleProperty = FindProperty("_RefractionScale", props, false);
            _RefractionRimMinProperty = FindProperty("_RefractionRimMin", props, false);
            _RefractionRimMaxProperty = FindProperty("_RefractionRimMax", props, false);
            _RefractionBackgroundExposureProperty = FindProperty("_RefractionBackgroundExposure", props, false);
            _RefractionColorRimInfluenceProperty = FindProperty("_RefractionColorRimInfluence", props, false);
            _RefractionTextureRimInfluenceProperty = FindProperty("_RefractionTextureRimInfluence", props, false);
            _RefractionPatternRimInfluenceProperty = FindProperty("_RefractionPatternRimInfluence", props, false);

            _DirectionVisibilityProperty = FindProperty("_DirectionVisibility", props);
            _DirVisActivationInfluenceProperty = FindProperty("_DirVisActivationInfluence", props);

            _ActivationTexProperty = FindProperty("_ActivationTex", props);
            _ActivationRimProperty = FindProperty("_ActivationRim", props);
            _ActivationRimPowerProperty = FindProperty("_ActivationRimPower", props);
            _ActivationFadeOutProperty = FindProperty("_ActivationFadeOut", props);
            _ActivationInluenceByMainTexProperty = FindProperty("_ActivationInluenceByMainTex", props);
            _ActivationInluenceByPatternTexProperty = FindProperty("_ActivationInluenceByPatternTex", props);
        }

        public override void OnGUI(MaterialEditor editor, MaterialProperty[] props)
        {
            _groupStyle = new GUIStyle("window");
            _groupStyle.fontStyle = FontStyle.Bold;

            _errorStyle = new GUIStyle(EditorStyles.boldLabel);
            _errorStyle.normal.textColor = Color.red;

            FindProperties(props);

            materialEditor = editor;
            Material material = materialEditor.target as Material;

            if (firstTimeApply)
            {

                firstTimeApply = false;
            }

            ShaderPropertiesGUI(material);
        }

        public override void OnMaterialPreviewGUI(MaterialEditor materialEditor, Rect r, GUIStyle background)
        {
            Material targetMat = materialEditor.target as Material;

            targetMat.SetFloat("_Preview", 1.0f);

            materialEditor.DefaultPreviewGUI(r, background);

            targetMat.SetFloat("_Preview", 0.0f);
        }

        public override void OnMaterialInteractivePreviewGUI(MaterialEditor materialEditor, Rect r, GUIStyle background)
        {
            Material targetMat = materialEditor.target as Material;

            targetMat.SetFloat("_Preview", 1.0f);

            materialEditor.DefaultPreviewGUI(r, background);

            targetMat.SetFloat("_Preview", 0.0f);
        }

        public void ShaderPropertiesGUI(Material targetMat)
        {
            string[] keyWords = targetMat.shaderKeywords;

            bool usePatternTexture = keyWords.Contains("USE_PATTERN_TEXTURE");
            bool usePatternTextureAnimation = keyWords.Contains("USE_PATTERN_TEXTURE_ANIMATION");
            bool useMainTexture = keyWords.Contains("USE_MAIN_TEXTURE");
            bool useMainTextureAnimation = keyWords.Contains("USE_MAIN_TEXTURE_ANIMATION");
            bool useDistortionForMainTexture = keyWords.Contains("USE_DISTORTION_FOR_MAIN_TEXTURE");
            bool useDepthOverlapRim = keyWords.Contains("USE_DEPTH_OVERLAP_RIM");
            bool useColorRim = keyWords.Contains("USE_COLOR_RIM");
            bool useDirectionVisibility = keyWords.Contains("USE_DIRECTION_VISIBILITY");
            bool refractionEnabled = keyWords.Contains("USE_REFRACTION");

            bool activationEnabled = true;

            ACTIVATION_TYPE_OPTIONS activationType = ACTIVATION_TYPE_OPTIONS.FinalColor;

            if (keyWords.Contains("ACTIVATION_TYPE_FINALCOLOR"))
                activationType = ACTIVATION_TYPE_OPTIONS.FinalColor;
            if (keyWords.Contains("ACTIVATION_TYPE_FINALCOLOR_UVX"))
                activationType = ACTIVATION_TYPE_OPTIONS.FinalColor_and_UVx;
            if (keyWords.Contains("ACTIVATION_TYPE_FINALCOLOR_UVY"))
                activationType = ACTIVATION_TYPE_OPTIONS.FinalColor_and_UVy;
            if (keyWords.Contains("ACTIVATION_TYPE_CUSTOM_TEX"))
                activationType = ACTIVATION_TYPE_OPTIONS.CustomTexture;
            if (keyWords.Contains("ACTIVATION_TYPE_UVX"))
                activationType = ACTIVATION_TYPE_OPTIONS.UVx;
            if (keyWords.Contains("ACTIVATION_TYPE_UVY"))
                activationType = ACTIVATION_TYPE_OPTIONS.UVy;
            if (keyWords.Contains("ACTIVATION_TYPE_FINALCOLOR_POSY"))
                activationType = ACTIVATION_TYPE_OPTIONS.FinalColor_and_PosY;
            if (keyWords.Contains("ACTIVATION_TYPE_FINALCOLOR_POSX"))
                activationType = ACTIVATION_TYPE_OPTIONS.FinalColor_and_PosX;
            if (keyWords.Contains("ACTIVATION_TYPE_FINALCOLOR_POSZ"))
                activationType = ACTIVATION_TYPE_OPTIONS.FinalColor_and_PosZ;

            FXV_RIM_SOURCE_OPTIONS rimSource = FXV_RIM_SOURCE_OPTIONS.NormalVector;
            if (keyWords.Contains("RIM_SOURCE_NORMAL"))
                rimSource = FXV_RIM_SOURCE_OPTIONS.NormalVector;
            if (keyWords.Contains("RIM_SOURCE_TEXTURE"))
                rimSource = FXV_RIM_SOURCE_OPTIONS.Texture;

            UV_COORDS_OPTIONS uvOptions = UV_COORDS_OPTIONS.UV0;
            if (keyWords.Contains("USE_TEXTURE_UV1"))
                uvOptions = UV_COORDS_OPTIONS.UV1;
            if (keyWords.Contains("USE_GENERATED_UV"))
                uvOptions = UV_COORDS_OPTIONS.GeneratedUV;

            EditorGUI.BeginChangeCheck();

            targetMat.SetFloat("_Preview", 0.0f);

            targetMat.SetVector("_ShieldDirection", new Vector4(1.0f, 0.0f, 0.0f, 0.0f));

            GUILayout.BeginVertical("Render Options", _groupStyle);

            GUILayout.Label("Blend Mode", EditorStyles.boldLabel);
            {
                FXV_BLEND_MODE_OPTIONS blendModeType = FXV_BLEND_MODE_OPTIONS.AdditiveBlend;
                if (targetMat.GetInt("_BlendSrcMode") == (int)UnityEngine.Rendering.BlendMode.SrcAlpha &&
                    targetMat.GetInt("_BlendDstMode") == (int)UnityEngine.Rendering.BlendMode.One)
                    blendModeType = FXV_BLEND_MODE_OPTIONS.AdditiveBlend;
                if (targetMat.GetInt("_BlendSrcMode") == (int)UnityEngine.Rendering.BlendMode.SrcAlpha &&
                    targetMat.GetInt("_BlendDstMode") == (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha)
                    blendModeType = FXV_BLEND_MODE_OPTIONS.AlphaBlend;

                if (refractionEnabled)
                {
                    blendModeType = FXV_BLEND_MODE_OPTIONS.AlphaBlend;
                    GUILayout.Label("Can't change when refraction is on", EditorStyles.label);
                }
                else
                    blendModeType = (FXV_BLEND_MODE_OPTIONS)EditorGUILayout.EnumPopup("", blendModeType);

                if (blendModeType == FXV_BLEND_MODE_OPTIONS.AdditiveBlend)
                {
                    targetMat.SetInt("_BlendSrcMode", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    targetMat.SetInt("_BlendDstMode", (int)UnityEngine.Rendering.BlendMode.One);
                }
                else if (blendModeType == FXV_BLEND_MODE_OPTIONS.AlphaBlend)
                {
                    targetMat.SetInt("_BlendSrcMode", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    targetMat.SetInt("_BlendDstMode", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                }
            }

            GUILayout.Label("Render Faces", EditorStyles.boldLabel);
            {
                FXV_RENDER_FACES renderFaces = FXV_RENDER_FACES.FrontAndBack;
                if (targetMat.GetInt("_CullMode") == (int)UnityEngine.Rendering.CullMode.Off)
                    renderFaces = FXV_RENDER_FACES.FrontAndBack;
                if (targetMat.GetInt("_CullMode") == (int)UnityEngine.Rendering.CullMode.Back)
                    renderFaces = FXV_RENDER_FACES.Front;
                if (targetMat.GetInt("_CullMode") == (int)UnityEngine.Rendering.CullMode.Front)
                    renderFaces = FXV_RENDER_FACES.Back;

                if (refractionEnabled)
                {
                    renderFaces = FXV_RENDER_FACES.Front;
                    GUILayout.Label("Only front faces are rendered when refraction is on", EditorStyles.label);
                }
                else
                    renderFaces = (FXV_RENDER_FACES)EditorGUILayout.EnumPopup("", renderFaces);

                targetMat.SetInt("_CullMode", (int)renderFaces);

                if (renderFaces == FXV_RENDER_FACES.FrontAndBack)
                {
                    materialEditor.ShaderProperty(_BackfacesIntensityProperty, "Backfaces Intensity");
                }
                else
                {
                    _BackfacesIntensityProperty.floatValue = 1.0f;
                }
            }

            GUILayout.Label("UV Source", EditorStyles.boldLabel);
            {
                uvOptions = (UV_COORDS_OPTIONS)EditorGUILayout.EnumPopup("UV Channel:", uvOptions);
            }

            GUILayout.Label("Rim Config", EditorStyles.boldLabel);
            {
                rimSource = (FXV_RIM_SOURCE_OPTIONS)EditorGUILayout.EnumPopup("Rim Source Type:", rimSource);

                if (rimSource == FXV_RIM_SOURCE_OPTIONS.Texture)
                {
                    materialEditor.TexturePropertySingleLine(new GUIContent("Texture", ""), _RimTextureProperty);
                    materialEditor.TextureScaleOffsetProperty(_RimTextureProperty);
                }
                else if (rimSource == FXV_RIM_SOURCE_OPTIONS.NormalVector)
                {
                    materialEditor.ShaderProperty(_NormalCorrectionProperty, "Normal Correction");
                }
            }

            GUILayout.EndVertical();

            GUILayout.Space(10);
            GUILayout.BeginVertical("Global Settings", _groupStyle);

            materialEditor.ShaderProperty(_GlobalIntensityProperty, "Intensity");
            materialEditor.ShaderProperty(_GlobalAlphaCurveProperty, "Curve");

            

            GUILayout.EndVertical();

            GUILayout.Space(10);
            GUILayout.BeginVertical("Color Rim", _groupStyle);

            useColorRim = EditorGUILayout.Toggle("Enabled", useColorRim);

            if (useColorRim)
            {
                materialEditor.ColorProperty(_ColorProperty, "Color");
                materialEditor.ShaderProperty(_ColorRimMinProperty, "Color Rim Min");
                materialEditor.ShaderProperty(_ColorRimMaxProperty, "Color Rim Max");
                materialEditor.ShaderProperty(_ColorRimHitInfluenceProperty, "Color Rim Hit Influence");
            }

            GUILayout.EndVertical();

            GUILayout.Space(10);
            GUILayout.BeginVertical("Texture Rim", _groupStyle);

            useMainTexture = EditorGUILayout.Toggle("Enabled", useMainTexture);

            if (useMainTexture)
            {
                materialEditor.TexturePropertySingleLine(new GUIContent("Texture", ""), _MainTexProperty);
                materialEditor.TextureScaleOffsetProperty(_MainTexProperty);
                materialEditor.ColorProperty(_TextureColorProperty, "Color");
                materialEditor.ShaderProperty(_TexturePowerProperty, "Texture Power");
                materialEditor.ShaderProperty(_TextureRimMinProperty, "Texture Rim Min");
                materialEditor.ShaderProperty(_TextureRimMaxProperty, "Texture Rim Max");
                materialEditor.ShaderProperty(_TextureScrollXProperty, "Texture Scroll Speed X");
                materialEditor.ShaderProperty(_TextureScrollYProperty, "Texture Scroll Speed Y");
                materialEditor.ShaderProperty(_TextureHitInfluenceProperty, "Texture Hit Influence");
                if (useDistortionForMainTexture)
                {
                    materialEditor.ShaderProperty(_TextureDistortionInfluenceProperty, "Distortion Influence");
                }

                useMainTextureAnimation = EditorGUILayout.Toggle("Animation", useMainTextureAnimation);
                if (useMainTextureAnimation)
                {
                    materialEditor.ShaderProperty(_TextureAnimationSpeedProperty, "   Animation Speed");
                    materialEditor.ShaderProperty(_TextureAnimationFactorProperty, "   Animation Factor");
                }
            }
            else
            {
                useMainTextureAnimation = false;
            }

            GUILayout.EndVertical();

            GUILayout.Space(10);
            GUILayout.BeginVertical("Pattern Texture - Inverted Rim", _groupStyle);

            usePatternTexture = EditorGUILayout.Toggle("Enabled", usePatternTexture);

            if (usePatternTexture)
            {
                materialEditor.TexturePropertySingleLine(new GUIContent("Texture", ""), _PatternTexProperty);
                materialEditor.TextureScaleOffsetProperty(_PatternTexProperty);
                materialEditor.ColorProperty(_PatternColorProperty, "Color");
                materialEditor.ShaderProperty(_PatternPowerProperty, "Pattern Power");
                materialEditor.ShaderProperty(_PatternRimMinProperty, "Pattern Rim Min");
                materialEditor.ShaderProperty(_PatternRimMaxProperty, "Pattern Rim Max");
                materialEditor.ShaderProperty(_PatternScrollXProperty, "Pattern Scroll Speed X");
                materialEditor.ShaderProperty(_PatternScrollYProperty, "Pattern Scroll Speed Y");
                materialEditor.ShaderProperty(_PatternHitInfluenceProperty, "Pattern Hit Influence");
                if (useDistortionForMainTexture)
                {
                    materialEditor.ShaderProperty(_PatternDistortionInfluenceProperty, "Distortion Influence");
                }

                usePatternTextureAnimation = EditorGUILayout.Toggle("Animation", usePatternTextureAnimation);
                if (usePatternTextureAnimation)
                {
                    materialEditor.ShaderProperty(_PatternAnimationSpeedProperty, "   Animation Speed");
                    materialEditor.ShaderProperty(_PatternAnimationFactorProperty, "   Animation Factor");
                }
            }
            else
            {
                usePatternTextureAnimation = false;
            }

            GUILayout.EndVertical();

            if (usePatternTexture || useMainTexture)
            {
                GUILayout.Space(10);
                GUILayout.BeginVertical("Effect Texture (RG - distortion, B - rim variation, A - fade)", _groupStyle);

                useDistortionForMainTexture = EditorGUILayout.Toggle("Enabled", useDistortionForMainTexture);

                if (useDistortionForMainTexture)
                {
                    materialEditor.TexturePropertySingleLine(new GUIContent("Texture", ""), _DistortTexProperty);
                    materialEditor.TextureScaleOffsetProperty(_DistortTexProperty);
                    materialEditor.ShaderProperty(_DistortionSpeedXProperty, "Scroll Speed X");
                    materialEditor.ShaderProperty(_DistortionSpeedYProperty, "Scroll Speed Y");

                    materialEditor.ShaderProperty(_DistortionFactorProperty, "Distortion Factor");

                    materialEditor.ShaderProperty(_RimVariationScaleProperty, "Rim Variation Scale");
                    materialEditor.ShaderProperty(_RimVariationFrequencyProperty, "Rim Variation Frequency");
                    
                    materialEditor.ShaderProperty(_FadeScaleProperty, "Fade Scale");
                    materialEditor.ShaderProperty(_FadePowProperty, "Fade Pow");
                }

                GUILayout.EndVertical();
            }

            if (_OverlapRimProperty != null)
            {
                GUILayout.Space(10);
                GUILayout.BeginVertical("Depth Test Overlap Rim", _groupStyle);

                useDepthOverlapRim = EditorGUILayout.Toggle("Enabled", useDepthOverlapRim);

                if (useDepthOverlapRim)
                {
#if UNITY_ANDROID || UNITY_IOS || UNITY_WEBGL
                    GUILayout.Label("!!! Depth Test should not be used on\nmobile platforms for performance reasons.", _errorStyle);
#endif
                    materialEditor.ShaderProperty(_OverlapRimProperty, "Rim Size");
                    materialEditor.ShaderProperty(_OverlapRimPowerProperty, "Rim Power");
                }

                GUILayout.EndVertical();
            }
            else
                useDepthOverlapRim = false;

            GUILayout.Space(10);
            GUILayout.BeginVertical("Direction Based Visibility", _groupStyle);

            useDirectionVisibility = EditorGUILayout.Toggle("Enabled", useDirectionVisibility);

            if (useDirectionVisibility)
            {
                materialEditor.ShaderProperty(_DirectionVisibilityProperty, "Visibility Factor");
                materialEditor.ShaderProperty(_DirVisActivationInfluenceProperty, "Influence On Activation");
            }

            GUILayout.EndVertical();

            if (_RefractionScaleProperty != null)
            {
                GUILayout.Space(10);
                GUILayout.BeginVertical("Refraction", _groupStyle);

                refractionEnabled = EditorGUILayout.Toggle("Refraction Enabled", refractionEnabled);

                if (refractionEnabled)
                {
#if UNITY_ANDROID || UNITY_IOS || UNITY_WEBGL
                    GUILayout.Label("!!! Refraction should not be used on\nmobile platforms for performance reasons.", _errorStyle);
#endif
                    materialEditor.ShaderProperty(_RefractionScaleProperty, "Refraction Scale");
                    materialEditor.ShaderProperty(_RefractionRimMinProperty, "Refraction Rim Min");
                    materialEditor.ShaderProperty(_RefractionRimMaxProperty, "Refraction Rim Max");
                    materialEditor.ShaderProperty(_RefractionBackgroundExposureProperty, "Refraction Background Exposure");
                    materialEditor.ShaderProperty(_RefractionColorRimInfluenceProperty, "Refraction Color Rim influence");
                    materialEditor.ShaderProperty(_RefractionTextureRimInfluenceProperty, "Refraction Texture Rim Influence");
                    materialEditor.ShaderProperty(_RefractionPatternRimInfluenceProperty, "Refraction Pattern Rim Influence");


                }

                GUILayout.EndVertical();
            }

            GUILayout.Space(10);
            GUILayout.BeginVertical("Activation Effect", _groupStyle);

            if (activationEnabled)
            {
                activationType = (ACTIVATION_TYPE_OPTIONS)EditorGUILayout.EnumPopup("Activation Type:", activationType);

                if (activationType == ACTIVATION_TYPE_OPTIONS.CustomTexture)
                {
                    materialEditor.TexturePropertySingleLine(new GUIContent("Texture", ""), _ActivationTexProperty);
                    materialEditor.TextureScaleOffsetProperty(_ActivationTexProperty);
                }

                materialEditor.ShaderProperty(_ActivationRimProperty, "Activation Rim Size");
                materialEditor.ShaderProperty(_ActivationRimPowerProperty, "Activation Rim Power");
                materialEditor.ShaderProperty(_ActivationFadeOutProperty, "Activation Fade Out");
                materialEditor.ShaderProperty(_ActivationInluenceByMainTexProperty, "Rim Texture Influence");
                materialEditor.ShaderProperty(_ActivationInluenceByPatternTexProperty, "Pattern Texture Influence");



                targetMat.SetFloat("_ActivationTime", 1.0f);
            }

            GUILayout.EndVertical();

            GUILayout.Space(10);
            GUILayout.BeginVertical("Advanced Options", _groupStyle);

            materialEditor.EnableInstancingField();
            materialEditor.RenderQueueField();

            GUILayout.EndVertical();

            if (EditorGUI.EndChangeCheck())
            {
                List<string> keywords = new List<string> { activationEnabled ? "ACTIVATION_EFFECT_ON" : "ACTIVATION_EFFECT_OFF" };

                if (usePatternTexture)
                    keywords.Add("USE_PATTERN_TEXTURE");

                if (usePatternTextureAnimation)
                    keywords.Add("USE_PATTERN_TEXTURE_ANIMATION");

                if (useMainTexture)
                    keywords.Add("USE_MAIN_TEXTURE");

                if (useMainTextureAnimation)
                    keywords.Add("USE_MAIN_TEXTURE_ANIMATION");

                if (useDistortionForMainTexture)
                    keywords.Add("USE_DISTORTION_FOR_MAIN_TEXTURE");

                if (useDepthOverlapRim)
                    keywords.Add("USE_DEPTH_OVERLAP_RIM");

                if (useColorRim)
                    keywords.Add("USE_COLOR_RIM");

                if (useDirectionVisibility)
                    keywords.Add("USE_DIRECTION_VISIBILITY");

                if (refractionEnabled)
                    keywords.Add("USE_REFRACTION");

                if (rimSource == FXV_RIM_SOURCE_OPTIONS.NormalVector)
                    keywords.Add("RIM_SOURCE_NORMAL");

                if (rimSource == FXV_RIM_SOURCE_OPTIONS.Texture)
                    keywords.Add("RIM_SOURCE_TEXTURE");

                if (uvOptions == UV_COORDS_OPTIONS.UV1)
                    keywords.Add("USE_TEXTURE_UV1");

                if (uvOptions == UV_COORDS_OPTIONS.GeneratedUV)
                    keywords.Add("USE_GENERATED_UV");
                

                if (activationEnabled)
                {
                    if (activationType == ACTIVATION_TYPE_OPTIONS.FinalColor)
                        keywords.Add("ACTIVATION_TYPE_FINALCOLOR");

                    if (activationType == ACTIVATION_TYPE_OPTIONS.FinalColor_and_UVx)
                        keywords.Add("ACTIVATION_TYPE_FINALCOLOR_UVX");

                    if (activationType == ACTIVATION_TYPE_OPTIONS.FinalColor_and_UVy)
                        keywords.Add("ACTIVATION_TYPE_FINALCOLOR_UVY");

                    if (activationType == ACTIVATION_TYPE_OPTIONS.CustomTexture)
                        keywords.Add("ACTIVATION_TYPE_CUSTOM_TEX");

                    if (activationType == ACTIVATION_TYPE_OPTIONS.UVx)
                        keywords.Add("ACTIVATION_TYPE_UVX");

                    if (activationType == ACTIVATION_TYPE_OPTIONS.UVy)
                        keywords.Add("ACTIVATION_TYPE_UVY");

                    if (activationType == ACTIVATION_TYPE_OPTIONS.FinalColor_and_PosY)
                        keywords.Add("ACTIVATION_TYPE_FINALCOLOR_POSY");

                    if (activationType == ACTIVATION_TYPE_OPTIONS.FinalColor_and_PosX)
                        keywords.Add("ACTIVATION_TYPE_FINALCOLOR_POSX");

                    if (activationType == ACTIVATION_TYPE_OPTIONS.FinalColor_and_PosZ)
                        keywords.Add("ACTIVATION_TYPE_FINALCOLOR_POSZ");
                }

                targetMat.shaderKeywords = keywords.ToArray();

                EditorUtility.SetDirty(targetMat);
            }
        }
    }
}