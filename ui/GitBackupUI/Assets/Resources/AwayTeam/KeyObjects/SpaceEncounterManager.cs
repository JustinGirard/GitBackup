using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/*




public class SpaceEncounterManager : MonoBehaviour{
{
    [SerializeField]
    public List<ResourceEntry> defaultResources;
    public void Awake()
    {
        // ContainsKey
        DictTable resourceTable = new DictTable();
        SetRecords((DictTable)resourceTable);
        foreach (ResourceEntry resource in defaultResources)
        {
            Debug.Log($"Adding Resource {resource.key}");
            AddToRecordField("Encounter", resource.key, resource.value, create: true);
        }      
    }

*/




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
    public class GUICommands {
        public static readonly List<string> all = new List<string> { Attack, Missile, Shield };
        public const string Attack = "Attack";
        public const string Missile = "Missile";
        public const string Shield = "Shield";
    }

    public class ObservableEffects {
        public static readonly List<string> all = new List<string> { AttackOn, MissileOn, ShieldOn, AttackOff, MissileOff, ShieldOff };
        public const string EncounterOver = "EncounterOver";
        public const string ShowPaused = "PauseEnable";
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


    private DockedButton attackButton;
    private DockedButton missileButton;
    private DockedButton shieldButton;

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
            { "status-value-3-1", "AttackPower" },
            { "status-value-3-2", "MissilePower" },
            { "status-value-4-1", "ShieldPower" },
        };
    }

    public Dictionary<string, float>  GetResourceDeltaForActions(string self_action, string other_action)
    {
        Dictionary<string, float> agentDelta = new Dictionary<string, float>();

        // Self Harm
        if (self_action == "blaster")
        {
            agentDelta["Ammunition"] = -1f;
        }
        if (self_action == "missile")
        {
            agentDelta["Missiles"] = -1f;
        }
        if (self_action == "shield")
        {
            agentDelta["Fuel"] = -1f;
        }

        // Reactions
        if (other_action == "blaster")
        {
            if (self_action != "shield")
            {
                agentDelta["Hull"] = -1f;
            }
        }
        if (other_action == "missile")
        {
            if (self_action != "blaster")
            {
                agentDelta["Hull"] = -2f;
            }
        }
        if (other_action == "shield")
        {
            //pass
        }        

        return agentDelta;
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
        //navigationManager.NavigateTo("AT_NewGame",false);
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
        Debug.Log("InitEncounterData is running");
        foreach (ResourceEntry resource in defaultResources)
        {
            if (resourceTypes.Contains(resource.key ))
            {
                accountResourceData.AddToRecordField("Encounter", resource.key, resource.value, create: true);
            }
            else
            {
                Debug.LogError($"-----------Could not find - {resource.key} in {DJson.Stringify(resourceTypes)}----------------------");
            }
        }     
        agent1UnitResourceData.ClearRecords();
        agent2UnitResourceData.ClearRecords();

    }    
    // Debug.Log("Agent 1 Resouce on Before");
    // Debug.Log((string)DJson.Stringify(agent1UnitResourceData.GetRecord("Encounter")));
    public void Begin()
    {
        float ammoAmount = 10f;
        accountResourceData.Withdraw("Ammunition",ammoAmount);
        agent1UnitResourceData.Deposit("Ammunition",ammoAmount); // Player Investment
        agent1UnitResourceData.Deposit("Hull",100); 
        agent1UnitResourceData.Deposit("Fuel",100); 

        agent2UnitResourceData.Deposit("Ammunition",ammoAmount);
        agent2UnitResourceData.Deposit("Hull",100); 
        agent2UnitResourceData.Deposit("Fuel",100); 
        
        // agent1UnitResourceData.Deposit("AttackPower",1); // Player Investment
        // agent1UnitResourceData.Deposit("ShieldPower",2); // AI Investment
        // agent1UnitResourceData.Deposit("MissilePower",3); // AI Investment
        
        Debug.Log($"encounterSquadPrefab {encounterSquadPrefab}");
        unit1 = Instantiate( Resources.Load<GameObject>(encounterSquadPrefab));
        unit1.transform.position = spawnOne.transform.position;
        unit1.transform.rotation  = spawnOne.transform.rotation;
        unit1.transform.parent = spawnOne.transform;
        
        unit2 =  Instantiate(Resources.Load<GameObject>(encounterSquadPrefab));
        unit2.transform.position = spawnTwo.transform.position;
        unit2.transform.rotation  = spawnTwo.transform.rotation;
        unit2.transform.parent = spawnTwo.transform;
        Debug.Log("Should be runnning");
        Run();
        Debug.Log("Should be runnning 2");
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

    }
    bool __isRunning = false;
    public void Run()
    {
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
                NotifyAllScreens(ObservableEffects.EncounterOver);
                return;
            }

        });

        intervalRunner.RunIfTime("doActions", 3f, Time.deltaTime, () =>
        {
            string agent_1_action = "blaster";
            string agent_2_action = "blaster"; // TODO -- choose actions "better" -- replace with agent policy.

            // Dictionary<string, float>   agent1Delta = GetResourceDeltaForActions(agent_1_action, agent_2_action);
            // Dictionary<string, float>   agent2Delta = GetResourceDeltaForActions(agent_2_action, agent_1_action);
            Dictionary<string, float>   agent1Delta = new Dictionary<string, float> {
                {"Hull",-10},
                {"Fuel",-10}
            };
            Dictionary<string, float> agent2Delta = agent1Delta;
            //Dictionary<string, float>   agent2Delta = GetResourceDeltaForActions(agent_2_action, agent_1_action);
            
            foreach (var delta in agent1Delta)
            {
                agent1UnitResourceData.Deposit(delta.Key, delta.Value);
            }

            foreach (var delta in agent2Delta)
            {
                agent2UnitResourceData.Deposit(delta.Key, delta.Value);
            }

        });        
        

    }
    public System.Collections.IEnumerator  ProcessGUIAction(string commandId)
    {
        if (commandId== GUICommands.Attack)
        {
            NotifyAllScreens(ObservableEffects.AttackOn);
            yield return new WaitForSeconds(0.5f);    
            NotifyAllScreens(ObservableEffects.AttackOff);
            // Debug.Log($"PrefabPath.BoltPath {PrefabPath.BoltPath}");
            //GameObject bolt = Instantiate( Resources.Load<GameObject>(PrefabPath.BoltPath));
            //  ShootBlasterAt(GameObject boltPrefab, int number, float duration, float delay, float maxDistance, Vector3 source, Vector3 target)
            StartCoroutine(EffectHandler.ShootBlasterAt(
                                boltPrefab:Resources.Load<GameObject>(PrefabPath.BoltPath),
                                explosionPrefab:Resources.Load<GameObject>(PrefabPath.Explosion),
                                number:20,
                                delay:1f,
                                duration:1f,
                                maxDistance:3f,
                                source:unit1.transform.position,
                                target:unit2.transform.position
                                ));
            // Gizmos.color = Color.yellow;
            // Debug.DrawLine(unit1.transform.position, unit2.transform.position, Color.yellow, 5f);                                
        }
        if (commandId== GUICommands.Missile)
        {
            NotifyAllScreens(ObservableEffects.MissileOn);
            yield return new WaitForSeconds(0.5f);    
            NotifyAllScreens(ObservableEffects.MissileOff);
            // ShootMissileAt(GameObject missilePrefab, GameObject explosionPrefab, int number, float duration, float delay, float arcHeight, Vector3 source, Vector3 target);
            StartCoroutine(EffectHandler.ShootMissileAt(
                                missilePrefab:Resources.Load<GameObject>(PrefabPath.Missile),
                                explosionPrefab:Resources.Load<GameObject>(PrefabPath.Explosion),
                                number:20,
                                delay:1f,
                                duration:1f,
                                arcHeight:2f,
                                //maxDistance:3f,
                                source:unit1.transform.position,
                                target:unit2.transform.position
                                ));            
        }
        if (commandId== GUICommands.Shield)
        {
            NotifyAllScreens(ObservableEffects.ShieldOn);
            yield return new WaitForSeconds(0.5f);    
            NotifyAllScreens(ObservableEffects.ShieldOff);
        }
        yield break;
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