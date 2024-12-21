using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.CompilerServices;
using System;
public class AgentActionPair
{
    public string destinationAgentId;
    public string sourceActionId;
}
public class AgentActions {

    public static bool IsValid(string action)
    {
        return all.Contains(action);
    }               
    public static readonly List<string> all = new List<string> { Attack, Missile, Shield };
    public const string Attack = "Attack";
    public const string Missile = "Missile";
    public const string Shield = "Shield";
}


public class Agent:MonoBehaviour, IPausable
{
    private List<SpaceEncounterObserver> __observers = new List<SpaceEncounterObserver>();

   [SerializeField]
    private ATResourceDataGroup resources;
    private GameObject __unitGameObject = null;
    [SerializeField]
    private GameObject unit;
    [SerializeField]
    private string __agentId = "";

    private Dictionary<string,Agent > __pendingAgents;
    private List<AgentActionPair > __pendingActions;

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
    public void DestroyUnit(string reasonCode)
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

    public ATResourceDataGroup GetResourceObject()
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
        return "";
    }    

    public virtual void ResetTargetAction()
    {
        throw new System.Exception("No Implemented reset");
    }    
    public virtual bool SetTargetAction(string actionId)
    {
        throw new System.Exception("No Implemented reset");
        return false;
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

            ATResourceData sourceResources = sourceAgent.GetResourceObject();
            ATResourceData destinationResources = destinationAgent.GetResourceObject();
            StandardSystem subsystem = null;
            if (sourceActionId == AgentActions.Attack)
                subsystem = (StandardSystem)__unitGameObject.GetComponent<BlasterSystem>();
            if (sourceActionId == AgentActions.Missile)
                subsystem = (StandardSystem)__unitGameObject.GetComponent<MissileSystem>();
            if (sourceActionId == AgentActions.Shield)
                subsystem = (StandardSystem)__unitGameObject.GetComponent<ShieldSystem>();
            ////
            if (subsystem == null)
            {
                Debug.LogError($"Could not find System on {sourceAgent.GetAgentId()} for {sourceActionId}");
                yield break;
            }

            CoroutineRunner.Instance.StartCoroutine(
                subsystem.Execute(
                        sourceAgentId:sourceAgent.GetAgentId(), 
                        sourcePowerId:sourceActionId, 
                        targetAgentId:destinationAgent.GetAgentId(), 
                        targetPowerIds:actionsTowardMe, 
                        sourceResources:sourceResources,
                        targetResources:destinationResources
                )
            );
                
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
