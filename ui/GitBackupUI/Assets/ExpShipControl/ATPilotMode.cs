using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VisualCommand;
[System.Serializable]
public class SpaceEncounterObserverMapping
{
    public string key;
    public SpaceEncounterObserver value;
}

[System.Serializable]
public  class AgentPrefab 
{
    public string agent_id;
    public Agent agentPrefab; 
}

public interface IATGameMode
{
    float GetTimerProgress();
    float GetTimerProgressMax();

    void RegisterNotificationWithAgents();
    void NotifyAllScreens(string effect);

    void Initalize();
    void Begin();
    void End();
    void Pause();
    void Run();
    void SetLevel(int lvl);
    int GetLevel();
    
    ATResourceData GetResourceObject(string agent_id);
    Agent GetPlayerAgent();
}



public class ATPilotMode : MonoBehaviour,IATGameMode,IPausable
{
    [SerializeField]
    public List<SpaceEncounterObserverMapping> gui_observers;
    bool __isRunning = false;
    //SurfaceNavigationCommand navigationCursor;
    [SerializeField]
    private string encounterUnitPrefab;
    public List<AgentPrefab> __initOnlyAgents = new List<AgentPrefab>();    
    public Dictionary<string,Agent> __agents = new Dictionary<string, Agent>();
    GameObject __unitGO;

    public bool IsRunning()
    {
        return __isRunning;
    }

    public void Run()
    {
        __isRunning = true;
    }
    public void Pause()
    {
        //ForEachAgent(agent => { agent.Pause(); });    
        //NotifyAllScreens(ObservableEffects.ShowPaused);        
        __isRunning = false;
        
    }    
    public float GetTimerProgress()
    {
        return 0.0f;
    }
    public float GetTimerProgressMax()
    {
        return 0.0f;
    }

    public void RegisterNotificationWithAgents() 
    {

        foreach (Agent agent in __agents.Values)
        {
            agent.ClearObservers();
            foreach (SpaceEncounterObserverMapping mapping in gui_observers) 
            {
                SpaceEncounterObserver observer = mapping.value;
                agent.AddObserver(observer);
            }
        }

    }        
    public void NotifyAllScreens(string effect) 
    {
        foreach (SpaceEncounterObserverMapping mapping in gui_observers) {
            SpaceEncounterObserver observer = mapping.value;
            if (observer != null) {
                observer.VisualizeEffect(effect,this.gameObject);
            }
        }
    }

    // Start is called before the first frame update
    public void Initalize()
    {
        if (__unitGO != null)
            GameObject.Destroy(__unitGO);
        //__unitGO = Instantiate( Resources.Load<GameObject>(encounterUnitPrefab));
        //SpaceMapUnitAgent unit = __unitGO.GetComponent<SpaceMapUnitAgent>();
        
        /*
        if (unit.GetComponent<BlasterSystem>() == null)
            Debug.LogError($"Could not find BlasterSystem attached to unit {i + 1}");
        unit.GetComponent<BlasterSystem>().SetEncounterManager(this);

        if (unit.GetComponent<MissileSystem>() == null)
            Debug.LogError($"Could not find MissileSystem attached to unit {i + 1}");
        unit.GetComponent<MissileSystem>().SetEncounterManager(this);

        if (unit.GetComponent<ShieldSystem>() == null)
            Debug.LogError($"Could not find ShieldSystem attached to unit {i + 1}");
        unit.GetComponent<ShieldSystem>().SetEncounterManager(this);*/

    }
    public void Begin()
    {
        if (__unitGO != null)
            GameObject.Destroy(__unitGO);
        __unitGO = Instantiate( Resources.Load<GameObject>(encounterUnitPrefab));
        SpaceMapUnitAgent unit = __unitGO.GetComponentInChildren<SpaceMapUnitAgent>();

        //
        Agent agent = GetPlayerAgent();
        if (agent == null)
        {
            Debug.LogError("Could not find player GetPlayerAgent() !!");
        }
        if (unit == null)
        {
            Debug.LogError($"Could not find player unit!! on {__unitGO}");
        }
        unit.transform.parent = agent.transform;
        unit.transform.position = agent.transform.position;

    }
    public void End()
    {
        if (__unitGO != null)
            GameObject.Destroy(__unitGO);

    }

    public void SetLevel(int lvl)
    {}
    public int GetLevel()
    {
        return 0;
    }
    public ATResourceData GetResourceObject(string agent_id)
    {
        Agent agent = GetPlayerAgent();
        if (agent == null)
        {
            Debug.LogError("GetPlayerAgent is returning null");
            return null;
        } 
        return agent.GetResourceObject();
    }

    public Agent GetPlayerAgent()
    {
        foreach (AgentPrefab apre in __initOnlyAgents)
        {
            // Debug.Log($"adding agent {apre.agent_id}");
            __agents[apre.agent_id] = apre.agentPrefab;
        }        
        return __agents["agent_1"]; 
    }    


}
