using System.Collections.Generic;

using UnityEngine;
using Cinemachine;
 
public class SpaceEncounterManager : GameEncounterBase,IPausable,IATGameMode
{
    
    [SerializeField]
    CinemachineVirtualCamera __agentOneCam;
    [SerializeField]
    CinemachineVirtualCamera __worldCam;

    [SerializeField]
    public List<ResourceEntry> defaultResources;
    int __currentLevel = 1;

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

    protected override void DoInitalize()
    {
        // RefAgentManager = GetComponent<AgentManager>();
        if (RefAgentManager == null)
            Debug.LogError("Could not find attached AgentManager");

        ATResourceData accountResourceData = GetResourceObject("account");
        accountResourceData.ClearRecords();
        List<string> resourceTypes = ResourceTypes.all;
        foreach (ResourceEntry resource in defaultResources)
        {
            if (resourceTypes.Contains(resource.key ))
            {
                accountResourceData.AddToRecordField("Encounter", resource.key, resource.value, create: true);
            }
        }    
        ForEachAgent(agent => { agent.ClearResourceRecords(); });   

    }

    
    ///
    /// LEVEL DATA
    /// 
    /// 
    public override int GetLevel() { return __currentLevel; } 
    public Dictionary<string,string> GetLevelData()
    {
        return new Dictionary<string, string> {
            {"level_id",$"{GetLevel().ToString()}"},
            {"title",$"ROUND {GetLevel().ToString()}"},
        }; 
    }
    public override void SetLevel(int lvl)
    {
        if (lvl < 1)
            throw new System.Exception("Can set such a small level value");

        if (IsRunning())
            throw new System.Exception("Cant set the level if am running level");
        __currentLevel = lvl;
    }
    
    ///
    /// Encounter Operations
    /// 
    /// 
    public override void  DoAwake(){ 


        if (RefAgentManager == null)
        {
            Debug.LogError("Must have agent manager as peer component");
            return;

        }
        ATResourceData accountResourceData = GetResourceObject("account");
        if (accountResourceData == null)
        {
            Debug.LogError("accountResourceData is not assigned to SpaceEncounterManager.");
            return;
        }    

    }
    protected override void DoBegin()
    {
        CameraToWorld();
        ForEachAgentId(agentKey => 
        {
            Agent agent = RefAgentManager.GetAgent(agentKey);
            if (!RefAgentManager.RecreateUnits(agent, agentKey))
            {
                return;
            }
         });
    }

    public override void DoEnd()
    {
        CameraToWorld();
        ForEachAgent(agent => { agent.Run(); 
            agent.DestroyUnits(reasonCode:"");
            agent.GetResourceObject().ClearRecords();
        });  
    }

    protected override void DoRun()
    {

        if (!RefAgentManager.HasAgentKey("agent_1"))
        {
            Debug.LogError("Agent 'agent_1' not found in __agents dictionary.");
            return;
        }

        Agent agent1 = RefAgentManager.GetAgent("agent_1");
        if (agent1 == null)
        {
            Debug.LogError("Agent 'agent_1' is null.");
            return;
        }
        Agent agent2 = RefAgentManager.GetAgent("agent_2");
        if (agent2 == null)
        {
            Debug.LogError("Agent 'agent_2' is null.");
            return;
        }

        var resourceObject = agent1.GetResourceObject();
        resourceObject.RefreshResources();
        //Debug.Log("RUN DEBUG");
        bool doDebug = false;
        agent1.GetResourceObject().RefreshResources(doDebug);
        agent2.GetResourceObject().RefreshResources();
        Dictionary<string,ATResourceData> redat = agent1.GetResourceObject().GetSubResources();

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
        float fuel = (float)agent1.GetResourceObject().GetRecordField("Encounter",ResourceTypes.Fuel);
        if (hull <= 0 || fuel <=0 )
        {
            Debug.LogError($"Cant run. Agent One is out of hull {hull.ToString()} or fuel {fuel.ToString()}");
            return;
        }       

        ForEachAgent(agent => { agent.Run(); });    
        NotifyAllScreens(ObservableEffects.ShowUnpaused);
    }


    protected override void DoPause()
    {
    }


    bool __actionsRunning = false;
    protected override void DoInnerUpdate(float deltaTime, float epochLength)
    {

        RunIfTime("endEncounterCheck", 1f, deltaTime, () =>
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

        RunIfTime("doActions", epochLength, deltaTime, () =>
        {
            if (__actionsRunning==false)
            {
                __actionsRunning = true;
                RunAllAgentActions();
            }
        });   

        if (__actionsRunning==true) RunIfTime("doActionsClear", 0.5f, deltaTime, () =>
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
                SetTimerProgress(0);            
            }                    
        });      
    }

    public void RunSingleAgentActions(Agent primaryAgent)
    {

        if (primaryAgent == null)
            Debug.LogError("primaryAgent was found to be null. This should be impossible (1)");
        ForEachAgent(targetAgent => 
        {           
            if (targetAgent == null)
                Debug.LogError("targetAgent was found to be null. This should be impossible (1)");
            if(primaryAgent.name == targetAgent.name) return;

            string primaryAction = GetTargetAction(primaryAgent.GetAgentId());
            if (primaryAction.Length > 0)
                primaryAgent.AddAgentAction(primaryAction,targetAgent);
        });

    }

    public void RunAllAgentActions(){
            // CHOOSE -
            ForEachAgent(agent => { agent.ChooseTargetAction(); });

            // TARGET -
            ForEachAgent(primaryAgent => { RunSingleAgentActions(primaryAgent); });
           
            // TRIGGER Actions
            ForEachAgent(primaryAgent => 
            {
                StartCoroutine(primaryAgent.RunActions());
            });
    }
    public string GetTargetAction(string agent_id){

        if (RefAgentManager.HasAgentKey(agent_id))
            return RefAgentManager.GetAgent(agent_id).GetTargetAction();
        return null;
    }
    ///
    ///
    ///
    ///
    //private AgentManager agentManager;

    public AgentManager RefAgentManager
    {
        get { 
                return GetComponent<AgentManager>(); 
            }
        set {}
    }

    public ATResourceData GetResourceObject(string agentId)
    {
        if (agentId == "account")
            return accountResourceData;
        if (agentId == "agent_1")
            return RefAgentManager.GetAgent("agent_1").GetResourceObject();
        if (agentId == "agent_2")
            return RefAgentManager.GetAgent("agent_2").GetResourceObject();
        Debug.LogError("Could not find requested agent data");
        return null;
    }       

    public Agent GetPlayerAgent()
    {
        return RefAgentManager.GetPlayerAgent();
    }    


    public void ForEachAgent(System.Action<Agent> action)
    {
        RefAgentManager.ForEachAgent(action);
    }

    public void ForEachAgentId(System.Action<string> action)
    {
        RefAgentManager.ForEachAgentId(action);
    }

    public float GetResourceValue(string agentId, string resourceType)
    {
        if (!RefAgentManager.HasAgentKey(agentId))
        {
            Debug.LogError($"Agent with ID {agentId} not found.");
            return 0.0f;
        }

        var agent = RefAgentManager.GetAgent(agentId);
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
    

}
