using System.Collections.Generic;
using System.Data.Common;
using Unity.VisualScripting;
using UnityEngine;
//            

public class UnitStatusPanel : MonoBehaviour
{
    [SerializeField]
    private GameObject powerStatusPanelPrefab;
    [SerializeField]
    private GameObject resourceStatusPanelPrefab;
    [SerializeField]
    private GameObject attachedUnit;
    [SerializeField]
    private GameEncounterBase encounterBase;
    [SerializeField]
    private bool showSelectedUnit = false;

    private List<StandardSystem> powerSystems = new List<StandardSystem>();
    private List<string> resourcesReported = new List<string>();

    private Dictionary<int, GameObject> powerPanels = new Dictionary<int, GameObject>();
    private Dictionary<int, GameObject> resourcePanels = new Dictionary<int, GameObject>();

    private GameObject lastAttachedUnit = null;
    private Agent lastTargetEntity = null;
    public void BindEncounter(GameEncounterBase encounterBase)
    {
        this.encounterBase= encounterBase;        
    }

    public void BindUnit(GameObject unit)
    {
        attachedUnit= unit;        
    }
    // UINotificationWaterfall.Instance().Dispatch("basic", "player_control_mode", $"nav_mode: {playerAgent.GetPlayerNavigationMode()}", 10f, true); 
    public bool NeedUpdate()
    {
          if (attachedUnit != lastAttachedUnit)
            return true;
          if (attachedUnit.GetComponent<PlayerAgent>() != null)
          {
            //lastTargetEntity
            Agent playersSelectedAgent = GetTargetAgent();
            if (lastTargetEntity != playersSelectedAgent &&showSelectedUnit == true )
                return true;
          }
        return false;
    }

    public Agent GetTargetAgent()
    {
        PlayerAgent agt = attachedUnit.GetComponent<PlayerAgent>() ;
        if(agt == null)
            return null;
        Agent playerTargetAgent = agt.GetPrimaryEnemyAgent();
        return playerTargetAgent;
    }
    void Update()
    {
        // Check if attachedUnit has changed
        if (NeedUpdate())
        {
            //if(showSelectedUnit == true)
            //    Debug.Log("UnitTargetStatus: Doing update ");
            ClearPanels();
            
            if (attachedUnit != null && GetResourceDataRef() != null)
            {
                AttachSystems();
                CreatePanels();
            }

            // Track the last attached unit
            lastAttachedUnit = attachedUnit;
            lastTargetEntity =   GetTargetAgent();

        }
    }

    private void AttachSystems()
    {
        //Debug.Log("Doing attach");
        powerSystems.Clear();
        resourcesReported.Clear();

        if (attachedUnit.TryGetComponent(out BlasterSystem blaster))
            powerSystems.Add(blaster);
        if (attachedUnit.TryGetComponent(out NavigationSystem shield))
            powerSystems.Add(shield);

        resourcesReported.Add(ResourceTypes.Ammunition);
        resourcesReported.Add(ResourceTypes.Hull);
        resourcesReported.Add(ResourceTypes.Fuel);
        resourcesReported.Add(ResourceTypes.Missiles);


    }
    private ATResourceData GetResourceDataRef()
    {
        if (attachedUnit == null)
        {
            Debug.LogError("GetResourceDataRef: attachedUnit is null");
            return null;
        }

        if (showSelectedUnit == true)
        {
            PlayerAgent targetAgent = attachedUnit.GetComponent<PlayerAgent>();
            if (targetAgent == null)
            {
                Debug.LogError("GetResourceDataRef: PlayerAgent component missing on attachedUnit");
                return null;
            }

            Agent selectedAgent = targetAgent.GetPrimaryEnemyAgent();
            if (selectedAgent == null)
            {
                //Debug.LogError("GetResourceDataRef: GetPrimaryEnemyAgent() returned null");
                return null;
            }
        }

        ATResourceData resourceData = attachedUnit.GetComponent<ATResourceData>();
        if (resourceData == null)
        {
            Debug.LogError("GetResourceDataRef: ATResourceData component missing on attachedUnit");
        }

        return resourceData;
    }

    private void CreatePanels()
    {

        if (powerStatusPanelPrefab != null)
        {
            foreach (var system in powerSystems)
            {
                GameObject panel = Instantiate(powerStatusPanelPrefab, this.transform);
                panel.SetActive(true);
                panel.GetComponent<StandardPowerStatusPanel>().BindSystem(system);
                powerPanels[system.gameObject.GetInstanceID()] = panel;
            }
        }
        if (resourceStatusPanelPrefab != null)
        {
            foreach (var resId in resourcesReported)
            {
                ATResourceData resourcesToBind = GetResourceDataRef();
                GameObject panel = Instantiate(resourceStatusPanelPrefab, this.transform);
                panel.SetActive(true);
                panel.GetComponent<StandardResourceStatusPanel>().BindResource(resId,resourcesToBind);

                int idHash = System.HashCode.Combine(resId.GetHashCode(), resourcesToBind.gameObject.GetInstanceID());
                resourcePanels[idHash] = panel;
            }
        }
    }

    private void ClearPanels()
    {
        foreach (var panel in powerPanels.Values)
            Destroy(panel);
        foreach (var panel in resourcePanels.Values)
            Destroy(panel);
        
        powerPanels.Clear();
        resourcePanels.Clear();
    }
}
