using UnityEngine;
using UnityEngine.UI;

public class DynamicControlPanel : MonoBehaviour
{
    private RawImage rawImageInstance; // Instance of the RawImage
    private IDynamicControl dockedButton; // Reference to the DockedButton component
    public GameObject dynamicControl; // Reference to the DockedButton component

    private void Start()
    {
        // Find the DockedButton component in the child GameObject
        dockedButton = dynamicControl.GetComponent<IDynamicControl>();

        if (dockedButton == null)
        {
            Debug.LogError($"No DockedButton found as a child of the panel: {gameObject.name}");
            return;
        }

        // Dynamically create a RawImage within the panel
        CreateRawImage();

        // Bind the RenderTexture from the DockedButton
        if (dockedButton.GetRenderTexture() != null)
        {
            rawImageInstance.texture = dockedButton.GetRenderTexture();
        }
        else
        {
            //Debug.LogWarning($"(ok on init) DockedButton {dockedButton.GetGameObject().name} has no RenderTexture assigned yet..");
        }
    }

    private void CreateRawImage()
    {
        // Dynamically create the RawImage as a child of this panel
        GameObject rawImageObject = new GameObject("RawImage", typeof(RawImage));
        rawImageObject.transform.SetParent(transform, false);

        // Get the RawImage component
        rawImageInstance = rawImageObject.GetComponent<RawImage>();

        if (rawImageInstance == null)
        {
            Debug.LogError("Failed to create RawImage component.");
            Destroy(rawImageObject);
            return;
        }

        // Configure the RawImage RectTransform
        RectTransform rectTransform = rawImageInstance.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero; // Stretch to fill parent
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private void Update()
    {
        // Optionally, update the RawImage texture if the DockedButton's RenderTexture changes
        if (dockedButton != null && dockedButton.GetRenderTexture() != rawImageInstance.texture)
        {
            rawImageInstance.texture = dockedButton.GetRenderTexture();
        }
        
        Vector2 localMousePosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rawImageInstance.rectTransform,
            Input.mousePosition,
            null,
            out localMousePosition
        );
        
        bool within = false;
        if (rawImageInstance.rectTransform.rect.Contains(localMousePosition))
        {
            within = true;
        }

        if (Input.GetMouseButtonDown(0)) // Left mouse button
            dockedButton.MouseDown(0,within);
        if (Input.GetMouseButtonDown(1)) // Left mouse button
            dockedButton.MouseDown(1,within);
        if (Input.GetMouseButtonDown(2)) // Left mouse button
            dockedButton.MouseDown(2,within);

        if (Input.GetMouseButtonUp(0)) // Left mouse button
            dockedButton.MouseUp(0,within);
        if (Input.GetMouseButtonUp(1)) // Left mouse button
            dockedButton.MouseUp(1,within);
        if (Input.GetMouseButtonUp(2)) // Left mouse button
            dockedButton.MouseUp(2,within);
    }
}
