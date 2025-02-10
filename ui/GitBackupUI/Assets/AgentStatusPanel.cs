using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentStatusPanel : MonoBehaviour
{
    [SerializeField]
    private GameObject unitStatusPanelPrefab;
    [SerializeField]
    private Agent attachedAgent;
    [SerializeField]
    private GameEncounterBase encounterBase;

    private List<GameObject> units = new List<GameObject>();
    private Dictionary<int, GameObject> unitPanels = new Dictionary<int, GameObject>();

    private Agent lastAttachedAgent = null;
    private int lastSig = 0;

    void Update()
    {
        // Check if attachedUnit has changed
        if (attachedAgent != lastAttachedAgent || GetUnitSig() != lastSig)
        {
            //Debug.Log("Attaching Units 1");
            ClearPowerPanels();
            
            //Debug.Log("Attaching Units 2");
            if (attachedAgent != null || GetUnitSig() != lastSig)
            {
                //Debug.Log("Attaching Units 2");
                AttachUnits();
                CreateUnitPanels();
                lastSig = GetUnitSig();
            }
            if (units.Count > 0) // Only finish attachment if you found units.
                lastAttachedAgent = attachedAgent;

            // Track the last attached unit
        }
    }
    private int GetUnitSig()
    {
        int sig=0;
        foreach(GameObject unit in units)
        {
            if (unit != null)
                sig = sig + unit.GetInstanceID();
        }
        return sig;
    }

    private void AttachUnits()
    {
        //Debug.Log("Doing attach Units inner");
        units.Clear();
        GameObject sourceUnit = attachedAgent.GetUnit();
        if (sourceUnit == null)
        {
            //Debug.LogWarning("a. Could not attach units to observation panel, seems Encounter Squad is not present on agent");
            return; 

        }
        EncounterSquad sq = sourceUnit.GetComponent<EncounterSquad>();
        if (sq == null)
        {
            Debug.LogWarning("b. Could not attach units to observation panel, seems Encounter Squad is not present on agent");
            return; 

        }
        List<SimpleShipController> squadUnits = sq.GetUnitList();
        foreach (SimpleShipController con in squadUnits)
        {
            units.Add(con.gameObject);
        }
    }

    private void CreateUnitPanels()
    {
        foreach (var unit in units)
        {
            GameObject panel = Instantiate(unitStatusPanelPrefab, this.transform);
            panel.GetComponent<UnitStatusPanel>().BindUnit(unit);
            panel.GetComponent<UnitStatusPanel>().BindEncounter(encounterBase);
            unitPanels[unit.gameObject.GetInstanceID()] = panel;
        }
    }

    private void ClearPowerPanels()
    {
        foreach (var panel in unitPanels.Values)
            Destroy(panel);
        unitPanels.Clear();
    }
}
