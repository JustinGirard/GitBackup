using UnityEditor;
using UnityEngine;

namespace FXV
{
    [CustomPropertyDrawer(typeof(fxvHeaderAttribute))]
    public class fxvHeaderDrawer : DecoratorDrawer
    {
#if UNITY_EDITOR
        fxvHeaderAttribute header
        {
            get { return ((fxvHeaderAttribute)attribute); }
        }

        public override float GetHeight()
        {
            return base.GetHeight() + header.spaceHeight;
        }

        public override void OnGUI(Rect position)
        {
            position = EditorGUI.IndentedRect(position);

            position.yMin += 4;

            float lineX = (position.x);
            float lineY = position.y + 4;
            float lineWidth = position.width;
            float lineHeight = header.lineHeight;

            GUI.DrawTexture(new Rect(lineX, lineY, lineWidth, header.spaceHeight + 7), Texture2D.grayTexture);
            EditorGUI.DrawRect(new Rect(lineX, lineY, lineWidth, lineHeight), header.lineColor);
            position.yMin += header.lineHeight + 4;
            EditorGUI.LabelField(position, header.name, HeaderStyle);
        }

        public static GUIStyle HeaderStyle
        {
            get
            {
                GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
                labelStyle.fontStyle = FontStyle.Normal;
                labelStyle.alignment = TextAnchor.UpperCenter;
                labelStyle.fontSize = 14;
                labelStyle.normal.textColor = new Color32(220, 220, 220, 255);
                return labelStyle;
            }
        }
#endif
    }
}