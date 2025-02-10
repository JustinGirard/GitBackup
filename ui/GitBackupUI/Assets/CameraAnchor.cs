using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraAnchor : MonoBehaviour
{
    [SerializeField] private Agent primaryAgent; // The primary agent (e.g., player's side)
    [SerializeField] private Agent targetAgent; // The target agent (e.g., enemy side)
    [SerializeField] private Vector3 targetOffset = new Vector3(10f, 5f,5f); // Look_at_distance and y-height
    //[SerializeField] private float distanceFromEnemy = 5f;
    private Vector3 __calculatedPosition; // The position to lerp towards
    private Coroutine updateCoroutine; // Coroutine for periodic updates
    [SerializeField] private float updateInterval = 0.5f; // Update interval (seconds)
    [SerializeField] private float updateVelocity = 0.3f;
    // Properties to expose in the inspector
    public Agent PrimaryAgent { get => primaryAgent; set => primaryAgent = value; }
    public Agent TargetAgent { get => targetAgent; set => targetAgent = value; }

    void Start()
    {
        if (primaryAgent == null)
        {
            Debug.LogError("PrimaryAgent is not assigned!");
        }

        if (targetAgent == null)
        {
            Debug.LogError("TargetAgent is not assigned!");
        }

        // Start the periodic update coroutine
        //updateCoroutine = StartCoroutine(UpdateAnchorPosition());
    }

    //private IEnumerator UpdateAnchorPosition()
    //{
    //    while (true)
    //    {
    //        UpdateAnchor();
    //        //yield return new WaitForSeconds(updateInterval);
    //    }
    //}
    /*
    private void UpdateAnchor()
    {
        // Calculate the center points of primary and target agents
        Vector3 primaryCenter = CalculateAgentCenter(primaryAgent);
        Vector3 targetCenter = CalculateAgentCenter(targetAgent);

        primaryCenter.y += targetOffset.y;
        Vector3 agentOffset = primaryCenter - targetCenter;
        __calculatedPosition = primaryCenter + (agentOffset.normalized * targetOffset.x);
    }
    */
    private void UpdateAnchor()
    {
    }

    private Vector3 CalculateAgentCenter(Agent agent)
    {
        if (agent == null) return Vector3.zero;

        Vector3 center = Vector3.zero;
        int unitCount = 0;

        EncounterSquad squad = agent.GetUnit()?.GetComponent<EncounterSquad>();
        if (squad != null)
        {
            List<SimpleShipController> units = squad.GetUnitList();
            foreach (var unit in units)
            {
                if (unit != null && unit.gameObject.activeInHierarchy)
                {
                    center += unit.transform.position;
                    unitCount++;
                }
            }
        }

        return unitCount > 0 ? center / unitCount : Vector3.zero;
    }
    float lastUpdateTime = 0f;
    Vector3 cameraAnchorVelocity = Vector3.zero;
    void Update()
    {
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            lastUpdateTime = Time.time;        
            UpdateAnchor();
            // Calculate the center points of primary and target agents
            Vector3 primaryCenter = CalculateAgentCenter(primaryAgent);
            Vector3 targetCenter = CalculateAgentCenter(targetAgent);
            //__calculatedPosition = primaryCenter + targetOffset;
            __calculatedPosition =  primaryAgent.GetPrimaryAim().transform.position + targetOffset;

            // Smoothly lerp the anchor position towards the calculated position
            transform.position = Vector3.SmoothDamp(transform.position, __calculatedPosition,ref cameraAnchorVelocity, Time.deltaTime *updateVelocity);
            
            //transform.position = Vector3.Lerp(transform.position, primaryAgent.GetPrimaryAim().transform.position, Time.deltaTime *updateVelocity);
            
        }
    }

    void OnDestroy()
    {
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
        }
    }
}
