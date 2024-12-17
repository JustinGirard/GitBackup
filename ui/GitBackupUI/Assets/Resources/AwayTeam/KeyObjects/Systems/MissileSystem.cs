

using System.Collections.Generic;
using UnityEngine;

public class MissileSystem : AgentSystemBase
{
    // Additional stats specific to Attack system could be added here
    // For example: damage modifiers, ammo cost, etc.
    [SerializeField]
    private float fuelCost = 1f;
    [SerializeField]
    private float baseDamage = 5f;
    
    private string system_id = SpaceEncounterManager.AgentActions.Shield;

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
    private float GetDamageMultiplierFor(string sourceAgentId, string sourcePowerId,string targetAgentId, string targetPowerId)
    {
        if (SpaceEncounterManager.AgentActions.Attack == targetPowerId)
        {
            return 1f;
        }
        if (SpaceEncounterManager.AgentActions.Shield == targetPowerId)
        {
            return 2f;
        }
        if (SpaceEncounterManager.AgentActions.Missile == targetPowerId)
        {
            return 1f;
        }
        return 1f;
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
            Debug.LogError($"Could not find source agent in power Fuel.Execute - {sourceAgentId}");
            yield break;
        }

        if ((float)agentResources[sourceAgentId].GetResourceAmount("Fuel") > 0)
        {
            Debug.Log("Execute Missile SUCCESS");

            Debug.Log($"{sourceAgentId} attacking {targetAgentId}");
            primaryDelta["Fuel"] = -1*fuelCost;
            spaceEncounter.NotifyAllScreens(SpaceEncounterManager.ObservableEffects.ShieldOff);
            yield return CoroutineRunner.Instance.StartCoroutine(EffectHandler.ShootMissileAt(
                missilePrefab: Resources.Load<GameObject>(SpaceEncounterManager.PrefabPath.Missile),
                explosionPrefab: Resources.Load<GameObject>(SpaceEncounterManager.PrefabPath.Explosion),
                number: 20,
                delay: 1f,
                duration: 1f,
                arcHeight: 2f,
                source: spaceEncounter.GetAgentPosition(sourceAgentId),
                target: spaceEncounter.GetAgentPosition(targetAgentId)
            ));
            targetDelta["Hull"] = -1f*baseDamage*GetDamageMultiplierFor( sourceAgentId,  
                                                    sourcePowerId, 
                                                    targetAgentId,  
                                                    targetPowerId);
        }
        else
        {
            Debug.Log("Execute Missile FAIL");

        }

        // Apply delta
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
}




//
//
//
//
//

