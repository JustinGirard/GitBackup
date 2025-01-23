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
public class AgentActionCommand
{
    public string commandType;
    public string destinationAgentId;
    public string sourceActionId;
}
public class AgentActionType {

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
    public static readonly List<string> all = new List<string> { NavigateTo, Halt };
    public const string NavigateTo = "NavigateTo";
    public const string Halt = "Halt";
}

class UnitAction
{
    public SimpleShipController sourceUnit;
    public SimpleShipController destinationUnit;
    public List<string> actionsTowardMe;
    public string sourceActionId;
}



public class Agent:MonoBehaviour, IPausable
{
    private List<SpaceEncounterObserver> __observers = new List<SpaceEncounterObserver>();

    [SerializeField]
    private GameObject primaryAim; // Primary Squad Aim location
    [SerializeField]
    private GameObject primaryNavigation; // Primary Squad Navigation Target

    public GameObject GetPrimaryAim(){
        return       primaryAim;  
    } 
    public GameObject GetPrimaryNavigation(){
        return       primaryNavigation;  
    } 

   [SerializeField]
    private ATResourceData resources;
    private GameObject __unitGameObject = null;
    //[SerializeField]
    //private GameObject unit;
    [SerializeField]
    private string __agentId = "";

    private Dictionary<string,Agent > __pendingAgents;
    private List<AgentActionCommand > __pendingCommands;

    [System.Serializable]
    public class AgentResourceEntry
    {
        public string resource_type;
        public float amount;
    }
    public List<AgentResourceEntry> agentResourceEntries = new List<AgentResourceEntry>();
    private SurfaceNavigationCommand __navCommand;

    public virtual bool AttachNavigationWaypoint(SurfaceNavigationCommand cmd)
    {
        __navCommand = cmd;
        return true;
    }
    public virtual bool DetachNavigationWaypoint()
    {
        __navCommand = null;
        return true;
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

        if (resources.GetRecords().Count == 0 && GetAgentId() == "agent_1")
        {
            //Debug.Log($"NO RECORDS FOR AGENT 1: {GetAgentId() } , {resources.GetRecords().Count }");
            foreach (var entry in defaultAgentOne)
            {
                resources.Deposit(entry.Key,entry.Value);
            }
        }

        if (resources.GetRecords().Count == 0 && GetAgentId() == "agent_2")
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

    void Awake(){
        __pendingCommands = new List<AgentActionCommand >();
        __pendingAgents = new Dictionary<string, Agent >();
        
        if (__agentId=="")
            Debug.LogError("No Agent ID assigned for agent");
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

        foreach( SpaceEncounterObserver observer in __observers)
        {
            Debug.Log("Showing Indicator from Agent.cs");
            observer.ShowFloatingActivePowers(allUnits[0].gameObject);
        }




        
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
    public void Run(){
        __unitGameObject.GetComponent<EncounterSquad>().Run();
    }
    public void Pause(){
        __unitGameObject.GetComponent<EncounterSquad>().Pause();
        
    }

    public ATResourceData GetResourceObject()
    {
        return resources;
    }

    public virtual void ChooseTargetAction()
    {
        throw new System.Exception("No Implemented choice");
    }
    public virtual void ChooseTargetNavigation()
    {
        throw new System.Exception("No Implemented choice");
    }

    public virtual string GetTargetAction()
    {
        throw new System.Exception("No Implemented choice");
    }    
    public virtual string GetTargetNavigation()
    {
        throw new System.Exception("No Implemented choice");
    }    
    

    public virtual void ResetTargetAction()
    {
        throw new System.Exception("No Implemented reset");
    }    

    public virtual void ResetTargetNavigation()
    {
        throw new System.Exception("No Implemented reset");
    }    
    

    public virtual bool SetTargetAction(string actionId)
    {
        throw new System.Exception("No Implemented reset");
        //return false;
    }
    public virtual bool SetTargetNavigation(string actionId)
    {
        throw new System.Exception("No Implemented reset");
    }

    // ChooseTargetNavigation() -- ChooseTargetNavigation [X]
    // GetTargetNavigation() -- GetTargetAction [X]
    // ResetTargetNavigation() -- ResetTargetAction
    // SetTargetNavigation(string actionId) -- SetTargetAction



    public void AddAgentCommand(string commandType,string targetActionId,Agent targetAgent)
    {
        string [] commandTypes = new string [] {"navigation","action"};
        if (targetActionId =="")
        {
            Debug.LogError("AddAgentAction missing valid action id");
            return;
        }
        //Debug.Log($"AddAgentAction({targetActionId},{targetAgent}({targetAgent.GetAgentId()}))");
        __pendingAgents[targetAgent.GetAgentId()] = targetAgent;
        string targetAgentId = targetAgent.GetAgentId();

        AgentActionCommand pr = new AgentActionCommand();
            pr.commandType = commandType;
            pr.sourceActionId = targetActionId;
            pr.destinationAgentId = targetAgentId;
        __pendingCommands.Add(pr);
    }
    public List<AgentActionCommand> GetActionsAffectingTarget(string destinationAgentId)
    {
        //if (this.GetAgentId() == "agent_1")
        //    Debug.Log($"agent_1 GetActionsAffectingTarget: Actions affecting {destinationAgentId}");
        List<AgentActionCommand> actionsAtAgent = new List<AgentActionCommand>();
        foreach (AgentActionCommand pr in __pendingCommands)
        {
            if (pr.destinationAgentId == destinationAgentId)
                actionsAtAgent.Add(pr);
        }
        return actionsAtAgent;
    }    

    public GameObject GetPrimaryEnemyUnit()
    {
        // Attempt to find the GameObject
        GameObject agent2 = GameObject.Find("AgentTwo");
        if (agent2 == null)
        {
            Debug.LogError("GameObject 'AgentTwo' could not be found.");
            return null;
        }

        // Attempt to get the Agent component
        Agent agentComponent = agent2.GetComponent<Agent>();
        if (agentComponent == null)
        {
            Debug.LogError($"GameObject 'AgentTwo' does not have an 'Agent' component.");
            return null;
        }

        // Attempt to get the unit from the Agent component
        GameObject unit = agentComponent.GetUnit();
        if (unit == null)
        {
            Debug.LogError($"'Agent' component on 'AgentTwo' returned a null unit.");
            return null;
        }

        // If all checks pass, return the unit
        return unit;
    }


    public System.Collections.IEnumerator RunActions()
    {

        //if (__navCommand != null)
        //{
        //    //Debug.Log("TODO: Have Navigation Command to process here");
        //    __navCommand.Hide();
        //}

        foreach (AgentActionCommand agentAction in __pendingCommands)
        {
            if (agentAction.commandType == "action")
            {
                // TODO : Generalize to GameObject, to allow targeting non AI entities. 
                Agent destinationAgent = __pendingAgents[agentAction.destinationAgentId];
                CoroutineRunner.Instance.StartCoroutine(RunAction(destinationAgent, agentAction.sourceActionId));
            }
            if (agentAction.commandType == "navigation")
            {
    
                CoroutineRunner.Instance.StartCoroutine(RunNavigation(__navCommand, agentAction.sourceActionId));
            }

        } 
        yield break;
    }
    bool __is_running = false;
    public bool IsRunning()
    {
        return __is_running;
    }
    
    public void ClearActions()
    {
        __pendingCommands.Clear();
        __pendingAgents.Clear();
        ResetTargetAction();
        ResetTargetNavigation();
        DetachNavigationWaypoint();
    }
    public System.Collections.IEnumerator RunNavigation( SurfaceNavigationCommand cursor,string sourceActionId)
    {
        Debug.Log($"Processing RunNavigation: agent:{this.gameObject.name}, navCommand: {sourceActionId}");
        if (sourceActionId == null)
        {
            Debug.LogError("sourceActionId is null.");
            yield break;
        }
        if (cursor == null)
        {
            Debug.LogError("cursor is null.");
            yield break;
        }
        if (cursor.gameObject == null)
        {
            Debug.LogError("cursor.gameObject is null.");
            yield break;
        }

        Debug.Log($"Running Navigation between {sourceActionId} and {cursor.gameObject.name}");
        EncounterSquad sourceSquad = GetUnit().GetComponent<EncounterSquad>();
        
        //
        // SET NAVIGATION TARGET
        //
        if (cursor.GetTarget() == null)
        {
            Debug.LogError("No active cursor target");
            yield break;
        }
        Transform cursorT = cursor.GetTarget().transform;
        GetPrimaryNavigation().transform.position = cursorT.position;
        GetPrimaryAim().transform.position = GetPrimaryEnemyUnit().transform.position;
        GetPrimaryNavigation().transform.LookAt(GetPrimaryAim().transform);

        sourceSquad.SetGoalPosition(GetPrimaryNavigation().transform,Vector3.zero);
        cursor.Hide();
        StandardSystem subsystem = null;
        List<SimpleShipController> sourceUnits = sourceSquad.GetUnitList();

        //
        // ACTIVATE NAVIGATION BEHAVIOUR
        //
        foreach (SimpleShipController sourceUnit in sourceUnits)
        {        
            if (sourceActionId == AgentNavigationType.NavigateTo)
                subsystem = (StandardSystem)sourceUnit.GetComponent<NavigationSystem>();
            if (sourceActionId == AgentNavigationType.Halt)
                subsystem = (StandardSystem)sourceUnit.GetComponent<NavigationSystem>();

            ATResourceData sourceResources = sourceUnit.GetComponent<ATResourceData>();
            ATResourceData destinationResources = null;

            CoroutineRunner.Instance.StartCoroutine(
            subsystem.Execute(
                    sourcePowerId:sourceActionId, 
                    targetPowerIds:new List<string>(), 
                    sourceUnit:sourceUnit.gameObject,
                    targetUnit:sourceUnit.GetGoalPosition(), 
                    sourceResources:sourceResources,
                    targetResources:destinationResources
            ));
            yield return null;        
        }

        yield break;
    }

    public System.Collections.IEnumerator RunAction(Agent destinationAgent,string sourceActionId)
    {
        //Debug.Log($"Agent Run Action {this.name}");
        if (__unitGameObject == null)
        {
            Debug.LogError("Cant proceed with RunAction on {destinationAgent.name} because unit is null");
            yield break;
        }
        __is_running = true;
        try
        {
            // Validate Call
            bool validAction = SpaceEncounterManager.IsValidAgentAction(sourceActionId);
            if (validAction == false)
            {
                Debug.Log($"Selected an invalid acton for agent {destinationAgent.GetAgentId()}: ({sourceActionId})");
                yield break;
            }
            Agent sourceAgent = this.GetComponent<Agent>();

            // Analyze Opponent Actions
            string selfAgentId = sourceAgent.GetAgentId();        
            List<AgentActionCommand> actionsAffectingSelf =  destinationAgent.GetActionsAffectingTarget(selfAgentId);

            //Debug.Log($"Running command for {this.name}:{sourceAgent.GetAgentId()}");
            //if (actionsAffectingSelf.Count == 0)
            //    Debug.LogWarning("No actions found");
            List<string> actionsTowardMe = new List<string>();
            //Debug.Log($"--I am targeting {destinationAgent.GetAgentId()} with {sourceActionId}");
            foreach (AgentActionCommand aap in actionsAffectingSelf)
            {
                actionsTowardMe.Add(aap.sourceActionId);                    
            }

            EncounterSquad sourceSquad = GetUnit().GetComponent<EncounterSquad>();
            EncounterSquad destinationSquad = destinationAgent.GetUnit().GetComponent<EncounterSquad>();
            if (sourceSquad == null)
                Debug.LogError("sourceSquad  was null, but it can't be");
            if (destinationSquad == null)
                Debug.LogError("destinationSquad  was null, but it can't be");

            List<SimpleShipController> sourceUnits = sourceSquad.GetUnitList();
            List<SimpleShipController> destinationUnits = destinationSquad.GetUnitList();

            if (sourceUnits.Count == 0)
                Debug.LogError("No units can make this action");
            if (destinationUnits.Count == 0)
                Debug.LogError("no destinations can make this action");
            List<UnitAction> unitActions = new List<UnitAction>();
            foreach (var sourceUnit in sourceUnits)
            {
                if (sourceUnit == null)
                {
                    Debug.Log("Have invalid source unit! ");
                    continue;
                }
                foreach (var destinationUnit in destinationUnits)
                {
                    if (destinationUnit == null)
                    {
                        Debug.Log("Have invalid destination unit! ");
                        continue;
                    }
                    unitActions.Add(new UnitAction
                    {
                        sourceUnit = sourceUnit,
                        destinationUnit = destinationUnit,
                        actionsTowardMe = actionsTowardMe,
                        sourceActionId = sourceActionId
                    });
                }
            }            
            
            foreach(UnitAction unitAction in unitActions)
            {
                ATResourceData sourceResources = unitAction.sourceUnit.GetComponent<ATResourceData>();
                //if(destinationResources == null)
                //    Debug.LogError($"destinationResources is null");

                ATResourceData destinationResources = unitAction.destinationUnit.GetComponent<ATResourceData>();
                ProjectileEmitter sourceEm = unitAction.sourceUnit.GetEmitter("primary");
                ProjectileEmitter destEm = unitAction.destinationUnit.GetEmitter("primary");

                /// TODOAGENT unitAction.sourceUnit.SetRootLookAt(unitAction.destinationUnit.transform.position);
                // unitAction.sourceUnit.SetRootLookAt(unitAction.destinationUnit.transform.position);
                StandardSystem subsystem = null;
                if (sourceActionId == AgentActionType.Attack)
                    subsystem = (StandardSystem)unitAction.sourceUnit.GetComponent<BlasterSystem>();
                if (sourceActionId == AgentActionType.Missile)
                    subsystem = (StandardSystem)unitAction.sourceUnit.GetComponent<MissileSystem>();
                if (sourceActionId == AgentActionType.Shield)
                    subsystem = (StandardSystem)unitAction.sourceUnit.GetComponent<ShieldSystem>();
                CoroutineRunner.Instance.StartCoroutine(
                    subsystem.Execute(
                            sourcePowerId:sourceActionId, 
                            targetPowerIds:actionsTowardMe, 
                            sourceUnit:sourceEm.gameObject,
                            targetUnit:destEm.gameObject, 
                            sourceResources:sourceResources,
                            targetResources:destinationResources
                    )
                );
            }


                
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
}
