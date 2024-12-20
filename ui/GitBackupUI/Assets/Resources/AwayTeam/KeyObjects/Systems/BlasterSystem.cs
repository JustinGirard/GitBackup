
using System.Collections.Generic;
using UnityEngine;

public class BlasterSystem : StandardSystem
{
    // Additional stats specific to Attack system could be added here
    // For example: damage modifiers, ammo cost, etc.
    [SerializeField]
    private float ammoCost = 1f;
    [SerializeField]
    private float fuelCost = 1f;
    [SerializeField]
    private float baseDamage = 2f;

    private string system_id = AgentActions.Attack;

    private float GetDamageMultiplierFor(string sourceAgentId, string sourcePowerId,string targetAgentId, string targetPowerId)
    {
        if (AgentActions.Attack == targetPowerId)
        {
            return 0.5f;
        }
        if (AgentActions.Shield == targetPowerId)
        {
            return 0.0f;
        }
        if (AgentActions.Missile == targetPowerId)
        {
            return 2f;
        }
        return 1f;
    }
    public override System.Collections.IEnumerator Execute(string sourceAgentId, 
                                string sourcePowerId, 
                                string targetAgentId, 
                                List<string> targetPowerIds, 
                                ATResourceData sourceResources,
                                ATResourceData targetResources)

    {

        Dictionary<string, float> primaryDelta = new Dictionary<string, float>();
        Dictionary<string, float> targetDelta = new Dictionary<string, float>();

        SpaceEncounterManager spaceEncounter = this.GetEncounterManager();
        spaceEncounter.NotifyAllScreens(SpaceEncounterManager.ObservableEffects.AttackOff);
        
        if ((float)sourceResources.GetResourceAmount(ResourceTypes.Ammunition) > 0)
        {
            
            primaryDelta[ResourceTypes.Fuel] = -1*fuelCost;
            primaryDelta[ResourceTypes.Ammunition] = -1*ammoCost;
            

            yield return CoroutineRunner.Instance.StartCoroutine(
                EffectHandler.ShootBlasterAt(
                    boltPrefab: Resources.Load<GameObject>(SpaceEncounterManager.PrefabPath.BoltPath),
                    explosionPrefab: Resources.Load<GameObject>(SpaceEncounterManager.PrefabPath.Explosion),
                    number: 20,
                    delay: 1f,
                    duration: 1f,
                    maxDistance: 3f,
                    source: spaceEncounter.GetAgentPosition(sourceAgentId),
                    target: spaceEncounter.GetAgentPosition(targetAgentId)
                )
            );
            float baseMultiplier = 1.0f;
            foreach(string targetPowerId in targetPowerIds)
            {
                baseMultiplier *= GetDamageMultiplierFor( sourceAgentId,  
                                                          sourcePowerId, 
                                                          targetAgentId,  
                                                          targetPowerId);

            }
            targetDelta[ResourceTypes.Hull] = -1f*baseDamage*baseMultiplier;

        }

        if (primaryDelta.Count > 0)
        {
            sourceResources.Deposit(primaryDelta);
        }
        if (targetDelta.Count > 0)
        {
            targetResources.Deposit(targetDelta);
        }

        yield break;
    }
}
