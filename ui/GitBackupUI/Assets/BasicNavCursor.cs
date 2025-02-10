using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VisualCommand;

public class BasicNavCursor : MonoBehaviour,ISurfaceNavigationCommand, IShowHide
{
    private string __commandSelectionState = SurfaceNavigationCommand.SelectionState.off;

    public string GetActiveState()
    {
        return __commandSelectionState;
    }
    
    public GameObject GameObject()
    {
        return this.gameObject;
    }

    public GameObject GetTarget()
    {
        return this.gameObject;
    }
    public void Show()
    {
        SetVisualState( SurfaceNavigationCommand.SelectionState.active);
    }
    public void Hide()
    {
        SetVisualState( SurfaceNavigationCommand.SelectionState.off);
    }
    public void SetVisualState(string state){

        if (SurfaceNavigationCommand.SelectionState.IsValid(state) == false)
        {
            Debug.Log("Could not set a valid state ");
        }
        if (state == SurfaceNavigationCommand.SelectionState.off)
        {
            //Debug.Log("Setting cursor on");

            gameObject.SetActive(false);
        }
        else
        {
            //Debug.Log("Setting cursor on");
            gameObject.SetActive(true);
        }
        __commandSelectionState = state;

    }
    [SerializeField]  private GameObject directionIndicator;


    public void UpdateDirectionIndicator(Vector3 observerPosition)
    {
        directionIndicator.SetActive(false);
        return;
        if (GetActiveState() == SurfaceNavigationCommand.SelectionState.active)
        {
            directionIndicator.SetActive(true);
            Vector3 directionVec = (transform.position - observerPosition).normalized;
            directionIndicator.transform.position = observerPosition + directionVec*1.5f;
            if (directionVec.magnitude > 0.01)
                directionIndicator.transform.forward = directionVec;
        }
        else
        {
            directionIndicator.SetActive(false);
        }        
    }

    void Start()
    {
        directionIndicator.SetActive(false);
        __commandSelectionState = SurfaceNavigationCommand.SelectionState.off;
        gameObject.SetActive(false);
    }

    void Update()
    {
        
    }
}
