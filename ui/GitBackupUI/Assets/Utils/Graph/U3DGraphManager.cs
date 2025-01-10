using UnityEngine;
using DataGraph;

namespace VisualGraph
{
    [ExecuteInEditMode]
    public class U3DGraphManager : MonoBehaviour
    {
        public GraphData graphData; // Reference to the ScriptableObject

        private void Start()
        {
            // Load and render the graph when entering play mode or edit mode
            if (graphData != null)
            {
                RenderGraph();
            }
        }

        public void RenderGraph()
        {
            Graph graph = GetComponent<Graph>();
            if (graph != null)
            {
                graph.RenderGraph(graphData.Nodes, graphData.Edges);
            }
        }

        public void AddNode(Vector3 position)
        {
            // Create a new NodeData and add it to the graphData
            int newID = graphData.Nodes.Count > 0 ? graphData.Nodes[graphData.Nodes.Count - 1].ID + 1 : 0;
            NodeData newNode = new NodeData { ID = newID, Position = position };
            graphData.Nodes.Add(newNode);
            SaveChanges();
            RenderGraph();
        }

        public void RemoveNode(Node node)
        {
            // Remove node and associated edges
            graphData.Nodes.RemoveAll(n => n.ID == node.ID);
            graphData.Edges.RemoveAll(e => e.SourceID == node.ID || e.DestinationID == node.ID);
            SaveChanges();
            RenderGraph();
        }

        public void CreateEdge(Node node1, Node node2)
        {
            // Check if edge already exists
            if (graphData.Edges.Exists(e =>
                (e.SourceID == node1.ID && e.DestinationID == node2.ID) ||
                (e.SourceID == node2.ID && e.DestinationID == node1.ID)))
            {
                Debug.LogWarning("Edge already exists between these nodes.");
                return;
            }

            // Add new edge to the graphData
            GroupData newEdge = new GroupData
            {
                SourceID = node1.ID,
                DestinationID = node2.ID
            };
            graphData.Edges.Add(newEdge);
            SaveChanges();
            RenderGraph();
        }

        public void RemoveEdge(Edge edge)
        {
            // Find the source and destination IDs of the edge
            int sourceID = edge.sourceNode.GetComponent<Node>().ID;
            int destinationID = edge.destinationNode.GetComponent<Node>().ID;

            // Find the exact edge in the graphData and remove it
            GroupData edgeToRemove = graphData.Edges.Find(e =>
                (e.SourceID == sourceID && e.DestinationID == destinationID) ||
                (e.SourceID == destinationID && e.DestinationID == sourceID));

            if (edgeToRemove != null)
            {
                graphData.Edges.Remove(edgeToRemove);
                Debug.Log($"Edge removed: {sourceID} -> {destinationID}");
            }
            else
            {
                Debug.LogWarning($"Edge not found: {sourceID} -> {destinationID}");
            }

            // Save changes to the ScriptableObject and re-render the graph
            SaveChanges();
            RenderGraph();
        }

        public void SaveChanges()
        {
            // Mark the ScriptableObject as dirty to persist changes
            #if UNITY_EDITOR        
            UnityEditor.EditorUtility.SetDirty(graphData);
            #endif
        }
        public void GenerateGraphData()
        {
            #if UNITY_EDITOR        

            // Delete the existing ScriptableObject
            if (graphData != null)
            {
                string path = UnityEditor.AssetDatabase.GetAssetPath(graphData);
                if (!string.IsNullOrEmpty(path))
                {
                    UnityEditor.AssetDatabase.DeleteAsset(path);
                    UnityEditor.AssetDatabase.SaveAssets();
                    UnityEditor.AssetDatabase.Refresh();
                }
            }

            // Create a new ScriptableObject in memory
            graphData = ScriptableObject.CreateInstance<GraphData>();

            // Save the new ScriptableObject as an asset
            string assetPath = "Assets/Utils/Graph/DemoGraphData.asset";
            UnityEditor.AssetDatabase.CreateAsset(graphData, assetPath);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            Debug.Log("New GraphData ScriptableObject created at: " + assetPath);

            // Populate the new graph data with default values
            InitializeGraph();

            // Render the graph
            RenderGraph();
            #endif

        }

        private void InitializeGraph()
        {
            // Clear existing data
            graphData.Nodes.Clear();
            graphData.Edges.Clear();

            // Create 4 nodes
            for (int i = 0; i < 4; i++)
            {
                NodeData node = new NodeData
                {
                    ID = i,
                    Position = new Vector3(i * 2.0f, 0, 0) // Spacing nodes along the X-axis
                };
                graphData.Nodes.Add(node);
            }

            // Create edges for a fully connected graph
            for (int i = 0; i < graphData.Nodes.Count; i++)
            {
                for (int j = i + 1; j < graphData.Nodes.Count; j++)
                {
                    GroupData edge = new GroupData
                    {
                        SourceID = graphData.Nodes[i].ID,
                        DestinationID = graphData.Nodes[j].ID
                    };
                    graphData.Edges.Add(edge);
                }
            }
        }


    }
}