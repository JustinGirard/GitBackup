
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using Unity.VisualScripting;
using UnityEngine.ProBuilder.MeshOperations;

public class NavigationSystem : StandardSystem
{
    // Additional stats specific to Attack system could be added here
    // For example: damage modifiers, ammo cost, etc.
    [SerializeField]
    private float ammoCost = 1f;
    [SerializeField]
    private float fuelCost = 1f;
    [SerializeField]
    private float baseDamage = 2f;

    // Last Args
    public float lastAdditionalForce;
    public GameObject lastSourceUnit;
    public List<GameObject> lastTargetUnits;
    public ATResourceData lastSourceResources;
    //public Agent lastSourceAgent;

    // private string system_id = AgentNavigationType.NavigateTo;
    public class NavigationDeactivated:StandardDeactivated
    {
        public NavigationDeactivated(StandardSystem system):base(system){}
        public override System.Collections.IEnumerator StateUpdate(float timeDelta)
        {
            yield return base.StateUpdate(timeDelta);
            yield return (system as NavigationSystem).SetUnitAimCursor( timeDelta);
            
        }
        public override System.Collections.IEnumerator Execute( string sourceActionId, GameObject sourceUnit,  List<GameObject> targetUnits, ATResourceData sourceResources, Agent sourceAgent)
        {
            yield return (system as NavigationSystem).SetUnitAimCursor( Time.deltaTime);
            yield return (system as NavigationSystem).TurnTowardsAgentsTarget(sourceAgent);
            yield return base.Execute( sourceActionId,  sourceUnit,  targetUnits,  sourceResources,  sourceAgent);
        }
    }

    public class NavigationCharge:StandardCharge
    {
        public NavigationCharge(StandardSystem system): base(system){}
        public override System.Collections.IEnumerator StateUpdate(float timeDelta)
        {
            yield return base.StateUpdate(timeDelta);
            yield return (system as NavigationSystem).SetUnitAimCursor( timeDelta);
        }
        public override System.Collections.IEnumerator Execute( string sourceActionId, GameObject sourceUnit,  List<GameObject> targetUnits, ATResourceData sourceResources, Agent sourceAgent)
        {
            yield return (system as NavigationSystem).SetUnitAimCursor( Time.deltaTime);
            yield return (system as NavigationSystem).TurnTowardsAgentsTarget(sourceAgent);
            yield return base.Execute( sourceActionId,  sourceUnit,  targetUnits,  sourceResources,  sourceAgent);
        }
    }

    public class NavigationExecute:StandardExecute
    {
        public NavigationExecute(StandardSystem system):base(system){}
        private bool isToggledOn = false;


        public override System.Collections.IEnumerator Execute( string sourceActionId, GameObject sourceUnit,  List<GameObject> targetUnits, ATResourceData sourceResources, Agent sourceAgent)
        {
            FloatRange overheatLevel = system.GetLevel("overheat");
            FloatRange executeLevel = system.GetLevel("execute");
            float additionalForce = 0f;

            if(sourceActionId == AgentNavigationType.NavigateToOn)
            {
                UINotificationWaterfall.Instance().Dispatch("basic", "nav_execution", $"NavExec.on: True", 5f, true);
                isToggledOn = true;
            }
            else
            {
                UINotificationWaterfall.Instance().Dispatch("basic", "nav_execution", $"NavExec.on: False", 5f, true);
                isToggledOn = false;
            }

            if (executeLevel.Percent() > 0.99 || overheatLevel.Percent() > 0.99)
            {
                //Debug.Log($"Reached Final state executeLevel:{executeLevel.Percent()}, overheatLevel:{overheatLevel.Percent()}");
                system.GetLevel("charge").Set(0);
                system.GetLevel("execute").Set(0);
                system.SetBehaviour("deactivated");
                yield break;
            }//
            NavigationSystem navSys = system as NavigationSystem;
            navSys.lastAdditionalForce = additionalForce;
            navSys.lastSourceUnit = sourceUnit; 
            navSys.lastTargetUnits = targetUnits; 
            navSys.lastSourceResources = sourceResources;  
            navSys.lastSourceAgent = sourceAgent;
            
            if (system is NavigationSystem b1) yield return b1.RunDashNav(  
                additionalForce:navSys.lastAdditionalForce,
                sourceUnit:navSys.lastSourceUnit, 
                targetUnits:navSys.lastTargetUnits, 
                sourceResources:navSys.lastSourceResources,  
                sourceAgent:navSys.lastSourceAgent);

            yield break;
        }  

        public override System.Collections.IEnumerator StateUpdate(float timeDelta)
        {
            
            UINotificationWaterfall.Instance().Dispatch("basic", "nav_execution_tick", $"NavExec.StateUpdate({timeDelta.ToString()})", 0.5f, true);
            FloatRange overheatLevel = system.GetLevel("overheat");
            //CoroutineRunner.Instance.DebugLog("StandardExecute: Slowly Overheating?");
            if (overheatLevel.Percent() > 0.99)
            {
                system.GetLevel("charge").Set(0);
                system.GetLevel("execute").Set(0);
                UINotificationWaterfall.Instance().Dispatch("basic", "nav_execution", $"NavExec.on: True", 5f, true);

                system.SetBehaviour("deactivated");
            }
            NavigationSystem navSys = system as NavigationSystem;
            if(navSys.lastSourceAgent == null)
            {
                UINotificationWaterfall.Instance().Dispatch("basic", "navSys.lastSourceAgent", $"WARNING: NavigationExecute.navSys.lastSourceAgent == null ({timeDelta.ToString()})", 30f, true);
                yield break;
            }
            //GameObject currentTarget = navSys.lastSourceAgent.GetPrimaryEnemyUnit();
            bool setNewTarget = false; 

            if(isToggledOn)
            {
                UINotificationWaterfall.Instance().Dispatch("basic", "nav_execution_tick", $"NavExec.isToggledOn({timeDelta.ToString()})", 0.5f, true);
                // Access the source unit, and ask for the latest enemy target. Adjust accordingly
               if (system is NavigationSystem b1) 
                    yield return b1.RunDashNav(  
                        additionalForce:navSys.lastAdditionalForce,
                        sourceUnit:navSys.lastSourceUnit, 
                        targetUnits:navSys.lastTargetUnits, 
                        sourceResources:navSys.lastSourceResources,  
                        sourceAgent:navSys.lastSourceAgent
                        );

                //overheatLevel.Add(timeDelta);
                
            }

            else if (overheatLevel.Percent() < 1.0f)
            {
                overheatLevel.Subtract( timeDelta);
                //CoroutineRunner.Instance.DebugLog("StandardExecute: Slowly Overheating");
            }            
        
            yield break;
        }        


    }       

    private new void Awake()
    {
        base.Awake();
        attachedStates["executing"] = new NavigationExecute(this);
        attachedStates["deactivated"] = new NavigationDeactivated(this);
        attachedStates["charging"] = new NavigationCharge(this);

        //
    }
   private Dictionary<string, GameObject> debugCubes = new Dictionary<string, GameObject>();    
    private void CreateDebugCube(Vector3 position, string id,Color cin)
    {
        // Check if a debug cube already exists for this ID
        if (debugCubes.TryGetValue(id, out GameObject cube))
        {
            // Move the existing cube to the new position
            cube.transform.position = position;
        }
        else
        {
            // Create a new cube
            cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.localScale =  new Vector3(1.2f,0.1f,0.1f);  // Scale down for better visibility
            cube.GetComponent<Renderer>().material.color = cin; // Make it red for easier identification
            cube.transform.position = position;
            cube.name = id;
            Destroy(cube.GetComponent<BoxCollider>());
            GameObject childCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            childCube.transform.localScale = new Vector3(1.2f, 0.1f, 0.1f);
            childCube.GetComponent<Renderer>().material.color = cin; // Same color for consistency
            childCube.transform.position = position; // Offset in Z direction
            childCube.transform.parent = cube.transform; // Make it a child
            childCube.name = id + "_Child";

            if (cube.TryGetComponent<BoxCollider>(out BoxCollider collider))
            {
                Destroy(collider); // Removes it completely
                // collider.enabled = false; // Alternative: Just disables it
            }            
            // Add the new cube to the dictionary
            debugCubes[id] = cube;
        }
    }
    public override string GetShortDescriptionText()
    {
        if (lastSourceAgent == null)
            return "[t]m:unknown";
        return (lastSourceAgent as PlayerAgent).GetPlayerNavigationMode();
    }
    string lastNavigationMode = "[t]m:unknown";
    private bool doEnemyAim = false;
    public Agent lastSourceAgent = null;

    public  System.Collections.IEnumerator SetUnitAimCursor(float timeDelta)
    {
        //Debug.Log("Reaiming");
        PlayerAgent parentAgent;
        parentAgent = gameObject.GetComponentInParent<PlayerAgent>();
            //Debug.Log("Re-aiming 3");
        if( parentAgent != null)
        {
            //Debug.Log("Re-aiming 2");
            if (parentAgent.GetPlayerNavigationMode() == PlayerNavigationMode.ZTargetingEnemy)
            {
                //Debug.Log("Re-aiming");
                if (parentAgent.GetPrimaryEnemyUnit())
                {
                    if (parentAgent.GetPrimaryAim().activeInHierarchy == false)
                        parentAgent.GetPrimaryAim().SetActive(true);
                    parentAgent.GetPrimaryAim().transform.position = parentAgent.GetPrimaryEnemyUnit().transform.position;
                    //sourceAgent.GetPrimaryAim().transform.parent   = sourceAgent.GetPrimaryEnemyUnit().transform; // **
                    parentAgent.GetPrimaryNavigation().transform.LookAt(parentAgent.GetPrimaryAim().transform);
                }
                else
                {
                    if (parentAgent.GetPrimaryAim().activeInHierarchy == true)
                        parentAgent.GetPrimaryAim().SetActive(false);

                }
            }        

        }
        yield break;
        
        //yield return CoroutineRunner.Instance.StartCoroutine(base.StateUpdate(timeDelta));
    }
    [SerializeField] float heldCursorTranslationSpeed = 0.5f;
    [SerializeField] float goalSeekingSmoothTime = 0.1f;
    [SerializeField] float goalSeekingRotationSpeed = 5.0f;
    /*    private System.Collections.IEnumerator ConvergeOnTargetHelper(Transform source, Transform target, float smoothTime, float updateDelay)
        {
            Vector3 velocity = Vector3.zero;

            while (target != null && source != null)
            {
                // Smoothly move towards the target position
                source.position = Vector3.SmoothDamp(
                    source.position, 
                    target.position, 
                    ref velocity, 
                    smoothTime
                );

                yield return new WaitForSeconds(updateDelay); // Small delay for smoother updates
            }
        }*/
        private int convergeExecutingToken = 0; // Unique token to track execution
        private float convergeStopDistance = 0.05f; // Distance threshold for stopping
        private float convergeMaxDuration = 2f; // Max duration before auto-exit

        private System.Collections.IEnumerator ConvergeOnTargetHelper(Transform source, Transform target, float smoothTime, float updateDelay)
        {
            int executionToken = Random.Range(int.MinValue, int.MaxValue); // Generate unique ID
            convergeExecutingToken = executionToken; // Store execution token
            
            Vector3 velocity = Vector3.zero;
            float elapsedTime = 0f;

            while (target != null && source != null)
            {
                // If another coroutine has started, exit immediately
                if (convergeExecutingToken != executionToken)
                {
                    yield break;
                }

                float distance = Vector3.Distance(source.position, target.position);
                if (distance < convergeStopDistance || elapsedTime > convergeMaxDuration)
                {
                    yield break; // Exit if close enough or timeout reached
                }

                // Smoothly move towards the target position
                source.position = Vector3.SmoothDamp(
                    source.position, 
                    target.position, 
                    ref velocity, 
                    smoothTime
                );

                elapsedTime += updateDelay;
                yield return new WaitForSeconds(updateDelay); // Small delay for smoother updates
            }
        }



    public  System.Collections.IEnumerator RunDashNav(
                                float additionalForce,
                                GameObject sourceUnit, 
                                List<GameObject> targetUnits, 
                                ATResourceData sourceResources,
                                Agent sourceAgent)
    {
        if (targetUnits.Count <= 0)
        {
            Debug.LogError("No cursor linked");
            yield break;
        }
        
        ISurfaceNavigationCommand cursor = targetUnits[0].GetComponent<ISurfaceNavigationCommand>();
        if (cursor == null)
        {
            Debug.LogError("cursor is null.");
            yield break;
        }
        if (cursor.GetTarget() == null)
        {
            Debug.LogError("cursor.gameObject is null.");
            yield break;
        }
        EncounterSquad sourceSquad = sourceUnit.GetComponentInParent<EncounterSquad>();
        
        //
        // SET NAVIGATION TARGET
        //
        Transform selectedT;
        if (cursor.GetTarget() == null)
        {
            Debug.LogError("No active cursor target");
            yield break;
        }

        if (sourceAgent is PlayerAgent)
        {
            lastSourceAgent = sourceAgent as PlayerAgent;
        }


        // 1. Set Position
        selectedT = cursor.GetTarget().transform;
        //sourceAgent.GetPrimaryNavigation().transform.position = selectedT.position;


        CreateDebugCube( sourceAgent.GetPrimaryNavigation().transform.position, $"NavigationSystemGoal-{this.gameObject.name}", Color.red);
        
        // 2.a Set Aim (At Enemy) 
        if ((lastSourceAgent as PlayerAgent).GetPlayerNavigationMode() == PlayerNavigationMode.ZTargetingEnemy)
        {
            //yield return StartCoroutine(ConvergeOnTargetHelper(
            //    sourceAgent.GetPrimaryNavigation().transform,  // Source transform
            //    selectedT,  // Target transform
            //    0.1f,  // SmoothTime (tweakable)
            //    0.02f  // Update delay for better performance
            //));
            sourceAgent.GetPrimaryNavigation().transform.position = selectedT.position;
            yield return TurnTowardsAgentsTarget(sourceAgent);
        }
        // 2.b Set Aim (at Command Direction)
        else
        {
            sourceAgent.GetPrimaryNavigation().transform.position = Vector3.Lerp(
                                sourceAgent.GetPrimaryNavigation().transform.position , 
                                selectedT.position, 
                                Time.deltaTime*heldCursorTranslationSpeed);

            if (sourceAgent.GetPrimaryAim().activeInHierarchy == false)
                sourceAgent.GetPrimaryAim().SetActive(true);

            Vector3 direc =  (sourceAgent.GetPrimaryNavigation().transform.position - this.transform.position).normalized;
            direc.y = 0;
            direc = direc.normalized;
            //sourceAgent.GetPrimaryAim().transform.position = sourceAgent.GetPrimaryNavigation().transform.position + direc*1.0f;

            sourceAgent.GetPrimaryAim().transform.position = Vector3.Lerp(
                            sourceAgent.GetPrimaryAim().transform.position, 
                            selectedT.position, 
                            Time.deltaTime*1f);

         //sourceAgent.GetPrimaryAim().transform.position  = Vector3.SmoothDamp(
          //sourceAgent.GetPrimaryAim().transform.position, aimGoalPosition, ref aimGoalVelocity, aimGoalSmoothTime);                            

            //sourceAgent.GetPrimaryAim().transform.parent   = sourceAgent.GetPrimaryNavigation().transform;
            sourceAgent.GetPrimaryNavigation().transform.LookAt(sourceAgent.GetPrimaryAim().transform);
            sourceSquad.SetGoalPosition(sourceAgent.GetPrimaryNavigation().transform,goalSeekingSmoothTime,goalSeekingRotationSpeed);
            //if (additionalForce > 0.1f)
            //{
            //    Debug.Log("Additional Force!");
            //    Rigidbody ship = this.transform.GetComponent<Rigidbody>();
            //    ship.AddForce(direc*additionalForce,ForceMode.Acceleration);

            //}
            //sourceSquad.SetGoalTarget(sourceAgent.GetPrimaryAim().transform,Vector3.zero);

        }
        yield break;
    }
    public System.Collections.IEnumerator TurnTowardsAgentsTarget(Agent sourceAgent)
    {
            if (sourceAgent.GetPrimaryEnemyUnit() == null)
            {
                if (sourceAgent.GetPrimaryAim().activeInHierarchy == true)
                    sourceAgent.GetPrimaryAim().SetActive(false);
                yield break;
            }
            if (sourceAgent.GetPrimaryAim().activeInHierarchy == false)
                sourceAgent.GetPrimaryAim().SetActive(true);
            sourceAgent.GetPrimaryAim().transform.position = sourceAgent.GetPrimaryEnemyUnit().transform.position;
            //sourceAgent.GetPrimaryAim().transform.parent   = sourceAgent.GetPrimaryEnemyUnit().transform; // **
            sourceAgent.GetPrimaryNavigation().transform.LookAt(sourceAgent.GetPrimaryAim().transform);        
            
            EncounterSquad sourceSquad = sourceAgent.GetComponentInChildren<EncounterSquad>();

            sourceSquad.SetGoalPosition(
                sourceAgent.GetPrimaryNavigation().transform,
                goalSeekingSmoothTime,
                goalSeekingRotationSpeed
                );

            yield break;
    }
    public override void Update(){
        //Debug.Log($"Nav sys running {currentState}");
        base.Update();
        //float timeDelta = Time.deltaTime;
        //Debug.Log($"Doing Update ...{currentState}");
        //StartCoroutine(GetBehaviour(currentState).StateUpdate(timeDelta));
    }    
}
