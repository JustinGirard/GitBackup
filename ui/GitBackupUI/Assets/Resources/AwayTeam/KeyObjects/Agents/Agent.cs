using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.CompilerServices;
using System;
using Cinemachine;
using VisualCommand;
public class AgentActionPair
{
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

class UnitAction
{
    public SpaceMapUnitAgent sourceUnit;
    public SpaceMapUnitAgent destinationUnit;
    public List<string> actionsTowardMe;
    public string sourceActionId;
}



public class Agent:MonoBehaviour, IPausable
{
    private List<SpaceEncounterObserver> __observers = new List<SpaceEncounterObserver>();

   [SerializeField]
    private ATResourceData resources;
    private GameObject __unitGameObject = null;
    //[SerializeField]
    //private GameObject unit;
    [SerializeField]
    private string __agentId = "";

    private Dictionary<string,Agent > __pendingAgents;
    private List<AgentActionPair > __pendingActions;

    [System.Serializable]
    public class AgentResourceEntry
    {
        public string resource_type;
        public float amount;
    }
    public List<AgentResourceEntry> agentResourceEntries = new List<AgentResourceEntry>();
    private SurfaceNavigationCommand __navCommand;

    public virtual bool SetNavigationAction(SurfaceNavigationCommand cmd)
    {
        __navCommand = cmd;
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
        __pendingActions = new List<AgentActionPair >();
        __pendingAgents = new Dictionary<string, Agent >();
        
        if (__agentId=="")
            Debug.LogError("No Agent ID assigned for agent");
    }
    public string GetAgentId(){
        return __agentId;
    }
    public void SetUnit(GameObject go)
    {
        __unitGameObject = go;
        EncounterSquad ec = go.GetComponent<EncounterSquad>();
        if(ec == null)
            Debug.LogError("");
        //resources.ClearSubResources();
        List<SpaceMapUnitAgent> allUnits = ec.GetUnitList();
        if (allUnits.Count == 0)
            Debug.LogError("Could not find any units");

        //Debug.Log($"Adding Unit in Set Unit {unit.name}");
        foreach(SpaceMapUnitAgent unit in allUnits)
        {
            //Debug.Log($"Adding Unit in Set Unit {unit.name}");
            ATResourceData unitResourceData = unit.GetComponent<ATResourceData>();
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
    public virtual string GetTargetAction()
    {
        throw new System.Exception("No Implemented choice");
        //return "";
    }    

    public virtual void ResetTargetAction()
    {
        throw new System.Exception("No Implemented reset");
    }    
    public virtual bool SetTargetAction(string actionId)
    {
        throw new System.Exception("No Implemented reset");
        //return false;
    }


    public void AddAgentAction(string targetActionId,Agent targetAgent)
    {
        if (targetActionId =="")
        {
            Debug.LogError("AddAgentAction missing valid action id");
            return;
        }
        //Debug.Log($"AddAgentAction({targetActionId},{targetAgent}({targetAgent.GetAgentId()}))");
        __pendingAgents[targetAgent.GetAgentId()] = targetAgent;
        string targetAgentId = targetAgent.GetAgentId();
        AgentActionPair pr = new AgentActionPair();
        pr.sourceActionId = targetActionId;
        pr.destinationAgentId = targetAgentId;
        //Debug.Log($"AddAgentAction Final: ({pr.sourceActionId},{pr.destinationAgentId}");
        __pendingActions.Add(pr);
    }
    public List<AgentActionPair> GetActionsAffectingTarget(string destinationAgentId)
    {
        //if (this.GetAgentId() == "agent_1")
        //    Debug.Log($"agent_1 GetActionsAffectingTarget: Actions affecting {destinationAgentId}");
        List<AgentActionPair> actionsAtAgent = new List<AgentActionPair>();
        foreach (AgentActionPair pr in __pendingActions)
        {
            if (pr.destinationAgentId == destinationAgentId)
                actionsAtAgent.Add(pr);
        }
        return actionsAtAgent;
    }    
    public System.Collections.IEnumerator RunActions()
    {

        if (__navCommand != null)
        {
            Debug.Log("TODO: Have Navigation Command to process here");
            __navCommand.Hide();

        }
        foreach (AgentActionPair agentAction in __pendingActions)
        {
            Agent agent = __pendingAgents[agentAction.destinationAgentId];
            CoroutineRunner.Instance.StartCoroutine(RunAction(agent, agentAction.sourceActionId));
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
        __pendingActions.Clear();
        __pendingAgents.Clear();
        ResetTargetAction();
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
            List<AgentActionPair> actionsAffectingSelf =  destinationAgent.GetActionsAffectingTarget(selfAgentId);

            //Debug.Log($"Running command for {this.name}:{sourceAgent.GetAgentId()}");
            if (actionsAffectingSelf.Count == 0)
                Debug.LogWarning("No actions found");
            List<string> actionsTowardMe = new List<string>();
            //Debug.Log($"--I am targeting {destinationAgent.GetAgentId()} with {sourceActionId}");
            foreach (AgentActionPair aap in actionsAffectingSelf)
            {
                actionsTowardMe.Add(aap.sourceActionId);                    
            }

            //yield break;

            //ATResourceData sourceResources = sourceAgent.GetResourceObject();
            //ATResourceData destinationResources = destinationAgent.GetResourceObject();
            //StandardSystem subsystem = null;
            //if (sourceActionId == AgentActionType.Attack)
            //    subsystem = (StandardSystem)__unitGameObject.GetComponent<BlasterSystem>();
            //if (sourceActionId == AgentActionType.Missile)
            //    subsystem = (StandardSystem)__unitGameObject.GetComponent<MissileSystem>();
            //if (sourceActionId == AgentActionType.Shield)
            //    subsystem = (StandardSystem)__unitGameObject.GetComponent<ShieldSystem>();
            ////
            //if (subsystem == null)
            //{
            //    Debug.LogError($"Could not find System on {sourceAgent.GetAgentId()} for {sourceActionId}");
            //    yield break;
            //}
            // Agent destinationAgent
            EncounterSquad sourceSquad = GetUnit().GetComponent<EncounterSquad>();
            EncounterSquad destinationSquad = destinationAgent.GetUnit().GetComponent<EncounterSquad>();
            if (sourceSquad == null)
                Debug.LogError("sourceSquad  was null, but it can't be");
            if (destinationSquad == null)
                Debug.LogError("destinationSquad  was null, but it can't be");
            // Dictionary<string,SpaceMapUnitAgent> squadUnits = __unitGameObject.GetComponent<EncounterSquad>.GetUnits();
            List<SpaceMapUnitAgent> sourceUnits = sourceSquad.GetUnitList();
            List<SpaceMapUnitAgent> destinationUnits = destinationSquad.GetUnitList();
            if (sourceUnits.Count == 0)
                Debug.LogError("No units can make this action");
            if (destinationUnits.Count == 0)
                Debug.LogError("no destinations can make this action");
            List<UnitAction> unitActions = new List<UnitAction>();
// Create UnitActions for each combination of source and destination
            //
            foreach (var sourceUnit in sourceUnits)
            {
                foreach (var destinationUnit in destinationUnits)
                {
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
