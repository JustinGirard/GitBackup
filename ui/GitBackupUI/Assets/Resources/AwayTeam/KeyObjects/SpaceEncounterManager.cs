using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


using System.Linq;
using UnityEditor;


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

public abstract class SpaceEncounterObserver : MonoBehaviour {
    public abstract bool VisualizeEffect(string effect);
}
interface IPausable
{
    public void Run();
    public void Pause();
    public bool IsRunning();
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
    public class AgentActions {
        public static readonly List<string> all = new List<string> { Attack, Missile, Shield };
        public const string Attack = "Attack";
        public const string Missile = "Missile";
        public const string Shield = "Shield";
    }

    public class ObservableEffects {
        public static readonly List<string> all = new List<string> { AttackOn, MissileOn, ShieldOn, AttackOff, MissileOff, ShieldOff,TimerTick };
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
    }


    [SerializeField]
    public List<ResourceEntry> defaultResources;
    [SerializeField]
    public List<SpaceEncounterObserverMapping> gui_observers;
    public ATResourceData accountResourceData;
    public ATResourceData agent1UnitResourceData;
    public ATResourceData agent2UnitResourceData;
    public string encounterSquadPrefab = "AwayTeam/KeyObjects/EncounterSquad"; // Path to SpaceMapUnit prefab in Resources
    // Assets/Resources/AwayTeam/KeyObjects/EncounterSquad.prefab
    private IntervalRunner intervalRunner;
    private int __dataRevisionAccount = 653948;
    private int __dataRevisionAgent1 = 653947;
    private int __dataRevisionAgent2 = 653946;
    public GameObject spawnOne;
    public GameObject spawnTwo;


    private IDynamicControl attackButton;
    private IDynamicControl missileButton;
    private IDynamicControl shieldButton;

/// <summary>
///  Actions are blaster, missile, shiled, none
/// </summary>
    private Dictionary<string,Dictionary<string,float>> agent1Transitions;
    private Dictionary<string,Dictionary<string,float>> agent2Transitions;

    public  List<string> GetResourceTypes ()
    {
        return new List<string> { "Food","Power","Clones","Parts","Currency","Pods","Soldiers","Missles","Hull","Fuel","Ammunition","AttackPower","MissilePower","ShieldPower"};


    }
    public   Dictionary<string, string> GetAccountFieldMapping ()
    {
        return new Dictionary<string, string>
        {
            { "status-value-1-1", "Hull" },
            { "status-value-1-2", "Currency" },
            { "status-value-2-1", "Ammunition" },
            { "status-value-2-2", "Fuel" }
        };
    }
    public   Dictionary<string, string> GetAgentGUIFieldMapping (string agent_id)
    {
        return new Dictionary<string, string>
        {
            { "status-value-1-1", "Ammunition" },
            { "status-value-1-2", "Fuel" },
            { "status-value-2-1", "Hull" },
            { "status-value-2-2", "Missiles" },
            // { "status-value-3-1", "AttackPower" },
            // { "status-value-3-2", "MissilePower" },
            // { "status-value-4-1", "ShieldPower" },
        };
    }
    public   Dictionary<string, string> ResourceIdToIconMapping (string agent_id)
    {
        return new Dictionary<string, string>
        {
            { "Ammunition","A" },
            { "Fuel","F" },
            { "Hull","H" },
            { "Missiles","M" },
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

        if (accountResourceData == null)
        {
            Debug.LogError("accountResourceData is not assigned to SpaceEncounterManager.");
            return;
        }        
        if (agent1UnitResourceData == null)
        {
            Debug.LogError("agent1UnitResourceData is not assigned to SpaceEncounterManager.");
            return;
        }        
        if (agent2UnitResourceData == null)
        {
            Debug.LogError("agent2UnitResourceData is not assigned to SpaceEncounterManager.");
            return;
        }        
        if (spawnOne == null)
        {
            Debug.LogError("spawnOne is not assigned to SpaceEncounterManager.");
            return;
        }               
        if (spawnTwo == null)
        {
            Debug.LogError("spawnOne is not assigned to SpaceEncounterManager.");
            return;
        } 

     

    }
    private GameObject unit1 = null;
    private GameObject unit2 = null;
    void InitEncounterData(){
        accountResourceData.ClearRecords();
        List<string> resourceTypes = GetResourceTypes();
        //Debug.Log("InitEncounterData is running");
        foreach (ResourceEntry resource in defaultResources)
        {
            if (resourceTypes.Contains(resource.key ))
            {
                accountResourceData.AddToRecordField("Encounter", resource.key, resource.value, create: true);
            }
            //else
            //{
            //    Debug.LogError($"-----------Could not find - {resource.key} in {DJson.Stringify(resourceTypes)}----------------------");
            //}
        }     
        agent1UnitResourceData.ClearRecords();
        agent2UnitResourceData.ClearRecords();

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

        float ammoAmount = 20f;
        //accountResourceData.Withdraw("Ammunition",ammoAmount);
        agent1UnitResourceData.Deposit("Ammunition",10); // Player Investment
        agent1UnitResourceData.Deposit("Missle",5); // Player Investment
        agent1UnitResourceData.Deposit("Hull",30); 
        agent1UnitResourceData.Deposit("Fuel",30); 

        agent2UnitResourceData.Deposit("Ammunition",ammoAmount);
        agent2UnitResourceData.Deposit("Hull",5*__currentLevel); 
        agent2UnitResourceData.Deposit("Fuel",5*__currentLevel); 
        
        unit1 = Instantiate( Resources.Load<GameObject>(encounterSquadPrefab));
        unit1.transform.position = spawnOne.transform.position;
        unit1.transform.rotation  = spawnOne.transform.rotation;
        unit1.transform.parent = spawnOne.transform;

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
        unit2.transform.position = spawnTwo.transform.position;
        unit2.transform.rotation  = spawnTwo.transform.rotation;
        unit2.transform.parent = spawnTwo.transform;
        
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
        //Debug.Log("Should be runnning");
        Run();
        //Debug.Log("Should be runnning 2");

    }
    public void End()
    {
        Pause();
        if (unit1 != null)
            GameObject.Destroy(unit1);
        if (unit2 != null)
            GameObject.Destroy(unit2);
        agent1UnitResourceData.ClearRecords();
        agent2UnitResourceData.ClearRecords();
        SetReadyToRun(false);

    }
    bool __isRunning = false;
    public void Run()
    {
        if (IsRunning())
            throw new System.Exception("Cant Run Twice");

        if (agent1UnitResourceData == null)
        {
            Debug.LogError("No Agents to run");
        }
        float hull = (float)agent1UnitResourceData.GetRecordField("Encounter","Hull");
        float fuel = (float)agent1UnitResourceData.GetRecordField("Encounter","Fuel");
        if (hull <= 0 || fuel <=0 )
        {
            Debug.LogError($"Cant run. Agent One is out of hull {hull.ToString()} or fuel {fuel.ToString()}");
            return;
        }        
        if (unit1 != null)
        {
            unit1.GetComponent<EncounterUnitController>().Run();
        }
        if (unit2 != null)
        {
            unit2.GetComponent<EncounterUnitController>().Run();
        }        
        NotifyAllScreens(ObservableEffects.ShowUnpaused);
        __isRunning = true;
    }
    public void Pause()
    {
        if (unit1 != null)
            unit1.GetComponent<EncounterUnitController>().Pause();
        if (unit2 != null)
            unit2.GetComponent<EncounterUnitController>().Pause();

        NotifyAllScreens(ObservableEffects.ShowPaused);        
        __isRunning = false;
        
    }
    public bool IsRunning()
    {
        return __isRunning;
    }

    void Update()
    {
         // PrefabPath
        // Instantiate( Resources.Load<GameObject>(PrefabPath.BoltPath));


        if (__isRunning == false)
        {
            return;
        }
        intervalRunner.RunIfTime("endEncounterCheck", 1f, Time.deltaTime, () =>
        {
            float hull = (float)agent1UnitResourceData.GetRecordField("Encounter","Hull");
            float fuel = (float)agent1UnitResourceData.GetRecordField("Encounter","Fuel");
            if (hull <= 0 || fuel <=0 )
            {
                End();
                NotifyAllScreens(ObservableEffects.EncounterOverLost);
                return;
            }
            hull = (float)agent2UnitResourceData.GetRecordField("Encounter","Hull");
            fuel = (float)agent2UnitResourceData.GetRecordField("Encounter","Fuel");
            if (hull <= 0 || fuel <=0 )
            {
                End();
                NotifyAllScreens(ObservableEffects.EncounterOverWon);
                return;
            }

        });
        intervalRunner.RunIfTime("showActionProgress", 0.25f, Time.deltaTime, () =>
        {
            __timerProgress = __timerProgress + (0.25f/3f)*__timerProgressMax;

            NotifyAllScreens(ObservableEffects.TimerTick);
        });

        intervalRunner.RunIfTime("doActions", 3f, Time.deltaTime, () =>
        {

            SetTargetAction("agent_2",AgentActions.Attack);
            int randomIndex = Random.Range(0, 3);
            if (randomIndex == 0)
            {
                SetTargetAction("agent_2", AgentActions.Attack);
            }
            else if (randomIndex == 1)
            {
                SetTargetAction("agent_2",AgentActions.Missile);
            }
            else if (randomIndex == 2)
            {
                SetTargetAction("agent_2", AgentActions.Shield);
            }            
            StartCoroutine(ProcessAgentActions(
                agent_1_id:"agent_1", 
                agent_1_commandId:GetTargetAction("agent_1"),                
                agent_2_id:"agent_2", 
                agent_2_commandId:GetTargetAction("agent_2")
                ));
            __targetAgent1Action = "";
            __timerProgress = 0;
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

    string __targetAgent1Action = "";
    string __targetAgent2Action = "";
    public void SetTargetAction(string agent_id,string commandId){

        
        string targEffect = "";
        if (commandId== AgentActions.Attack)
            targEffect= ObservableEffects.AttackOn;
        if (commandId== AgentActions.Missile)
            targEffect= ObservableEffects.MissileOn;
        if (commandId== AgentActions.Shield)
            targEffect= ObservableEffects.ShieldOn;

        if(agent_id == "agent_1")
        {
            if(targEffect.Length > 0)
                NotifyAllScreens(targEffect);
            __targetAgent1Action = commandId;
        }
        if(agent_id == "agent_2")
        {
            __targetAgent2Action = commandId;
        }
    }
    public string GetTargetAction(string agent_id){
        if(agent_id == "agent_1")
        {
            return __targetAgent1Action;
        }
        if(agent_id == "agent_2")
        {
            return __targetAgent2Action;
        }
        return null;
    }

    public static Dictionary<string, float> AddDeltas(Dictionary<string, float> dict1, Dictionary<string, float> dict2)
    {
        return dict1.Keys.Union(dict2.Keys)
                    .ToDictionary(key => key, key => dict1.GetValueOrDefault(key) + dict2.GetValueOrDefault(key));
    }

    public static Dictionary<string, float> SubtractDeltas(Dictionary<string, float> dict1, Dictionary<string, float> dict2)
    {
        return dict1.Keys.Union(dict2.Keys)
                    .ToDictionary(key => key, key => dict1.GetValueOrDefault(key) - dict2.GetValueOrDefault(key));
    }

    public static Dictionary<string, float> MultiplyDeltas(Dictionary<string, float> dict1, Dictionary<string, float> dict2)
    {
        return dict1.Keys.Union(dict2.Keys)
                    .ToDictionary(key => key, key => dict1.GetValueOrDefault(key) * dict2.GetValueOrDefault(key));
    }

    public static Dictionary<string, float> DivideDeltas(Dictionary<string, float> dict1, Dictionary<string, float> dict2)
    {
        return dict1.Keys.Union(dict2.Keys)
                    .ToDictionary(key => key, key => dict1.GetValueOrDefault(key) / dict2.GetValueOrDefault(key));
    }

    private System.Collections.IEnumerator ProcessAgentActions(string agent_1_id, string agent_1_commandId, string agent_2_id, string agent_2_commandId)
    {
        var agentIds = new List<string> { agent_1_id, agent_2_id };
        var agentResources = new Dictionary<string, ATResourceData>
        {
            { agent_1_id, agent1UnitResourceData },
            { agent_2_id, agent2UnitResourceData }
        };
        var agentUnits = new Dictionary<string, GameObject>
        {
            { agent_1_id, unit1.gameObject },
            { agent_2_id, unit2.gameObject }
        };        
        var agentCommandIds = new Dictionary<string, string>
        {
            { agent_1_id, agent_1_commandId },
            { agent_2_id, agent_2_commandId }
        };
 
        foreach (var primaryAgentId in agentIds)
        {
            string primaryCommandId = agentCommandIds[primaryAgentId];
            //Dictionary<string, float> primaryDelta = new Dictionary<string, float>();
            //primaryDelta["Fuel"] = -1f;

            foreach (var targetAgentId in agentIds.Where(id => id != primaryAgentId)) // Secondary loop
            {
                string targetCommandId = agentCommandIds[targetAgentId];

                if (primaryCommandId == AgentActions.Attack)
                {
                    BlasterSystem bs = unit1.GetComponent<BlasterSystem>();
                    if (bs == null)
                        Debug.LogError("Could not find BlasterSystem on unit1");
                    StartCoroutine(bs.Execute(
                                sourceAgentId:primaryAgentId, 
                                sourcePowerId:AgentActions.Attack, 
                                targetAgentId:targetAgentId, 
                                targetPowerId:targetCommandId, 
                                agentResources:agentResources
                    ));
                }
                else if (primaryCommandId == AgentActions.Missile)
                {
                    //Debug.Log("MISSILE ACTION");
                    //yield break;
                    MissileSystem bs = unit1.GetComponent<MissileSystem>();
                    if (bs == null)
                    {
                        Debug.LogError("Could not find MissileSystem on unit1!!!");
                    }
                    else
                    {
                        Debug.Log(bs);
                        
                        StartCoroutine(bs.Execute(
                                    sourceAgentId:primaryAgentId, 
                                    sourcePowerId:AgentActions.Missile, 
                                    targetAgentId:targetAgentId, 
                                    targetPowerId:targetCommandId, 
                                    agentResources:agentResources
                        ));
                    }

                }
                else if (primaryCommandId == AgentActions.Shield)
                {
                    ShieldSystem bs = unit1.GetComponent<ShieldSystem>();
                    if (bs == null)
                    {
                        Debug.LogError("Could not find MissileSystem on unit1!!!");
                    }
                    else
                    {
                        Debug.Log(bs);
                        StartCoroutine(bs.Execute(
                                    sourceAgentId:primaryAgentId, 
                                    sourcePowerId:AgentActions.Shield, 
                                    targetAgentId:targetAgentId, 
                                    targetPowerId:targetCommandId, 
                                    agentResources:agentResources
                        ));
                    }                    
                }


            }

        }

        yield break;
    }
    public Vector3 GetAgentPosition(string agent_id)
    {
        // Replace with actual logic to retrieve the agent's GameObject
        if (agent_id == "agent_1")
            return unit1.transform.position;
        if (agent_id == "agent_2")
            return unit2.transform.position;
        return new Vector3();
    }

    
    public void NotifyAllScreens(string effect) 
    {
        foreach (SpaceEncounterObserverMapping mapping in gui_observers) {
            SpaceEncounterObserver observer = mapping.value;
            if (observer != null) {
                observer.VisualizeEffect(effect);
            }
        }
    }

    


    public ATResourceData GetResourceObject(string agentId)
    {
        if (agentId == "account")
            return accountResourceData;
        if (agentId == "agent_1")
            return agent1UnitResourceData;
        if (agentId == "agent_2")
            return agent2UnitResourceData;
        Debug.LogError("Could not find requested agent data");
        return null;
    }

}