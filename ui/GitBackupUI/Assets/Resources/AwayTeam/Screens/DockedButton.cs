using UnityEngine;
using UnityEngine.InputSystem; // Ensure you have the Input System package installed
using System;

using System.Collections.Generic;
public interface IDynamicControl
{
    public RenderTexture GetRenderTexture();
    public GameObject GetGameObject();
    public Vector3 GetCameraOffset();
    public Vector3 GetOrthoWHS();
    public void MouseDown(int buttonId, bool within);
    public void MouseUp(int buttonId, bool within);
    public void RegisterInteractionHandler(System.Action<string,string,bool> handler);

    public void SetState(string state);
}


public abstract class StandardDynamicControl : MonoBehaviour, IDynamicControl
{
    public Camera mainCamera; // Reference to the main camera
    public float offsetX = 100.0f; // Offset in pixels from the left of the screen
    public float offsetY = 100.0f; // Offset in pixels from the bottom of the screen

    private RenderTexture renderTexture; // Render texture for the control
    public Vector3 cameraOffset = new Vector3(0, 0, -50); // Default camera offset

    [Header("Render Texture Settings")]
    public int renderTextureWidth = 256; // Width of the RenderTexture
    public int renderTextureHeight = 256; // Height of the RenderTexture
    public float orthoSize = 5f;
    public float orthoW = 1f;

    public float orthoH = 1f;

    private InputAction action01; // Input action for mouse/keyboard interaction

    public bool useCamera = true;
    public bool useInputSystem = true;

    [HideInInspector]
    public event Action<string, string, bool> OnButtonInteracted;
    public Vector3 GetOrthoWHS(){
        return new Vector3(orthoW,orthoH,orthoSize);
    }
    protected virtual void Start()
    {
        
        if (useCamera == true)
        {

            // Initialize render texture
            renderTexture = new RenderTexture(renderTextureWidth, renderTextureHeight, 16)
            {
                name = $"RenderTextureFor{this.name}",
                format = RenderTextureFormat.ARGB32,
                useMipMap = false,
                antiAliasing = 2
            };
            renderTexture.Create();

            // Assign the main camera if not already assigned
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera == null)
            {
                Debug.LogError("Main Camera not found. Please assign a camera to the script.");
            }

            // Input action initialization

            // Ensure registration in a central renderer system
            DynamicControlRenderer.Instance.RegisterButton(this);

        }
        
        if (useInputSystem == true)
        {
            action01 = new InputAction(name: "do_action_01", type: InputActionType.Button);
            action01.AddBinding("<Mouse>/rightButton");
            action01.Enable();
            // Bind to input system changes
            InputSystem.onActionChange += HandleInputAction;
        }
        
    }

    public virtual  void OnDestroy()
    {
        // Release render texture resources
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }

        if (useInputSystem == true)
        {
            // Unbind from input system
            InputSystem.onActionChange -= HandleInputAction;
        }
    }

    // IDynamicControl Implementation
    public RenderTexture GetRenderTexture() => renderTexture;
    public GameObject GetGameObject() => this.gameObject;
    public Vector3 GetCameraOffset() => cameraOffset;

    public virtual void RegisterInteractionHandler(Action<string, string, bool> handler)
    {
        OnButtonInteracted += handler;
    }

    public virtual void MouseDown(int buttonId, bool within)
    {
        OnButtonInteracted?.Invoke(GetCommandId(), "MouseDown", within);
    }

    public virtual  void MouseUp(int buttonId, bool within)
    {
        OnButtonInteracted?.Invoke(GetCommandId(), "MouseUp", within);
    }

    public virtual void HandleInputAction(object action, InputActionChange change)
    {
        /*
        if (action is InputAction inputAction && inputAction.name == "do_action_01")
        {
            if (inputAction.phase == InputActionPhase.Started) // Key down
            {
                OnButtonInteracted?.Invoke(GetCommandId(), "MouseDown", true);
            }
            else if (inputAction.phase == InputActionPhase.Canceled) // Key up
            {
                Debug.Log($"Clicking! {name}");
                OnButtonInteracted?.Invoke(GetCommandId(), "MouseUp", true);
            }
        }*/
    }

    // Abstract methods to be implemented by derived classes
    public abstract string GetCommandId();
    public abstract void SetState(string state);
}


public interface IShowProgress
{
    bool SetProgress( int progress,string id="");
    int GetProgress(string id="");

    int GetProgressMax(string id="");

    void SetProgressMax(int max,string id = "");
}

public class DockedButton : StandardDynamicControl
{
    public string commandId;
    private Renderer sphereRenderer;
    public Material activeMaterial;
    public Material inactiveMaterial;

    protected override void Start()
    {
        base.Start();

        // Cache the renderer
        sphereRenderer = GetComponent<Renderer>();
        if (sphereRenderer == null)
        {
            Debug.LogError("Renderer not found on DockedButton GameObject.");
        }
    }

    public override string GetCommandId() => commandId;

    public override void SetState(string state)
    {
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

    private void SetActive(bool status)
    {
        if (status)
        {
            sphereRenderer.material = activeMaterial;
            foreach (var childRenderer in GetComponentsInChildren<Renderer>())
            {
                childRenderer.material = activeMaterial;
            }
        }
        else
        {
            sphereRenderer.material = inactiveMaterial;
            foreach (var childRenderer in GetComponentsInChildren<Renderer>())
            {
                childRenderer.material = inactiveMaterial;
            }
        }
    }
}

