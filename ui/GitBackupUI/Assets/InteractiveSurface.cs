using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

/// <summary>
/// Handles spawning objects directly on the interactive surface.
/// </summary>
public class InteractiveSurface : MonoBehaviour, ICommandReceiver
{
    public GameObject cubePrefab; // Prefab for the cube to spawn
    private GeneralInputManager inputManager;
    [SerializeField]
    private VisualCommand.SurfaceNavigationCommand viscmd_surfaceNav;

    [SerializeField]
    private GameObject __quickNavSurfaceLow;
    [SerializeField]
    private GameObject __quickNavSurfaceHigh;

    private Vector3 __lastHitPoint;
    private Vector3 __lastHitNormal;
    private bool __hasLastHit = false;
    void OnDrawGizmos()
    {
        if (__hasLastHit)
        {
            // Draw the contact point as a small red ball
            //Gizmos.color = Color.white;
            //Gizmos.DrawSphere(__lastHitPoint, 0.1f);

            // Draw the surface normal as a white line
            //Gizmos.color = Color.white;
            //Gizmos.DrawLine(__lastHitPoint, __lastHitPoint + __lastHitNormal * 3.5f);
        }
    }
    void Awake()
    {
        inputManager = FindObjectOfType<GeneralInputManager>();
        if (inputManager != null)
        {
            inputManager.RegisterObserver(this);
        }
    }
    void OnDestroy()
    {
        if (inputManager != null)
        {
            inputManager.UnregisterObserver(this);
        }
    }
    public GameObject GetGameObject()
    {
        return this.gameObject;
    }
    public void OnCommandReceived(string commandId, Vector2 mousePosition)
    {
        if(commandId == GeneralInputManager.Command.primary_down && !viscmd_surfaceNav.IsShowing() )
            viscmd_surfaceNav.Show();
        if(commandId == GeneralInputManager.Command.secondary_down && viscmd_surfaceNav.IsShowing() )
            viscmd_surfaceNav.Hide();

        ProcessQuickNavCommand(commandId);

        //Debug.Log($"Checking {commandId} for {this.name} on {viscmd_surfaceNav.name}");
        if(viscmd_surfaceNav.IsShowing() == true)
        {
            Camera viewportCamera = GeneralInputManager.Instance().GetCamera();
            Ray ray = viewportCamera.ScreenPointToRay(mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit) && hit.collider.gameObject == gameObject)
            {
                //Debug.Log($"processing {commandId} for {this.name}");
                Vector3 spawnPosition = hit.point + Vector3.up * 1f;
                viscmd_surfaceNav.ProcessEvent(commandId,mousePosition, hit.point,hit.normal);
                 if(commandId == GeneralInputManager.Command.primary_up )
                 {
                    __lastHitPoint = hit.point;
                    __lastHitNormal = hit.normal;
                    __hasLastHit = true;                

                 }                
            }
        }
        else
        {
            //Debug.Log($"IS NOT SHOWING");

        }

    }

    // Mapping of commands to their respective navigation vectors
    private static readonly Dictionary<string, Vector2> commandToVectorMap = new Dictionary<string, Vector2>
    {
        { GeneralInputManager.Command.nav_up_down, new Vector2(0, 1) },    // Up
        { GeneralInputManager.Command.nav_down_down, new Vector2(0, -1) }, // Down
        { GeneralInputManager.Command.nav_left_down, new Vector2(-1, 0) }, // Left
        { GeneralInputManager.Command.nav_right_down, new Vector2(1, 0) }  // Right
    };

    private Vector2 navState = Vector2.zero; // Tracks the current navigation vector
    private bool running = false;
    private void ProcessQuickNavCommand(string commandId)
    {
        //Debug.Log(__quickNavSurfaceLow);
        if (__quickNavSurfaceLow == null)
        {
            //Debug.LogWarning("QuickNav low plane is not assigned."); /// THIS ERROR IS BEING PRINTED
            return;
        }

        // Step 1: Update navigation state or save recorded vector
        if (commandToVectorMap.TryGetValue(commandId, out Vector2 adjustment))
        {
            // KeyDown: Update navigation state
            running = true;
            navState += adjustment;
            Debug.Log(navState);
        }
        else if (commandId == GeneralInputManager.Command.nav_up_up ||
                commandId == GeneralInputManager.Command.nav_down_up ||
                commandId == GeneralInputManager.Command.nav_left_up ||
                commandId == GeneralInputManager.Command.nav_right_up)
        {
            // KeyUp: Save the recorded vector and reset state
            if (running == true)
            {
                Debug.Log($"!{navState}");
                SaveRecordedPoint(new Vector2(navState.x,navState.y));
                navState = Vector2.zero;
                running = false;
            }
        }

    }
    GameObject debugCube = null;
    // Save the recorded navigation point
    private void SaveRecordedPoint(Vector2 navVector)
    {
        // Calculate and save the recorded point based on navVector
        Renderer renderer = __quickNavSurfaceLow.GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogError("QuickNav low plane does not have a Renderer component.");
            return;
        }

        Bounds bounds = renderer.bounds;

        Vector3 recordedPoint = new Vector3(
            Mathf.Lerp(bounds.min.x, bounds.max.x, (navVector.x + 1) / 2f), // Map [-1, 1] to [0, 1]
            bounds.max.y, // Fixed Y plane
            Mathf.Lerp(bounds.min.z, bounds.max.z, (navVector.y + 1) / 2f)  // Map [-1, 1] to [0, 1]
        );
        //if (debugCube == null)
        //{
        //    debugCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //    debugCube.transform.localScale = Vector3.one * 0.5f; // Scale down for better visibility
        //    debugCube.GetComponent<Renderer>().material.color = Color.red; // Make it red for easier identification
        // }
        //debugCube.transform.position = recordedPoint;
        Vector3 surfacePoint = new Vector3(recordedPoint.x,this.gameObject.transform.position.y,recordedPoint.z);
        viscmd_surfaceNav.QuickNavTo(recordedPoint, surfacePoint);
        Debug.Log($"Saved QuickNav point: {recordedPoint}");
    }




}
