using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


using System.Linq;
using UnityEditor;
using System.Runtime.InteropServices.WindowsRuntime;


[System.Serializable]
public class ResourceEntry
{
    public string key;
    public float value;
}

[System.Serializable]
public class SpaceEncounterObserverMapping
{
    public string key;
    public SpaceEncounterObserver value;
}

[System.Serializable]
public  class AgentPrefab {
    public string agent_id;
    public Agent agentPrefab; 
}
public class SpaceEncounterManager : MonoBehaviour,IPausable
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
    public List<ResourceEntry> defaultResources;
    [SerializeField]
    public List<SpaceEncounterObserverMapping> gui_observers;
    public ATResourceData accountResourceData;
    //public ATResourceData agent1UnitResourceData;
    //public ATResourceData agent2UnitResourceData;
    public string encounterSquadPrefab = "AwayTeam/KeyObjects/EncounterSquad"; // Path to SpaceMapUnit prefab in Resources
    // Assets/Resources/AwayTeam/KeyObjects/EncounterSquad.prefab
    private IntervalRunner intervalRunner;
    /*
    private int __dataRevisionAccount = 653948;
    private int __dataRevisionAgent1 = 653947;
    private int __dataRevisionAgent2 = 653946;
    */

    public List<AgentPrefab> __initOnlyAgents = new List<AgentPrefab>();

    public Dictionary<string,Agent> __agents = new Dictionary<string, Agent>();

    /*
    private IDynamicControl attackButton;
    private IDynamicControl missileButton;
    private IDynamicControl shieldButton;

    private Dictionary<string,Dictionary<string,float>> agent1Transitions;
    private Dictionary<string,Dictionary<string,float>> agent2Transitions;
    */

    public Agent GetPlayerAgent()
    {
        return __agents["agent_1"]; 
    }

    public   Dictionary<string, string> GetAccountFieldMapping ()
    {
        return new Dictionary<string, string>
        {
            { "status-value-1-1", ResourceTypes.Hull },
            { "status-value-1-2", ResourceTypes.Currency},
            { "status-value-2-1", ResourceTypes.Ammunition},
            { "status-value-2-2", ResourceTypes.Fuel}
        };
    }
    public   Dictionary<string, string> GetAgentGUIFieldMapping (string agent_id)
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
    public static bool IsValidAgentAction(string actionId)
    {
        bool validAction = false;
        if (actionId == AgentActions.Missile)
            validAction = true;
        if (actionId == AgentActions.Shield)
            validAction = true;
        if (actionId == AgentActions.Attack)
            validAction = true;        
        return validAction;
    }
    public   Dictionary<string, string> ResourceIdToIconMapping (string agent_id)
    {
        return new Dictionary<string, string>
        {
            { ResourceTypes.Ammunition,"A" },
            { ResourceTypes.Fuel,"F" },
            { ResourceTypes.Hull,"H" },
            { ResourceTypes.Missiles,"M" },
        };
    }

    
    public   string GetAgentGUICardId (string agent_id)
    {
        var d = new Dictionary<string, string>
        {
            { "account", "account-card-status" },
            { "agent_1", "agent-1-card-status" },
            { "agent_2", "agent-2-card-status" }
        };
        return d[agent_id];
    }


    public void Initalize(){

       InitEncounterData();
    }
    void Awake()
    {
        intervalRunner = new IntervalRunner();
        if (__initOnlyAgents.Count <= 0)
        {
            Debug.LogError("Encounter MUST have agents attached. For now an agent_1 and agent_2");
            return;
        }        
        foreach (AgentPrefab apre in __initOnlyAgents)
        {
            Debug.Log($"adding agent {apre.agent_id}");
            __agents[apre.agent_id] = apre.agentPrefab;
        }

        if (accountResourceData == null)
        {
            Debug.LogError("accountResourceData is not assigned to SpaceEncounterManager.");
            return;
        }             
        if (!__agents.ContainsKey("agent_1") || __agents["agent_1"] == null)
        {
            Debug.LogError("agent_1 is not assigned to SpaceEncounterManager.");
            return;
        }               
        if (!__agents.ContainsKey("agent_2") || __agents["agent_2"] == null)
        {
            Debug.LogError("agent_2 is not assigned to SpaceEncounterManager.");
            return;
        } 

     

    }

    void InitEncounterData(){
        accountResourceData.ClearRecords();
        List<string> resourceTypes = ResourceTypes.all;
        //Debug.Log("InitEncounterData is running");
        foreach (ResourceEntry resource in defaultResources)
        {
            if (resourceTypes.Contains(resource.key ))
            {
                accountResourceData.AddToRecordField("Encounter", resource.key, resource.value, create: true);
            }
        }     
        __agents.Values.ToList().ForEach(agent =>
        {
           agent.ClearResourceRecords();
        });        
        //agent1UnitResourceData.ClearRecords();
        //agent2UnitResourceData.ClearRecords();

    }    
    int __currentLevel = 1;
    // Debug.Log("Agent 1 Resouce on Before");
    // Debug.Log((string)DJson.Stringify(agent1UnitResourceData.GetRecord("Encounter")));
    public int GetLevel()
    {
        
        return __currentLevel;

    } 
    public Dictionary<string,string> GetLevelData()
    {
        return new Dictionary<string, string> {
            {"level_id",$"{GetLevel().ToString()}"},
            {"title",$"ROUND {GetLevel().ToString()}"},
        }; 
    }
    public void SetLevel(int lvl)
    {
        if (lvl < 1)
            throw new System.Exception("Can set such a small level value");

        if (IsRunning())
            throw new System.Exception("Cant set the level if am running level");
        __currentLevel = lvl;

    }
    bool __isReady = false;
    public bool AmReady()
    {
        
        return __isReady;

    } 
    public void SetReadyToRun(bool rdy)
    {
        if (IsRunning())
            throw new System.Exception("Cant set the level if am running level");
        __isReady = rdy;

    }
    public void Begin()
    {
        
        if (IsRunning())
            throw new System.Exception("Cant begin a new match, one is already initalized");
        if (AmReady() || IsRunning())
            throw new System.Exception("Cant begin a new match, one is already initalized");
        RegisterNotificationWithAgents();


        float ammoAmount = 20f;
        Agent agent1 = __agents["agent_1"];
        Agent agent2 = __agents["agent_2"];

        agent1.GetResourceObject().Deposit(ResourceTypes.Ammunition,10); // Player Investment
        agent1.GetResourceObject().Deposit(ResourceTypes.Missiles,5); // Player Investment
        agent1.GetResourceObject().Deposit(ResourceTypes.Hull,30); 
        agent1.GetResourceObject().Deposit(ResourceTypes.Fuel,30); 

        agent2.GetResourceObject().Deposit(ResourceTypes.Ammunition,ammoAmount);
        agent2.GetResourceObject().Deposit(ResourceTypes.Hull,5*__currentLevel); 
        agent2.GetResourceObject().Deposit(ResourceTypes.Fuel,5*__currentLevel); 
        
        GameObject unit1 = null;
        GameObject unit2 = null;        
        unit1 = Instantiate( Resources.Load<GameObject>(encounterSquadPrefab));
        agent1.SetUnit(unit1);        
        unit1.transform.position = agent1.transform.position;
        unit1.transform.rotation  = agent1.transform.rotation;
        unit1.transform.parent = agent1.transform;

        if (unit1.GetComponent<BlasterSystem>() == null)
            Debug.LogError("Could not find BlasterSystem attached to unit 1");
        unit1.GetComponent<BlasterSystem>().SetEncounterManager(this);

        if (unit1.GetComponent<MissileSystem>() == null)
            Debug.LogError("Could not find MissileSystem attached to unit 1");
        unit1.GetComponent<MissileSystem>().SetEncounterManager(this);

        if (unit1.GetComponent<ShieldSystem>() == null)
            Debug.LogError("Could not find ShieldSystem attached to unit 1");
        unit1.GetComponent<ShieldSystem>().SetEncounterManager(this);


        /////
        unit2 =  Instantiate(Resources.Load<GameObject>(encounterSquadPrefab));
        agent2.GetComponent<Agent>().SetUnit(unit2);        
        unit2.transform.position = agent2.transform.position;
        unit2.transform.rotation  = agent2.transform.rotation;
        unit2.transform.parent = agent2.transform;
        
        if (unit2.GetComponent<BlasterSystem>() == null)
            Debug.LogError("Could not find BlasterSystem attached to unit 2");
        unit2.GetComponent<BlasterSystem>().SetEncounterManager(this);

        if (unit2.GetComponent<MissileSystem>() == null)
            Debug.LogError("Could not find MissileSystem attached to unit 2");
        unit2.GetComponent<MissileSystem>().SetEncounterManager(this);

       if (unit2.GetComponent<ShieldSystem>() == null)
            Debug.LogError("Could not find ShieldSystem attached to unit 1");
        unit2.GetComponent<ShieldSystem>().SetEncounterManager(this);        

        SetReadyToRun(true);
        NotifyAllScreens(SpaceEncounterManager.ObservableEffects.ShieldOff);        
        NotifyAllScreens(SpaceEncounterManager.ObservableEffects.AttackOff);        
        NotifyAllScreens(SpaceEncounterManager.ObservableEffects.MissileOff);        
        Run();
    }

    public void End()
    {
        Pause();
        string reasonConstant = "";
        foreach (string agent_id in new string [] {"agent_1","agent_2"})
        {
            __agents[agent_id].DestroyUnit(reasonConstant);
            __agents[agent_id].GetResourceObject().ClearRecords();
        } 
        SetReadyToRun(false);

    }
    bool __isRunning = false;
    private void ForEachAgent(System.Action<Agent> action)
    {
        foreach (var agent in __agents.Values)
        {
            action(agent);
        }
    }

    private void ForEachAgentId(System.Action<string> action)
    {
        foreach (var agentId in __agents.Keys)
        {
            action(agentId);
        }
    }    
    public void Run()
    {
        if (IsRunning())
            throw new System.Exception("Cant Run Twice");

        float hull = (float)__agents["agent_1"].GetResourceObject().GetRecordField("Encounter",ResourceTypes.Hull);
        float fuel = (float)__agents["agent_1"].GetResourceObject().GetRecordField("Encounter",ResourceTypes.Fuel);
        if (hull <= 0 || fuel <=0 )
        {
            Debug.LogError($"Cant run. Agent One is out of hull {hull.ToString()} or fuel {fuel.ToString()}");
            return;
        }       

        ForEachAgent(agent => { agent.Run(); });    
        NotifyAllScreens(ObservableEffects.ShowUnpaused);
        __isRunning = true;
    }
    public void Pause()
    {
        ForEachAgent(agent => { agent.Pause(); });    
        NotifyAllScreens(ObservableEffects.ShowPaused);        
        __isRunning = false;
        
    }
    public bool IsRunning()
    {
        return __isRunning;
    }
    bool __actionsRunning = false;
    void Update()
    {

        if (__isRunning == false)
        {
            return;
        }
        intervalRunner.RunIfTime("endEncounterCheck", 1f, Time.deltaTime, () =>
        {
            ForEachAgentId(agentId =>
            {
                float hull = (float)__agents[agentId].GetResourceObject().GetRecordField("Encounter", ResourceTypes.Hull);
                float fuel = (float)__agents[agentId].GetResourceObject().GetRecordField("Encounter", ResourceTypes.Fuel);

                if (hull <= 0 || fuel <= 0)
                {
                    End();
                    if (agentId == "agent_1")
                        NotifyAllScreens(ObservableEffects.EncounterOverLost);
                    if (agentId == "agent_2")
                        NotifyAllScreens(ObservableEffects.EncounterOverWon);
                    return;
                }
            });
        });

        intervalRunner.RunIfTime("showActionProgress", 0.25f, Time.deltaTime, () =>
        {
            __timerProgress = __timerProgress + (0.25f/3f)*__timerProgressMax;

            NotifyAllScreens(ObservableEffects.TimerTick);
        });

        intervalRunner.RunIfTime("doActions", 3f, Time.deltaTime, () =>
        {
            if (__actionsRunning==false)
            {
                __actionsRunning = true;
                StartActionInterval();
            }
        });   

        if (__actionsRunning==true) intervalRunner.RunIfTime("doActionsClear", 0.5f, Time.deltaTime, () =>
        {
            bool is_agent_running = false;
            ForEachAgent(agent =>{
                is_agent_running = is_agent_running || agent.IsRunning();
            });
            if (is_agent_running == false)
            {
                ForEachAgent(agent =>{ agent.ClearActions(); });
                is_agent_running = false;
                __actionsRunning = false;
                __timerProgress = 0.0f;                
            }                    
        });      
    }

    public void StartActionInterval(){
           // CHOOSE your actions
            //List<Agent> agents = new List<Agent> { __agents["agent_1"], __agents["agent_2"] };            
         
            // CHOOSE -
            ForEachAgent(agent => { 
                agent.ChooseTargetAction();
            });

            // TARGET -
            ForEachAgent(primaryAgent => 
            {
                ForEachAgent(targetAgent => 
                {           
                    if(primaryAgent.name == targetAgent.name) return;

                    string primaryAction = GetTargetAction(primaryAgent.GetAgentId());
                    if (primaryAction.Length > 0)
                        primaryAgent.AddAgentAction(primaryAction,targetAgent);
                });
            });

            // TRIGGER Actions
            ForEachAgent(primaryAgent => 
            {
                StartCoroutine(primaryAgent.RunActions());
            });

    }
    public float GetTimerProgress()
    {
        return __timerProgress;
    }
    public float GetTimerProgressMax()
    {
        return __timerProgressMax;
    }
    float __timerProgress = 0f;
    float __timerProgressMax = 100f;

    
    public string GetTargetAction(string agent_id){

        if (__agents.ContainsKey(agent_id))
            return __agents[agent_id].GetTargetAction();
        return null;
    }


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
    }


    public void RegisterNotificationWithAgents() 
    {

        foreach (Agent agent in __agents.Values)
        {
            agent.ClearObservers();
            Debug.Log("Registering Observers");
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

    public ATResourceData GetResourceObject(string agentId)
    {
        if (agentId == "account")
            return accountResourceData;
        if (agentId == "agent_1")
            return __agents["agent_1"].GetResourceObject();
        if (agentId == "agent_2")
            return __agents["agent_2"].GetResourceObject();
        Debug.LogError("Could not find requested agent data");
        return null;
    }

}