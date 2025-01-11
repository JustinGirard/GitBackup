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

[System.Serializable]
public class ResourceEntry
{
    public string key;
    public float value;
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
public class GameEncounterBase : MonoBehaviour,IGameEncounter
{
    public class PrefabPath
    {
        public const string BoltPath = "AwayTeam/KeyObjects/Effects/BlasterBolt";
        public const string Explosion = "AwayTeam/KeyObjects/Effects/Explosion";
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
    [SerializeField]
    public string encounterSquadPrefab = "AwayTeam/KeyObjects/EncounterSquad"; // Path to SpaceMapUnit prefab in Resources

    [SerializeField]
    public List<SpaceEncounterObserverMapping> gui_observers;


    // WTF is this. WTF did I make here.
    public void RegisterNotificationWithAgents() 
    {

        /*
        foreach (Agent agent in __agents.Values)
        {
            agent.ClearObservers();
            //Debug.Log("Registering Observers");
            foreach (SpaceEncounterObserverMapping mapping in gui_observers) 
            {
                SpaceEncounterObserver observer = mapping.value;
                agent.AddObserver(observer);
            }
        }*/
        AgentManager agentMan = GetComponent<AgentManager>();
        if(agentMan == null)
            Debug.LogError("CRITICAL ERROR: no AgentManager attached");
        agentMan.ForEachAgent(agent => 
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

    private EncounterTimeKeeper __timeKeeper;

    public bool IsRunning(){
        return __timeKeeper.IsRunning();
    }
    public bool AmReady(){
        return __timeKeeper.AmReady();
    }

    public virtual void Awake()
    {
        intervalRunner = new IntervalRunner();
        __timeKeeper = GetComponent<EncounterTimeKeeper>();
        DoAwake();
    }
    public virtual void DoAwake() { throw new System.Exception($"IMPLEMENT ME {this.gameObject.name}:{this.name}");}
    
    
    private IntervalRunner intervalRunner;

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

        DoBegin(); 
        
        __timeKeeper.SetReadyToRun(true);
        Run();

    }
    protected virtual void DoBegin() { throw new System.Exception("IMPLEMENT ME");}

    public virtual void End() { 
        
        Pause();
        DoEnd(); 
        intervalRunner.ClearAllTimers();
        __timeKeeper.SetReadyToRun(false);

    }
    public virtual void DoEnd() { throw new System.Exception("IMPLEMENT ME");}

    public virtual void Run() 
    { 
        DoRun(); 
        __timeKeeper.Run();

    }
    protected virtual void DoRun() { throw new System.Exception("IMPLEMENT ME");}

    public virtual void Pause() 
    { 
        __timeKeeper.Pause();
        DoPause(); 
    }
    protected virtual void DoPause() { throw new System.Exception("IMPLEMENT ME");}

    public virtual void DoUpdate(float deltaTime) { DoInnerUpdate(deltaTime); }
    protected virtual void DoInnerUpdate(float deltaTime) { throw new System.Exception("IMPLEMENT ME");}

}

   /*
    public Vector3 GetAgentPosition(string agent_id)
    {
        if (!__agents.ContainsKey(agent_id))
        {
            Debug.LogError($"Agent ID '{agent_id}' does not exist in the agent dictionary.");
            return Vector3.zero;
        }

        var agent = __agents[agent_id];
        if (agent == null)
        {
            Debug.LogError($"Agent with ID '{agent_id}' is null.");
            return Vector3.zero;
        }

        var unit = agent.GetUnit();
        if (unit == null)
        {
            Debug.LogError($"Unit for agent '{agent_id}' is null.");
            return Vector3.zero;
        }

        var transform = unit.transform;
        if (transform == null)
        {
            Debug.LogError($"Transform for unit of agent '{agent_id}' is null.");
            return Vector3.zero;
        }

        return transform.position;
    }*/
