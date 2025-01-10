using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FXV.ShieldEditorUtils
{
    [CustomEditor(typeof(ShieldPostprocess), true)]
    public class fxvShieldPostprocessEditor : UnityEditor.Editor
    {
#if UNITY_ANDROID || UNITY_IOS || UNITY_WEBGL
        GUIStyle _errorStyle = null;
#endif

        public override void OnInspectorGUI()
        {
            ShieldPostprocess p = (ShieldPostprocess)target;
            serializedObject.Update();

            List<string> hiddenProperties = new List<string>();

#if UNITY_ANDROID || UNITY_IOS || UNITY_WEBGL
            if (p.enabled)
            {
                if (_errorStyle == null)
                {
                    _errorStyle = new GUIStyle(EditorStyles.boldLabel);
                    _errorStyle.normal.textColor = Color.red;
                }
                GUILayout.Label("!!! Postprocess should not be used on\nmobile platforms for performance reasons.", _errorStyle);
            }
#endif

            if (GraphicsSettings.currentRenderPipeline != null) //not BuiltIn ?
            {
                hiddenProperties.Add("drawOrder");
            }

            if (!p.PostprocesOnSeparateAxes())
            {
                hiddenProperties.Add("kernelRadiusVertical");
                hiddenProperties.Add("sigmaVertical");
                hiddenProperties.Add("sampleStepVertical");
            }

            if (!p.IsGloballIlluminationSupported())
            {
                hiddenProperties.Add("globalIllumination");
            }

            if (!p.IsGloballIlluminationSupported() || !p.IsGloballIlluminationEnabled())
            {
                hiddenProperties.Add("giSampleRadius");
                hiddenProperties.Add("giNumberOfSamples");
                hiddenProperties.Add("giIntensity");
                hiddenProperties.Add("giLightRange");
                hiddenProperties.Add("giDenoiseStepWidth");
                hiddenProperties.Add("giDenoiseStepChange");
                hiddenProperties.Add("giDenoiseIterations");
                hiddenProperties.Add("giAtIteration");
            }

            DrawPropertiesExcluding(serializedObject, hiddenProperties.ToArray());

            if (!p.IsGloballIlluminationSupported())
            {
                EditorGUILayout.LabelField("    [INFO] Global Illumination is only avaialble in Deffered rendering.");
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}