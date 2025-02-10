
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
    // [SerializeField]
    private bool __isActivated = false;

    [SerializeField] private List<GameObject> emitter;
    [SerializeField] private GameObject muzzleChargePrefab;
    [SerializeField] private GameObject muzzleFlashPrefab;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private GameObject impactDefaultPrefab;
    [SerializeField] private GameObject damageDefaultPrefab;

    [SerializeField] private List<GameObject> __muzzleChargeInstances;

    private string system_id = AgentPowerType.Attack;
    private BlasterPowerExecution __execution = null;

    private new void Awake()
    {
        base.Awake();
        attachedStates["charging"] = new BlasterCharge(this);
        attachedStates["executing"] = new BlasterExecute(this);
    }

    public class BlasterCharge:StandardCharge
    {
        public BlasterCharge(StandardSystem system):base(system){}

        public override System.Collections.IEnumerator Deactivate(ATResourceData sourceResources) 
        {
            system.SetBehaviour("deactivated"); 
            if (system is BlasterSystem b1) yield return b1.ClearMuzzleCharges();
            //CoroutineRunner.Instance.DebugLog("BlasterCharge: Deactivated");
            yield break;
        }  

        public override System.Collections.IEnumerator StateUpdate(float timeDelta)
        {
            FloatRange chargeLevel = system.GetLevel("charge");
            if (system is BlasterSystem b2) yield return b2.ShowMuzzleCharge();
            if (chargeLevel.Percent() < 1.0f )
            {
                chargeLevel.Add( timeDelta);
                //CoroutineRunner.Instance.DebugLog("BlasterCharge: Charging up");
            }            
            if (chargeLevel.Percent() > 0.99f)
            {
                if (system is BlasterSystem b1) yield return b1.ClearMuzzleCharges();
                system.SetBehaviour("executing");
            }
            yield break;
        }

    }

    public class BlasterExecute:StandardExecute
    {
        public BlasterExecute(StandardSystem system):base(system){}

        public override System.Collections.IEnumerator Execute( string sourceActionId, GameObject sourceUnit,  List<GameObject> targetUnits, ATResourceData sourceResources, Agent sourceAgent)
        {
            FloatRange overheatLevel = system.GetLevel("overheat");
            FloatRange executeLevel = system.GetLevel("execute");
            overheatLevel.Add( 0.25f);
            executeLevel.Add( 0.5f);
            //CoroutineRunner.Instance.DebugLog("StandardExecute:Executing ");

            if (executeLevel.Percent() > 0.99 || overheatLevel.Percent() > 0.99)
            {
                //Debug.Log($"Reached Final state executeLevel:{executeLevel.Percent()}, overheatLevel:{overheatLevel.Percent()}");
                system.GetLevel("charge").Set(0);
                system.GetLevel("execute").Set(0);
                system.SetBehaviour("deactivated");
                yield break;
            }
            if (system is BlasterSystem b1) yield return b1.ShootBlaster(  
                sourceUnit:sourceUnit, 
                targetUnits:targetUnits, 
                sourceResources:sourceResources,  
                sourceAgent:sourceAgent);

            yield break;
        }  

        //public override System.Collections.IEnumerator StateUpdate(float timeDelta){yield break;}
    }       



    public  System.Collections.IEnumerator ShowMuzzleCharge( )
    {
        if (__muzzleChargeInstances.Count ==0)
        {
            __muzzleChargeInstances.Clear();
            foreach (var em in emitter)
            {
                if (em != null && muzzleChargePrefab != null)
                {
                    GameObject muzzleChargeInstance = ObjectPool.Instance().Load(muzzleChargePrefab);
                    if (muzzleChargeInstance != null)
                    {
                        muzzleChargeInstance.transform.parent =  em.transform;
                        muzzleChargeInstance.transform.localScale = Vector3.one;
                        muzzleChargeInstance.transform.position = em.transform.position;
                        muzzleChargeInstance.transform.rotation = em.transform.rotation;
                        muzzleChargeInstance.SetActive(true);
                        __muzzleChargeInstances.Add(muzzleChargeInstance);
                    }
                }
            }
        }
        yield break;
    }

    public  System.Collections.IEnumerator ClearMuzzleCharges()
    {
        foreach (var instance in __muzzleChargeInstances)
        {
            if (instance != null)
            {
                instance.SetActive(false);
            }
        }
        __muzzleChargeInstances.Clear();
        yield break;
    }    
    public override string GetShortDescriptionText()
    {
        return "m:normal";
    }

    public  System.Collections.IEnumerator ShootBlaster(GameObject sourceUnit, 
                                List<GameObject> targetUnits, 
                                ATResourceData sourceResources,
                                Agent sourceAgent)
    {
        if (__execution != null)
        {
            Debug.LogWarning("No Already shooting!");
            yield break;
        }

        GameEncounterBase spaceEncounter = this.GetEncounterManager();
        if (spaceEncounter == null)
        {
            Debug.LogError("Missing space encounter context. Execution aborted.");
            yield return StartCoroutine(Deactivate(sourceResources));
            yield break;
        }
        //Debug.Log($"Linked Prefab {this.muzzleChargePrefab}");
     
        __execution = new BlasterPowerExecution(
            muzzleChargePrefab:this.muzzleChargePrefab,
            boltPrefab:this.projectilePrefab,
            damagePrefab:this.damageDefaultPrefab,
            impactPrefab:this.impactDefaultPrefab,
            muzzleFlashPrefab:this.muzzleChargePrefab,
            sourceResources: sourceResources,
            sourceUnit: sourceUnit,
            targetUnits: targetUnits,
            spaceEncounter: spaceEncounter,
            fuelCost: fuelCost,
            ammoCost: ammoCost,
            baseDamage: baseDamage
        );

        bool success = false;
        // TODO kill this complexity
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
        __execution = null;
        yield break;
    }
}