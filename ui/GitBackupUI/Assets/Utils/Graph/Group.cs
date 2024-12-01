using UnityEngine;

[ExecuteInEditMode]
public class Edge : MonoBehaviour
{
    public GameObject sourceNode;
    public GameObject destinationNode;

    void Update()
    {
        if (sourceNode != null && destinationNode != null)
        {
            UpdatePosition();
        }
    }

    public void SetNodes(GameObject source, GameObject destination)
    {
        sourceNode = source;
        destinationNode = destination;
        UpdatePosition();
    }
    private void OnDestroy()
    {
        U3DGraphManager graphManager = FindObjectOfType<U3DGraphManager>();
        if (graphManager != null)
        {
            graphManager.RemoveEdge(this);
        }
    }    

    void UpdatePosition()
    {
        Vector3 startPos = sourceNode.transform.position;
        Vector3 endPos = destinationNode.transform.position;
        Vector3 midPoint = (startPos + endPos) / 2;

        transform.position = midPoint;
        transform.LookAt(destinationNode.transform);

        float distance = Vector3.Distance(startPos, endPos);

        // Create or update the visual representation
        Transform visual;
        if (transform.childCount == 0)
        {
            GameObject visualObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visualObj.transform.SetParent(transform, false);
            visual = visualObj.transform;
        }
        else
        {
            visual = transform.GetChild(0);
        }
        visual.localScale = new Vector3(0.1f, 0.1f, distance);
    }
}
