

using System.Collections.Generic;
using UnityEditor.ShortcutManagement;
using UnityEngine;

public class MissileSystem : StandardSystem
{
    // Additional stats specific to Attack system could be added here
    // For example: damage modifiers, ammo cost, etc.
    [SerializeField]
    private float fuelCost = 1f;
    [SerializeField]
    private float baseDamage = 5f;
    
    //private string system_id = AgentAttackType.Shield;


    public override System.Collections.IEnumerator Execute(
                                string sourceActionId,
                                GameObject sourceUnit, 
                                List<GameObject> targetUnits, 
                                ATResourceData sourceResources,
                                Agent sourceAgent
                                )
    {
        //
        //
        Dictionary<string, float> primaryDelta = new Dictionary<string, float>();
        Dictionary<string, float> targetDelta = new Dictionary<string, float>();


        GameEncounterBase spaceEncounter = this.GetEncounterManager();
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
                explosionPrefab: Resources.Load<GameObject>(SpaceEncounterManager.PrefabPath.ExplosionRed),
                number: 20,
                delay: 1f,
                duration: 1f,
                arcHeight: 2f,
                source: sourceUnit.transform.position,
                target: targetUnits[0].transform.position
            ));
            float baseMultiplier = 1.0f;
            targetDelta[ResourceTypes.Hull] = -1f*baseDamage*baseMultiplier;
        }
        else
        {
            Debug.Log("Execute Missile FAIL");

        }
        ATResourceData targetResources =  targetUnits[0].GetComponent<ATResourceData>();
        if (targetResources == null)
        {
            Debug.LogError("Could not find resources on enemy");
            yield break;
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
            remainder =targetResources.Deposit(targetDelta);
            //float totalRemainder = remainder.Values.Sum();
        }
        if ((float)targetResources.Balance(ResourceTypes.Hull) <= 0)
        {
            GameObject explosionPrefab = Resources.Load<GameObject>(SpaceEncounterManager.PrefabPath.ExplosionRed);
            
            yield return CoroutineRunner.Instance.StartCoroutine(
                EffectHandler.SingleExplosion( 
                                           explosionPrefab:explosionPrefab,  
                                            targetParent:targetUnits[0],
                                            target:  targetUnits[0].transform.position,
                                            sizeSmall:2f, 
                                            sizeLarge:5f,
                                            cleanUp:null,
                                            cleanupDelay:0f)
             );
            if (targetUnits[0] != null)
            {
                SimpleShipController unit = targetUnits[0].GetComponent<SimpleShipController>();
                if (unit == null)
                    unit = targetUnits[0].GetComponentInParent<SimpleShipController>();
                unit.SafeDestroy();
            }
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

