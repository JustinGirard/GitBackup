using System.Collections.Generic;
using UnityEngine;

public class ShieldSystem : StandardSystem
{
    // Additional stats specific to Attack system could be added here
    // For example: damage modifiers, ammo cost, etc.
    [SerializeField]
    private float missileCost = 1f;
    [SerializeField]
    private float fuelCost = 1f;
    [SerializeField]
    private float baseDamage = 5f;
    
    private string system_id = AgentActions.Attack;


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
        spaceEncounter.NotifyAllScreens(SpaceEncounterManager.ObservableEffects.ShieldOff);

        if ((float)sourceResources.GetResourceAmount(ResourceTypes.Fuel) > 0)
        {
            primaryDelta["Fuel"] = -1*fuelCost;
            yield return CoroutineRunner.Instance.StartCoroutine(EffectHandler.CreateShield(
                            shieldPrefab: Resources.Load<GameObject>(SpaceEncounterManager.PrefabPath.UnitShield),
                            source:spaceEncounter.GetAgentPosition(sourceAgentId)
            ));

        }
        else
        {
            Debug.Log("Execute Sheild FAIL");
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
    //
    //
    //
    //

    
}