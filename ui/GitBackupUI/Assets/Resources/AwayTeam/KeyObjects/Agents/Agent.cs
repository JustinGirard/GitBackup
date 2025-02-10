using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.CompilerServices;
using System;
using Cinemachine;
using VisualCommand;
using TMPro;
[System.Serializable]
public class AgentCommand
{
    public string commandType;
    public string sourceActionId;
    public GameObject subject;
    public string currentStatus;
    public class Status 
    {

        public static bool IsValid(string action)
        {
            return all.Contains(action);
        }               
        public static readonly List<string> all = new List<string> { Pending, Running, Canceled,Finished };
        public const string Pending = "Pending";
        public const string Running = "Running";
        public const string Canceled = "Canceled";
        public const string Finished = "Finished";
    }
    public class Type
    {
        public static bool IsValid(string action)
        {
            return all.Contains(action);
        }               
        public static readonly List<string> all = new List<string> { Navigation, Combat, Formation,AttackPattern };
        public const string Navigation = "Navigation";
        public const string Combat = "Combat";
        public const string Formation = "Formation";
        public const string AttackPattern = "AttackPattern";

    }


}

public class AgentPowerType {

    public static bool IsValid(string action)
    {
        return all.Contains(action);
    }               
    public static readonly List<string> all = new List<string> { Attack, Missile, Shield };
    public const string Attack = "Attack";
    public const string Missile = "Missile";
    public const string Shield = "Shield";
}

public class AgentNavigationType {

    public static bool IsValid(string action)
    {
        return all.Contains(action);
    }               
    public static readonly List<string> all = new List<string> { NavigateToOn,NavigateToOff, NavigateToOnce,NavigateAim };
    public const string NavigateToOn = "NavigateToOn";
    public const string NavigateToOff = "NavigateToOff";
    public const string NavigateToOnce = "NavigateToOnce";
    public const string NavigateAim = "NavigateAim";
    public const string DashTo = "DashTo";
    public const string Halt = "Halt";
}

public class AgentFormation {

    public static bool IsValid(string action)
    {
        return all.Contains(action);
    }               
    public static readonly List<string> all = new List<string> { formation_claw, formation_diamond,formation_line };
    public const string formation_claw = "formation_claw";
    public const string formation_diamond = "formation_diamond";
    public const string formation_line = "formation_line";
}

public class AgentTargetPattern {

    public static bool IsValid(string action)
    {
        return all.Contains(action);
    }               
    public static readonly List<string> all = new List<string> { attack_even, attack_flank,attack_focus,attack_forward };
    public const string attack_even = "attack_even";
    public const string attack_flank = "attack_flank";
    public const string attack_focus = "attack_focus";
    public const string attack_forward = "attack_forward";
}

public class PlayerNavigationMode {

    public static bool IsValid(string action)
    {
        return all.Contains(action);
    }               
    public static readonly List<string> all = new List<string> { TopDownTraditional, ZTargetingEnemy };
    public const string TopDownTraditional = "TopDownTraditional";
    public const string ZTargetingEnemy = "ZTargetingEnemy";
    
}

class UnitAction
{
    public GameObject sourceUnit;
    public List<GameObject> destinationUnits;
    public string sourceActionId;
}

public interface ISurfaceNavigationCommand{

    void SetVisualState(string state);
    string GetActiveState();
    public GameObject GetTarget();
    public GameObject GameObject();
    public void Show();
    public void Hide();


}

public class Agent:MonoBehaviour, IPausable
{
    private List<SpaceEncounterObserver> __observers = new List<SpaceEncounterObserver>();
    
    [SerializeField]
    private GameObject primaryAim; // Primary Squad Aim location
    [SerializeField]
    private GameObject primaryNavigation; // Primary Squad Navigation Target

    public GameObject GetPrimaryAim()
    {
        return       primaryAim;  
    } 
    public GameObject GetPrimaryNavigation()
    {
        return       primaryNavigation;  
    } 

    [SerializeField]
    private ATResourceData resources;
    private GameObject __unitGameObject = null;

    [SerializeField]
    private string __agentId = "";
    protected List<AgentCommand > __pendingCommands;
    
    [SerializeField]
    public List<AgentCommand> debugPendingCommands = new List<AgentCommand>(); // Inspector-safe list

    [System.Serializable]
    public class AgentResourceEntry
    {
        public string resource_type;
        public float amount;
    }

    public List<AgentResourceEntry> agentResourceEntries = new List<AgentResourceEntry>();
    private ISurfaceNavigationCommand __navCommand;
    GameEncounterBase spaceEncounter;
    public virtual bool AttachNavigationWaypoint(ISurfaceNavigationCommand cmd)
    {
        __navCommand = cmd;
        return true;
    }
    public virtual bool DetachNavigationWaypoint()
    {
        if(__navCommand != null && __navCommand.GetActiveState() == SurfaceNavigationCommand.SelectionState.active)
        {
            Debug.LogError("Forcing the nev cursor off");
            __navCommand.SetVisualState(SurfaceNavigationCommand.SelectionState.off);
        }
        __navCommand = null;
        return true;
    }
    public void SetEncounter(GameEncounterBase enc)
    {
        spaceEncounter = enc;
    }
    public GameEncounterBase GetEncounter()
    {
        return spaceEncounter;
    }

    public void ResetResources()
    {
        float ammoAmount = 20f;
        float __currentLevel = 1f;
        var defaultAgentOne = new Dictionary<string, float>
        {
            { ResourceTypes.Ammunition, 100f },
            { ResourceTypes.Missiles, 50 },
            { ResourceTypes.Hull, 300 },
            { ResourceTypes.Fuel, 300 }
        };

        var defaultAgentTwo = new Dictionary<string, float>
        {
            { ResourceTypes.Ammunition, ammoAmount * __currentLevel },
            { ResourceTypes.Missiles, 50 },
            { ResourceTypes.Hull, 250 * __currentLevel },
            { ResourceTypes.Fuel, 250 * __currentLevel }
        };

        var agentResources = new Dictionary<string, Dictionary<string, float>>
        {
            { "agent1", new Dictionary<string, float>() },
            { "agent2", new Dictionary<string, float>() }
        };

        resources.Unlock();
        foreach (var entry in agentResourceEntries)
        {
            float remainder = resources.Deposit(entry.resource_type,entry.amount);
        }

        resources.RefreshResources();

        if (resources.GetRecords().Count == 0 && this is PlayerAgent)
        {
            //Debug.Log($"NO RECORDS FOR AGENT 1: {GetAgentId() } , {resources.GetRecords().Count }");
            foreach (var entry in defaultAgentOne)
            {
                resources.Deposit(entry.Key,entry.Value);
            }
        }

        if (resources.GetRecords().Count == 0 && this is not PlayerAgent)
        {
            foreach (var entry in defaultAgentTwo)
            {
                resources.Deposit(entry.Key,entry.Value);
            }
        }
        resources.Lock();

    }




    public void ClearResourceRecords()
    {
        resources.ClearRecords();
    }
    private GameEncounterBase encounterBase;
    void Awake(){
        __pendingCommands = new List<AgentCommand >();
        intervalRunner = new IntervalRunner();
        if (__agentId=="")
            Debug.LogError("No Agent ID assigned for agent");
        encounterBase = GetComponentInParent<GameEncounterBase>();
        if (encounterBase == null)
            Debug.LogError("Could not find encounter to register self");
        encounterBase.AddAgent(this);
    }
    public string GetAgentId(){
        return __agentId;
    }
    
    //public GameObject GetUnit()
    //{
    //    return __unitGameObject;
    //}
    public void SetUnit(GameObject go)
    {
        __unitGameObject = go;
        EncounterSquad ec = go.GetComponent<EncounterSquad>();
        if(ec == null)
            Debug.LogError("");

        //resources.ClearSubResources();
        List<SimpleShipController> allUnits = ec.GetUnitList();
        if (allUnits.Count == 0)
        {
            Debug.LogError($"Could not find any units on {ec.gameObject.name}");
            Debug.Break();
            return;
        }


        //Debug.Log($"Adding Unit in Set Unit {unit.name}");
        foreach(SimpleShipController unit in allUnits)
        {
            //Debug.Log($"Adding Unit in Set Unit {unit.name}");
            ATResourceData unitResourceData = unit.GetComponentInChildren<ATResourceData>();
            if (unitResourceData == null)
            {
                Debug.LogError($"Could not find resource attached to {unit.name}");
            }
            else
            {
                //Debug.Log($"Adding Sub Resource in Set Unit {unitResourceData.name}");
                resources.AddSubResource(unit.name,unitResourceData);
            }
        }

        //foreach( SpaceEncounterObserver observer in __observers)
        //{
        //    //Debug.Log("Showing Indicator from Agent.cs");
        //    observer.ShowFloatingActivePowers(allUnits[0].gameObject);
        //}
        
    }
    public GameObject GetUnit()
    {
       return  __unitGameObject;
        
    }    
    public void DestroyUnits(string reasonCode)
    {
        if(__unitGameObject != null)
           GameObject.Destroy( __unitGameObject);        
    }    

    public void Run()
    {
        bool doDebug = false;
        GetResourceObject().RefreshResources(doDebug);
        ATResourceData dt = GetResourceObject();
        float fuel = dt.Balance(ResourceTypes.Fuel);
        float hull =  dt.Balance(ResourceTypes.Hull);
        if (hull <= 0 || fuel <=0 )
        {
            Debug.LogError($"Cant run. Agent One is out of hull {hull.ToString()} or fuel {fuel.ToString()}");
            return;
        }
        __unitGameObject.GetComponent<EncounterSquad>().Run();
    }

    public void Pause()
    {
        __unitGameObject.GetComponent<EncounterSquad>().Pause();
    }

    public ATResourceData GetResourceObject()
    {
        return resources;
    }

    public ATResourceData GetAccountResourceObject()
    {
        return resources;
    }

    /// <summary>
    /// AI Trigger 
    /// </summary>
    /// <exception cref="System.Exception"></exception>
    public virtual void ChooseCommand(string commandType)
    {
        throw new System.Exception("No Implemented choice");
    }
    
    public virtual string GetSelectedCommand(string commandType)
    {
        throw new System.Exception("No Implemented choice");
    }

    public virtual bool AddCommand(string commandType,string commandId, GameObject subject)
    {
        throw new System.Exception("No Implemented reset");
        //return false;
    }

    public void EnqueueAgentCommand(string commandType,string targetActionId,GameObject subject)
    {
        if (!AgentCommand.Type.IsValid(commandType))
        {
            Debug.LogError($"Could not detect valid command type for agent commandType:{commandType}, targetActionId:{targetActionId}");
            return;
        }

        if (targetActionId =="")
        {
            Debug.LogError("AddAgentAction missing valid action id");
            return;
        }
        //Debug.Log($"AddAgentAction({targetActionId},{targetAgent}({targetAgent.GetAgentId()}))");

        //Agent targetAgent = subject.GetComponent<Agent>();
        AgentCommand pr = new AgentCommand();
            pr.commandType = commandType;
            pr.sourceActionId = targetActionId;
            pr.subject = subject;
            pr.currentStatus = AgentCommand.Status.Pending;
        //  Debug.Log("Adding command??");
        StartCoroutine(AddCommandQueue(pr));
    }

    AgentCommand PeekCommandQueue()
    {
        AgentCommand agentAction = null;
        if (!Sema.TryAcquireLock($"RunningAgentActions{this.GetInstanceID()}"))
            return null;
        if(__pendingCommands.Count > 0)
            agentAction =__pendingCommands[0];
        Sema.ReleaseLock($"RunningAgentActions{this.GetInstanceID()}");    
        return     agentAction;
    }

    public System.Collections.IEnumerator AddCommandQueue(AgentCommand command)
    {
          
        float timeout = 1.0f; // 1-second timeout
        float elapsed = 0f;
        string lockKey = $"RunningAgentActions{this.GetInstanceID()}";

        while (!Sema.TryAcquireLock(lockKey))
        {
            if (elapsed >= timeout)
            {
                Debug.LogError($"[Agent] Failed to acquire lock for {lockKey} after {timeout} seconds.");
                yield break;
            }
            yield return null; // Wait for the next frame
            elapsed += Time.deltaTime;
        }

        try
        {
            __pendingCommands.Add(command);
        }
        finally
        {
            Sema.ReleaseLock(lockKey);
        }
    }
    public System.Collections.IEnumerator RemoveCommandQueue(AgentCommand command)
    {
        float timeout = 1.0f; // 1-second timeout
        float elapsed = 0f;
        string lockKey = $"RunningAgentActions{this.GetInstanceID()}";

        while (!Sema.TryAcquireLock(lockKey))
        {
            if (elapsed >= timeout)
            {
                Debug.LogError($"[Agent] Failed to acquire lock for {lockKey} after {timeout} seconds.");
                yield break;
            }
            yield return null; // Wait for the next frame
            elapsed += Time.deltaTime;
        }

        try
        {
            __pendingCommands.Remove(command);
        }
        finally
        {
            Sema.ReleaseLock(lockKey);
        }
    }
    int __targetIndex = 0;
    Agent targetAgent = null;
    public void ClearEnemyAgent()
    {
        __targetIndex = -1;
        targetAgent = null;
    }

    public void BlindToggleEnemyAgent()
    {
        List<Agent> agents =  encounterBase.GetAgentManager().AgentsInRadius(10000f,this);

        if (agents.Count == 0)
        {
            __targetIndex = -1;
            targetAgent = null;
            return;
        }

        __targetIndex = (__targetIndex + 1) % (agents.Count + 1);
        targetAgent = (__targetIndex >= agents.Count) ? null : agents[__targetIndex];        
    }
    public Agent GetPrimaryEnemyAgent()
    {
        // Attempt to find the GameObject
        GameObject agent2 = null;
        if(targetAgent != null)
             agent2 = targetAgent.gameObject;
        if (agent2 == null)
        {
            //Debug.Log($"Agent[{this.gameObject.name}].GetPrimaryEnemyAgent(): agent2 could not be found.");
            return null;
        }

        // Attempt to get the Agent component
        Agent agentComponent = agent2.GetComponent<Agent>();
        if (agentComponent == null)
        {
            Debug.LogError($"GameObject 'AgentTwo' does not have an 'Agent' component.");
            return null;
        }
        return agentComponent;
    }

    public GameObject GetPrimaryEnemyUnit()
    {
        Agent agentComponent = GetPrimaryEnemyAgent();
        if (agentComponent == null)
        {
            //Debug.Log($"Agent[{this.gameObject.name}].GetPrimaryEnemyUnit(): No Agent Found");
            return null;
        }

        // Attempt to get the unit from the Agent component
        GameObject unit = agentComponent.GetUnit();
        if (unit == null)
        {
             Debug.Log($"Agent[{this.gameObject.name}].GetPrimaryEnemyUnit(): No Unit Found");
            return null;
        }

        // If all checks pass, return the unit
        return unit;
    }
    /*
    public System.Collections.IEnumerator ActivateAction(string sourceActionId)
    {
        //Debug.Log($"ActivateAction({sourceActionId})");
        // Called when you want to cosmetically activate the chosen actions for the group. This typically looks like a weapon powering up, or a vehichle changing orientation
        EncounterSquad sourceSquad = GetUnit().GetComponent<EncounterSquad>();
        StandardSystem subsystem = null;
        List<SimpleShipController> sourceUnits = sourceSquad.GetUnitList();
        foreach (SimpleShipController sourceUnit in sourceUnits)
        {
            ATResourceData sourceResources = sourceUnit.GetComponent<ATResourceData>();            
            if (sourceActionId == AgentAttackType.Attack)
                subsystem = (StandardSystem)sourceUnit.GetComponent<BlasterSystem>();
            if (sourceActionId == AgentAttackType.Missile)
                subsystem = (StandardSystem)sourceUnit.GetComponent<MissileSystem>();
            if (sourceActionId == AgentAttackType.Shield)
                subsystem = (StandardSystem)sourceUnit.GetComponent<ShieldSystem>();
            CoroutineRunner.Instance.StartCoroutine(
                subsystem.Activate(sourceResources:sourceResources)
            );
        }
        yield break;
    }
    public System.Collections.IEnumerator DeactivateAction(string sourceActionId)
    {
        // Called when you want to activate the chosen actions for the group. This typically looks like a weapon powering up, or a vehichle changing orientation
        EncounterSquad sourceSquad = GetUnit().GetComponent<EncounterSquad>();
        StandardSystem subsystem = null;
        List<SimpleShipController> sourceUnits = sourceSquad.GetUnitList();
        foreach (SimpleShipController sourceUnit in sourceUnits)
        {
            ATResourceData sourceResources = sourceUnit.GetComponent<ATResourceData>();            
            if (sourceActionId == AgentAttackType.Attack)
                subsystem = (StandardSystem)sourceUnit.GetComponent<BlasterSystem>();
            if (sourceActionId == AgentAttackType.Missile)
                subsystem = (StandardSystem)sourceUnit.GetComponent<MissileSystem>();
            if (sourceActionId == AgentAttackType.Shield)
                subsystem = (StandardSystem)sourceUnit.GetComponent<ShieldSystem>();
            CoroutineRunner.Instance.StartCoroutine(
                subsystem.Deactivate(sourceResources:sourceResources)
            );
        }
        yield break;        
    }*/

    private IntervalRunner intervalRunner;
    public void RunIfTime(string id, float delay, float deltaTime, System.Action action)
    {
        intervalRunner.RunIfTime( id,  delay,  deltaTime, action);
    }

    public System.Collections.IEnumerator RunActions()
    {
        AgentCommand agentAction = PeekCommandQueue(); 
        while (agentAction != null)
        {
            agentAction.currentStatus = AgentCommand.Status.Running;
            if (agentAction.commandType == AgentCommand.Type.Navigation)
            {
                yield return CoroutineRunner.Instance.StartCoroutine(RunNavigation(agentAction));
            }
            if (agentAction.commandType == AgentCommand.Type.Combat)
            {
                //Agent destinationAgent = __opponentAgents[agentAction.destinationAgentId];
                yield return CoroutineRunner.Instance.StartCoroutine(RunCombatAction(agentAction));
            }
            if (agentAction.commandType == AgentCommand.Type.Formation)
            {
                string formation = this.GetSelectedCommand(AgentCommand.Type.Formation);
                EncounterSquad sourceSquad = GetUnit().GetComponent<EncounterSquad>();
                sourceSquad.SetFormation(formation);
                sourceSquad.SetGoalTarget(GetPrimaryAim().transform,Vector3.zero);
            }
            yield return null;
            agentAction = PeekCommandQueue(); 
        } 
        yield break;
    }
    bool __is_running = false;
    public bool IsRunning()
    {
        return __is_running;
    }
    /*
    public void ClearActions()
    {
        __pendingCommands.Clear();
        __opponentAgents.Clear();
        ResetSelectedAction(AgentCommand.Type.Combat);
        ResetSelectedAction(AgentCommand.Type.Formation);
        ResetSelectedAction(AgentCommandType.Navigation);
        ResetSelectedAction(AgentCommandType.AttackPattern);
        DetachNavigationWaypoint();
    }*/

    public System.Collections.IEnumerator RunNavigation(AgentCommand agentAction)
    {
        //
        // Find Units, and any relevant unit group for this command
        //
        string sourceActionId = agentAction.sourceActionId;
        List<SimpleShipController> sourceUnits;
        // Get Units from squad
        if (GetUnit() == null)
        {
            Debug.LogWarning("No squad attached to agent, cant RunNavigation.");
            yield break; // We cant run navigation if there is no squad attached.

        }
        EncounterSquad sourceSquad = GetUnit().GetComponent<EncounterSquad>();
        StandardSystem subsystem = null;
        sourceUnits = sourceSquad.GetUnitList();

        //
        // ACTIVATE NAVIGATION BEHAVIOUR
        //
        foreach (SimpleShipController sourceUnit in sourceUnits)
        {
            if (AgentNavigationType.IsValid(sourceActionId) )
                subsystem = (StandardSystem)sourceUnit.GetComponent<NavigationSystem>();

            ATResourceData sourceResources = sourceUnit.GetComponent<ATResourceData>();

            CoroutineRunner.Instance.StartCoroutine(
            subsystem.Execute(
                    sourceActionId:sourceActionId,
                    sourceUnit:sourceUnit.gameObject,
                    targetUnits:new List<GameObject>{agentAction.subject},
                    sourceResources:sourceResources,
                    sourceAgent:this
            ));
            yield return null;        
        }
        // Debug.Log("Finished Action");
        
        yield return RemoveCommandQueue(agentAction);
        yield break;
    }

    public System.Collections.IEnumerator RunCombatAction(AgentCommand agentAction)
    {
        //Debug.Log($"Agent Run Action {this.name}");
        if (__unitGameObject == null)
        {
            Debug.LogError($"Cant proceed with RunCombatAction on {this.name} because unit is null");
            yield break;
        }
        if ( agentAction.subject == null)
        {
            Debug.LogError($"Cant proceed with RunCombatAction on {this.name} because subject is null");
            yield break;
        }

        Agent destinationAgent = agentAction.subject.GetComponent<Agent>();
        string sourceActionId = agentAction.sourceActionId;

        if (destinationAgent == null)
        {
            Debug.LogError($"Cant proceed with RunCombatAction on {agentAction.subject} because destinationAgent is null");
            yield break;

        }
        __is_running = true;
        try
        {
            //Debug.Log($"4 - Trying to complete actions for {this.gameObject}");
            // Validate Call
            bool validAction = SpaceEncounterManager.IsValidAgentAction(agentAction.sourceActionId);
            if (validAction == false)
            {
                Debug.Log($"Selected an invalid acton for agent {destinationAgent.GetAgentId()}: ({sourceActionId})");
                __is_running = false;
                yield break;
            }
            Agent sourceAgent = this.GetComponent<Agent>();

            // Analyze Opponent Actions
            string selfAgentId = sourceAgent.GetAgentId();        

            ///
            ///
            /// REFACTOR HERE
            ///
            EncounterSquad sourceSquad = GetUnit().GetComponent<EncounterSquad>();
            EncounterSquad destinationSquad = destinationAgent.GetUnit().GetComponent<EncounterSquad>();

            List<SimpleShipController> sourceUnits = sourceSquad.GetUnitList();
            //List<SimpleShipController> destinationUnits = destinationSquad.GetUnitList();

            if (sourceUnits.Count == 0)
                Debug.LogError("No units can complete this action");
            
            List<UnitAction> unitActions = new List<UnitAction>();
            foreach (var sourceUnit in sourceUnits)
            {
                if (sourceUnit == null)
                {
                    Debug.Log("Have invalid source unit! ");
                    continue;
                }
                List<GameObject> destinationUnits = GetComponent<TargetingModule>().GetAssignedTargets(sourceUnit.gameObject,destinationSquad);
                if(destinationUnits.Count == 0)
                {
                    Debug.Log($"No Target selected for unit {sourceUnit}");
                    continue;
                }
                unitActions.Add(new UnitAction
                {
                    sourceUnit = sourceUnit.gameObject,
                    destinationUnits = destinationUnits,
                    sourceActionId = sourceActionId
                });
            }            
            
            foreach(UnitAction unitAction in unitActions)
            {
                ATResourceData sourceResources = unitAction.sourceUnit.GetComponent<ATResourceData>();

                ProjectileEmitter sourceEm = unitAction.sourceUnit.GetComponent<SimpleShipController>().GetEmitter("primary");
                StandardSystem subsystem = null;

                if (sourceActionId == AgentPowerType.Attack)
                    subsystem = (StandardSystem)unitAction.sourceUnit.GetComponent<BlasterSystem>();
                if (sourceActionId == AgentPowerType.Missile)
                    subsystem = (StandardSystem)unitAction.sourceUnit.GetComponent<MissileSystem>();
                if (sourceActionId == AgentPowerType.Shield)
                    subsystem = (StandardSystem)unitAction.sourceUnit.GetComponent<ShieldSystem>();
                //Debug.Log($"!!!5 - Execute for for {this.gameObject}");
                CoroutineRunner.Instance.StartCoroutine(
                    subsystem.Execute(
                            sourceActionId:sourceActionId,
                            sourceUnit:sourceEm.gameObject,
                            targetUnits: unitAction.destinationUnits, 
                            sourceResources:sourceResources,
                            sourceAgent:this
                    )
                );
            }
            yield return RemoveCommandQueue(agentAction);
                
        }
        finally
        {
            __is_running = false;
        }
        
        yield break;
    }
    public void ClearObservers()
    {
        __observers.Clear();
    }

    public void AddObserver(SpaceEncounterObserver observer)
    {
        if (observer == null || __observers.Contains(observer))
            return;
        __observers.Add(observer);
    }

    public void RemoveObserver(SpaceEncounterObserver observer)
    {
        if (observer == null || !__observers.Contains(observer))
            return;
        __observers.Remove(observer);
    }

    public bool ContainsObserver(SpaceEncounterObserver observer)
    {
        return __observers.Contains(observer);
    }       

    public void NotifyObservers(string effect)
    {
//        Debug.Log($"Agent.NotifyObservers {this.name}is observing effect {effect} ");
        if(!SpaceEncounterManager.ObservableEffects.IsValid(effect))
        {
            Debug.LogError($"Agent {this.name} asked to process NotifyObservers with incorrect effect {effect}");
            return;
        }
        foreach( SpaceEncounterObserver observer in __observers)
        {
            observer.VisualizeEffect(effect,this.gameObject);
        }


    }
    //
    //
    ////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////
    //
    //
    
    public void RunAllAgentDecisions(){
        ChooseCommand(AgentCommand.Type.Combat); 
        ChooseCommand(AgentCommand.Type.Navigation); 
        ChooseCommand(AgentCommand.Type.Formation); ;
        ChooseCommand(AgentCommand.Type.AttackPattern); 
    }





}
