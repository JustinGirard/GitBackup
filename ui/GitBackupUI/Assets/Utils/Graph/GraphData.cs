using System.Collections.Generic;
using UnityEngine;
using VisualGraph;

namespace DataGraph
{
    [CreateAssetMenu(fileName = "GraphData", menuName = "Graph/GraphData", order = 1)]
    public class GraphData : ScriptableObject
    {
        public List<NodeData> Nodes = new List<NodeData>();
        public List<GroupData> Edges = new List<GroupData>();
    }


    [System.Serializable]
    public class NodeData
    {
        public int ID;
        public Vector3 Position;
    }

    [System.Serializable]
    public class GroupData
    {
        public int SourceID;
        public int DestinationID;
    }
}