using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FXV.ShieldEditorUtils
{
    [CustomEditor(typeof(Shield), true)]
    public class fxvShieldEditor : UnityEditor.Editor
    {
        readonly GUIStyle style = new GUIStyle();

        void OnEnable()
        {
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.white;
            style.normal.background = (Texture2D)(Resources.Load("GizmoLabelBg") as Texture2D);
        }

        public void OnSceneGUI()
        {
            var property = serializedObject.GetIterator();
            while (property.Next(true))
            {
                if (property.propertyType == SerializedPropertyType.Vector3)
                {
                    Shield shieldObject = (Shield)serializedObject.targetObject;
                    var field = serializedObject.targetObject.GetType().GetField(property.name, BindingFlags.Instance | BindingFlags.NonPublic);
                    if (field == null)
                    {
                        continue;
                    }
                    var draggablePoints = field.GetCustomAttributes(typeof(ShieldDraggablePoint), false);
                    if (draggablePoints.Length > 0)
                    {
                        Vector3 worldPosition = shieldObject.GetRootTransform().TransformPoint(property.vector3Value);
                        Handles.Label(worldPosition, property.name, style);
                        Handles.DrawLine(shieldObject.transform.position, worldPosition);
                        Vector3 newPosition = Handles.PositionHandle(worldPosition, Quaternion.identity);
                        if ((newPosition - worldPosition).magnitude > 0.001f)
                        {
                            property.vector3Value = shieldObject.GetRootTransform().InverseTransformPoint(newPosition);

                            serializedObject.ApplyModifiedProperties();
                        }
                    }
                }
            }
        }

        public override void OnInspectorGUI()
        {
            Shield shieldObject = (Shield)target;
            serializedObject.Update();

            List<string> hiddenProperties = new List<string>();
            if (!shieldObject.IsHitDecalTextureEnabled())
            {
                hiddenProperties.Add("hitRippleScale");
                hiddenProperties.Add("hitRippleDistortion");
            }

            DrawPropertiesExcluding(serializedObject, hiddenProperties.ToArray());

            serializedObject.ApplyModifiedProperties();
        }
    }
}