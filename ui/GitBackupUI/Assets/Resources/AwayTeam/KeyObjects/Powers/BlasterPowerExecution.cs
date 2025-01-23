using System.Linq.Expressions;
using PhysicalModel;
using UnityEditor.Build.Content;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
namespace AwayTeam
{
    // A Basic power. Should handle all power concerns
    class BlasterPowerExecution 
    { 

        float __fuelCost;
        float __ammoCost;
        float __baseDamage;
        GameEncounterBase __spaceEncounter;
        GameObject __sourceUnit;
        string __sourcePowerId;
        GameObject __targetUnit;
        List<string> __targetPowerIds;
        ATResourceData __sourceResources;
        ATResourceData __targetResources;

        PhysicalModel.Transaction __physicalExecution = null;

        // Constructor
        public BlasterPowerExecution(
            ATResourceData sourceResources,
            ATResourceData targetResources,
            GameObject sourceUnit,
            GameObject targetUnit,
            GameEncounterBase spaceEncounter,
            string sourcePowerId,
            List<string> targetPowerIds,
            float fuelCost,
            float ammoCost,
            float baseDamage)
        {
            // Null checks with Debug.LogError
            if (sourceResources == null)
                Debug.LogError("BlasterPowerExecution: sourceResources is null.");
            if (targetResources == null)
                Debug.LogError("BlasterPowerExecution: targetResources is null.");
            if (sourceUnit == null)
                Debug.LogError("BlasterPowerExecution: sourceUnit is null.");
            if (targetUnit == null)
                Debug.LogError("BlasterPowerExecution: targetUnit is null.");
            if (spaceEncounter == null)
                Debug.LogError("BlasterPowerExecution: spaceEncounter is null.");
            if (string.IsNullOrEmpty(sourcePowerId))
                Debug.LogError("BlasterPowerExecution: sourcePowerId is null or empty.");
            if (targetPowerIds == null)
                Debug.LogError("BlasterPowerExecution: targetPowerIds is null or empty.");

            __sourceResources = sourceResources;
            __targetResources = targetResources;
            __sourceUnit = sourceUnit;
            __targetUnit = targetUnit;
            __spaceEncounter = spaceEncounter;
            __sourcePowerId = sourcePowerId;
            __targetPowerIds = targetPowerIds;
            __fuelCost = fuelCost;
            __ammoCost = ammoCost;
            __baseDamage = baseDamage;
        }

        public System.Collections.IEnumerator CanExecute(System.Action<bool> onFinish)
        {
            /// 1 - [ ] Remove Physics
            /// 2 - [ ] Remove Resource Processing
            /// 3 - [ ] Remove Animation
            if ((float)__sourceResources.GetResourceAmount(ResourceTypes.Ammunition) < __ammoCost)
                onFinish.Invoke(false);
            if ((float)__sourceResources.GetResourceAmount(ResourceTypes.Fuel) < __fuelCost)
                onFinish.Invoke(false);

            if (onFinish!= null)
                onFinish.Invoke(true);
            yield break;
        }

        public System.Collections.IEnumerator  BeforeExecute(System.Action<bool> onFinish)
        {
            //
            // RESOURCES
            // 
            // Withdraw resources from source
            Dictionary<string, float> primaryDelta = new Dictionary<string, float>();
            primaryDelta[ResourceTypes.Fuel] = -1*__fuelCost;
            primaryDelta[ResourceTypes.Ammunition] = -1*__ammoCost;
            Dictionary<string,float> remainder;            
            remainder = __sourceResources.Deposit(primaryDelta);
            float totalRemainder = remainder.Values.Sum();
            if (totalRemainder >0)
                onFinish.Invoke(false);

            if (onFinish!= null)
                onFinish.Invoke(true);
            yield break;
        }
        
        public System.Collections.IEnumerator  AfterExecute(System.Action<bool> onFinish)
        {
            //CoroutineRunner.Instance.DebugLog("");
            float baseMultiplier = 1.0f;
            foreach(string targetPowerId in __targetPowerIds)
            {
                baseMultiplier *= GetDamageMultiplierFor( __sourceUnit,  
                                                        __sourcePowerId, 
                                                        __targetUnit,  
                                                        targetPowerId);

            }
            Dictionary<string, float> targetDelta = new Dictionary<string, float>();
            targetDelta[ResourceTypes.Hull] = -1f*__baseDamage*baseMultiplier;

            Dictionary<string,float> remainder;
            //((ATResourceDataGroup) sourceResources).
            if (targetDelta.Count > 0)
            {
                remainder = __targetResources.Deposit(targetDelta);
                //float totalRemainder = remainder.Values.Sum();
            }
            if ((float)__targetResources.Balance(ResourceTypes.Hull) <= 0)
            {
                GameObject explosionPrefab = Resources.Load<GameObject>(SpaceEncounterManager.PrefabPath.ExplosionRed);
                
                yield return CoroutineRunner.Instance.StartCoroutine(
                    EffectHandler.SingleExplosion( 
                                            explosionPrefab:explosionPrefab,  
                                            targetParent:__targetUnit,
                                            target:  __targetUnit.transform.position,
                                            sizeSmall:2f, 
                                            sizeLarge:5f,
                                            cleanUp:null,
                                            cleanupDelay:0f
                                            )
                );
                SimpleShipController unit = __targetUnit.GetComponentInParent<SimpleShipController>();
                //Debug.Log("DESTROYING UNIT");
                unit.SafeDestroy();
            }

            if (onFinish!= null)
                onFinish.Invoke(true);
            yield break;
        }

        public System.Collections.IEnumerator  Execute(System.Action<bool> onFinish)
        {
            // Submit power to the physics engine
            PhysicalModel.Graph graph = __spaceEncounter.GetPhysicalModel();
            //Transaction t = new Transaction( 
            //    TransactionType.Projectile, 
            //    __sourceUnit,
            //    __targetUnit);
            //graph.AddTransaction(t);

            // Get Physics result
            // Save Records for processing (?)
            yield return InnerExecute(sourceUnit: __sourceUnit,targetUnit: __targetUnit);
            if (onFinish!= null)
                onFinish.Invoke(true);
            yield break;
        }

        private static float GetDamageMultiplierFor(GameObject sourceAgent, string sourcePowerId,GameObject targetAgent, string targetPowerId)
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

        private static System.Collections.IEnumerator InnerExecute( GameObject sourceUnit, GameObject targetUnit)
        {
            yield return CoroutineRunner.Instance.StartCoroutine(
                PhysicsHandler.ShootBlasterAt(
                    boltPrefab: Resources.Load<GameObject>(SpaceEncounterManager.PrefabPath.BoltPath),
                    explosionPrefab: Resources.Load<GameObject>(SpaceEncounterManager.PrefabPath.ExplosionBlue),
                    number: 4,
                    delay: 0.01f,
                    speed: 30f,
                    lifetime:15f,
                    projectileKickback:50f,
                    impactKickback:50f,
                    sourceUnit: sourceUnit,
                    sourceOffset: sourceUnit.transform.forward * 2,
                    targetUnit: targetUnit,
                    deviation:0.01f
                )
            );
            yield break;
        }


    }

}