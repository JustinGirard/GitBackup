
/*
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
        /// 0] PowerTransaction powerTrans = new PhysicalPowers.BlasterPower(PARAMS);
        /// 1] EncounterDelta worldChange = PhysicalEnvironment.AttemptPower(Power) -> (System)
        /// ---- EncounterDelta = PhysicalEvent
        /// ---- Timeout
        /// 2] EncounterManager.ApplyDelta(worldChange);
        /// 3] 
        Dictionary<string, float> primaryDelta = new Dictionary<string, float>();
        Dictionary<string, float> targetDelta = new Dictionary<string, float>();

        GameEncounterBase spaceEncounter = this.GetEncounterManager();
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
            SimpleShipController unit = targetUnit.GetComponentInParent<SimpleShipController>();
            unit.SafeDestroy();

        }


        yield break;
    }
}
*/

using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using AwayTeam;
using System;

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
    private BlasterPowerExecution __execution = null;
    public void TurnOff()
    {
        //Debug.Log("(1)__execution = null");
        __execution = null;
        GetEncounterManager().NotifyAllScreens(SpaceEncounterManager.ObservableEffects.AttackOff);

    }
    public override System.Collections.IEnumerator Execute(GameObject sourceUnit, 
                                string sourcePowerId, 
                                GameObject targetUnit, 
                                List<string> targetPowerIds, 
                                ATResourceData sourceResources,
                                ATResourceData targetResources)
    {
        // Check if an execution is already ongoing
        if (__execution != null)
        {
            //Debug.LogWarning("Previous power is already executing. Aborting new execution.");
            yield break;
        }
        //Debug.Log("Executing Blaster");

        // Load encounter context
        GameEncounterBase spaceEncounter = this.GetEncounterManager();
        if (spaceEncounter == null)
        {
            Debug.LogError("Missing space encounter context. Execution aborted.");
            yield break;
        }
        
        // Initialize BlasterExecution with required parameters
        //Debug.Log("(1)__execution = NEW");
        __execution = new BlasterPowerExecution(
            sourceResources: sourceResources,
            targetResources: targetResources,
            sourceUnit: sourceUnit,
            targetUnit: targetUnit,
            spaceEncounter: spaceEncounter,
            sourcePowerId: sourcePowerId,
            targetPowerIds: targetPowerIds,
            fuelCost: fuelCost,
            ammoCost: ammoCost,
            baseDamage: baseDamage
        );

        bool success = false;
        //The best overloaded Add method 'Dictionary<string, Func<Action<bool>, IEnumerator>>.Add(string, Func<Action<bool>, IEnumerator>)' for the collection initializer has some invalid argumentsCS1950
        Dictionary<string, Func<System.Action<bool>, System.Collections.IEnumerator>> executionPhases = 
            new Dictionary<string, Func<System.Action<bool>, System.Collections.IEnumerator>>
            {
                { "CanExecute", onFinishHandle => __execution.CanExecute(onFinishHandle) },
                { "BeforeExecute", onFinishHandle => __execution.BeforeExecute(onFinishHandle) },
                { "Execute", onFinishHandle => __execution.Execute(onFinishHandle) },
                { "AfterExecute", onFinishHandle => __execution.AfterExecute(onFinishHandle) }
            };
        foreach (KeyValuePair<string, Func<System.Action<bool>, System.Collections.IEnumerator>> phase in executionPhases)
        {
            /*success = false;
            yield return StartCoroutine(phase.Value(result => success = result));
            if (!success)
            {
                Debug.LogWarning($"Phase {phase.Key} failed.");
                TurnOff();
                yield break;
            }*/
            if (phase.Key == null)
            {
                Debug.LogError($"Null key found in execution phase.");
                break;
            }

            if (phase.Value == null)
            {
                Debug.LogError($"Null function found for phase {phase.Key}.");
                break;
            }

            success = false;
            if (__execution == null)
            {
                Debug.LogError($"__execution is null before phase {phase.Key} execution.");
                break;
            }

            // Execute phase and handle success
            yield return StartCoroutine(phase.Value(result => success = result));
            if (!success)
            {
                Debug.LogWarning($"Phase {phase.Key} failed.");
                break;
            }
        }

        // If all phases succeed
        TurnOff();
        yield break;
       
    }
}