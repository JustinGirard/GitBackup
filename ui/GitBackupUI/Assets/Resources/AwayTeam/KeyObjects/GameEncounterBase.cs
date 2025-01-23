using System.Collections;
using System.Collections.Generic;

using Unity.VisualScripting;
using UnityEngine;
using Cinemachine;

using System.Linq;
using UnityEditor;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEditor.MPE;
using System.IO;
using UnityEngine.SocialPlatforms;
using PhysicalModel;
using UnityEngine.PlayerLoop;
[System.Serializable]
public class ResourceEntry
{
    public string key;
    public float value;
}

public interface IATGameMode
{
    float GetTimerProgress();
    float GetTimerProgressMax();

    void AttachGUIOberversToAgents();
    void NotifyAllScreens(string effect);

    void Initalize();
    void Begin();
    void End();
    void Pause();
    void Run();

    bool AmReady();
    bool IsRunning();
    void SetLevel(int lvl);
    int GetLevel();
    void DoUpdate(float deltaTime);
    
    ATResourceData GetResourceObject(string agent_id);
    Agent GetPlayerAgent();

}


public class SystemConstants
{
    public  static Dictionary<string, string> GetAccountFieldMapping ()
    {
        return new Dictionary<string, string>
        {
            { "status-value-1-1", ResourceTypes.Hull },
            { "status-value-1-2", ResourceTypes.Currency},
            { "status-value-2-1", ResourceTypes.Ammunition},
            { "status-value-2-2", ResourceTypes.Fuel}
        };
    }
    public static  Dictionary<string, string> GetAgentGUIFieldMapping (string agent_id)
    {
        return new Dictionary<string, string>
        {
            { "status-value-1-1",ResourceTypes.Ammunition },
            { "status-value-1-2", ResourceTypes.Fuel},
            { "status-value-2-1", ResourceTypes.Hull },
            { "status-value-2-2", ResourceTypes.Missiles },
            // { "status-value-3-1", "AttackPower" },
            // { "status-value-3-2", "MissilePower" },
            // { "status-value-4-1", "ShieldPower" },
        };
    }

   public static   Dictionary<string, string> ResourceIdToIconMapping (string agent_id)
    {
        return new Dictionary<string, string>
        {
            { ResourceTypes.Ammunition,"A" },
            { ResourceTypes.Fuel,"F" },
            { ResourceTypes.Hull,"H" },
            { ResourceTypes.Missiles,"M" },
        };
    }

    
    public static  string GetAgentGUICardId (string agent_id)
    {
        var d = new Dictionary<string, string>
        {
            { "account", "account-card-status" },
            { "agent_1", "agent-1-card-status" },
            { "agent_2", "agent-2-card-status" }
        };
        return d[agent_id];
    }

}

public class GameEncounterBase : MonoBehaviour,IATGameMode
{

    private PhysicalModel.Graph __physicalGraph;
    public class PrefabPath
    {
        public const string BoltPath = "AwayTeam/KeyObjects/Effects/BlasterBolt";
        public const string ExplosionRed = "AwayTeam/KeyObjects/Effects/ExplosionRed";
        public const string ExplosionBlue = "AwayTeam/KeyObjects/Effects/ExplosionBlue";
        public const string Missile = "AwayTeam/KeyObjects/Effects/Missile";
        public const string UnitShield = "AwayTeam/KeyObjects/Effects/UnitShield";
    }

    public class ObservableEffects {
        public static readonly List<string> all = new List<string> {EncounterOverLost, EncounterOverWon,ShowPaused, 
                                                                    ShowUnpaused, AttackOn, MissileOn, ShieldOn, AttackOff, 
                                                                    MissileOff, ShieldOff,TimerTick };
        public const string EncounterOverLost = "EncounterOverLost";
        public const string EncounterOverWon = "EncounterOverWon";
        public const string ShowPaused = "PauseEnable";
        public const string TimerTick = "TimerTick";
        public const string ShowUnpaused = "PauseDisable";
        public const string AttackOn = "AttackOn";
        public const string MissileOn = "MissileOn";
        public const string ShieldOn = "ShieldOn";
        public const string AttackOff = "AttackOff";
        public const string MissileOff = "MissileOff";
        public const string ShieldOff = "ShieldOff";
        public static bool IsValid(string action)
        {
            return all.Contains(action);
        }        
    }
    
    //[SerializeField]
    //public string encounterSquadPrefab = "AwayTeam/KeyObjects/EncounterSquad"; // Path to SpaceMapUnit prefab in Resources

    [SerializeField]
    public ATResourceData accountResourceData = null;

    [SerializeField]
    public List<SpaceEncounterObserverMapping> gui_observers = null;
    private IntervalRunner intervalRunner;
    private EncounterTimeKeeper __timeKeeper;
    
    public virtual void SetLevel(int lvl)
    {
        throw new System.Exception("NOT IMPLEMENTED");
    }

    public virtual int GetLevel()
    {
        throw new System.Exception("NOT IMPLEMENTED");
    }

    // The Encounter may have GUI observers. Make sure these 
    // Obervers are also attached to agents, so agents notify the observers of 
    // Game events.
    public void AttachGUIOberversToAgents() 
    {
        
        if(RefAgentManager == null)
            Debug.LogError("CRITICAL ERROR: no AgentManager attached");
        RefAgentManager.ForEachAgent(agent => 
        {
            agent.ClearObservers();
            foreach (SpaceEncounterObserverMapping mapping in gui_observers) 
            {
                SpaceEncounterObserver observer = mapping.value;
                agent.AddObserver(observer);
            }
        });    
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

    public void RunIfTime(string id, float delay, float deltaTime, System.Action action)
    {
        intervalRunner.RunIfTime( id,  delay,  deltaTime, action);
    }

    public static bool IsValidAgentAction(string actionId)
    {
        bool validAction = false;
        if (actionId == AgentActionType.Missile)
            validAction = true;
        if (actionId == AgentActionType.Shield)
            validAction = true;
        if (actionId == AgentActionType.Attack)
            validAction = true;        
        return validAction;
    }

    public bool IsRunning()
    {
        return __timeKeeper.IsRunning();
    }
    public bool AmReady()
    {
        return __timeKeeper.AmReady();
    }

    public virtual void Awake()
    {
        __physicalGraph = new PhysicalModel.Graph();
        intervalRunner = new IntervalRunner();
        __timeKeeper = GetComponent<EncounterTimeKeeper>();
        DoAwake();
    }

    public PhysicalModel.Graph GetPhysicalModel()
    {
        return __physicalGraph;
    }

    public virtual void DoAwake() { 

        throw new System.Exception($"IMPLEMENT ME {this.gameObject.name}:{this.name}");
    }
        

    public float GetTimerProgress()
    {
        return __timeKeeper.GetTimerProgress();
    }
    public float GetTimerProgressMax()
    {
        return __timeKeeper.GetTimerProgressMax();
    }

    public void SetTimerProgress(float number)
    {
        __timeKeeper.SetTimerProgress(number);
    }

    
    public void Initalize()
    {
        DoInitalize();
    } 
    protected virtual void DoInitalize() 
    { 
        throw new System.Exception("IMPLEMENT ME");
    }

    public virtual void Begin() 
    {     
        
        if (IsRunning())
            throw new System.Exception("Cant begin a new match, one is already initalized");
        if (AmReady() || IsRunning())
            throw new System.Exception("Cant begin a new match, one is already initalized");

        string uniqueString = $"{gameObject.name}_{gameObject.GetInstanceID()}";        
        __timeKeeper.RegisterEncounter(uniqueString, this);        
        __timeKeeper.InitLoop();
        intervalRunner.ClearAllTimers();
        __timeKeeper.SetTimerProgress(0);

        /////// OLD DoBegin
        AttachGUIOberversToAgents();
        DoBegin(onFinish:() => {
            // Notify all screens after agents have been initialized
            NotifyAllScreens(SpaceEncounterManager.ObservableEffects.ShieldOff);
            NotifyAllScreens(SpaceEncounterManager.ObservableEffects.AttackOff);
            NotifyAllScreens(SpaceEncounterManager.ObservableEffects.MissileOff);
            SetReadyToRun(true);
            //Debug.Log("FINISHED INIT. All Ships should be in place");
            //Debug.Break();
            Run();
        }); 
        


    }
    private void SetReadyToRun(bool ready)
    {
        __timeKeeper.SetReadyToRun(ready);
    }

    public AgentManager RefAgentManager
    {
        get { 
                return GetComponent<AgentManager>(); 
            }
        set {}
    }

    protected virtual void DoBegin(System.Action onFinish) { 
         throw new System.Exception("IMPLEMENT ME");
        }

    public virtual void End() { 
        
        Pause();
        DoEnd(); 
        __timeKeeper.SetTimerProgress(0);
        intervalRunner.ClearAllTimers();
        SetReadyToRun(false);

    }
    public virtual void DoEnd() 
    { 
        throw new System.Exception("IMPLEMENT ME");
    }

    public virtual void Run() 
    { 
        
        if (IsRunning())
            throw new System.Exception("Cant Run Twice");
        if (!AmReady())
            throw new System.Exception("Run() NOT READY TO RUN");
        DoRun(); 
        __timeKeeper.Run();
    }
    protected virtual void DoRun() 
    { 
        throw new System.Exception("IMPLEMENT ME");
    }

    public virtual void Pause() 
    { 
        __timeKeeper.Pause();
        // TODO ForEachAgent(agent => { agent.Pause(); });    
        NotifyAllScreens(ObservableEffects.ShowPaused);        
        DoPause(); 
    }

    /// **** ---------------------------------------------

    protected virtual void DoPause() 
    { 
        throw new System.Exception("IMPLEMENT ME");
    }

    public virtual void DoUpdate(float deltaTime) 
    {
        if (!IsRunning())
            throw new System.Exception("Cant DoUpdate-- Not Running().");
        if (!AmReady())
            throw new System.Exception("Am not ready, so can't update");
        
        float epochLength = 5.0f;
        RunIfTime("showActionProgress", 0.25f, deltaTime, () =>
        {
            float timerProgress = GetTimerProgress();
            float timerProgressMax = GetTimerProgressMax();
            // Debug.Log($"Setting TImer Progress { timerProgress + (0.25f/epochLength)*timerProgressMax}/{timerProgressMax}");
            SetTimerProgress( timerProgress + (0.25f/epochLength)*timerProgressMax);
            //Debug.Log($"Getting: {timerProgress}");

            NotifyAllScreens(ObservableEffects.TimerTick);
        });
        DoInnerUpdate(deltaTime,epochLength); 
    
    }
    protected virtual void DoInnerUpdate(float deltaTime, float epochLength) 
    { 
        throw new System.Exception("IMPLEMENT ME");
    }

    /// <summary>
    ///  
    /// </summary>
    /// <param name="agentId"></param>
    /// <returns></returns>
    public ATResourceData GetResourceObject(string agentId)
    {
        if (agentId == "account")
            return accountResourceData;
        if (agentId == "agent_1")
            return RefAgentManager.GetAgent("agent_1").GetResourceObject();
        if (agentId == "agent_2")
            return RefAgentManager.GetAgent("agent_2").GetResourceObject();
        Debug.LogError("Could not find requested agent data");
        return null;
    }      
    public Agent GetPlayerAgent()
    {
        return RefAgentManager.GetPlayerAgent();
    }    
    /// **** ---------------------------------------------

    public void ForEachAgent(System.Action<Agent> action)
    {
        RefAgentManager.ForEachAgent(action);
    }

    public void ForEachAgentId(System.Action<string> action)
    {
        RefAgentManager.ForEachAgentId(action);
    }  


    public float GetResourceValue(string agentId, string resourceType)
    {
        if (!RefAgentManager.HasAgentKey(agentId))
        {
            Debug.LogError($"Agent with ID {agentId} not found.");
            return 0.0f;
        }

        var agent = RefAgentManager.GetAgent(agentId);
        if (agent == null)
        {
            Debug.LogError($"Agent with ID {agentId} is null.");
            return 0.0f;
        }

        var resourceObject = agent.GetResourceObject();
        if (resourceObject == null)
        {
            Debug.LogError($"Resource object for agent {agentId} is null.");
            return 0.0f;
        }

        var record = resourceObject.GetRecordField("Encounter", resourceType);
        if (record == null)
        {
            //Debug.LogError($"Record for resource {resourceType} not found for agent {agentId}.");
            return 0.0f;
        }

        return (float)record;
    }    

}
