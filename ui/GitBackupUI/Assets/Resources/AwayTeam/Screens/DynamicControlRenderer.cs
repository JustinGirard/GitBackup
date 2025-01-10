using UnityEngine.UIElements;
/*
using UnityEngine;
using System.Collections.Generic;

public class DynamicControlRenderer : MonoBehaviour
{
    public static DynamicControlRenderer Instance; // Singleton instance for easy access

    private List<IDynamicControl> registeredButtons = new List<IDynamicControl>(); // List of registered buttons

    private void Awake()
    {
        // Set up the singleton instance
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("Multiple instances of RenderCamera found. Ensure only one RenderCamera exists in the scene.");
            Destroy(this);
        }
    }

    public void RegisterButton(IDynamicControl button)
    {
        // Add the button to the list if it's not already registered
        if (!registeredButtons.Contains(button))
        {
            registeredButtons.Add(button);
        }
    }

    private void LateUpdate()
    {
        // Render all registered buttons
        foreach (var button in registeredButtons)
        {
            if (button != null && button.GetRenderTexture() != null)
            {
                RenderButton(button);
            }
        }
    }

    private void RenderButton(IDynamicControl button)
    {
        // Position the camera
        Transform transButt = button.GetGameObject().transform; // ooooeeeee
        transform.position = transButt.position + button.GetCameraOffset();
        transform.LookAt(transform.position);

        GetComponent<Camera>().targetTexture = button.GetRenderTexture();
        GetComponent<Camera>().Render();
        GetComponent<Camera>().targetTexture = null;        
    }
}
*/

using UnityEngine;
using System.Collections.Generic;

public class DynamicControlRenderer : MonoBehaviour
{


    public static DynamicControlRenderer Instance; // Singleton instance for easy access

    private List<IDynamicControl> registeredButtons = new List<IDynamicControl>(); // List of registered buttons
    public bool debugMode = false; // Toggle debugging mode
    private Dictionary<IDynamicControl, GameObject> debugCameras = new Dictionary<IDynamicControl, GameObject>(); // Dictionary to store debug cameras

    private void Awake()
    {
        // Set up the singleton instance
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("Multiple instances of RenderCamera found. Ensure only one RenderCamera exists in the scene.");
            Destroy(this);
        }
    }

    public void RegisterButton(IDynamicControl button)
    {
        // Add the button to the list if it's not already registered
        if (!registeredButtons.Contains(button))
        {
            registeredButtons.Add(button);
        }
    }

    private void LateUpdate()
    {
        if (debugMode)
        {
            DebugRenderButtons();
        }
        else
        {
            // Render all registered buttons
            float count = 0;
            foreach (var button in registeredButtons)
            {
                count ++;
                if (button != null && button.GetRenderTexture() != null)
                {
                    RenderButton(button,this.GetComponent<Camera>(),count);
                }
            }
        }
    }
    /*
    private void RenderButton(IDynamicControl button)
    {
        // Position the camera
        Transform transButt = button.GetGameObject().transform;
        transform.position = transButt.position + button.GetCameraOffset();

        //public Vector3 GetOrthoWHS(){
        //    return new Vector3(orthoW,orthoH,orthoSize);
       // }


        transform.LookAt(transButt.position);

        Camera cam = GetComponent<Camera>();
        cam.targetTexture = button.GetRenderTexture();
        
        cam.Render();
        cam.targetTexture = null;
    }*/
    private void RenderButton(IDynamicControl button, Camera cam, float  at_index)
    {
        // Position the camera
        Transform transButt = button.GetGameObject().transform;
        if (transButt.position.x < ( at_index*10f -0.5f) || transButt.position.x > ( at_index*10f -0.5f)  )
            transButt.position = new Vector3(at_index*10f ,transButt.position.y ,transButt.position.z );
        cam.transform.position = transButt.position + button.GetCameraOffset();

        transform.LookAt(transButt.position);
        if(cam == null) 
            cam = GetComponent<Camera>();
        cam.orthographic = true;
        cam.targetTexture = button.GetRenderTexture();

        // Adjust the camera's H, W, and Size
        Vector3 orthoWHS = button.GetOrthoWHS();
        cam.orthographicSize = orthoWHS.z; // Orthographic size
        cam.rect = new Rect(0, 0, orthoWHS.x, orthoWHS.y); // Adjust H and W

        // Render the button
        cam.Render();
        //cam.targetTexture = null;
    }

   

    private void DebugRenderButtons()
    {
        float count = 0;
        foreach (var button in registeredButtons)
        {
            if (button != null)
            {
                // Check if a debug camera already exists for this button
                if (!debugCameras.ContainsKey(button))
                {
                    // Create a new debug camera
                    GameObject debugCam = new GameObject($"DebugCamera_{button.GetGameObject().name}");
                    Camera camComponent = debugCam.AddComponent<Camera>();
                    //camComponent.CopyFrom(GetComponent<Camera>()); // Copy settings from the current camera
                    count ++;
                    RenderButton(button, camComponent,count);
                    //camComponent.targetTexture = null; // Ensure no render texture is set for debugging

                    debugCameras[button] = debugCam; // Add to the dictionary
                }

                // Update the debug camera's position and orientation
                GameObject debugCamera = debugCameras[button];
                Transform transButt = button.GetGameObject().transform;
                Vector3 camPosition = transButt.position + button.GetCameraOffset();

                debugCamera.transform.position = camPosition;
                debugCamera.transform.LookAt(transButt.position);
            }
        }
    }

private VisualProjectionFrame debugCanvas;
    private VisualProjectionFrame debugCanvasPlaceholder;
    private VisualProjectionFrame debugUIDocument;
    private VisualProjectionFrame debugUIDocumentPlaceholder;

    /// <summary>
    /// Updates the bounds of a button's Canvas to align with the placeholder defined in the UI Document.
    /// 
    /// The method performs the following steps:
    /// 1. Validates the provided `buttonCanvas` and `placeholderName`, logging errors if invalid.
    /// 2. Retrieves and validates the placeholder from the `UIDocument` structure.
    /// 3. Calculates transformations to map the placeholder's position and size in UI Document space 
    ///    to the corresponding position and size in the Canvas space.
    /// 4. Applies the calculated position and size to the Canvas's panel transform to ensure alignment.
    ///
    /// Errors are logged if the Canvas, panel, or placeholder is invalid or has unfinalized layout bounds.
    /// Debug information can be enabled for additional insight into frame transformations.
    /// </summary>
    /// <param name="buttonCanvas">The Canvas containing the button to be updated.</param>
    /// <param name="placeholderName">The name of the placeholder element in the UI Document.</param>
    /// <param name="uiDocument">The `UIDocument` containing the root VisualElement for layout calculation.</param>
    public static void UpdateCanvasBounds(Canvas buttonCanvas, string placeholderName, UIDocument uiDocument)
    {
        if (buttonCanvas == null)
        {
            Debug.LogError("Button Canvas is null. Cannot update bounds.");
            return;
        }

        RectTransform panelTransform = buttonCanvas.transform.GetChild(0).GetComponent<RectTransform>();
        if (panelTransform == null)
        {
            Debug.LogError("No panel found as the first child of the Canvas.");
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;
        //VisualElement placeholder = root.Q<VisualElement>(placeholderName);
        VisualElement placeholder = FindPlaceholder(root, placeholderName);    
        if (placeholder == null)
        {
            Debug.LogError($"Placeholder '{placeholderName}' is missing in the UI Document.");
            return;
        }

        // Check placeholder bounds
        if (placeholder.worldBound.width <= 0 || placeholder.worldBound.height <= 0)
        {
            //Debug.LogWarning($"Placeholder '{placeholderName}' has invalid bounds (layout may not be finalized). Skipping.");
            return;
        }


        // Step a: Define the Canvas space
        RectTransform canvasRectTransform = buttonCanvas.GetComponent<RectTransform>();
        VisualProjectionFrame canvasFrame = new VisualProjectionFrame(
            canvasRectTransform.rect.width * canvasRectTransform.localScale.x,
            canvasRectTransform.rect.height * canvasRectTransform.localScale.y,
            0, 0,
            1, 1
        );

        // Step b: Define the Panel space within the Canvas
        VisualProjectionFrame canvasPanelFrame = new VisualProjectionFrame(
            panelTransform.rect.width * panelTransform.localScale.x,
            panelTransform.rect.height * panelTransform.localScale.y,
            canvasRectTransform.TransformPoint(panelTransform.localPosition).x,
            canvasRectTransform.TransformPoint(panelTransform.localPosition).y,
            canvasFrame.Basis.x,
            canvasFrame.Basis.y
        );

        // Step c: Define the UI Document space
        VisualProjectionFrame uiDocumentFrame = new VisualProjectionFrame(
            root.worldBound.width,
            root.worldBound.height,
            0, 0,
            1, -1
        );

        // Step d: Define the Placeholder space within the UI Document
        VisualProjectionFrame uiDocumentPlaceholderFrame = new VisualProjectionFrame(
            placeholder.worldBound.width,
            placeholder.worldBound.height,
            placeholder.worldBound.position.x,
            placeholder.worldBound.position.y,
            uiDocumentFrame.Basis.x,
            uiDocumentFrame.Basis.y
        );
        //Debug.Log($"Canvas: {canvasFrame}");
        //Debug.Log($"Panel: {canvasPanelFrame}");
        //Debug.Log($"UI Document: {uiDocumentFrame}");
        //Debug.Log($"Placeholder: {uiDocumentPlaceholderFrame}");


        ///
        // Calculate Transform

        // Step 1: Normalize placeholder position in UI Document space
        Vector2 normalizedPlaceholderPosition = new Vector2(
            uiDocumentPlaceholderFrame.Position.x / uiDocumentFrame.Size.x,
            (uiDocumentFrame.Size.y - uiDocumentPlaceholderFrame.Position.y) / uiDocumentFrame.Size.y // Invert Y-axis
        );

        // Step 2: Scale normalized position to Canvas space
        Vector2 scaledPlaceholderPosition = new Vector2(
            normalizedPlaceholderPosition.x * canvasFrame.Size.x,
            normalizedPlaceholderPosition.y * canvasFrame.Size.y
        );

        // Step 3: Scale placeholder size to Canvas space
        Vector2 scaledPlaceholderSize = new Vector2(
            (uiDocumentPlaceholderFrame.Size.x / uiDocumentFrame.Size.x) * canvasFrame.Size.x,
            (uiDocumentPlaceholderFrame.Size.y / uiDocumentFrame.Size.y) * canvasFrame.Size.y
        );

        // Step 4: Create the placeholder frame in Canvas space
        VisualProjectionFrame placeholderCanvasFrame = new VisualProjectionFrame(
            scaledPlaceholderSize.x,
            scaledPlaceholderSize.y,
            scaledPlaceholderPosition.x,
            scaledPlaceholderPosition.y -scaledPlaceholderSize.y ,
            canvasFrame.Basis.x,
            canvasFrame.Basis.y
        );


        ///
        // Assign Transformation
        //Debug.Log($"Placeholder in Canvas Space: {placeholderCanvasFrame}");
        panelTransform.localPosition = canvasRectTransform.InverseTransformPoint(new Vector3(
            placeholderCanvasFrame.Position.x,
            placeholderCanvasFrame.Position.y,
            panelTransform.localPosition.z // Keep the existing Z position to maintain depth
        ));
        panelTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, placeholderCanvasFrame.Size.x/panelTransform.localScale.x);
        panelTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, placeholderCanvasFrame.Size.y/panelTransform.localScale.y);

        // Debug log to confirm the new panel position
        //Debug.Log($"Moved Panel to: {panelTransform.localPosition}");

        //
        //
        //


        //if (debugCanvas == null)
        //    debugCanvas = canvasFrame;
        //if (debugCanvasPlaceholder == null)
        //    debugCanvasPlaceholder = placeholderCanvasFrame;
        //if (debugUIDocument == null)
        //    debugUIDocument = uiDocumentFrame;
        //if (debugUIDocumentPlaceholder == null)
        //    debugUIDocumentPlaceholder = uiDocumentPlaceholderFrame;

        //string comparisonOutput =
        //    DebugCompareFrames("Canvas", canvasFrame, debugCanvas) +"\n\n" + 
        //    DebugCompareFrames("Placeholder in Canvas Space", placeholderCanvasFrame, debugCanvasPlaceholder) +"\n\n" +
        //    DebugCompareFrames("UI Document", uiDocumentFrame, debugUIDocument)+"\n\n" +
        //    DebugCompareFrames("UI Document Placeholder", uiDocumentPlaceholderFrame, debugUIDocumentPlaceholder);
        //Debug.Log(comparisonOutput);

    
    }    



    public static VisualElement FindPlaceholder(VisualElement root, string placeholderPath)
    {
        if (string.IsNullOrWhiteSpace(placeholderPath))
        {
            Debug.LogError("Placeholder path is null or empty.");
            return null;
        }

        if (!placeholderPath.Contains(","))
        {
            // Treat as globally unique placeholder
            VisualElement element = root.Q<VisualElement>(placeholderPath.Trim());
            if (element == null)
            {
                Debug.LogError($"Placeholder '{placeholderPath}' not found in the UI Document.");
            }
            return element;
        }

        // Split the path by "," and traverse the hierarchy
        string[] pathElements = placeholderPath.Split(',');
        VisualElement currentElement = root;

        foreach (string elementName in pathElements)
        {
            currentElement = currentElement.Q<VisualElement>(elementName.Trim());
            if (currentElement == null)
            {
                Debug.LogError($"Element '{elementName}' in path '{placeholderPath}' not found.");
                return null;
            }
        }

        return currentElement;
    }

}

