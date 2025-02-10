using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using System;

public class DynamicCameraGroupManager : MonoBehaviour
{
    [SerializeField]
    private CinemachineTargetGroup targetGroup; // The Cinemachine Target Group to manage
    [SerializeField]
    private List<Agent> agents; // List of agents to track
    [SerializeField]
    private Agent playerAgent; // Special player agent
    public float bias = 3f;
    private float updateInterval = 2f; // Time between scans (in seconds)
    private Coroutine updateCoroutine;

    void Start()
    {
        targetGroup = this.GetComponent<CinemachineTargetGroup>();
    }
    void Update(){
        UpdateTargets();
    }

    //private IEnumerator UpdateTargetGroup()
    //{
    //    while (true)
    //    {    //        yield return new WaitForSeconds(updateInterval);
    //    }
    //}

    private void UpdateTargets()
    {
        // Clear existing targets in the Cinemachine Target Group
        targetGroup.m_Targets = new CinemachineTargetGroup.Target[0];

        // Temporary list to store units
        List<SimpleShipController> allUnits = new List<SimpleShipController>();

        // Iterate through agents to find their units
        foreach (var agent in agents)
        {
            if (agent == null) continue;

            EncounterSquad sourceSquad = agent.GetUnit()?.GetComponent<EncounterSquad>();
            if (sourceSquad == null) continue;

            List<SimpleShipController> sourceUnits = sourceSquad.GetUnitList();
            if (sourceUnits == null) continue;

            allUnits.AddRange(sourceUnits);
        }

        // Add units to the Cinemachine Target Group
        foreach (var unit in allUnits)
        {
            if (unit != null && unit.gameObject.activeInHierarchy)
            {
                targetGroup.AddMember(unit.transform, 1f, bias); // Weight = 1, Radius = 2
            }
        }

        // Clean up orphaned units (units no longer associated with any agent)
        RemoveOrphanedUnits(allUnits);
    }

    private void RemoveOrphanedUnits(List<SimpleShipController> activeUnits)
    {
        // Check for units currently in the Target Group
        for (int i = targetGroup.m_Targets.Length - 1; i >= 0; i--)
        {
            var target = targetGroup.m_Targets[i];
            if (target.target == null || !activeUnits.Contains(target.target.GetComponent<SimpleShipController>()))
            {
                // Remove any orphaned or inactive targets
                targetGroup.RemoveMember(target.target);
            }
        }
    }

    public void AddAgent(Agent agent)
    {
        if (!agents.Contains(agent))
        {
            agents.Add(agent);
        }
        if (!agents.Contains(playerAgent))
        {
            agents.Add(playerAgent);
        }
    }

    public void RemoveAgent(Agent agent)
    {
        agents.Remove(agent);
        if (!agents.Contains(playerAgent))
        {
            agents.Add(playerAgent);
        }
    }

    public void ClearAgents()
    {
        agents.Clear();
        agents.Add(playerAgent);
    }


    void OnDestroy()
    {
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
        }
    }
}
