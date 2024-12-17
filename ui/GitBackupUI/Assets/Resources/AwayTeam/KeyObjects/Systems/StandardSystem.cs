using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public struct FloatRange
{
    public float min;
    public float index;
    public float max;
    public FloatRange(float min, float index, float max)
    {
        this.min = min;
        this.index = index;
        this.max = max;
    }

    // Static factory for clean initialization
    public static FloatRange Create(float min, float index, float max)
    {
        return new FloatRange(min, index, max);
    }    
}

public abstract class StandardSystem : MonoBehaviour
{
    // Indicates whether this system is currently active and can perform actions.
    public bool IsActive { get; private set; }

    // A duration for how long the system remains active once triggered.
    [SerializeField]
    protected FloatRange powerupTime = new FloatRange(min:0f,index:0f,max:3f);
    [SerializeField]
    protected FloatRange runTime = new FloatRange(min:0f,index:0f,max:3f);
    [SerializeField]
    protected FloatRange cooldownTimeMax  = new FloatRange(min:0f,index:0f,max:0f);


    public virtual System.Collections.IEnumerator Execute(string sourceAgentId, 
                                string sourcePowerId, 
                                string targetAgentId, 
                                List<string> targetPowerIds, 
                                ATResourceData sourceResources,
                                ATResourceData targetResources)

    {
        yield break;
    }    
    private SpaceEncounterManager spaceEncounter;

    public void SetEncounterManager(SpaceEncounterManager eman){

        spaceEncounter = eman;
    }    

    public SpaceEncounterManager GetEncounterManager(){

        return spaceEncounter;
    }        
    
}

