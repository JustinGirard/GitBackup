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

    }

    public override void DoEnd()
    {
        CameraToWorld();
        ForEachAgent(agent => { agent.Run(); 
            agent.DestroyUnits(reasonCode:"");
            agent.GetResourceObject().ClearRecords();
        });  
    }
        //bool __actionsRunning = false;
    protected override void DoInnerUpdate(float deltaTime, float epochLength)
    {
        RunIfTime("endEncounterCheck", 1f, deltaTime, () =>
        {
            ForEachAgent(agent =>
            {
                float hull = GetResourceValue(agent.name, ResourceTypes.Hull);
                float fuel = GetResourceValue(agent.name, ResourceTypes.Fuel);

                if (hull <= 0 || fuel <= 0)
                {
                    if (agent is PlayerAgent)
                    {
                        End();
                        NotifyAllScreens(ObservableEffects.EncounterOverLost);
                    }
                    return;
                }
            });
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
        ForEachAgent(agent => { agent.Run(); });    
    }
}