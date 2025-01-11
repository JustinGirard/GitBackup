using System.Collections.Generic;

using UnityEngine;
using Cinemachine;

public class SpaceEncounterManager : GameEncounterBase,IPausable,IATGameMode, IGameEncounter
{
    [SerializeField]
    CinemachineVirtualCamera __agentOneCam;
    [SerializeField]
    CinemachineVirtualCamera __worldCam;

    [SerializeField]
    public List<ResourceEntry> defaultResources;
    
    
    public ATResourceData accountResourceData;
    //public ATResourceData agent1UnitResourceData;
    //public ATResourceData agent2UnitResourceData;
    // Assets/Resources/AwayTeam/KeyObjects/EncounterSquad.prefab
    AgentManager agentManager;

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
        return agentManager.GetPlayerAgent();
    }

    public void Initalize(){

       InitEncounterData();
    }

    public override void  DoAwake()
    {
        agentManager = GetComponent<AgentManager>();
        if (agentManager == null)
        {
            Debug.LogError("Must have agent manager as peer component");
            return;

        }
        if (accountResourceData == null)
        {
            Debug.LogError("accountResourceData is not assigned to SpaceEncounterManager.");
            return;
        }             

    }



    public void ForEachAgent(System.Action<Agent> action)
    {
        agentManager.ForEachAgent(action);
    }

    public void ForEachAgentId(System.Action<string> action)
    {
        agentManager.ForEachAgentId(action);
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
        /*
        ForEachAgent(agent => { agent.Run(); });   
        */ 
        ForEachAgent(agent => { agent.ClearResourceRecords(); });   
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
    
    protected override void DoBegin()
    {
        // Validate match state
        if (IsRunning() || AmReady())
            throw new System.Exception("Cant begin a new match, one is already initialized");

        RegisterNotificationWithAgents();
        __timerProgress = 0;

        // Validate agents
        string[] agentKeys = { "agent_1", "agent_2" };
        foreach (var agentKey in agentKeys)
        {
            if (!agentManager.HasAgentKey(agentKey) || agentManager.GetAgent(agentKey) == null)
            {
                Debug.LogError($"{agentKey} is not assigned to agentManager.");
                return;
            }
        }

        // Initialize agents
        foreach (var agentKey in agentKeys)
        {
            Agent agent = agentManager.GetAgent(agentKey);
            if (!agentManager.InitializeAgent(agent, agentKey))
            {
                return; // Exit on failure
            }
        }

        // Notify all screens after agents have been initialized
        NotifyAllScreens(SpaceEncounterManager.ObservableEffects.ShieldOff);
        NotifyAllScreens(SpaceEncounterManager.ObservableEffects.AttackOff);
        NotifyAllScreens(SpaceEncounterManager.ObservableEffects.MissileOff);
    }





    public override void DoEnd()
    {
        CameraToWorld();
        string reasonConstant = "";
        /*foreach (string agent_id in new string [] {"agent_1","agent_2"})
        {
            __agents[agent_id].DestroyUnit(reasonConstant);
            __agents[agent_id].GetResourceObject().ClearRecords();
        } */
        ForEachAgent(agent => { agent.Run(); 
            agent.DestroyUnit(reasonConstant);
            agent.GetResourceObject().ClearRecords();
        });  

        __timerProgress = 0;
    }



    protected override void DoRun()
    {

        if (IsRunning())
            throw new System.Exception("Cant Run Twice");

        //float hull = (float)__agents["agent_1"].GetResourceObject().GetRecordField("Encounter",ResourceTypes.Hull);
        ///////////////
        if (!agentManager.HasAgentKey("agent_1"))
        {
            Debug.LogError("Agent 'agent_1' not found in __agents dictionary.");
            return;
        }

        Agent agent1 = agentManager.GetAgent("agent_1");
        if (agent1 == null)
        {
            Debug.LogError("Agent 'agent_1' is null.");
            return;
        }
        Agent agent2 = agentManager.GetAgent("agent_2");
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
        ForEachAgent(agent => { agent.Pause(); });    
        NotifyAllScreens(ObservableEffects.ShowPaused);        
    }
    float GetResourceValue(string agentId, string resourceType)
    {
        if (!agentManager.HasAgentKey(agentId))
        {
            Debug.LogError($"Agent with ID {agentId} not found.");
            return 0.0f;
        }

        var agent = agentManager.GetAgent(agentId);
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
    protected override void DoInnerUpdate(float deltaTime)
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
        float epochLength = 5.0f;
        RunIfTime("showActionProgress", 0.25f, deltaTime, () =>
        {
            __timerProgress = __timerProgress + (0.25f/epochLength)*__timerProgressMax;

            NotifyAllScreens(ObservableEffects.TimerTick);
        });

        RunIfTime("doActions", epochLength, deltaTime, () =>
        {
            if (__actionsRunning==false)
            {
                __actionsRunning = true;
                StartActionInterval();
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
            });
            // Debug.Log("(x)Starting Actions");
            // TRIGGER Actions
            ForEachAgent(primaryAgent => 
            {
                if (primaryAgent == null)
                    Debug.LogError("primaryAgent was found to be null. This should be impossible (2)");
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

        if (agentManager.HasAgentKey(agent_id))
            return agentManager.GetAgent(agent_id).GetTargetAction();
        return null;
    }

    public ATResourceData GetResourceObject(string agentId)
    {
        if (agentId == "account")
            return accountResourceData;
        if (agentId == "agent_1")
            return agentManager.GetAgent("agent_1").GetResourceObject();
        if (agentId == "agent_2")
            return agentManager.GetAgent("agent_2").GetResourceObject();
        Debug.LogError("Could not find requested agent data");
        return null;
    }
}





    /*protected override void DoBegin()
    {
        //CinemachineVirtualCamera __agenOneCam;
        //CinemachineVirtualCamera __worldCam;
        // Debug.Log("RUNNING BEGIN");
        if (!agentManager.HasAgentKey("agent_1") || agentManager.GetAgent("agent_1") == null)
        {
            Debug.LogError("agent_1 is not assigned to agentManager.");
            return;
        }               
        if (!agentManager.HasAgentKey("agent_2") || agentManager.GetAgent("agent_2") == null)
        {
            Debug.LogError("agent_2 is not assigned to agentManager.");
            return;
        } 

        Dictionary<string,ATResourceData> redat ;
        
        if (IsRunning())
            throw new System.Exception("Cant begin a new match, one is already initalized");
        if (AmReady() || IsRunning())
            throw new System.Exception("Cant begin a new match, one is already initalized");
        //intervalRunner.ClearAllTimers();
        RegisterNotificationWithAgents();
        __timerProgress = 0;

        Agent agent1 = agentManager.GetAgent("agent_1");
        Agent agent2 = agentManager.GetAgent("agent_2");
        
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
        
        //CameraToAgent();
        NotifyAllScreens(SpaceEncounterManager.ObservableEffects.ShieldOff);        
        NotifyAllScreens(SpaceEncounterManager.ObservableEffects.AttackOff);        
        NotifyAllScreens(SpaceEncounterManager.ObservableEffects.MissileOff);

    }*/
    /// <summary>
    /// /////////////////////////
    /// </summary>
    /// <exception cref="System.Exception"></exception>