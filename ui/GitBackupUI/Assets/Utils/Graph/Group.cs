using UnityEngine;
using UnityEngine.ProBuilder.Shapes;

namespace VisualGraph
{
    [ExecuteInEditMode]
    public class Edge : MonoBehaviour
    {
        public GameObject sourceNode;
        public GameObject destinationNode;
        public IEdgeStyle selectedStyle = null;
        public float thickness = 0.05f;
        public Color lineColor = Color.white;
        public float gapLengthA = 0.2f;  // Gap from start and end of the line
        public float gapLengthB = 0.3f;  // Gap from start and end of the line



        public enum EdgeStyleType
        {
            Line,
            Cube
        }

        [Header("Edge Settings")]
        [SerializeField] private EdgeStyleType edgeStyleType = EdgeStyleType.Line;

        /// <summary>
        /// Sets the edge type.
        /// </summary>
        public void SetType(EdgeStyleType type)
        {
            edgeStyleType = type;
        }

        /// <summary>
        /// Gets the current edge type.
        /// </summary>
        public EdgeStyleType GetType()
        {
            return edgeStyleType;
        }

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
            //U3DGraphManager graphManager = FindObjectOfType<U3DGraphManager>();
            //if (graphManager != null)
            //{
            //    graphManager.RemoveEdge(this);
            //}
        }    

        public virtual void UpdatePosition()
        {
            if (edgeStyleType == EdgeStyleType.Line)
                selectedStyle = new LineEdgeStyle();
            if (edgeStyleType == EdgeStyleType.Cube)
                selectedStyle = new CubeEdgeStyle();
            //Debug.Log("-------------------------");
            //Debug.Log(this.gameObject);
            //Debug.Log(sourceNode);
            //Debug.Log(destinationNode);
            //Debug.Log(selectedStyle);
            
            selectedStyle.UpdatePosition( 
                                self:this.gameObject,  
                                sourceNode:sourceNode,  
                                destinationNode:destinationNode,
                                thickness:thickness,
                                gapLengthA:gapLengthA,
                                gapLengthB:gapLengthB,
                                color:lineColor); 
            

            
        }
    }
    

}
