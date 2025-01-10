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
            }
        }
        else
        {
            //Debug.Log($"IS NOT SHOWING");

        }

    }

}
