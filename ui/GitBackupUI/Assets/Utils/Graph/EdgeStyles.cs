using UnityEngine;


/*
    void Update()
    {
        if (lineRenderer == null)
            SetupLineRenderer();
        if (dottedLineA != null && dottedLineB != null)
        {
            DrawGappedLine(dottedLineA.transform.position, dottedLineB.transform.position);
        }
    }

    /// <summary>
    /// Initializes the LineRenderer with default settings.
    /// </summary>
    private void SetupLineRenderer()
    {
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        lineRenderer.startWidth = thickness;
        lineRenderer.endWidth = thickness;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        lineRenderer.positionCount = 2;  // Two points for a straight line
    }

    /// <summary>
    /// Draws a single continuous line between two points with a gap at each end.
    /// </summary>
    private void DrawGappedLine(Vector3 start, Vector3 end)
    {
        Vector3 direction = (end - start).normalized;
        float distance = Vector3.Distance(start, end);

        // Apply gap at the start and end
        Vector3 newStart = start + direction * gapLengthA;
        Vector3 newEnd = end - direction * gapLengthB;

        // Update LineRenderer positions
        lineRenderer.SetPosition(0, newStart);
        lineRenderer.SetPosition(1, newEnd);
    }
*/

namespace VisualGraph
{
 
   public interface IEdgeStyle
    {
       void UpdatePosition(GameObject self, 
                            GameObject sourceNode, 
                            GameObject destinationNode,
                            float thickness,
                            float gapLengthA,
                            float gapLengthB,
                            Color color
                            );
    }

    public class CubeEdgeStyle: IEdgeStyle
    {

        public virtual void UpdatePosition(GameObject self, 
                            GameObject sourceNode, 
                            GameObject destinationNode,
                            float thickness,
                            float gapLengthA,
                            float gapLengthB,
                            Color color        
        )
        {
            Vector3 startPos = sourceNode.transform.position;
            Vector3 endPos = destinationNode.transform.position;
            Vector3 midPoint = (startPos + endPos) / 2;

            self.transform.position = midPoint;
            self.transform.LookAt(destinationNode.transform);

            float distance = Vector3.Distance(startPos, endPos);

            // Create or update the visual representation
            Transform visual;
            if (self.transform.childCount == 0)
            {
                GameObject visualObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visualObj.transform.SetParent(self.transform, false);
                visual = visualObj.transform;
            }
            else
            {
                visual = self.transform.GetChild(0);
            }
            visual.localScale = new Vector3(0.1f, 0.1f, distance);            
        }

    }

    public class LineEdgeStyle : IEdgeStyle
    {

        //public float thickness = 0.05f;
        //public Color lineColor = Color.white;
        //public float gapLengthA = 0.2f;  // Gap from start and end of the line
        //public float gapLengthB = 0.3f;  // Gap from start and end of the line

        private LineRenderer lineRenderer;

         public virtual void UpdatePosition(GameObject self, 
                            GameObject sourceNode, 
                            GameObject destinationNode,
                            float thickness,
                            float gapLengthA,
                            float gapLengthB,
                            Color color                 
         )
        {
            //Debug.Log("UpdatePosition running");
            if (lineRenderer == null)
                SetupLineRenderer(self,
                             thickness,
                             gapLengthA,
                             gapLengthB,
                             color                   
                
                );
            if (sourceNode != null && destinationNode != null)
            {
                //Debug.Log("Should be drawing a line");
                DrawLine(sourceNode.transform.position, destinationNode.transform.position, gapLengthA,  gapLengthB);
            }
        }

        /// <summary>
        /// Initializes the LineRenderer with default settings.
        /// </summary>
        private void SetupLineRenderer(GameObject self,
                            float thickness,
                            float gapLengthA,
                            float gapLengthB,
                            Color color           
        )
        {
            if (lineRenderer != null)
                return;
            if (lineRenderer == null)
                lineRenderer = self.GetComponent<LineRenderer>();
            if (lineRenderer != null)
                return;
            if (lineRenderer == null)
                lineRenderer = self.AddComponent<LineRenderer>();

            lineRenderer.startWidth = thickness;
            lineRenderer.endWidth = thickness;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.positionCount = 2;  // Two points for a straight line
        }

        /// <summary>
        /// Draws a single continuous line between two points.
        /// </summary>
        private void DrawLine(Vector3 start, Vector3 end, float gapLengthA, float gapLengthB)
        {
            Vector3 direction = (end - start).normalized;
            float distance = Vector3.Distance(start, end);

            // Apply gap at the start and end
            Vector3 newStart = start + direction * gapLengthA;
            Vector3 newEnd = end - direction * gapLengthB;

            // Update LineRenderer positions
            lineRenderer.SetPosition(0, newStart);
            lineRenderer.SetPosition(1, newEnd);
        }

    }    

}
