
using System.Collections.Generic;
using UnityEngine;

public class BlasterSystem : AgentSystemBase
{
    // Additional stats specific to Attack system could be added here
    // For example: damage modifiers, ammo cost, etc.
    [SerializeField]
    private float ammoCost = 1f;
    [SerializeField]
    private float fuelCost = 1f;
    [SerializeField]
    private float baseDamage = 2f;
    
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
    private float GetDamageMultiplierFor(string sourceAgentId, string sourcePowerId,string targetAgentId, string targetPowerId)
    {
        if (SpaceEncounterManager.AgentActions.Attack == targetPowerId)
        {
            return 0.5f;
        }
        if (SpaceEncounterManager.AgentActions.Shield == targetPowerId)
        {
            return 0.0f;
        }
        if (SpaceEncounterManager.AgentActions.Missile == targetPowerId)
        {
            return 2f;
        }
        return 1f;
    }
    public override System.Collections.IEnumerator Execute(string sourceAgentId, 
                                string sourcePowerId, 
                                string targetAgentId, 
                                string targetPowerId, 
                                Dictionary<string, ATResourceData> agentResources)
    {
        Dictionary<string, float> primaryDelta = new Dictionary<string, float>();
        Dictionary<string, float> targetDelta = new Dictionary<string, float>();

        if (agentResources.ContainsKey(sourceAgentId) == false)
        {
            Debug.LogError($"Could not find source agent in power Blaster.Execute - {sourceAgentId}");
            yield break;
        }

        if ((float)agentResources[sourceAgentId].GetResourceAmount("Ammunition") > 0)
        {
//            Debug.Log($"{primaryAgentId} attacking {targetAgentId}");
            primaryDelta["Fuel"] = -1*fuelCost;
            primaryDelta["Ammunition"] = -1*ammoCost;
            spaceEncounter.NotifyAllScreens(SpaceEncounterManager.ObservableEffects.AttackOff);
            yield return CoroutineRunner.Instance.StartCoroutine(EffectHandler.ShootBlasterAt(
                boltPrefab: Resources.Load<GameObject>(SpaceEncounterManager.PrefabPath.BoltPath),
                explosionPrefab: Resources.Load<GameObject>(SpaceEncounterManager.PrefabPath.Explosion),
                number: 20,
                delay: 1f,
                duration: 1f,
                maxDistance: 3f,
                source: spaceEncounter.GetAgentPosition(sourceAgentId),
                target: spaceEncounter.GetAgentPosition(targetAgentId)
            ));
            //targetDelta = SpaceEncounterManager.AddDeltas(targetDelta) TODO Generalize, maybe?
            targetDelta["Hull"] = -1f*baseDamage*GetDamageMultiplierFor( sourceAgentId,  sourcePowerId, targetAgentId,  targetPowerId);
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
