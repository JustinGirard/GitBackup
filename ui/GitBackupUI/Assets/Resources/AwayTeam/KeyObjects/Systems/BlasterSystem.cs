
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

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

    private string system_id = AgentActionType.Attack;

    private float GetDamageMultiplierFor(GameObject sourceAgent, string sourcePowerId,GameObject targetAgent, string targetPowerId)
    {
        if (AgentActionType.Attack == targetPowerId)
        {
            return 0.5f;
        }
        if (AgentActionType.Shield == targetPowerId)
        {
            return 0.0f;
        }
        if (AgentActionType.Missile == targetPowerId)
        {
            return 2f;
        }
        return 1f;
    }
    public override System.Collections.IEnumerator Execute(GameObject sourceUnit, 
                                string sourcePowerId, 
                                GameObject targetUnit, 
                                List<string> targetPowerIds, 
                                ATResourceData sourceResources,
                                ATResourceData targetResources)

    {

        Dictionary<string, float> primaryDelta = new Dictionary<string, float>();
        Dictionary<string, float> targetDelta = new Dictionary<string, float>();

        SpaceEncounterManager spaceEncounter = this.GetEncounterManager();
        if (spaceEncounter == null)
            Debug.LogError("MISSING spaceEncounter");
        spaceEncounter.NotifyAllScreens(SpaceEncounterManager.ObservableEffects.AttackOff);
        
        if ((float)sourceResources.GetResourceAmount(ResourceTypes.Ammunition) > 0)
        {
            
            primaryDelta[ResourceTypes.Fuel] = -1*fuelCost;
            primaryDelta[ResourceTypes.Ammunition] = -1*ammoCost;
            // sourceUnit.transform.position,
            // targetUnit.transform.position,
            yield return CoroutineRunner.Instance.StartCoroutine(
                EffectHandler.ShootBlasterAt(
                    boltPrefab: Resources.Load<GameObject>(SpaceEncounterManager.PrefabPath.BoltPath),
                    explosionPrefab: Resources.Load<GameObject>(SpaceEncounterManager.PrefabPath.Explosion),
                    number: 4,
                    delay: 1f,
                    duration: 1f,
                    maxDistance: 3f,
                    source: sourceUnit.transform.position,
                    target: targetUnit.transform.position,
                    deviation:0.1f
                )
            );
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

        }


        yield break;
    }
}
