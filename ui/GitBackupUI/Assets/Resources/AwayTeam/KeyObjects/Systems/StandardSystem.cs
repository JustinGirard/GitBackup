using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.VisualScripting;

[Serializable]
public class FloatRange
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
    public bool AbovePercent(float val)
    {
        if (val >  this.index / this.max )
            return true;
        return false;
    }
    public bool BelowPercent(float val)
    {
        if (val <  this.index / this.max )
            return true;
        return false;
    }
    public float Percent()
    {
        return  this.index / this.max ;
    }


    public void Set(float value)
    {
        index = Mathf.Clamp(value, min, max);
    }

    public void Add(float value)
    {
        Set(index + value);
    }

    public void Subtract(float value)
    {
        Set(index - value);
    }

    public void Multiply(float value)
    {
        Set(index * value);
    }

    public void Divide(float value)
    {
        if (value != 0)
            Set(index / value);
    }    
    // Static factory for clean initialization
    public static FloatRange Create(float min, float index, float max)
    {
        return new FloatRange(min, index, max);
    }    
}

/*
    public class StandardWait:StandardBehaviour
    {

    }

    public class StandardCharge:StandardBehaviour
    {

    }
    public class StandardExecute:StandardBehaviour
    {

    }
    public class StandardCool:StandardBehaviour
    {

    }
*/

public abstract class StandardSystem : MonoBehaviour
{
    public class StandardBehaviour
    {
        public StandardBehaviour(StandardSystem system)
        {
            this.system = system;
        }
        protected StandardSystem system;

        public virtual System.Collections.IEnumerator Activate(ATResourceData sourceResources) {
            //CoroutineRunner.Instance.DebugLog($"StandardBehaviour: Activate");
            yield break;
        }  

        public virtual System.Collections.IEnumerator Deactivate(ATResourceData sourceResources) {
            //CoroutineRunner.Instance.DebugLog($"StandardBehaviour: Deactivate");
            yield break;
        }  

        public virtual System.Collections.IEnumerator Execute( string sourceActionId,GameObject sourceUnit, List<GameObject> targetUnits, ATResourceData sourceResources, Agent sourceAgent)
        {
            //CoroutineRunner.Instance.DebugLog($"Error:StandardBehaviour: Execute");
            yield break;
        }           
        public virtual System.Collections.IEnumerator StateUpdate(float timeDelta)
        {            
            //CoroutineRunner.Instance.DebugLog($"StandardBehaviour: StateUpdate");
            yield break;
        }
    }

    public class StandardDeactivated:StandardBehaviour
    {   
        public StandardDeactivated(StandardSystem system): base(system){}

        public override System.Collections.IEnumerator Activate(ATResourceData sourceResources) 
        {
            if (system.GetLevel("cooldown").index >= 0.01f)
            {
                //CoroutineRunner.Instance.DebugLog("StandardWait: Still cooling down (cooldown)");
                yield break;
            }
            if (system.GetLevel("overheat").index >= 0.01f)
            {
                //CoroutineRunner.Instance.DebugLog("StandardWait: Still cooling down (overheat)");
                yield break;
            }
            
            //yield return  DoActivate(sourceResources);

            system.SetBehaviour("charging"); 
            //CoroutineRunner.Instance.DebugLog("Moving into charging");
            yield return system.Activate(sourceResources);
            yield break;
        }  

        public override System.Collections.IEnumerator Execute( string sourceActionId, GameObject sourceUnit,  List<GameObject> targetUnits, ATResourceData sourceResources,  Agent sourceAgent)
        {
            yield return Activate(sourceResources);
        }  

        public override System.Collections.IEnumerator StateUpdate(float timeDelta)
        {
            if (system.GetLevel("cooldown").index >= 0)
            {
                system.GetLevel("cooldown").Subtract( timeDelta);
            }
            if (system.GetLevel("overheat").index >= 0)
            {
                system.GetLevel("overheat").Subtract( timeDelta);
            }            
            yield break;
        }
    }

    public class StandardCharge:StandardBehaviour
    {
        public StandardCharge(StandardSystem system): base(system){}

        public override System.Collections.IEnumerator Deactivate(ATResourceData sourceResources) 
        {
            system.SetBehaviour("deactivated"); // If we can skip into charging, do so.
            CoroutineRunner.Instance.DebugLog("StandardCharge: Deactivated");
            yield break;
        }  

        public override System.Collections.IEnumerator StateUpdate(float timeDelta)
        {
            FloatRange chargeLevel = system.GetLevel("charge");
            if (chargeLevel.Percent() < 1.0f )
            {
                chargeLevel.Add( timeDelta);
                CoroutineRunner.Instance.DebugLog($"StandardCharge: Charging up {Time.time}");
            }            
            if (chargeLevel.Percent() > 0.99f)
            {
                CoroutineRunner.Instance.DebugLog($"StandardCharge: Moving to execute {Time.time}");
                system.SetBehaviour("executing");
            }
            yield break;
        }

    }

    public class StandardExecute:StandardBehaviour
    {
        public StandardExecute(StandardSystem system): base(system){}        
        public override System.Collections.IEnumerator Deactivate(ATResourceData sourceResources) 
        {
            system.SetBehaviour("deactivated"); // If we can skip into charging, do so.
            system.GetLevel("charge").Set(0);
            
            //CoroutineRunner.Instance.DebugLog("StandardExecute:Deactivated");
            yield break;
        }  
        public override System.Collections.IEnumerator StateUpdate(float timeDelta)
        {
            FloatRange overheatLevel = system.GetLevel("overheat");
            //CoroutineRunner.Instance.DebugLog("StandardExecute: Slowly Overheating?");
            if (overheatLevel.Percent() < 1.0f)
            {
                overheatLevel.Add( timeDelta);
                //CoroutineRunner.Instance.DebugLog("StandardExecute: Slowly Overheating");
            }            
            if (overheatLevel.Percent() > 0.99)
            {
                system.GetLevel("charge").Set(0);
                system.GetLevel("execute").Set(0);
                system.SetBehaviour("deactivated");
            }
            yield break;
        }        

        public override System.Collections.IEnumerator Execute( string sourceActionId,
                                            GameObject sourceUnit, 
                                            List<GameObject> targetUnits, 
                                            ATResourceData sourceResources, 
                                            Agent sourceAgent)
        {
            FloatRange overheatLevel = system.GetLevel("overheat");
            FloatRange executeLevel = system.GetLevel("execute");
            overheatLevel.Add( 0.25f);
            executeLevel.Add( 0.5f);
            //CoroutineRunner.Instance.DebugLog("StandardExecute:Executing ");

            if (executeLevel.Percent() > 0.99 || overheatLevel.Percent() > 0.99)
            {
                system.GetLevel("charge").Set(0);
                system.GetLevel("execute").Set(0);
                system.SetBehaviour("deactivated");
            }
            yield break;
        }   
    }

    public bool IsActive { get; private set; }

    [SerializeField] protected FloatRange chargeLevel = new FloatRange(min:0f,index:0f,max:5f);
    [SerializeField] protected FloatRange executeLevel = new FloatRange(min:0f,index:0f,max:5f);
    [SerializeField] protected FloatRange cooldownLevel  = new FloatRange(min:0f,index:0f,max:5f);
    [SerializeField] protected FloatRange heatLevel  = new FloatRange(min:0f,index:0f,max:5f);

    Dictionary<string, FloatRange> levels = null;
     [SerializeField] 
    protected string currentState = "deactivated";
    //ATResourceData internalResources = new ATResourceData();
    private GameEncounterBase spaceEncounter;
    
    [SerializeField]
    protected GameObject iconPrefab;
    protected Dictionary<string,StandardBehaviour> attachedStates;

    public GameObject GetIconPrefab(){
        return iconPrefab;
    }

    public virtual string GetShortDescriptionText()
    {
        return "unimplemented";
    }
    public void Awake(){
        attachedStates = new Dictionary<string,StandardBehaviour> {
            {"deactivated",new StandardDeactivated(this)},
            {"charging",new StandardCharge(this)},
            {"executing",new StandardExecute(this)}
        };
    }
    public virtual void Update(){
        float timeDelta = Time.deltaTime;
        //Debug.Log($"Doing Update ...{currentState}");
        StartCoroutine(GetBehaviour(currentState).StateUpdate(timeDelta));
    }

    public virtual FloatRange GetLevel(string levelName)
    {
        if (levels == null) levels = new Dictionary<string, FloatRange>
        {
            { "charge", chargeLevel },
            { "execute", executeLevel },
            { "cooldown", cooldownLevel },
            { "overheat", heatLevel }
        };        
        return levels[levelName];
    }
    public virtual void SetLevel(string levelName, float value)
    {
        if (levels == null) levels = new Dictionary<string, FloatRange>
        {
            { "charge", chargeLevel },
            { "execute", executeLevel },
            { "cooldown", cooldownLevel },
            { "overheat", heatLevel }
        };        
        levels[levelName].Set(value);
    }    

    public virtual List<string> GetValidStates()
    {
        return new List<string>(attachedStates.Keys);
    }
    public StandardBehaviour GetBehaviour(string stateId)
    {
        return attachedStates[stateId];
    }
    public void SetBehaviour(string stateId)
    {
         currentState = stateId;
    }    
    public virtual System.Collections.IEnumerator Activate(ATResourceData sourceResources)
    {
        yield return GetBehaviour(currentState).Activate(sourceResources);
    }  

    public virtual System.Collections.IEnumerator Deactivate(ATResourceData sourceResources)
    {
        yield return GetBehaviour(currentState).Deactivate(sourceResources);
    }  

    public virtual System.Collections.IEnumerator Execute(
                                string sourceActionId,
                                GameObject sourceUnit, 
                                List<GameObject> targetUnits, 
                                ATResourceData sourceResources,
                                Agent sourceAgent)
    {
        //Debug.Log("Root calling Execute");
          yield return GetBehaviour(currentState).Execute(
                                sourceActionId:sourceActionId,
                                sourceUnit:sourceUnit, 
                                targetUnits:targetUnits, 
                                sourceResources:sourceResources,
                                sourceAgent:sourceAgent);
    }

    public void SetEncounterManager(GameEncounterBase eman)
    {
        spaceEncounter = eman;
    }    

    public GameEncounterBase GetEncounterManager()
    {
        return spaceEncounter;
    }        
    
}

