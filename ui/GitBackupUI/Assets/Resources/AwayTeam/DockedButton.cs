using UnityEngine;
using UnityEngine.InputSystem; // Ensure you have the Input System package installed

public class DockedButton : MonoBehaviour
{
    public Camera mainCamera; // Reference to the main camera
    public float offsetX = 100.0f; // Offset in pixels from the left of the screen
    public float offsetY = 100.0f; // Offset in pixels from the bottom of the screen

    private Renderer sphereRenderer; // Renderer for changing the color

    private InputAction action01; // Reference to "do_action_01"


    public RenderTexture renderTexture; // Assign the RenderTexture for this button in the Inspector
    public Vector3 cameraOffset = new Vector3(0, 0, -2); // Offset for rendering

    [Header("Render Texture Settings")]
    public int renderTextureWidth = 256; // Width of the RenderTexture
    public int renderTextureHeight = 256; // Height of the RenderTexture

    [HideInInspector]

    public void MouseDown(int buttonId, bool within)
    {
        Debug.Log($"{gameObject.name} clicked!");
        //PerformAction();
        // Perform button-specific logic here
    }
    public void MouseUp(int buttonId, bool within)
    {
        if (within == true)
        {
            PerformAction();
        }
        // Perform button-specific logic here
    }
    private void Start()
    {
        renderTexture = new RenderTexture(renderTextureWidth, renderTextureHeight, 16)
        {
            name = $"RenderTextureFor{this.name}",
            format = RenderTextureFormat.ARGB32,
            useMipMap = false,
            antiAliasing = 2 // Adjust for smoother edges
        };
        renderTexture.Create();

        ///
        ///
        ///
        // Create or retrieve the InputAction for "do_action_01"
        action01 = new InputAction(name: "do_action_01", type: InputActionType.Button);
        action01.AddBinding("<Mouse>/rightButton");
        action01.Enable();

        DynamicButtonRenderer.Instance.RegisterButton(this);
        // Assign main camera if not already assigned
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found. Please assign a camera to the script.");
            return;
        }

        // Cache the renderer for the sphere
        sphereRenderer = GetComponent<Renderer>();

        // Set initial position
        UpdatePosition();

        // Bind to the input manager event
        InputSystem.onActionChange += HandleInputAction;
    }

    private void Update()
    {
        // Continuously maintain position relative to the camera
        // UpdatePosition();
    }

    private void UpdatePosition()
    {
        if (mainCamera == null) return;

        // Convert screen offsets to world position
        //Vector3 screenPosition = new Vector3(offsetX, offsetY, mainCamera.nearClipPlane + 1.0f);
        //Vector3 worldPosition = mainCamera.ScreenToWorldPoint(screenPosition);

        // Set the button's position
        //transform.position = worldPosition;
    }

    private void OnMouseDown()
    {
        // Trigger the action when clicked
        PerformAction();
    }

    private void HandleInputAction(object action, InputActionChange change)
    {
        if (action is InputAction inputAction && inputAction.name == "do_action_01" && change == InputActionChange.ActionPerformed)
        {
            // Trigger the action when the event is fired
            PerformAction();
        }
    }

    private void PerformAction()
    {
        // Change the sphere's color to a random one
        if (sphereRenderer != null)
        {
            sphereRenderer.material.color = new Color(Random.value, Random.value, Random.value);
        }
    }

    private void OnDestroy()
    {
        // Unbind the event to avoid memory leaks
        InputSystem.onActionChange -= HandleInputAction;
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }        
    }
}
