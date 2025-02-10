using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.VisualScripting;
using System.Linq;
public class AgentManager : MonoBehaviour
{
    public List<Agent> AgentsInRadius(float rad, Agent agent)
    {
        return __agents.Values
            .Where(a => a != agent) // Exclude yourself
            .Where(a => Vector3.Distance(a.transform.position, agent.transform.position) <= rad) // Check distance
            .OrderBy(a => Vector3.Distance(a.transform.position, GetPlayerAgent().transform.position)) // Order by distance to player
            .ToList();        
    }    
    
    private Dictionary<string,Agent> __agents = new Dictionary<string, Agent>();
    [SerializeField]
    private List<AgentPrefab> __initOnlyAgents = new List<AgentPrefab>();

    public Agent GetPlayerAgent()
    {
        foreach (var agent in __agents.Values)
        {
            if (agent is PlayerAgent)
            {
                return agent;
            }
        }
        return null; // Return null if no PlayerAgent is found
    }
    public void AddAgent(Agent addMe)
    {
        __agents[addMe.name] = addMe;
    }

    void Awake(){
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
        if(__agents.ContainsKey(agentId) == false)
        {
            Debug.Log($"Do not contain agent {agentId}");
            return null;
        }
        return __agents[agentId] ;
    }

    public bool HasAgentKey(string agentId)
    {
        return __agents.ContainsKey(agentId) ;
    }


    public IEnumerator ForEachAgentCo(System.Func<Agent, IEnumerator> coroutine)
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
                // Execute the provided coroutine with the current agent
                yield return coroutine(kvp.Value);
            }
        }

        foreach (var key in toRemove)
        {
            __agents.Remove(key);
        }
    }



    public void ForEachAgent(System.Action<Agent> action)
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
                action(kvp.Value);
            }
        }

        foreach (var key in toRemove)
        {
            __agents.Remove(key);
        }
    }

    //public string encounterSquadPrefab = "AwayTeam/KeyObjects/EncounterSquad"; // Path to SpaceMapUnit prefab in Resources
    [SerializeField]
    private GameObject encounterSquadPrefab; // Path to SpaceMapUnit prefab in Resources
    
    public System.Collections.IEnumerator RecreateAllUnitsCoRoutine(System.Action onFinish){

        foreach (var agentKVP in __agents)
        {
            yield return RecreateUnitCoRoutine( (Agent)agentKVP.Value,agentKVP.Key);
        }
        onFinish.Invoke();
        yield break;
    }
    
    
    public bool RecreateSingleUnit(Agent agent, string agentKey)
    {
        StartCoroutine(RecreateUnitCoRoutine(agent, agentKey));
        return true;
    }

    public System.Collections.IEnumerator RecreateUnitCoRoutine(Agent agent, string agentKey)
    {
        //GameObject unit = Instantiate(Resources.Load<GameObject>(encounterSquadPrefab));
        GameObject unit = Instantiate(encounterSquadPrefab);
        unit.name = $"{agent.gameObject.name}:Squad_{agentKey.Last()}";
        unit.transform.parent = agent.transform;
        yield return new WaitForEndOfFrame(); 
        yield return new WaitForFixedUpdate();

        unit.transform.position = agent.transform.position;
        unit.transform.rotation = agent.transform.rotation;
        yield return new WaitForEndOfFrame(); 
        yield return new WaitForFixedUpdate();
        
        if (unit.GetComponent<EncounterSquad>() == null)
        {
            Debug.LogError($"Unit{agentKey.Last()} could not create EncounterSquad");
            yield break;
        }
        unit.name = $"{agent.gameObject.name}(2):Squad_{agentKey.Last()}";

        yield  return unit.GetComponent<EncounterSquad>().Rebuild(); // IMPORTANT <-- Bulds and sets position
        yield return new WaitForEndOfFrame(); 
        yield return new WaitForFixedUpdate();

        agent.SetUnit(unit);
        yield return new WaitForEndOfFrame(); 
        yield return new WaitForFixedUpdate();
        unit.name = $"{agent.gameObject.name}(3):Squad_{agentKey.Last()}";

        Dictionary<string, ATResourceData> redat = agent.GetResourceObject().GetSubResources();
        if (redat.Keys.Count == 0)
        {
            Debug.LogError($"No Agents in Unit {agentKey.Last()} Agent");
            yield break;
        }
        unit.name = $"{agent.gameObject.name}(4):Squad_{agentKey.Last()}";

        // unit.GetComponent<EncounterSquad>().UpdatePosition();

        GameEncounterBase encounter = this.GetComponent<GameEncounterBase>();
        unit.name = $"{agent.gameObject.name}(5):Squad_{agentKey.Last()}";

        if (encounter == null)
            Debug.LogError("Critica: Could not find attached GameEncounterBase ");
        List<SimpleShipController> sourceUnits = unit.GetComponent<EncounterSquad>().GetUnitList();


        
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
            yield break;
        }

        bool doDebug = false;
        agent.GetResourceObject().RefreshResources(doDebug);

        var resourceObject = agent.GetResourceObject();
        if (resourceObject == null)
        {
            Debug.LogError($"GetResourceObject() returned null for {agentKey}.");
            yield break;
        }

        var recordField = resourceObject.GetRecordField("Encounter", ResourceTypes.Ammunition);
        if (recordField == null)
        {
            Debug.LogError($"GetRecordField() returned null for 'Encounter' and 'Ammunition'. Records:");
            Debug.LogError(DJson.Stringify(resourceObject.GetRecords()));
            yield break;
        }

        yield break;
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
