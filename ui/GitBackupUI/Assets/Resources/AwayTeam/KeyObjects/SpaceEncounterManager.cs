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




public class SpaceEncounterManager : MonoBehaviour,IPausable,IATGameMode
{
    [SerializeField]
    CinemachineVirtualCamera __agentOneCam;
    [SerializeField]
    CinemachineVirtualCamera __worldCam;


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
    public void CameraToAgent()
    {
        __agentOneCam.Priority = 20;
        __worldCam.Priority = 10;
    }

    public void CameraToWorld()
    {
        __worldCam.Priority = 20;
        __agentOneCam.Priority = 10;
    }
    public Agent GetPlayerAgent()
    {
        return __agents["agent_1"]; 
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
            //Debug.Log($"adding agent {apre.agent_id}");
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

        //CinemachineVirtualCamera __agenOneCam;
        //CinemachineVirtualCamera __worldCam;

        // Debug.Log("RUNNING BEGIN");
        Dictionary<string,ATResourceData> redat ;
        
        if (IsRunning())
            throw new System.Exception("Cant begin a new match, one is already initalized");
        if (AmReady() || IsRunning())
            throw new System.Exception("Cant begin a new match, one is already initalized");
        RegisterNotificationWithAgents();
        intervalRunner.ClearAllTimers();
        __timerProgress = 0;

        Agent agent1 = __agents["agent_1"];
        Agent agent2 = __agents["agent_2"];
        
        GameObject unit1 = null;
        GameObject unit2 = null;        
        unit1 = Instantiate( Resources.Load<GameObject>(encounterSquadPrefab));
        if ( unit1.GetComponent<EncounterSquad>() == null)
            Debug.LogError("Unit1 could not create EncounterSquad");
        unit1.name = "Squad1";
        unit1.GetComponent<EncounterSquad>().Rebuild();
        agent1.SetUnit(unit1);    
        redat = agent1.GetResourceObject().GetSubResources();
        if (redat.Keys.Count == 0)
        {
            Debug.LogError($"No Agents in Unit 2 Agent");
            return;
        }

        unit1.transform.position = agent1.transform.position;
        unit1.transform.rotation  = agent1.transform.rotation;
        //Debug.Log($"Have resoruces for agent 1 {agent1.GetResourceObject().GetSubResources().Keys.Count }");
        unit1.transform.parent = agent1.transform;
        unit1.GetComponent<EncounterSquad>().UpdatePosition(); 
        //Debug.Log($"Have resoruces for agent 1 {agent1.GetResourceObject().GetSubResources().Keys.Count }");
        // GetSubResources

        List<SpaceMapUnitAgent> unit1sourceUnits = unit1.GetComponent<EncounterSquad>().GetUnitList();

        for (int i = 0; i < unit1sourceUnits.Count; i++)
        {
            if (unit1sourceUnits[i].GetComponent<BlasterSystem>() == null)
                Debug.LogError($"Could not find BlasterSystem attached to unit {i + 1}");
            unit1sourceUnits[i].GetComponent<BlasterSystem>().SetEncounterManager(this);

            if (unit1sourceUnits[i].GetComponent<MissileSystem>() == null)
                Debug.LogError($"Could not find MissileSystem attached to unit {i + 1}");
            unit1sourceUnits[i].GetComponent<MissileSystem>().SetEncounterManager(this);

            if (unit1sourceUnits[i].GetComponent<ShieldSystem>() == null)
                Debug.LogError($"Could not find ShieldSystem attached to unit {i + 1}");
            unit1sourceUnits[i].GetComponent<ShieldSystem>().SetEncounterManager(this);
        }
        /////
        unit2 =  Instantiate(Resources.Load<GameObject>(encounterSquadPrefab));
        if ( unit2.GetComponent<EncounterSquad>() == null)
            Debug.LogError("Unit2 could not create EncounterSquad");

        unit2.name = "Squad2";
        unit2.GetComponent<EncounterSquad>().Rebuild();
        agent2.GetComponent<Agent>().SetUnit(unit2);        
        redat = agent2.GetResourceObject().GetSubResources();
        if (redat.Keys.Count == 0)
        {
            Debug.LogError($"No Agents in Unit 2 Agent");
            return;
        }

        unit2.transform.position = agent2.transform.position  + new Vector3(0,2,0);
        unit2.transform.rotation  = agent2.transform.rotation;
        unit2.transform.parent = agent2.transform;
        unit2.GetComponent<EncounterSquad>().UpdatePosition(); 
        
        List<SpaceMapUnitAgent> unit2sourceUnits = unit2.GetComponent<EncounterSquad>().GetUnitList();
        for (int i = 0; i < unit2sourceUnits.Count; i++)
        {
            if (unit2sourceUnits[i].GetComponent<BlasterSystem>() == null)
                Debug.LogError($"Could not find BlasterSystem attached to unit {i + 1}");
            unit2sourceUnits[i].GetComponent<BlasterSystem>().SetEncounterManager(this);

            if (unit2sourceUnits[i].GetComponent<MissileSystem>() == null)
                Debug.LogError($"Could not find MissileSystem attached to unit {i + 1}");
            unit2sourceUnits[i].GetComponent<MissileSystem>().SetEncounterManager(this);

            if (unit2sourceUnits[i].GetComponent<ShieldSystem>() == null)
                Debug.LogError($"Could not find ShieldSystem attached to unit {i + 1}");
            unit2sourceUnits[i].GetComponent<ShieldSystem>().SetEncounterManager(this);
        }    
        //Directory<string,string> agentOne = new Directory<string,string >();
        //Directory<string,string> agentTeo = new Directory<string,string >();
        //System.Collections.Generic.Directory travis;

        /*
        float ammoAmount = 20f;
        Dictionary<string,float> agentOneResources = new Dictionary<string,float >{
                {ResourceTypes.Ammunition,100f},
                {ResourceTypes.Missiles,50},
                {ResourceTypes.Hull,300},
                {ResourceTypes.Fuel,300}
            };
        Dictionary<string,float> agentTwoResources = new Dictionary<string,float >{
                {ResourceTypes.Ammunition,ammoAmount*__currentLevel},
                {ResourceTypes.Missiles,50},
                {ResourceTypes.Hull,250*__currentLevel},
                {ResourceTypes.Fuel,250*__currentLevel}
            };

        // Resource Add
        //Debug.Log("Adding Resources");
        agent1.GetResourceObject().Unlock();
        agent1.GetResourceObject().Deposit(ResourceTypes.Ammunition,100); // Player Investment
        agent1.GetResourceObject().Deposit(ResourceTypes.Missiles,50); // Player Investment
        agent1.GetResourceObject().Deposit(ResourceTypes.Hull,300); 
        agent1.GetResourceObject().Deposit(ResourceTypes.Fuel,300); 
        agent1.GetResourceObject().Lock();

        agent2.GetResourceObject().Unlock();
        agent2.GetResourceObject().Deposit(ResourceTypes.Ammunition,ammoAmount*__currentLevel);
        agent2.GetResourceObject().Deposit(ResourceTypes.Missiles,50);
        agent2.GetResourceObject().Deposit(ResourceTypes.Hull,250*__currentLevel); 
        agent2.GetResourceObject().Deposit(ResourceTypes.Fuel,250*__currentLevel); 
        agent2.GetResourceObject().Lock();*/
        /*
        float ammoAmount = 20f;
        var agentResourceTemplates = new List<Dictionary<string, float>>
        {
            new Dictionary<string, float>
            {
                { ResourceTypes.Ammunition, 100f },
                { ResourceTypes.Missiles, 50 },
                { ResourceTypes.Hull, 300 },
                { ResourceTypes.Fuel, 300 }
            },
            new Dictionary<string, float>
            {
                { ResourceTypes.Ammunition, ammoAmount * __currentLevel },
                { ResourceTypes.Missiles, 50 },
                { ResourceTypes.Hull, 250 * __currentLevel },
                { ResourceTypes.Fuel, 250 * __currentLevel }
            }
        };

        // Apply resources to agents
        var resources = agentResourceTemplates[0];
        agent1.GetResourceObject().Unlock();
        foreach (var resource in resources)
        {
            agent1.GetResourceObject().Deposit(resource.Key, resource.Value);
        }
        agent1.GetResourceObject().Lock();
        

        resources = agentResourceTemplates[1];
        agent2.GetResourceObject().Unlock();
        foreach (var resource in resources)
        {
            agent2.GetResourceObject().Deposit(resource.Key, resource.Value);
        }
        agent2.GetResourceObject().Lock();

        */
        //Debug.Log($"------------------");
        //Debug.Log($"SET UP RESOURCES------------------");
        //Debug.Log($"------------------");
        agent1.ResetResources();
        agent2.ResetResources();


        redat = agent1.GetResourceObject().GetSubResources();
        if (redat.Keys.Count == 0)
        {
            Debug.LogError($"No Resources in Unit 1 Agent");
            return;
        }

       // Debug.Log($"Have Sub resources: {redat.Keys.Count.ToString()}");
        bool doDebug = false;
        agent1.GetResourceObject().RefreshResources(doDebug);
        agent2.GetResourceObject().RefreshResources();
        redat = agent1.GetResourceObject().GetSubResources();
        //////
        var resourceObject = agent1.GetResourceObject();
        if (resourceObject == null)
        {
            Debug.LogError("GetResourceObject() returned null for agent1.");
            return;
        }

        var recordField = resourceObject.GetRecordField("Encounter", ResourceTypes.Ammunition);
        if (recordField == null)
        {
            Debug.LogError("GetRecordField() returned null for 'Encounter' and 'Ammunition'.Records:");
            Debug.LogError(DJson.Stringify(resourceObject.GetRecords()));
            return;
        }

        object val = recordField;
        
        //if((float)val > 9 && (float)val < 11)
        //    Debug.Log("");
        //else
        //{
        //    Debug.LogError("Could not verify the ammunition -- something is wrong with resources");
        //}
        //Debug.Log($"Inspecting Resources for Agent 1");
        //foreach(string key in redat.Keys)
        //{   
        //    //Debug.Log($"Have Agent 1 resource {key}");
        //    ATResourceData unitData = redat[key];
        //    var obj = unitData.GetRecords();  
        //    Debug.Log($"Values {DJson.Stringify(obj)}");
        //}
        //Debug.Log($"==================");
        //Debug.Log($"==================");


        SetReadyToRun(true);
        //CameraToAgent();
        NotifyAllScreens(SpaceEncounterManager.ObservableEffects.ShieldOff);        
        NotifyAllScreens(SpaceEncounterManager.ObservableEffects.AttackOff);        
        NotifyAllScreens(SpaceEncounterManager.ObservableEffects.MissileOff);
        // Debug.Log("ENDING BEGIN");
        Run();
    }

    public void End()
    {
        CameraToWorld();
        Pause();
        string reasonConstant = "";
        foreach (string agent_id in new string [] {"agent_1","agent_2"})
        {
            __agents[agent_id].DestroyUnit(reasonConstant);
            __agents[agent_id].GetResourceObject().ClearRecords();
        } 
        SetReadyToRun(false);
        intervalRunner.ClearAllTimers();
        __timerProgress = 0;

    }
    /*
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
    }*/
    bool __isRunning = false;
    private void ForEachAgent(System.Action<Agent> action)
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

    private void ForEachAgentId(System.Action<string> action)
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


    public void Run()
    {
        if (IsRunning())
            throw new System.Exception("Cant Run Twice");

        //float hull = (float)__agents["agent_1"].GetResourceObject().GetRecordField("Encounter",ResourceTypes.Hull);
        ///////////////
        if (!__agents.ContainsKey("agent_1"))
        {
            Debug.LogError("Agent 'agent_1' not found in __agents dictionary.");
            return;
        }

        var agent = __agents["agent_1"];
        if (agent == null)
        {
            Debug.LogError("Agent 'agent_1' is null.");
            return;
        }

        var resourceObject = agent.GetResourceObject();
        resourceObject.RefreshResources();
        //Debug.Log("RUN DEBUG");
        bool doDebug = false;
        __agents["agent_1"].GetResourceObject().RefreshResources(doDebug);
        __agents["agent_2"].GetResourceObject().RefreshResources();
        Dictionary<string,ATResourceData> redat = __agents["agent_1"].GetResourceObject().GetSubResources();

        // Debug.Log($"Inspecting Resources for Agent 1");
        foreach(string key in redat.Keys)
        {   
            //Debug.Log($"Have Agent 1 resource {key}");
            ATResourceData unitData = redat[key];
            var obj = unitData.GetRecords();  
            //Debug.Log($"Values {DJson.Stringify(obj)}");
        }        

        if (resourceObject == null)
        {
            Debug.LogError("Resource object for 'agent_1' is null.");
            return;
        }

        var hullObj = resourceObject.GetRecordField("Encounter", ResourceTypes.Hull);
        if (hullObj == null)
        {
            Debug.LogError("'Hull' record not found for 'agent_1' in Encounter.");
            return;
        }

        float hull;
        try
        {
            hull = (float)hullObj;
        }
        catch
        {
            Debug.LogError("Failed to cast Hull record to float for 'agent_1'.");
            return;
        }        
        ///////////////
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
    float GetResourceValue(string agentId, string resourceType)
    {
        if (!__agents.ContainsKey(agentId))
        {
            Debug.LogError($"Agent with ID {agentId} not found.");
            return 0.0f;
        }

        var agent = __agents[agentId];
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

                float hull = GetResourceValue(agentId, ResourceTypes.Hull);
                float fuel = GetResourceValue(agentId, ResourceTypes.Fuel);

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
        float epochLength = 5.0f;
        intervalRunner.RunIfTime("showActionProgress", 0.25f, Time.deltaTime, () =>
        {
            __timerProgress = __timerProgress + (0.25f/epochLength)*__timerProgressMax;

            NotifyAllScreens(ObservableEffects.TimerTick);
        });

        intervalRunner.RunIfTime("doActions", epochLength, Time.deltaTime, () =>
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
            // Debug.Log("(x)Starting Actions");
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


    public void RegisterNotificationWithAgents() 
    {

        foreach (Agent agent in __agents.Values)
        {
            agent.ClearObservers();
            //Debug.Log("Registering Observers");
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