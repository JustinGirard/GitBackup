

using System.Collections.Generic;
using UnityEngine;

public class MissileSystem : StandardSystem
{
    // Additional stats specific to Attack system could be added here
    // For example: damage modifiers, ammo cost, etc.
    [SerializeField]
    private float fuelCost = 1f;
    [SerializeField]
    private float baseDamage = 5f;
    
    private string system_id = AgentActions.Shield;

    private float GetDamageMultiplierFor(string sourceAgentId, string sourcePowerId,string targetAgentId, string targetPowerId)
    {
        if (AgentActions.Attack == targetPowerId)
        {
            return 1f;
        }
        if (AgentActions.Shield == targetPowerId)
        {
            return 2f;
        }
        if (AgentActions.Missile == targetPowerId)
        {
            return 1f;
        }
        return 1f;
    }
    public override System.Collections.IEnumerator Execute(
                                string sourceAgentId, 
                                string sourcePowerId, 
                                string targetAgentId, 
                                List<string> targetPowerIds, 
                                ATResourceData sourceResources,
                                ATResourceData targetResources)
    {
        //
        //
        Dictionary<string, float> primaryDelta = new Dictionary<string, float>();
        Dictionary<string, float> targetDelta = new Dictionary<string, float>();


        SpaceEncounterManager spaceEncounter = this.GetEncounterManager();
        spaceEncounter.NotifyAllScreens(SpaceEncounterManager.ObservableEffects.MissileOff);

        if ((float)sourceResources.GetResourceAmount(ResourceTypes.Fuel) > 0)
        {
         //   Debug.Log("Execute Missile SUCCESS");

         //   Debug.Log($"{sourceAgentId} attacking {targetAgentId}");
            primaryDelta[ResourceTypes.Fuel] = -1*fuelCost;
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
        else
        {
            Debug.Log("Execute Missile FAIL");

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




//
//
//
//
//

