using UnityEngine;
using UnityEngine.UIElements;
[ExecuteInEditMode]
public class Node : MonoBehaviour
{
    public int ID;

    void Start()
    {
        CreateVisual();
    }
    private void Update()
    {
        AlignLabelWithCamera();
        SyncPosition();
    }
    void CreateVisual()
    {
        // Check if visual already exists
        if (transform.childCount == 0)
        {
            // Visual representation of the node (e.g., a square)
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.transform.SetParent(transform, false);
            visual.transform.localScale = Vector3.one * 0.5f;
        }
    }
    private void OnDestroy()
    {
        U3DGraphManager graphManager = FindObjectOfType<U3DGraphManager>();
        if (graphManager != null)
        {
            graphManager.RemoveNode(this);
        }
    }
    // Public method to request an edge creation

    private void SyncPosition()
    {
        U3DGraphManager graphManager = FindObjectOfType<U3DGraphManager>();
        if (graphManager != null && graphManager.graphData != null)
        {
            NodeData nodeData = graphManager.graphData.Nodes.Find(n => n.ID == ID);
            if (nodeData != null)
            {
                nodeData.Position = transform.position;
                graphManager.SaveChanges();
            }
        }
    }
private void AlignLabelWithCamera()
{
    Camera cameraToFace = null;

    // In play mode, use the main camera
    if (Application.isPlaying)
    {
        cameraToFace = Camera.main;
    }
    else
    {
        // In edit mode, find the Scene View camera
        var sceneViewCamera = UnityEditor.SceneView.lastActiveSceneView?.camera;
        if (sceneViewCamera != null)
        {
            cameraToFace = sceneViewCamera;
        }
    }

    if (cameraToFace == null) return;

    // Find the child Canvas
    Canvas canvas = GetComponentInChildren<Canvas>();
    if (canvas == null) return;

    // Align the Canvas with the camera's forward direction, ensuring it's not backwards
    Vector3 cameraForward = cameraToFace.transform.forward;
    canvas.transform.rotation = Quaternion.LookRotation(cameraForward, Vector3.up);
}



    
 
}
