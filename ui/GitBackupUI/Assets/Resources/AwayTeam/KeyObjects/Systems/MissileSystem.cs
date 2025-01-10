

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
    
    private string system_id = AgentActionType.Shield;

    private float GetDamageMultiplierFor(GameObject sourceUnit, string sourcePowerId,GameObject targetUnit, string targetPowerId)
    {
        if (AgentActionType.Attack == targetPowerId)
        {
            return 1f;
        }
        if (AgentActionType.Shield == targetPowerId)
        {
            return 2f;
        }
        if (AgentActionType.Missile == targetPowerId)
        {
            return 1f;
        }
        return 1f;
    }
    public override System.Collections.IEnumerator Execute(
                                GameObject sourceUnit, 
                                string sourcePowerId, 
                                GameObject targetUnit, 
                                List<string> targetPowerIds, 
                                ATResourceData sourceResources,
                                ATResourceData targetResources)
    {
        //
        //
        Dictionary<string, float> primaryDelta = new Dictionary<string, float>();
        Dictionary<string, float> targetDelta = new Dictionary<string, float>();


        SpaceEncounterManager spaceEncounter = this.GetEncounterManager();
        if (spaceEncounter == null)
            Debug.LogError("MISSING spaceEncounter");        
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
                source: sourceUnit.transform.position,
                target: targetUnit.transform.position
            ));
            float baseMultiplier = 1.0f;
            foreach(string targetPowerId in targetPowerIds)
            {
                baseMultiplier *= GetDamageMultiplierFor( sourceUnit,  
                                                                         sourcePowerId, 
                                                                         targetUnit,  
                                                                         targetPowerId);

            }
            targetDelta[ResourceTypes.Hull] = -1f*baseDamage*baseMultiplier;
        }
        else
        {
            Debug.Log("Execute Missile FAIL");

        }

        Dictionary<string,float> remainder;
        //((ATResourceDataGroup) sourceResources).
        if (primaryDelta.Count > 0)
        {
            remainder = sourceResources.Deposit(primaryDelta);
            //float totalRemainder = remainder.Values.Sum();
        }
        if (targetDelta.Count > 0)
        {
            remainder = targetResources.Deposit(targetDelta);
            //float totalRemainder = remainder.Values.Sum();
        }
        if ((float)targetResources.Balance(ResourceTypes.Hull) <= 0)
        {
            GameObject explosionPrefab = Resources.Load<GameObject>(SpaceEncounterManager.PrefabPath.Explosion);
            
            yield return CoroutineRunner.Instance.StartCoroutine(
                EffectHandler.SingleExplosion( 
                                        explosionPrefab,  
                                        targetUnit.transform.position,  
                                        2f, 
                                        5f)
             );
            SpaceMapUnitAgent unit = targetUnit.GetComponentInParent<SpaceMapUnitAgent>();
            unit.SafeDestroy();
            //if (unit != null)
            //{
            //}

        }  
        yield break;
    }
}




//
//
//
//
//

