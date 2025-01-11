using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class AgentManager : MonoBehaviour
{
    private Dictionary<string,Agent> __agents = new Dictionary<string, Agent>();
    [SerializeField]
    private List<AgentPrefab> __initOnlyAgents = new List<AgentPrefab>();

    public Agent GetPlayerAgent()
    {
        return __agents["agent_1"]; 
    }

    void Awake(){
        if (__initOnlyAgents.Count <= 0)
        {
            Debug.LogError("agentManager MUST have agents attached. For now an agent_1 and agent_2");
            return;
        }        
        foreach (AgentPrefab apre in __initOnlyAgents)
        {
            //Debug.Log($"adding agent {apre.agent_id}");
            __agents[apre.agent_id] = apre.agentPrefab;
        }        


    }
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public Agent GetAgent(string agentId)
    {
        return __agents[agentId] ;
    }

    public bool HasAgentKey(string agentId)
    {
        return __agents.ContainsKey(agentId) ;
    }




    public void ForEachAgent(System.Action<Agent> action)
    {
        //Sema.TryAcquireLock($"ForEachAgent{this.GetInstanceID()}");
        
        List<string> toRemove = new List<string>();
        
        foreach (var kvp in __agents)
        {
            if (kvp.Value == null || kvp.Value.gameObject == null)
            {
                toRemove.Add(kvp.Key);
            }
            else
            {
                action(kvp.Value);
            }
        }

        foreach (var key in toRemove)
        {
            __agents.Remove(key);
        }
    }

    public string encounterSquadPrefab = "AwayTeam/KeyObjects/EncounterSquad"; // Path to SpaceMapUnit prefab in Resources

    public bool InitializeAgent(Agent agent, string agentKey)
    {
        GameObject unit = Instantiate(Resources.Load<GameObject>(encounterSquadPrefab));
        if (unit.GetComponent<EncounterSquad>() == null)
        {
            Debug.LogError($"Unit{agentKey.Last()} could not create EncounterSquad");
            return false;
        }

        unit.name = $"Squad{agentKey.Last()}";
        unit.GetComponent<EncounterSquad>().Rebuild();
        agent.SetUnit(unit);

        Dictionary<string, ATResourceData> redat = agent.GetResourceObject().GetSubResources();
        if (redat.Keys.Count == 0)
        {
            Debug.LogError($"No Agents in Unit {agentKey.Last()} Agent");
            return false;
        }

        unit.transform.position = agent.transform.position + (agentKey == "agent_2" ? new Vector3(0, 2, 0) : Vector3.zero);
        unit.transform.rotation = agent.transform.rotation;
        unit.transform.parent = agent.transform;
        unit.GetComponent<EncounterSquad>().UpdatePosition();
        GameEncounterBase encounter = this.GetComponent<GameEncounterBase>();
        if (encounter == null)
            Debug.LogError("Critica: Could not find attached GameEncounterBase ");
        List<SpaceMapUnitAgent> sourceUnits = unit.GetComponent<EncounterSquad>().GetUnitList();
        foreach (var unitAgent in sourceUnits)
        {
            if (unitAgent.GetComponent<BlasterSystem>() == null)
                Debug.LogError($"Could not find BlasterSystem attached to unit {sourceUnits.IndexOf(unitAgent) + 1}");
            else
                unitAgent.GetComponent<BlasterSystem>().SetEncounterManager(encounter);

            if (unitAgent.GetComponent<MissileSystem>() == null)
                Debug.LogError($"Could not find MissileSystem attached to unit {sourceUnits.IndexOf(unitAgent) + 1}");
            else
                unitAgent.GetComponent<MissileSystem>().SetEncounterManager(encounter);

            if (unitAgent.GetComponent<ShieldSystem>() == null)
                Debug.LogError($"Could not find ShieldSystem attached to unit {sourceUnits.IndexOf(unitAgent) + 1}");
            else
                unitAgent.GetComponent<ShieldSystem>().SetEncounterManager(encounter);
        }

        agent.ResetResources();

        redat = agent.GetResourceObject().GetSubResources();
        if (redat.Keys.Count == 0)
        {
            Debug.LogError($"No Resources in Unit {agentKey.Last()} Agent");
            return false;
        }

        bool doDebug = false;
        agent.GetResourceObject().RefreshResources(doDebug);

        var resourceObject = agent.GetResourceObject();
        if (resourceObject == null)
        {
            Debug.LogError($"GetResourceObject() returned null for {agentKey}.");
            return false;
        }

        var recordField = resourceObject.GetRecordField("Encounter", ResourceTypes.Ammunition);
        if (recordField == null)
        {
            Debug.LogError($"GetRecordField() returned null for 'Encounter' and 'Ammunition'. Records:");
            Debug.LogError(DJson.Stringify(resourceObject.GetRecords()));
            return false;
        }

        return true;
    }


    public void ForEachAgentId(System.Action<string> action)
    {
        List<string> toRemove = new List<string>();
        
        foreach (var kvp in __agents)
        {
            if (kvp.Value == null || kvp.Value.gameObject == null)
            {
                toRemove.Add(kvp.Key);
            }
            else
            {
                action(kvp.Key);
            }
        }

        foreach (var key in toRemove)
        {
            __agents.Remove(key);
        }
    }

}
