using System.Collections.Generic;
using UnityEngine;

class TargetingModule: MonoBehaviour
{
    private GameEncounterBase spaceEncounter;
    private List<GameObject> enemyUnits;
    private List<GameObject> agentUnits;

    Dictionary<GameObject,List<GameObject>> assignedTargets;

    public List<GameObject> GetAssignedTargets(GameObject forUnit, EncounterSquad squad)
    {
        //if (!assignedTargets.ContainsKey(forUnit))
        //{
        //    Debug.LogError($"Could not find a target entry for {forUnit.name}");
        //    return null;
        //}
        List<SimpleShipController> ships = squad.GetUnitList();
        List<GameObject> gos = new List<GameObject>();
        foreach(SimpleShipController ship in ships)
        {
            gos.Add(ship.gameObject);
        }
        return gos;
        //return assignedTargets[forUnit];
    }

    public void LoadList()
    {

    }

}