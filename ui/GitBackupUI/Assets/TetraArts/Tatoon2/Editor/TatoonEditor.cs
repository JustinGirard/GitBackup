using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TatoonEditor : ShaderGUI
{
    

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {


        Diffuse(materialEditor, properties);
        //NormalMap(materialEditor, properties);
        Emissive(materialEditor, properties);
        Shadow(materialEditor, properties);
        Specular(materialEditor,properties);
        Rim(materialEditor, properties);
        Gradient(materialEditor, properties);
       
        
    }
    void Diffuse(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        
        MaterialProperty MainTexture = ShaderGUI.FindProperty("_MainTexture", properties);
        MaterialProperty DiffuseColor = ShaderGUI.FindProperty("_DiffuseColor", properties);

        GUILayout.Label("MAIN COLOR AND TEXTURE", EditorStyles.boldLabel);

        materialEditor.ShaderProperty(MainTexture, MainTexture.displayName);
        materialEditor.ShaderProperty(DiffuseColor, DiffuseColor.displayName);
        
        GUILayout.Label("_____________________________________________________________", EditorStyles.boldLabel);
    }
    /*void NormalMap(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        MaterialProperty UseNormalMap = ShaderGUI.FindProperty("_UseNormalMap", properties);
        MaterialProperty NormalMap = ShaderGUI.FindProperty("_NormalMap", properties);
        MaterialProperty NormalStrength = ShaderGUI.FindProperty("_NormalStrength", properties);
        MaterialProperty Smoothness = ShaderGUI.FindProperty("_Smoothness", properties);

        GUILayout.Label("NORMAL MAP", EditorStyles.boldLabel);

        materialEditor.ShaderProperty(UseNormalMap, UseNormalMap.displayName);

        if(UseNormalMap.floatValue == 1)
        {
            
            //materialEditor.ShaderProperty(Normal, Normal.displayName);
            materialEditor.ShaderProperty(NormalMap, NormalMap.displayName);
            materialEditor.ShaderProperty(NormalStrength, NormalStrength.displayName);
            materialEditor.ShaderProperty(Smoothness, Smoothness.displayName);

        }
        GUILayout.Label("_____________________________________________________________", EditorStyles.boldLabel);
    }*/

    void Emissive(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        MaterialProperty UseEmissive = ShaderGUI.FindProperty("_UseEmissive", properties);
        MaterialProperty EmissiveMap = ShaderGUI.FindProperty("_EmissiveMap", properties);
        MaterialProperty EmissiveColor = ShaderGUI.FindProperty("_EmissiveColor", properties);

        GUILayout.Label("EMISSIVE", EditorStyles.boldLabel);

        materialEditor.ShaderProperty(UseEmissive, UseEmissive.displayName);

        if (UseEmissive.floatValue == 1)
        {

            //materialEditor.ShaderProperty(Normal, Normal.displayName);
            materialEditor.ShaderProperty(EmissiveMap, EmissiveMap.displayName);
            materialEditor.ShaderProperty(EmissiveColor, EmissiveColor.displayName);

        }
        GUILayout.Label("_____________________________________________________________", EditorStyles.boldLabel);
    }

    void Shadow(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        MaterialProperty UseShadow = ShaderGUI.FindProperty("_UseShadow", properties);
        MaterialProperty UseRamp = ShaderGUI.FindProperty("_UseRamp", properties);
        MaterialProperty TextureRamp = ShaderGUI.FindProperty("_TextureRamp", properties);
        MaterialProperty ShadowColor = ShaderGUI.FindProperty("_ShadowColor", properties);
        MaterialProperty ShadowTexture = ShaderGUI.FindProperty("_ShadowTexture", properties);
        MaterialProperty ShadowTextureViewProjection = ShaderGUI.FindProperty("_ShadowTextureViewProjection", properties);
        MaterialProperty ShadowTextureTiling = ShaderGUI.FindProperty("_ShadowTextureTiling", properties);
        MaterialProperty ShadowTextureRotation = ShaderGUI.FindProperty("_ShadowTextureRotation", properties);
        MaterialProperty ShadowSize = ShaderGUI.FindProperty("_ShadowSize", properties);
        MaterialProperty ShadowBlend = ShaderGUI.FindProperty("_ShadowBlend", properties);
        MaterialProperty Level2 = ShaderGUI.FindProperty("_Level2", properties);
        MaterialProperty Level3 = ShaderGUI.FindProperty("_Level3", properties);
        MaterialProperty Animate = ShaderGUI.FindProperty("_Animate", properties);
        MaterialProperty XDirectionSpeed = ShaderGUI.FindProperty("_XDirectionSpeed", properties);
        MaterialProperty YDirectionSpeed = ShaderGUI.FindProperty("_YDirectionSpeed", properties);
        MaterialProperty UseShadowTexture = ShaderGUI.FindProperty("_UseShadowTexture", properties);
        //MaterialProperty AttenuationPower = ShaderGUI.FindProperty("_AttenuationPower", properties);

        GUILayout.Label("SHADOWS", EditorStyles.boldLabel);

        materialEditor.ShaderProperty(UseShadow, UseShadow.displayName);

        if (UseShadow.floatValue == 1 && UseRamp.floatValue == 0)
        {
            materialEditor.ShaderProperty(UseRamp, UseRamp.displayName);
            materialEditor.ShaderProperty(ShadowColor, ShadowColor.displayName);
            materialEditor.ShaderProperty(ShadowSize, ShadowSize.displayName);
            materialEditor.ShaderProperty(ShadowBlend, ShadowBlend.displayName);
            materialEditor.ShaderProperty(UseShadowTexture, UseShadowTexture.displayName);
            if (UseShadowTexture.floatValue == 1)
            {
                materialEditor.ShaderProperty(ShadowTexture, ShadowTexture.displayName);
                materialEditor.ShaderProperty(ShadowTextureTiling, ShadowTextureTiling.displayName);
                materialEditor.ShaderProperty(ShadowTextureRotation, ShadowTextureRotation.displayName);
                materialEditor.ShaderProperty(Level2, Level2.displayName);
                materialEditor.ShaderProperty(Level3, Level3.displayName);
                materialEditor.ShaderProperty(ShadowTextureViewProjection, ShadowTextureViewProjection.displayName);
                if (ShadowTextureViewProjection.floatValue == 1)
                {
                    materialEditor.ShaderProperty(Animate, Animate.displayName);
                    if (Animate.floatValue == 1)
                    {
                        materialEditor.ShaderProperty(XDirectionSpeed, XDirectionSpeed.displayName);
                        materialEditor.ShaderProperty(YDirectionSpeed, YDirectionSpeed.displayName);
                    }
                }
            }
        }
        if (UseShadow.floatValue == 1 && UseRamp.floatValue == 1)
        {
            materialEditor.ShaderProperty(UseRamp, UseRamp.displayName);
            materialEditor.ShaderProperty(TextureRamp, TextureRamp.displayName);
            materialEditor.ShaderProperty(ShadowSize, ShadowSize.displayName);
            //ShadowSize.floatValue = 0.475f;
        }
        GUILayout.Label("_____________________________________________________________", EditorStyles.boldLabel);
    }

    void Specular(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        MaterialProperty UseSpecular = FindProperty("_UseSpecular", properties);
        MaterialProperty SpecularMap = ShaderGUI.FindProperty("_SpecularMap", properties);
        MaterialProperty SpecularTextureViewProjextion = ShaderGUI.FindProperty("_SpecularTextureViewProjection", properties);
        MaterialProperty SpecularTextureTiling = ShaderGUI.FindProperty("_SpecularTextureTiling", properties);
        MaterialProperty SpecularTextureRotation = ShaderGUI.FindProperty("_SpecularTextureRotation", properties);
        MaterialProperty SpecularColor = ShaderGUI.FindProperty("_SpecularColor", properties);
        MaterialProperty SpecularLightColor = ShaderGUI.FindProperty("_SpecLightColor", properties);
        MaterialProperty SpecularLightIntensity = ShaderGUI.FindProperty("_SpecularLightIntensity", properties);
        MaterialProperty SpecularSize = ShaderGUI.FindProperty("_SpecularSize", properties);
        MaterialProperty SpecularBlend = ShaderGUI.FindProperty("_SpecularBlend", properties);

        GUILayout.Label("SPECULAR", EditorStyles.boldLabel);

        materialEditor.ShaderProperty(UseSpecular, UseSpecular.displayName);

        if (UseSpecular.floatValue == 1)
        {
            EditorGUILayout.BeginHorizontal();
            {

                GUILayout.Label("Specular Texture", FxStyles.labelStyle);
                materialEditor.TexturePropertySingleLine(FxStyles.textureLabel, SpecularMap);
            }
            EditorGUILayout.EndHorizontal();
            materialEditor.ShaderProperty(SpecularTextureViewProjextion, SpecularTextureViewProjextion.displayName);
            materialEditor.ShaderProperty(SpecularTextureTiling, SpecularTextureTiling.displayName);
            materialEditor.ShaderProperty(SpecularTextureRotation, SpecularTextureRotation.displayName);
            materialEditor.ShaderProperty(SpecularColor, SpecularColor.displayName);
            materialEditor.ShaderProperty(SpecularLightColor, SpecularLightColor.displayName);
            if (SpecularLightColor.floatValue == 1)
            {
                materialEditor.ShaderProperty(SpecularLightIntensity, SpecularLightIntensity.displayName);
            }
            materialEditor.ShaderProperty(SpecularSize, SpecularSize.displayName);
            materialEditor.ShaderProperty(SpecularBlend, SpecularBlend.displayName);
        }

        

        GUILayout.Label("_____________________________________________________________", EditorStyles.boldLabel);
    }

    void Rim (MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        MaterialProperty UseRim = FindProperty("_UseRim", properties);
        MaterialProperty RimColor = FindProperty("_RimColor", properties);
        MaterialProperty RimLightColor = FindProperty("_RimLightColor", properties);
        MaterialProperty RimIntensity = FindProperty("_RimLightIntensity", properties);
        MaterialProperty RimTexture = FindProperty("_RimTexture", properties);
        MaterialProperty RimTextureViewProjection = FindProperty("_RimTextureViewProjection", properties);
        MaterialProperty RimTextureTiling = FindProperty("_RimTextureTiling", properties);
        MaterialProperty RimTextureRotation = FindProperty("_RimTextureRotation", properties);
        MaterialProperty RimSize = FindProperty("_RimSize", properties);
        MaterialProperty RimBlend = FindProperty("_RimBlend", properties);

        GUILayout.Label("RIM HIGHLIGHT", EditorStyles.boldLabel);

        materialEditor.ShaderProperty(UseRim, UseRim.displayName);

        if(UseRim.floatValue == 1)
        {
            
            EditorGUILayout.BeginHorizontal();
            {

                GUILayout.Label("Rim Texture", FxStyles.labelStyle);
                materialEditor.TexturePropertySingleLine(FxStyles.textureLabel, RimTexture);
            }
            EditorGUILayout.EndHorizontal();
            materialEditor.ShaderProperty(RimTextureViewProjection, RimTextureViewProjection.displayName);
            materialEditor.ShaderProperty(RimTextureTiling, RimTextureTiling.displayName);
            materialEditor.ShaderProperty(RimTextureRotation, RimTextureRotation.displayName);
            materialEditor.ShaderProperty(RimColor, RimColor.displayName);
            materialEditor.ShaderProperty(RimLightColor, RimLightColor.displayName);
            if (RimLightColor.floatValue == 1)
            {
                materialEditor.ShaderProperty(RimIntensity, RimIntensity.displayName);
            }
            materialEditor.ShaderProperty(RimSize, RimSize.displayName);
            materialEditor.ShaderProperty(RimBlend, RimBlend.displayName);
            
        }

        

            GUILayout.Label("_____________________________________________________________", EditorStyles.boldLabel);
    }

    void Gradient(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        MaterialProperty UseGradient = FindProperty("_UseGradient", properties);
        MaterialProperty ColorA = FindProperty("_ColorA", properties);
        MaterialProperty ColorB = FindProperty("_ColorB", properties);
        MaterialProperty GradientSize = FindProperty("_GradientSize", properties);
        MaterialProperty GradientPosition = FindProperty("_GradientPosition", properties);
        MaterialProperty GradientRotation = FindProperty("_GradientRotation", properties);
        MaterialProperty ChangeAxis = FindProperty("_ChangeAxis", properties);

        GUILayout.Label("GRADIENT", EditorStyles.boldLabel);

        materialEditor.ShaderProperty(UseGradient, UseGradient.displayName);

        if(UseGradient.floatValue == 1)
        {
            materialEditor.ShaderProperty(ColorA, ColorA.displayName);
            materialEditor.ShaderProperty(ColorB, ColorB.displayName);
            materialEditor.ShaderProperty(GradientSize, GradientSize.displayName);
            materialEditor.ShaderProperty(GradientPosition, GradientPosition.displayName);
            materialEditor.ShaderProperty(GradientRotation, GradientRotation.displayName);
            materialEditor.ShaderProperty(ChangeAxis, ChangeAxis.displayName);
        }
        GUILayout.Label("_____________________________________________________________", EditorStyles.boldLabel);
    }

    

    public static class FxStyles
    {
        public static GUIStyle header;
        public static GUIStyle headerCheckbox;
        public static GUIStyle headerFoldout;
        public static GUIStyle headerTab;
        public static GUIStyle labelStyle;
        public static GUIStyle HeaderTexture;
        public static GUIContent textureLabel;
        public static GUIContent textureLabel2;
        public static GUIStyle colorPicker;
        public static GUIStyle topIMG;


        static FxStyles()
        {
            // Tab header
            header = new GUIStyle("ShurikenModuleTitle");
            header.font = (new GUIStyle("Label")).font;
            header.border = new RectOffset(15, 7, 4, 4);
            header.fixedHeight = 24;
            header.contentOffset = new Vector2(20f, -2f);
            header.alignment = TextAnchor.MiddleCenter;
            header.fontSize = 12;
            header.fontStyle = FontStyle.Bold;

            // Tab header checkbox
            headerCheckbox = new GUIStyle("ShurikenCheckMark");
            headerFoldout = new GUIStyle("Foldout");

            labelStyle = new GUIStyle(EditorStyles.label);
            //labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.alignment = TextAnchor.MiddleLeft;
            labelStyle.fontSize = 11;
            labelStyle.normal.textColor = new Color32(0, 0, 0, 255);
            //labelStyle.stretchWidth = 10;

            HeaderTexture = new GUIStyle(EditorStyles.label);
            HeaderTexture.alignment = TextAnchor.MiddleCenter;



            textureLabel = new GUIContent();
            textureLabel2 = new GUIContent();

            colorPicker = new GUIStyle(EditorStyles.colorField);
            colorPicker.fixedWidth = 85;

            topIMG = new GUIStyle();
            topIMG.alignment = TextAnchor.MiddleCenter;
        }

        public static bool Header(string title, bool foldout, Color color)
        {
            var rect = GUILayoutUtility.GetRect(16f, 22f, FxStyles.header);
            var auxColor = GUI.color;
            GUI.color = color;
            UnityEngine.GUI.Box(rect, title, FxStyles.header);
            GUI.color = auxColor;

            var foldoutRect = new Rect(rect.x + 4f, rect.y + 2f, 13f, 13f);
            var e = Event.current;



            return foldout;
        }

        public static bool Header(string title, bool foldout, SerializedProperty enabledField, Color color)
        {
            var enabled = enabledField.boolValue;

            var rect = GUILayoutUtility.GetRect(16f, 22f, FxStyles.header);
            var auxColor = GUI.color;
            GUI.color = color;
            UnityEngine.GUI.Box(rect, title, FxStyles.header);
            GUI.color = auxColor;

            var toggleRect = new Rect(rect.x + 4f, rect.y + 4f, 13f, 13f);
            var e = Event.current;

            if (e.type == EventType.Repaint) FxStyles.headerCheckbox.Draw(toggleRect, false, false, enabled, false);

            if (e.type == EventType.MouseDown)
            {
                const float kOffset = 2f;
                toggleRect.x -= kOffset;
                toggleRect.y -= kOffset;
                toggleRect.width += kOffset * 2f;
                toggleRect.height += kOffset * 2f;

                if (toggleRect.Contains(e.mousePosition))
                {
                    enabledField.boolValue = !enabledField.boolValue;
                    e.Use();
                }
                else if (rect.Contains(e.mousePosition))
                {
                    foldout = !foldout;
                    e.Use();
                }
            }

            return foldout;
        }

        public static bool Header(string title, bool foldout, MaterialProperty enabledField, Color color)
        {
            var enabled = (enabledField.floatValue == 1);

            var rect = GUILayoutUtility.GetRect(16f, 22f, FxStyles.header);
            var auxColor = GUI.color;
            GUI.color = color;
            UnityEngine.GUI.Box(rect, title, FxStyles.header);
            GUI.color = auxColor;

            var toggleRect = new Rect(rect.x + 4f, rect.y + 4f, 13f, 13f);
            var e = Event.current;

            if (e.type == EventType.Repaint) FxStyles.headerCheckbox.Draw(toggleRect, false, false, enabled, false);

            if (e.type == EventType.MouseDown)
            {
                const float kOffset = 2f;
                toggleRect.x -= kOffset;
                toggleRect.y -= kOffset;
                toggleRect.width += kOffset * 2f;
                toggleRect.height += kOffset * 2f;

                if (toggleRect.Contains(e.mousePosition))
                {
                    enabledField.floatValue = (enabledField.floatValue == 0) ? 1 : 0;
                    e.Use();
                }

            }

            return foldout;
        }
    }
}

