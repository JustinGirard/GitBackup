using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Node))]
public class NodeEditor : Editor
{
    private static Node startEdge = null; // Keeps track of the starting node for edge creation

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Node currentNode = (Node)target;

        if (GUILayout.Button("Draw Edge"))
        {
            // Set the current node as the start of the edge
            startEdge = currentNode;
            Debug.Log($"Start Node for Edge Set: {currentNode.name}");
        }
    }

    // Respond to scene clicks
    private void OnSceneGUI()
    {
        // If we have a start node selected and the user clicks on a destination node
        if (startEdge != null && Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Node clickedNode = hit.collider.GetComponent<Node>();
                if (clickedNode != null && clickedNode != startEdge)
                {
                    // Create an edge between startEdge and clickedNode
                    Debug.Log($"Creating Edge from {startEdge.name} to {clickedNode.name}");

                    U3DGraphManager graphManager = FindObjectOfType<U3DGraphManager>();
                    if (graphManager != null)
                    {
                        graphManager.CreateEdge(startEdge, clickedNode);
                    }
                    else
                    {
                        Debug.LogError("No U3DGraphManager found in the scene!");
                    }

                    // Reset startEdge
                    startEdge = null;

                    // Consume the event
                    Event.current.Use();
                }
            }
        }
    }
}
