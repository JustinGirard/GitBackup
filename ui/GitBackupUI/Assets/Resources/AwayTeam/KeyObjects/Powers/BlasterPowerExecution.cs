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
        GameObject __muzzleChargePrefab;
        GameObject __boltPrefab;
        GameObject __damagePrefab;
        GameObject __impactPrefab;
        GameObject __muzzleFlashPrefab;

        float __fuelCost;
        float __ammoCost;
        float __baseDamage;
        GameEncounterBase __spaceEncounter;
        GameObject __sourceUnit;
        string __sourcePowerId;
        List<GameObject> __targetUnits;
        ATResourceData __sourceResources;

        PhysicalModel.Transaction __physicalExecution = null;

        // Constructor
        public BlasterPowerExecution(
            GameObject muzzleChargePrefab,
            GameObject boltPrefab,
            GameObject damagePrefab,
            GameObject impactPrefab,
            GameObject muzzleFlashPrefab,
            ATResourceData sourceResources,
            GameObject sourceUnit,
            List<GameObject> targetUnits,
            GameEncounterBase spaceEncounter,
            float fuelCost,
            float ammoCost,
            float baseDamage)
        {
            // Null checks with Debug.LogError
            if (sourceResources == null)
                Debug.LogError("BlasterPowerExecution: sourceResources is null.");
            if (sourceUnit == null)
                Debug.LogError("BlasterPowerExecution: sourceUnit is null.");
            if (targetUnits == null)
                Debug.LogError("BlasterPowerExecution: targetUnit is null.");
            if (spaceEncounter == null)
                Debug.LogError("BlasterPowerExecution: spaceEncounter is null.");
            if (muzzleChargePrefab == null)
                Debug.LogError("BlasterPowerExecution: muzzleChargePrefab is null.");

            __muzzleChargePrefab = muzzleChargePrefab;
            __boltPrefab = boltPrefab;
            __damagePrefab = damagePrefab;
            __impactPrefab = impactPrefab;
            __muzzleFlashPrefab = muzzleFlashPrefab;

            //Debug.Log($"Loaded Muzzle {__muzzleChargePrefab}");
            __sourceResources = sourceResources;
            __sourceUnit = sourceUnit;
            __targetUnits = targetUnits;
            __spaceEncounter = spaceEncounter;
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
            //Debug.Log("Loading Muzzle");
            //__muzzleChargeInstance = ObjectPool.Instance().Load(__muzzleChargePrefab);
            //Debug.Log($"Loading Muzzle {__muzzleChargeInstance} ");
            //__muzzleChargeInstance.transform.parent = __sourceUnit.transform;
            //__muzzleChargeInstance.transform.position = __sourceUnit.transform.position;

            if (onFinish!= null)
                onFinish.Invoke(true);
            yield break;
        }
        
        public System.Collections.IEnumerator  AfterExecute(System.Action<bool> onFinish)
        {
            //CoroutineRunner.Instance.DebugLog("");
            /*
            float baseMultiplier = 1.0f;
            Dictionary<string, float> targetDelta = new Dictionary<string, float>();
            targetDelta[ResourceTypes.Hull] = -1f*__baseDamage*baseMultiplier;

            Dictionary<string,float> remainder;
            ATResourceData targetResources = __targetUnits[0].GetComponent<ATResourceData>();
            if (targetResources == null)
            {
                if (onFinish!= null)
                    onFinish.Invoke(true);
                yield break;
            }

            if (targetDelta.Count > 0)
            {
                remainder = targetResources.Deposit(targetDelta);
            }

            if ((float)targetResources.Balance(ResourceTypes.Hull) <= 0)
            {
                GameObject explosionPrefab = Resources.Load<GameObject>(SpaceEncounterManager.PrefabPath.ExplosionRed);
                
                yield return CoroutineRunner.Instance.StartCoroutine(
                    EffectHandler.SingleExplosion( 
                                            explosionPrefab:explosionPrefab,  
                                            targetParent:__targetUnits[0],
                                            target:  __targetUnits[0].transform.position,
                                            sizeSmall:2f, 
                                            sizeLarge:5f,
                                            cleanUp:null,
                                            cleanupDelay:0f
                                            )
                );
                SimpleShipController unit = __targetUnits[0].GetComponentInParent<SimpleShipController>();
                //Debug.Log("DESTROYING UNIT");
                unit.SafeDestroy();
            }
            */

            if (onFinish!= null)
                onFinish.Invoke(true);
            yield break;
        }

        public System.Collections.IEnumerator  Execute(System.Action<bool> onFinish)
        {
            // Submit power to the physics engine
            PhysicalModel.Graph graph = __spaceEncounter.GetPhysicalModel();

            // Save Records for processing (?)
            //__muzzleChargeInstance.SetActive(false);
            //__muzzleChargeInstance = null;
            AudioManager.Instance.Play("laser_gun_01");            
            yield return InnerExecute(sourceUnit: __sourceUnit,
                                        targetUnit: __targetUnits[0],
                                        boltPrefab:__boltPrefab,
                                        explosionPrefab:__impactPrefab,
                                        baseDamage:__baseDamage);
            if (onFinish!= null)
                onFinish.Invoke(true);
            yield break;
        }

        private static System.Collections.IEnumerator InnerExecute( GameObject sourceUnit, 
                                                                    GameObject targetUnit,
                                                                    GameObject boltPrefab,
                                                                    GameObject explosionPrefab,
                                                                    float baseDamage
                                                                    )
        {
            
            yield return CoroutineRunner.Instance.StartCoroutine(
                PhysicsHandler.ShootBlasterAt(
                    boltPrefab: boltPrefab,
                    explosionPrefab: explosionPrefab,
                    number: 1,
                    baseDamage:baseDamage,
                    delay: 0.01f,
                    speed: 30f,
                    lifetime:1f,
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