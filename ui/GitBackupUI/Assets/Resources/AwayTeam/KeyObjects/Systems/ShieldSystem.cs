using System.Collections.Generic;
using UnityEngine;

public class ShieldSystem : AgentSystemBase
{
    // Additional stats specific to Attack system could be added here
    // For example: damage modifiers, ammo cost, etc.
    [SerializeField]
    private float missileCost = 1f;
    [SerializeField]
    private float fuelCost = 1f;
    [SerializeField]
    private float baseDamage = 5f;
    
    private string system_id = SpaceEncounterManager.AgentActions.Attack;

    protected override void OnActivate(string [] sourceAgentIds,string [] targetAgentIds)
    {
        base.OnActivate( sourceAgentIds,targetAgentIds);
    }

    protected override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);

    }

    protected override void OnDeactivate()
    {
        base.OnDeactivate();
    }

    public override System.Collections.IEnumerator Execute(
                                string sourceAgentId, 
                                string sourcePowerId, 
                                string targetAgentId, 
                                string targetPowerId, 
                                Dictionary<string, ATResourceData> agentResources)
    {
        //
        //
        Dictionary<string, float> primaryDelta = new Dictionary<string, float>();
        Dictionary<string, float> targetDelta = new Dictionary<string, float>();

        if (agentResources.ContainsKey(sourceAgentId) == false)
        {
            Debug.LogError($"Could not find source agent in power Missile.Execute - {sourceAgentId}");
            yield break;
        }

        if ((float)agentResources[sourceAgentId].GetResourceAmount("Fuel") > 0)
        {
            primaryDelta["Fuel"] = -1*fuelCost;
            spaceEncounter.NotifyAllScreens(SpaceEncounterManager.ObservableEffects.AttackOff);
            yield return CoroutineRunner.Instance.StartCoroutine(EffectHandler.CreateShield(
                            shieldPrefab: Resources.Load<GameObject>(SpaceEncounterManager.PrefabPath.UnitShield),
                            source:spaceEncounter.GetAgentPosition(sourceAgentId)
            ));

        }
        else
        {
            Debug.Log("Execute Sheild FAIL");
        }

        if (primaryDelta.Count > 0)
        {
            agentResources[sourceAgentId].Deposit(primaryDelta);
        }
        if (targetDelta.Count > 0)
        {
            agentResources[targetAgentId].Deposit(targetDelta);
        }
        yield break;
    }
    //
    //
    //
    //

    
}