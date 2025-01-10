using UnityEditor;
using UnityEngine;

namespace FXV.VolumetricFogEditorUtils
{
    public partial class fxvIntroWindow : EditorWindow
    {
#pragma warning disable CS0414
        static string builtInVersion = "version 1.0.11";
#pragma warning restore CS0414

        void Setup_BuiltIn_AfterImport()
        {
            FXV.Internal.fxvFogAssetConfig.UpdateShadersForActiveRenderPipeline();
        }

        void GUI_BuiltIn_AfterImport()
        {
            GUILine(Color.gray);
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Check demo scenes for fog examples.");

                if (GUILayout.Button("Show", GUILayout.Width(buttonWidth)))
                {
                    Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(assetPath + "/Demo/Demo1.unity");
                }
            }
            GUILayout.EndHorizontal();

            GUILine(Color.gray);
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("To quickly add fog to scene [RightClick -> FXV -> Fog (type)] in Hierarchy panel.");
            }
            GUILayout.EndHorizontal();

            GUILine(Color.gray);
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("When adding big fog groups consider parenting them under object with VolumeFogGroup component.");

                if (GUILayout.Button("Show", GUILayout.Width(buttonWidth)))
                {
                    Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(assetPath + "/Scripts/VolumeFogGroup.cs");
                }
            }
            GUILayout.EndHorizontal();

            GUILine(Color.gray);
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Please read documentation for implementation guidelines.");

                if (GUILayout.Button("Show", GUILayout.Width(buttonWidth)))
                {
                    Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(assetPath + "/Documentation.pdf");
                }
            }
            GUILayout.EndHorizontal();
            GUILine(Color.gray);
        }
    }
}