using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
[ExecuteInEditMode]
public class Graph : MonoBehaviour
{
    public GameObject NodePrefab;
    public GameObject EdgePrefab;
    public PanelSettings NodePanelSettings;
    private Dictionary<int, GameObject> nodeObjects = new Dictionary<int, GameObject>();


    public void RenderGraph(List<NodeData> nodes, List<GroupData> edges)
    {
        // Step 1: Reconcile Node GameObjects
        HashSet<int> existingNodeIDs = new HashSet<int>(nodeObjects.Keys);

        foreach (NodeData nodeData in nodes)
        {
            if (!nodeObjects.ContainsKey(nodeData.ID))
            {
                // Create a new GameObject for this node
                GameObject nodeObj = Instantiate(NodePrefab, nodeData.Position, Quaternion.identity, transform);
                nodeObj.name = "Node_" + nodeData.ID;

                Node nodeScript = nodeObj.GetComponent<Node>();
                if (nodeScript == null)
                {
                    nodeScript = nodeObj.AddComponent<Node>();
                }
                nodeScript.ID = nodeData.ID;

                nodeObjects[nodeData.ID] = nodeObj;
            }
            else
            {
                // Update existing node position if necessary
                GameObject existingNodeObj = nodeObjects[nodeData.ID];
                existingNodeObj.transform.position = nodeData.Position;
            }

            // Mark this node as reconciled
            existingNodeIDs.Remove(nodeData.ID);
        }

        // Remove any GameObjects for nodes that no longer exist in the data
        foreach (int orphanNodeID in existingNodeIDs)
        {
            if (nodeObjects.TryGetValue(orphanNodeID, out GameObject orphanNode))
            {
                DestroyImmediate(orphanNode);
                nodeObjects.Remove(orphanNodeID);
            }
        }

        // Step 2: Reconcile Edge GameObjects
        HashSet<string> existingEdgeNames = new HashSet<string>();
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Edge_"))
            {
                existingEdgeNames.Add(child.name);
            }
        }

        foreach (GroupData edgeData in edges)
        {
            string edgeName = $"Edge_{edgeData.SourceID}_{edgeData.DestinationID}";

            if (!existingEdgeNames.Contains(edgeName))
            {
                if (nodeObjects.ContainsKey(edgeData.SourceID) && nodeObjects.ContainsKey(edgeData.DestinationID))
                {
                    // Create a new GameObject for this edge
                    GameObject edgeObj = Instantiate(EdgePrefab, transform);
                    edgeObj.name = edgeName;

                    Edge edgeScript = edgeObj.GetComponent<Edge>();
                    if (edgeScript == null)
                    {
                        edgeScript = edgeObj.AddComponent<Edge>();
                    }

                    edgeScript.SetNodes(nodeObjects[edgeData.SourceID], nodeObjects[edgeData.DestinationID]);
                }
            }

            // Mark this edge as reconciled
            existingEdgeNames.Remove(edgeName);
        }

        // Remove any GameObjects for edges that no longer exist in the data
        foreach (string orphanEdgeName in existingEdgeNames)
        {
            Transform orphanEdge = transform.Find(orphanEdgeName);
            if (orphanEdge != null)
            {
                DestroyImmediate(orphanEdge.gameObject);
            }
        }
    }

}
