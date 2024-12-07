using UnityEngine;
using UnityEngine.InputSystem; // Ensure you have the Input System package installed
using System;

public class DockedButton : MonoBehaviour
{
    public Camera mainCamera; // Reference to the main camera
    public float offsetX = 100.0f; // Offset in pixels from the left of the screen
    public float offsetY = 100.0f; // Offset in pixels from the bottom of the screen

    public string commandId;
    private Renderer sphereRenderer; // Renderer for changing the color

    private InputAction action01; // Reference to "do_action_01"
    private bool _isActivated ;

    public Material activeMaterial;
    public Material inactiveMaterial;

    public RenderTexture renderTexture; // Assign the RenderTexture for this button in the Inspector
    public Vector3 cameraOffset = new Vector3(0, 0, -2); // Offset for rendering

    [Header("Render Texture Settings")]
    public int renderTextureWidth = 256; // Width of the RenderTexture
    public int renderTextureHeight = 256; // Height of the RenderTexture

    [HideInInspector]
    public event Action<string,string,bool> OnButtonInteracted;

    /// <summary>
    ///  External Handlers
    /// </summary>
    /// <param name="buttonId"></param>
    /// <param name="within"></param>
    public void MouseDown(int buttonId, bool within) // CALLED FROM Panel
    {
        OnButtonInteracted?.Invoke(commandId,"MouseDown",within); 
    }
    public void MouseUp(int buttonId, bool within) // CALLED FROM Panel
    {
        OnButtonInteracted?.Invoke(commandId,"MouseUp",within); 
    }
    private void OnMouseDown()  // CALLED FROM Button Object
    {
        OnButtonInteracted?.Invoke(commandId,"MouseDown",true); 
    }

    private void HandleInputAction(object action, InputActionChange change) // CALLED FROM Keyboard
    {
        if (action is InputAction inputAction && inputAction.name == "do_action_01")
        {
            if (inputAction.phase == InputActionPhase.Started) // Key down
            {
                OnButtonInteracted?.Invoke(commandId, "MouseDown", true);
            }
            else if (inputAction.phase == InputActionPhase.Canceled) // Key up
            {
                OnButtonInteracted?.Invoke(commandId, "MouseUp", true);
            }
        }
}
    /// <summary>
    /// End External Handlers
    /// </summary>

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
        
        UpdatePosition();
        SetActive(false);
        // Bind to the input manager event
        InputSystem.onActionChange += HandleInputAction;
    }

    private void Update()
    {
        // Continuously maintain position relative to the camera
        // UpdatePosition();
    }

    public void SetState(string state)
    {
        //Debug.Log($"Setting state for {commandId}: {state}");
        if (state == "active")
        {
            SetActive(true);
        }
        else if (state == "inactive")
        {
            SetActive(false);
        }
        else
        {
            Debug.LogWarning($"Unhandled state: {state}");
        }
    }
    private void UpdatePosition()
    {
        if (mainCamera == null) return;
    }

    private void SetActive(bool status)
    {
        _isActivated = status;
        if (status == true)
        {
            sphereRenderer.material = activeMaterial;
        }
        if (status == false)
        {
            sphereRenderer.material = inactiveMaterial;
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
