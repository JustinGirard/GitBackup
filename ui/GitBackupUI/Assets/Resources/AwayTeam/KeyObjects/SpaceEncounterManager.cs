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
    


    protected override void DoBegin(System.Action onFinish)
    {
        CameraToWorld();

        StartCoroutine(RefAgentManager.RecreateAllUnitsCoRoutine(onFinish));
        /*ForEachAgentId(agentKey => 
        {
            Agent agent = RefAgentManager.GetAgent(agentKey);
            if (!RefAgentManager.RecreateSingleUnit(agent, agentKey))
            {
                return;
            }
         });*/
         // Early warning of any resource problems
        /*
        ForEachAgent(agent => {
            float fuel = GetAgentResourceField(agent,ResourceTypes.Fuel);
            float hull = GetAgentResourceField(agent,ResourceTypes.Hull);
            if (hull <= 0 || fuel <=0 )
            {
                Debug.LogError($"Cant run. Agent One is out of hull {hull.ToString()} or fuel {fuel.ToString()}");
            }       
        });*/

    }

    public override void DoEnd()
    {
        CameraToWorld();
        ForEachAgent(agent => { agent.Run(); 
            agent.DestroyUnits(reasonCode:"");
            agent.GetResourceObject().ClearRecords();
        });  
    }

    private float GetAgentResourceField(Agent agent,  string resourceType)
    {
        if (agent == null)
        {
            Debug.LogError("Agent is null.");
            return 0f;
        }

        ATResourceData resourceObject = agent.GetResourceObject();
        if (resourceObject == null)
        {
            Debug.LogError("ResourceObject is null.");
            return 0f;
        }

        object field = resourceObject.GetRecordField("Encounter", resourceType);
        if (field == null)
        {
            Debug.LogError($"GetRecordField() returned null for {agent.gameObject.name}:{resourceType}.");
            return 0f;
        }

        try
        {
            return System.Convert.ToSingle(field);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to convert {resourceType} field to float. Error: {ex.Message}");
            return 0f;
        }
    }


    protected override void DoRun()
    {
        ForEachAgent(agent => {
            bool doDebug = false;
            agent.GetResourceObject().RefreshResources(doDebug);
        });
        if (!RefAgentManager.HasAgentKey("agent_1") || !RefAgentManager.HasAgentKey("agent_2"))
        {
            Debug.LogError("Agent 'agent_1/agent_2' not found in __agents dictionary.");
            return;
        }
        bool exit=false;
        ForEachAgent(agent => {
            float fuel = GetAgentResourceField(agent,ResourceTypes.Fuel);
            float hull = GetAgentResourceField(agent,ResourceTypes.Hull);
            if (hull <= 0 || fuel <=0 )
            {
                Debug.LogError($"Cant run. Agent One is out of hull {hull.ToString()} or fuel {fuel.ToString()}");
                exit = true;
            }       
        });
        if (exit)
            return;

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

        RunIfTime("doActionsKickoff", epochLength, deltaTime, () =>
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
                primaryAgent.AddAgentCommand("action",primaryAction,targetAgent);
            
            string primaryNavigation = GetTargetNavigation(primaryAgent.GetAgentId());
            if (primaryNavigation.Length > 0)
                primaryAgent.AddAgentCommand("navigation",primaryNavigation,targetAgent);
        });

    }

    public void RunAllAgentActions(){
            // CHOOSE -
            ForEachAgent(agent => { agent.ChooseTargetAction(); });
            ForEachAgent(agent => { agent.ChooseTargetNavigation(); });

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
    public string GetTargetNavigation(string agent_id){

        if (RefAgentManager.HasAgentKey(agent_id))
            return RefAgentManager.GetAgent(agent_id).GetTargetNavigation();
        return null;
    }    
 
}



// Issues:
// 1 - Health not scaling properly
// 2 - Nav Cursor not rendering
// 3 - Pilot mode does not exist