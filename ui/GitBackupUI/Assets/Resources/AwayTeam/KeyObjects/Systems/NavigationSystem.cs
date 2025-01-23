
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class NavigationSystem : StandardSystem
{
    // Additional stats specific to Attack system could be added here
    // For example: damage modifiers, ammo cost, etc.
    [SerializeField]
    private float ammoCost = 1f;
    [SerializeField]
    private float fuelCost = 1f;
    [SerializeField]
    private float baseDamage = 2f;

    private string system_id = AgentNavigationType.NavigateTo;

    public override System.Collections.IEnumerator Execute(GameObject sourceUnit, 
                                string sourcePowerId, 
                                GameObject targetUnit, 
                                List<string> targetPowerIds, 
                                ATResourceData sourceResources,
                                ATResourceData targetResources)

    {
        //Debug.Log($"RUNNING NavigationSystem COMMAND for: {sourceUnit.name}");
        Dictionary<string, float> primaryDelta = new Dictionary<string, float>();
        Dictionary<string, float> targetDelta = new Dictionary<string, float>();

        //GameEncounterBase spaceEncounter = this.GetEncounterManager();
        //if (spaceEncounter == null)
        //    Debug.LogError("MISSING spaceEncounter");
        //spaceEncounter.NotifyAllScreens(SpaceEncounterManager.ObservableEffects.AttackOff);
        
        if ((float)sourceResources.GetResourceAmount(ResourceTypes.Fuel) > 0)
        {
            

        }
        yield break;
    }
}
