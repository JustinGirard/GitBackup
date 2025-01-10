using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FXV.ShieldEditorUtils
{
    [CustomEditor(typeof(ShieldRimTextureBake))]
    public class fxvShieldRimTextureBakeEditor : UnityEditor.Editor
    {
        readonly GUIStyle style = new GUIStyle();

        void OnEnable()
        {
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.white;
            style.normal.background = (Texture2D)(Resources.Load("GizmoLabelBg") as Texture2D);
        }

        private void OnSceneGUI()
        {
            var property = serializedObject.GetIterator();
            while (property.Next(true))
            {
                if (property.propertyType == SerializedPropertyType.Vector3)
                {
                    ShieldRimTextureBake bakeScript = (ShieldRimTextureBake)serializedObject.targetObject;
                    var field = serializedObject.targetObject.GetType().GetField(property.name, BindingFlags.Instance | BindingFlags.NonPublic);
                    if (field == null)
                    {
                        continue;
                    }
                    var draggablePoints = field.GetCustomAttributes(typeof(ShieldDraggablePoint), false);
                    if (draggablePoints.Length > 0)
                    {
                        Vector3 worldPosition = bakeScript.transform.TransformPoint(property.vector3Value);
                        Vector3 direction = (bakeScript.transform.position - worldPosition).normalized;
                        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

                        Handles.Label(worldPosition, property.name, style);
                        Handles.DrawLine(bakeScript.transform.position, worldPosition);

                        Handles.color = Color.yellow;
                        Handles.RectangleHandleCap(0, worldPosition, rotation, 1.0f, EventType.Repaint);

                        Vector3 newPosition = Handles.PositionHandle(worldPosition, Quaternion.identity);
                        if ((newPosition - worldPosition).magnitude > 0.001f)
                        {
                            property.vector3Value = bakeScript.transform.InverseTransformPoint(newPosition);

                            serializedObject.ApplyModifiedProperties();
                        }
                    }
                }
            }
        }

        public static Texture2D SaveTextureToPath(Texture2D texture, string texturePath)
        {
            ShieldEditorUtils.fxvEditorTextureUtils.SetTextureReadable(texture, true);
            ShieldEditorUtils.fxvEditorTextureUtils.SetTextureCompression(texture, TextureImporterCompression.Uncompressed);

            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(texturePath, bytes);

            ShieldEditorUtils.fxvEditorTextureUtils.SetTextureReadable(texture, false);
            ShieldEditorUtils.fxvEditorTextureUtils.SetTextureCompression(texture, TextureImporterCompression.CompressedHQ);

            AssetDatabase.Refresh();

            return (Texture2D)AssetDatabase.LoadAssetAtPath(texturePath, typeof(Texture2D));
        }

        public override void OnInspectorGUI()
        {
            ShieldRimTextureBake bakeScript = (ShieldRimTextureBake)target;

            DrawDefaultInspector();

            EditorGUILayout.Separator();

            if (GUILayout.Button("Bake Rim Texture"))
            {
                ShieldRimTextureBake.BakeRimParams bakeParams = bakeScript.GetBakeParams();

                Texture2D texture = ShieldEditorUtils.fxvEditorMeshUtils.BakeUsingEdgeDetect(bakeScript.transform, bakeScript.GetComponent<MeshFilter>().sharedMesh, ShieldEditorUtils.fxvEditorUVUtils.UVINDEX.UV, bakeParams);

                var g = AssetDatabase.FindAssets($"t:Script {nameof(fxvShieldRimTextureBakeEditor)}");
                string scriptPath = AssetDatabase.GUIDToAssetPath(g[0]);

                string savePath = Path.GetDirectoryName(scriptPath);
                savePath = Path.GetDirectoryName(savePath);

                savePath = Path.Combine(savePath, "Generated");

                if (!Directory.Exists(savePath))
                {
                    Directory.CreateDirectory(savePath);
                }

                savePath = Path.Combine(savePath, "texture.png");

                SaveTextureToPath(texture, savePath);
            }

            EditorGUILayout.Separator();
        }
    }
}
