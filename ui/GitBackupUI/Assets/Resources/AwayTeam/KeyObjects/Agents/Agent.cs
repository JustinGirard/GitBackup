using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using System.Runtime.InteropServices.WindowsRuntime;
class AgentActionPair
{
    public string destinationAgentId;
    public string sourceActionId;
}
class Agent:MonoBehaviour
{
    
   [SerializeField]
    private ATResourceData resources;
    private GameObject __unitGameObject = null;
    [SerializeField]
    private GameObject unit;
    [SerializeField]
    private string __agentId = "";

    private Dictionary<string,Agent > __pendingAgents;
    private List<AgentActionPair > __pendingActions;
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
    }
    public ATResourceData GetResourceObject()
    {
        return resources;
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
        Debug.Log($"ACTIONS LENGTH FOR {this.GetAgentId()}: {__pendingAgents.Count.ToString()}");
        foreach (AgentActionPair agentAction in __pendingActions)
        {
            Debug.Log($"Action {this.GetAgentId()}-> {agentAction.sourceActionId} -> {agentAction.destinationAgentId},");
            Agent agent = __pendingAgents[agentAction.destinationAgentId];
            CoroutineRunner.Instance.StartCoroutine(RunAction(agent, agentAction.sourceActionId));
        } 
        //__pendingAgents.Clear();
        //__pendingActions.Clear();
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
            bool validAction = false;
            if (sourceActionId == SpaceEncounterManager.AgentActions.Missile)
                validAction = true;
            if (sourceActionId == SpaceEncounterManager.AgentActions.Shield)
                validAction = true;
            if (sourceActionId == SpaceEncounterManager.AgentActions.Attack)
                validAction = true;        
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
                //Debug.Log($"INVESTIGATING action towards me: {aap.destinationAgentId} {aap.sourceActionId}");
                actionsTowardMe.Add(sourceActionId);                    
            }

            //yield break;

            ATResourceData sourceResources = sourceAgent.GetResourceObject();
            ATResourceData destinationResources = destinationAgent.GetResourceObject();
            StandardSystem subsystem = null;
            if (sourceActionId == SpaceEncounterManager.AgentActions.Attack)
                subsystem = (StandardSystem)__unitGameObject.GetComponent<BlasterSystem>();
            if (sourceActionId == SpaceEncounterManager.AgentActions.Missile)
                subsystem = (StandardSystem)__unitGameObject.GetComponent<MissileSystem>();
            if (sourceActionId == SpaceEncounterManager.AgentActions.Shield)
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
        

        /*
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
        */
        yield break;
    }
}
