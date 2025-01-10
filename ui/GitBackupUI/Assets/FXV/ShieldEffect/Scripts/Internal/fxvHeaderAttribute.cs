using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FXV
{
    public class fxvHeaderAttribute : PropertyAttribute
    {
        public readonly string name;
        public readonly float spaceHeight;
        public readonly float lineHeight;
        public readonly Color lineColor = Color.red;

        public fxvHeaderAttribute(string name)
        {
            this.name = name;
            this.spaceHeight = 14;
            this.lineHeight = 1;

            this.lineColor = new Color32(220, 220, 220, 255);
        }
    }
}