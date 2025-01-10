/*
using UnityEngine;

[ExecuteAlways]
public class EffectLineRenderer : MonoBehaviour
{
    public GameObject dottedLineA;
    public GameObject dottedLineB;

    [Header("Line Settings")]
    public float thickness = 0.1f;
    public Color lineColor = Color.white;

    private LineRenderer lineRenderer;

    void Start()
    {
        SetupLineRenderer();
    }

    void Update()
    {
        if (lineRenderer == null)
            SetupLineRenderer();
        if (dottedLineA != null && dottedLineB != null)
        {
            DrawLine(dottedLineA.transform.position, dottedLineB.transform.position);
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
    /// Draws a single continuous line between two points.
    /// </summary>
    private void DrawLine(Vector3 start, Vector3 end)
    {
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }
}
*/

using UnityEngine;

[ExecuteAlways]
public class EffectLineRenderer : MonoBehaviour
{
    public GameObject dottedLineA;
    public GameObject dottedLineB;

    [Header("Line Settings")]
    public float thickness = 0.1f;
    public Color lineColor = Color.white;

    [Header("Gap Settings")]
    public float gapLengthA = 0.3f;  // Gap from start and end of the line
    public float gapLengthB = 0.3f;  // Gap from start and end of the line

    private LineRenderer lineRenderer;

    void Start()
    {
        SetupLineRenderer();
    }

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
}
