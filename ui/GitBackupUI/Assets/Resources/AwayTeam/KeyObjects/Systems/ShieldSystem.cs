using System.Collections.Generic;
using UnityEngine;
public class ShieldSystem : StandardSystem
{
    [SerializeField]
    private float missileCost = 1f;
    [SerializeField]
    private float fuelCost = 1f;
    [SerializeField]
    private float baseDamage = 5f;
    private string system_id = AgentPowerType.Attack;
    [SerializeField]
    private GameObject shieldInstance;
    private GameObject shieldChargePrefab;
    
    private bool __isRunning = false;
    private float __secondsLeft = 0f;
    public override System.Collections.IEnumerator Execute(
                                string sourceActionId,
                                GameObject sourceUnit, 
                                List<GameObject> targetUnit, 
                                ATResourceData sourceResources,
                                Agent sourceAgent)
    {
        //Debug.Log("Executing Shield");
        Dictionary<string, float> primaryDelta = new Dictionary<string, float>();
        GameEncounterBase spaceEncounter = this.GetEncounterManager();
        spaceEncounter.NotifyAllScreens(SpaceEncounterManager.ObservableEffects.ShieldOff);
        
        if ((float)sourceResources.GetResourceAmount(ResourceTypes.Fuel) > 0)
        {
            if (__isRunning == true)
            {
               // Debug.Log("Adding to Shield");
                __secondsLeft = __secondsLeft + 6f;
                yield break;
            }
            __isRunning = true;
            __secondsLeft =  5f;

            //Debug.Log("Running Shield");
            primaryDelta["Fuel"] = -1*fuelCost;
            sourceResources.Deposit(primaryDelta);
            shieldInstance.gameObject.SetActive(true);
            while(__secondsLeft > 0)
            {
                __secondsLeft -= 1;
                //Debug.Log($"...waiting Shield {__secondsLeft}");
                yield return new WaitForSeconds(1f);
            }
            //Debug.Log("Deactivating Shield");
            shieldInstance.gameObject.SetActive(false);
            __isRunning = false;
        }
        else
        {
            Debug.Log("Could not activate shield");
        }
        __isRunning = false;
        yield break;
    }
    
}