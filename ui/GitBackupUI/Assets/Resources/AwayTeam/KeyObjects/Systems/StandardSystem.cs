using System.Collections.Generic;
using UnityEngine;

public abstract class AgentSystemBase : MonoBehaviour
{
    // Indicates whether this system is currently active and can perform actions.
    public bool IsActive { get; private set; }

    // A duration for how long the system remains active once triggered.
    [SerializeField]
    protected float activationDuration = 3f;

    // Tracks how much time has elapsed since the system was activated.
    protected float elapsedTime = 0f;

    // Each system might have internal stats or modifiers. For simplicity, 
    // we store them in a dictionary. They can be expanded or replaced as needed.
    protected Dictionary<string, float> stats = new Dictionary<string, float>();

    // Called to activate the system’s behavior. Resets timer and sets IsActive.
    public virtual void Activate(string [] sourceAgentIds,string [] targetAgentIds)
    {
        IsActive = true;
        elapsedTime = 0f;
        OnActivate(sourceAgentIds,targetAgentIds);
    }

    // Called every frame or time step to handle system logic while active.
    public virtual void UpdateSystem(float deltaTime)
    {
        if (!IsActive) return;

        elapsedTime += deltaTime;
        OnUpdate(deltaTime);

        // If the activation time is over, deactivate the system.
        if (elapsedTime >= activationDuration)
        {
            Deactivate();
        }
    }

    // Deactivate the system, stopping it from invoking further actions.
    public virtual void Deactivate()
    {
        IsActive = false;
        OnDeactivate();
    }

    // Hook for system-specific logic when activated
    protected virtual void OnActivate(string [] sourceAgentIds,string [] targetAgentIds) { }

    // Hook for system-specific logic updated every frame while active
    protected virtual void OnUpdate(float deltaTime) { }

    // Hook for system-specific logic when deactivated
    protected virtual void OnDeactivate() { }


    public virtual System.Collections.IEnumerator Execute(string sourceAgentId, 
                                string sourcePowerId, 
                                string targetAgentId, 
                                string targetPowerId, 
                                Dictionary<string, ATResourceData> agentResources)  
    {
        yield break;
    }    
    public SpaceEncounterManager spaceEncounter;

    //private U Should bind to target agent
    public void SetEncounterManager(SpaceEncounterManager eman){

        spaceEncounter = eman;
    }    
}

