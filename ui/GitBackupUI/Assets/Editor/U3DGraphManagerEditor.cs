using UnityEditor;
using UnityEngine;
using VisualGraph;

[CustomEditor(typeof(U3DGraphManager))]
public class U3DGraphManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        //Debug.Log("U3DGraphManagerEditor is active.");         
        DrawDefaultInspector();

        U3DGraphManager myScript = (U3DGraphManager)target;

        if (GUILayout.Button("Generate Graph"))
        {
            myScript.GenerateGraphData();
        }
    }
}
