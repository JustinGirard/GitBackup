using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using Unity.VisualScripting;
using UnityEngine;
using VisualCommand;

public class CombatNavAnchor : MonoBehaviour, ICommandReceiver
{
    [SerializeField] private bool useMainCamera = false;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Agent primaryAgent; // The primary agent (e.g., player's side)
    [SerializeField] private Vector2 targetOffset = new Vector2(10f, 5f); // Look_at_distance and y-height
    [SerializeField] private DynamicCameraGroupManager cameraGroup; // The target agent (e.g., enemy side)

    private Vector3 __navigationPosition; // The position to lerp towards
    private Vector3 __lookAtCenter;
    private Coroutine updateCoroutine; // Coroutine for periodic updates
    [SerializeField] private float updateInterval = 0.05f; // Update interval (seconds)
    private SpaceCombatScreen __targetScreen;
    // Properties to expose in the inspector
    public Agent PrimaryAgent { get => primaryAgent; set => primaryAgent = value; }
    [SerializeField] public float translationSpeed = 5f;
    [SerializeField] public float rotationSpeed = 5f;
     

    public string GetNavigationMode()
    {
        if(primaryAgent is PlayerAgent)
        {
            return (primaryAgent as PlayerAgent).GetPlayerNavigationMode();

        }
        return null;
    }

    public GameObject GetGameObject()
    {
        return this.gameObject;
    }
    
    void Start()
    {
        SetActiveNavPlane(__quickNavSurfaceMedium);
        GeneralInputManager.Instance().RegisterObserver(this);
        __targetScreen = SpaceCombatScreen.Instance();
        if (primaryAgent == null)
        {
            Debug.LogError("PrimaryAgent is not assigned!");
        }


    }

    //private IEnumerator UpdateAnchorPosition()
    //{
    //    while (true)
    //    {
    //        UpdateAnchor();
    //        yield return new WaitForSeconds(updateInterval);
    //    }
   // }

   public enum AimMode
    {
        UnitCenter,   // Aim at unit center
        AgentNavigationTarget,     // Aim at unit head
    }
    [SerializeField] private AimMode cAimAtUnitCenter = AimMode.AgentNavigationTarget;

    private void UpdateAnchor()
    {
        Agent targetAgent = primaryAgent.GetPrimaryEnemyAgent();
        Vector3 primaryCenter = CalculateAgentCenter(primaryAgent);
        if (targetAgent == null)
        {
            if(__currentNavPlane.activeInHierarchy == true)
                __currentNavPlane.SetActive(true);
            // a: Find where we are to be
            __lookAtCenter = primaryAgent.GetPrimaryAim().transform.position;
            __lookAtCenter.y = primaryCenter.y;

            // b: Lerp Target for the Nav Anchor
            Vector3 agentOffset = __lookAtCenter - primaryCenter;
            agentOffset = agentOffset.normalized;
            __navigationPosition = primaryCenter + agentOffset* targetOffset.x; // Move toward the 
            
            // c: Set Look Target for Nav Anchor
            __lookAtCenter.y = this.transform.position.y; 
            return;
        }
        else // HAS TARGET AGENT targetAgent :
        {
            if(__currentNavPlane.activeInHierarchy == false)
                __currentNavPlane.SetActive(true);

            Vector3 targetCenter = CalculateAgentCenter(targetAgent);
            (visualCursor.GetComponent<ISurfaceNavigationCommand>() as BasicNavCursor).UpdateDirectionIndicator( primaryCenter);        

            primaryCenter.y = targetCenter.y;
            Vector3 agentOffset = primaryCenter - targetCenter;
            agentOffset = agentOffset.normalized;
            __navigationPosition = targetCenter + agentOffset* targetOffset.x; // Move toward the 
            targetCenter.y = this.transform.position.y;  //######## MOVE THIS TODO
            
            __lookAtCenter = targetCenter;
        }
    }

    private Vector3 CalculateAgentCenter(Agent agent)
    {

        if (agent == null) return Vector3.zero;
        if(cAimAtUnitCenter == AimMode.UnitCenter)
        {
            Debug.LogError("Should be disabled");
            //primaryCenter = CalculateAgentCenter(primaryAgent);
        }
        if(cAimAtUnitCenter == AimMode.AgentNavigationTarget && agent.GetPrimaryNavigation() != null)
        {
            return agent.GetPrimaryNavigation().transform.position;
        }

        Vector3 center = Vector3.zero;
        int unitCount = 0;

        EncounterSquad squad = agent.GetUnit()?.GetComponent<EncounterSquad>();
        if (squad != null)
        {
            List<SimpleShipController> units = squad.GetUnitList();
            foreach (var unit in units)
            {
                if (unit != null && unit.gameObject.activeInHierarchy)
                {
                    center += unit.transform.position;
                    unitCount++;
                }
            }
        }

        return unitCount > 0 ? center / unitCount : Vector3.zero;
    }
    Vector3 selfVelocity = Vector3.zero;
    void Update()
    {
        UpdateAnchor();
        // Smoothly  the anchor position towards the calculated position
        transform.position = Vector3.SmoothDamp(transform.position, __navigationPosition,ref selfVelocity, Time.deltaTime*translationSpeed);
        // Look At __lookAtCenter with smooth damp
          //this.transform.LookAt(__lookAtCenter,Vector3.up); //######## MOVE THIS TODO
        Quaternion targetRotation = Quaternion.LookRotation(__lookAtCenter - transform.position, Vector3.up);
            
        // Smoothly interpolate using Slerp (rotation smoothing)
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        
        
        if(__doCursorUpdate == true)
        {
            UpdateNavCursorPosition(doHalt:false);
        }
        //if(GetNavigationMode() == PlayerNavigationMode.TopDownTraditional)
        //{
        //    ProcessTopDownNav();
        //}

    }

    void OnDestroy()
    {
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
        }
    }

    private static readonly HashSet<string> __allowedCommands = new HashSet<string>
    {
        GeneralInputManager.Command.nav_up_down,
        GeneralInputManager.Command.nav_down_down,
        GeneralInputManager.Command.nav_left_down,
        GeneralInputManager.Command.nav_right_down,
        GeneralInputManager.Command.nav_up_up,
        GeneralInputManager.Command.nav_down_up,
        GeneralInputManager.Command.nav_left_up,
        GeneralInputManager.Command.nav_right_up
    };

    Dictionary<string, bool> isPressed = new Dictionary<string, bool>
    {
        { GeneralInputManager.Command.nav_up_down, false },    // Up
        { GeneralInputManager.Command.nav_down_down, false }, // Down
        { GeneralInputManager.Command.nav_left_down, false}, // Left
        { GeneralInputManager.Command.nav_right_down, false }  // Right
    };

    private void UpdatePressed(string commandId)
    {
        if (commandId.EndsWith("_down") && isPressed.ContainsKey(commandId))
        {
            isPressed[commandId] = true;
            
        }
        else if (commandId.EndsWith("_up"))
        {
            string correspondingDownCommand = commandId.EndsWith("_up") 
            ? commandId.Substring(0, commandId.Length - 3) + "_down" 
            : commandId;
            if (isPressed.ContainsKey(correspondingDownCommand))
            {
                isPressed[correspondingDownCommand] = false;
            }
        }
    }
    private bool SomeNavPressed()
    {
        foreach (var entry in isPressed)
        {
            if (entry.Value &&downCommands.Contains(entry.Key))
            {
                return true;
            }
        }          
        return false;
    }
    private bool IsPressed(string commandId)
    {
        if (isPressed.ContainsKey(commandId))
        {
            return isPressed[commandId];
        }
        return false;
    }

    List<string> downCommands = new List<string>
    {
        GeneralInputManager.Command.nav_up_down,
        GeneralInputManager.Command.nav_down_down,
        GeneralInputManager.Command.nav_left_down,
        GeneralInputManager.Command.nav_right_down
    };

    string __navMode = "hold"; // "tap"
    //string __navMode = "tap"; 
    private void ProcessTopDownNav(string commandId)
    {
        //Debug.Log($"processing Top Down Nav {commandId}");
        bool doHalt = false;
        bool doRun = false;
        if (commandId ==  GeneralInputManager.Command.nav_up_down ||
            commandId ==  GeneralInputManager.Command.nav_down_down ||
            commandId ==  GeneralInputManager.Command.nav_left_down ||
            commandId ==  GeneralInputManager.Command.nav_right_down )
        {
            doHalt = false;
            doRun = true;
        }
        if (commandId ==  GeneralInputManager.Command.nav_up_up ||
            commandId ==  GeneralInputManager.Command.nav_down_up ||
            commandId ==  GeneralInputManager.Command.nav_left_up ||
            commandId ==  GeneralInputManager.Command.nav_right_up )
        {
            if(!SomeNavPressed())
            {
                //Debug.Log("SomeNavPressed says halt");
                doHalt = true;
            } 
            else
            {
                //Debug.Log("SomeNavPressed says continue");
                doHalt = false;
            }
            doRun = true;
        }
        if (!doRun)
            return;
        //Agent targetAgent = primaryAgent.GetPrimaryEnemyAgent();
        cameraGroup.ClearAgents();
        //Debug.Log($"Running with doHalt {doHalt}");
        UpdateNavCursorPosition(doHalt);

        // issue command
        ISurfaceNavigationCommand visualCursorCommand = visualCursor.GetComponent<ISurfaceNavigationCommand>();
        visualCursorCommand.SetVisualState(SurfaceNavigationCommand.SelectionState.active);         
        if (__navMode   == "tap")
        {
            __doCursorUpdate = false;
            __targetScreen.SetPlayerNavigation(visualCursorCommand, AgentNavigationType.NavigateToOnce);
        }
        if (__navMode   == "hold" && doHalt == true)
        {
            __doCursorUpdate = false;
            __targetScreen.SetPlayerNavigation(visualCursorCommand, AgentNavigationType.NavigateToOff);

        }
        if (__navMode   == "hold" && doHalt == false)
        {
            __doCursorUpdate = true;
            __targetScreen.SetPlayerNavigation(visualCursorCommand, AgentNavigationType.NavigateToOn);

        }
        
    }
    bool __doCursorUpdate = false; // Should we keep updating the cursor in our update function, to follow the cursor?
    [SerializeField] float cfgVisualCursorSmoothSpeed = 1f;
    private void UpdateNavCursorPosition(bool doHalt)
    {
        //Debug.Log("Processing Top Down");

        Vector3 primaryCenter = CalculateAgentCenter(primaryAgent);
        primaryCenter.y = __currentNavPlane.transform.position.y;
        Vector3 navCommandPoint = new Vector3(primaryCenter.x,primaryCenter.y,primaryCenter.z);
        Vector3 forwardDir;
        Vector3 rightDir;

        if (useMainCamera == false)
        {
            forwardDir = mainCamera.transform.rotation * Vector3.forward;
            rightDir = mainCamera.transform.rotation * Vector3.right;
        }
        else
        {
            forwardDir = Vector3.forward;
            rightDir = Vector3.right;
        }

        forwardDir.y = 0f; // Ensure movement stays on the plane
        rightDir.y = 0f;
        forwardDir.Normalize();
        rightDir.Normalize();

        Dictionary<string, Vector3> commandToVectorMap = new Dictionary<string, Vector3>
        {
            { GeneralInputManager.Command.nav_up_down, forwardDir },    // Up
            { GeneralInputManager.Command.nav_down_down,-1f*forwardDir }, // Down
            { GeneralInputManager.Command.nav_left_down, -1f*rightDir}, // Left
            { GeneralInputManager.Command.nav_right_down, rightDir }  // Right
        };
        if (IsPressed( GeneralInputManager.Command.nav_up_down) ||
            IsPressed(  GeneralInputManager.Command.nav_down_down) ||
            IsPressed( GeneralInputManager.Command.nav_left_down) ||
            IsPressed(  GeneralInputManager.Command.nav_right_down) || doHalt
            )
        {
            Vector3 navState = Vector3.zero;
            foreach (var entry in isPressed)
            {
                if (entry.Value && commandToVectorMap.TryGetValue(entry.Key, out Vector3 direction))
                {
                    navState += direction;
                }
            }            
            if (doHalt == true)
            {
                //cursorTopdownStepSize = 0.1f;
                navState = (visualCursor.transform.position - primaryCenter);
                navState.y = 0;
                navState = navState.normalized * 1.0f; // Just use the last cursor position

                navState = navState.normalized * cursorTopdownStepSize;
                navCommandPoint = primaryCenter + navState;
                CreateDebugCube(navCommandPoint, "recorded_point", Color.green);

                // visualCursor.transform.position = navCommandPoint;
                
                visualCursor.transform.position = Vector3.SmoothDamp(
                                visualCursor.transform.position, 
                                navCommandPoint, 
                                ref navCommandPointVelocity,
                                Time.deltaTime*cfgVisualCursorSmoothSpeed);
            }
            else
            {
                navState = navState.normalized * cursorTopdownStepSize;
                navCommandPoint = primaryCenter + navState;
                CreateDebugCube(navCommandPoint, "recorded_point", Color.green);

                // visualCursor.transform.position = navCommandPoint;
                
                visualCursor.transform.position = Vector3.SmoothDamp(
                                visualCursor.transform.position, 
                                navCommandPoint, 
                                ref navCommandPointVelocity,
                                Time.deltaTime*cfgVisualCursorSmoothSpeed);

            }

            
            ISurfaceNavigationCommand visualCursorCommand = visualCursor.GetComponent<ISurfaceNavigationCommand>();
            visualCursorCommand.SetVisualState(SurfaceNavigationCommand.SelectionState.active);
            

            navState = Vector3.zero;
        }


    }
    [SerializeField]    float cursorTopdownStepSize = 5.0f; // Adjust movement scale as needed
    Vector3 navCommandPointVelocity = Vector3.zero;
    //private void ProcessTopDownDash(string commandId)
    //{
    //    if (commandId == GeneralInputManager.Command.dash_up)
    //    {
    //        ISurfaceNavigationCommand visualCursorCommand = visualCursor.GetComponent<ISurfaceNavigationCommand>();
    //        visualCursorCommand.SetVisualState(SurfaceNavigationCommand.SelectionState.active);
    //        //__targetScreen.HandleContinuousNavigationEvent(visualCursorCommand);
    //        __targetScreen.HandleDashNavigationEvent(visualCursorCommand);
    //
    //    }
    //}

    /*private void ProcessTopDownDash(string commandId)
    {
        if (!__allowedCommands.Contains(commandId))
            return;        
        //Debug.Log("Processing Top Down");
        Vector3 primaryCenter = CalculateAgentCenter(primaryAgent);
        primaryCenter.y = __currentNavPlane.transform.position.y;
        Vector3 navCommandPoint = new Vector3(primaryCenter.x,primaryCenter.y,primaryCenter.z);

        Vector3 forwardDir = mainCamera.transform.rotation * Vector3.forward;
        Vector3 rightDir = mainCamera.transform.rotation * Vector3.right;

        forwardDir.y = 0f; // Ensure movement stays on the plane
        rightDir.y = 0f;
        forwardDir.Normalize();
        rightDir.Normalize();
        float navDistance = 8.0f; // Adjust movement scale as needed

        Dictionary<string, Vector3> commandToVectorMap = new Dictionary<string, Vector3>
        {
            { GeneralInputManager.Command.nav_up_down, forwardDir },    // Up
            { GeneralInputManager.Command.nav_down_down,-1f*forwardDir }, // Down
            { GeneralInputManager.Command.nav_left_down, -1f*rightDir}, // Left
            { GeneralInputManager.Command.nav_right_down, rightDir }  // Right
        };
        if (commandToVectorMap.TryGetValue(commandId, out Vector3 adjustment))
        {
            // KeyDown: Update navigation state
            running = true;
            navStateDash += adjustment;
            //Debug.Log(navState);
        }
        else if (commandId == GeneralInputManager.Command.nav_up_up ||
                commandId == GeneralInputManager.Command.nav_down_up ||
                commandId == GeneralInputManager.Command.nav_left_up ||
                commandId == GeneralInputManager.Command.nav_right_up)
        {
            if (running == true)
            {
                navStateDash = navStateDash.normalized * navDistance;
                navCommandPoint = primaryCenter + navStateDash;

                CreateDebugCube(navCommandPoint, "recorded_point", Color.green);

                visualCursor.transform.position = navCommandPoint;
                ISurfaceNavigationCommand visualCursorCommand = visualCursor.GetComponent<ISurfaceNavigationCommand>();
                visualCursorCommand.SetVisualState(SurfaceNavigationCommand.SelectionState.active);
                __targetScreen.HandleNavigationEvent(visualCursorCommand);
                navStateDash = Vector3.zero;
                running = false;

            }
        }

    }*/


    private Vector3 navStateDash = Vector3.zero; // Tracks the current navigation vector
    private bool running = false;
    
    private void ProcessZTargeting(string commandId)
    {
        //Debug.Log("Running z");
        Agent targetAgent = primaryAgent.GetPrimaryEnemyAgent();
        if (targetAgent != null)
            cameraGroup.AddAgent(targetAgent);
        else
        {
            PlayerAgent playerAgent = (primaryAgent as PlayerAgent);
            if(playerAgent) playerAgent.TogglePlayerNavigationMode();
            ProcessTopDownNav(commandId);
        }
        
        // mainCamera //
        Dictionary<string, Vector3> commandToVectorMap = new Dictionary<string, Vector3>
        {
            { GeneralInputManager.Command.nav_up_down, new Vector3(0,0, 1 )},    // Up
            { GeneralInputManager.Command.nav_down_down, new Vector3(0,0,-1) }, // Down
            { GeneralInputManager.Command.nav_left_down, new Vector3(-1,0,0) }, // Left
            { GeneralInputManager.Command.nav_right_down, new Vector3(1,0,0) }  // Right
        };

        //Debug.Log(__quickNavSurfaceLow);
        if (__currentNavPlane == null)
        {
            Debug.LogWarning("QuickNav low plane is not assigned."); 
            return;
        }

        // Step 1: Update navigation state or save recorded vector
        if (commandToVectorMap.TryGetValue(commandId, out Vector3 adjustment))
        {
            // KeyDown: Update navigation state
            running = true;
            navStateDash += adjustment;
            //Debug.Log(navState);
        }
        else if (commandId == GeneralInputManager.Command.nav_up_up ||
                commandId == GeneralInputManager.Command.nav_down_up ||
                commandId == GeneralInputManager.Command.nav_left_up ||
                commandId == GeneralInputManager.Command.nav_right_up ||
                commandId == GeneralInputManager.Command.player_aim_cycle_up 
                )
        {
            // KeyUp: Save the recorded vector and reset state
            if (running == true)
            {
                Debug.Log($"Should set state active!{navStateDash}");
                SaveRecordedPoint(navStateDash);
                ISurfaceNavigationCommand visualCursorCommand = visualCursor.GetComponent<ISurfaceNavigationCommand>();
                visualCursorCommand.SetVisualState(SurfaceNavigationCommand.SelectionState.active);
                __targetScreen.SetPlayerNavigation(visualCursorCommand,AgentNavigationType.NavigateToOnce);
                navStateDash = Vector2.zero;
                running = false;
            }
            else if (commandId == GeneralInputManager.Command.player_aim_cycle_up )
            {
                //SaveRecordedPoint(navStateDash);
                visualCursor.transform.position = CalculateAgentCenter(primaryAgent);
                ISurfaceNavigationCommand visualCursorCommand = visualCursor.GetComponent<ISurfaceNavigationCommand>();
                visualCursorCommand.SetVisualState(SurfaceNavigationCommand.SelectionState.active);
                __targetScreen.SetPlayerNavigation(visualCursorCommand,AgentNavigationType.NavigateToOnce);
                navStateDash = Vector2.zero;
                running = false;

            }
        }

    }
    GameObject debugCube = null;
    // Save the recorded navigation point
    private void SaveRecordedPoint(Vector3 navVector)
    {
        // Calculate and save the recorded point based on navVector
        Renderer renderer = __currentNavPlane.GetComponent<Renderer>();

        if (renderer == null)
        {
            Debug.LogError("__currentNavPlane does not have a Renderer component.");
            return;
        }

        Bounds bounds = renderer.bounds;

        Vector3 planeRight = __currentNavPlane.transform.right; // Plane's local X-axis
        Vector3 planeForward = __currentNavPlane.transform.forward; // Plane's local Z-axis
        Vector3 planeCenter = __currentNavPlane.transform.position; // Plane's world position            
        MeshFilter meshFilter = __currentNavPlane.GetComponent<MeshFilter>();            
        Vector3 meshSize = meshFilter.mesh.bounds.size;
        Vector3 worldSize = Vector3.Scale(meshSize, __currentNavPlane.transform.localScale);

        
        Vector3 cameraForwardDir;
        Vector3 cameraRightDir;
        if (useMainCamera == false)
        {
            cameraForwardDir = mainCamera.transform.rotation * Vector3.forward;
            cameraRightDir = mainCamera.transform.rotation * Vector3.right;
        }
        else
        {
            cameraForwardDir = Vector3.forward;
            cameraRightDir = Vector3.right;
        }        

        cameraForwardDir.y = 0f; // Ensure movement stays on the plane
        cameraRightDir.y = 0f;
        cameraForwardDir.Normalize();
        cameraRightDir.Normalize();
        

        
        navVector = AdjustNavVectorForPlane( navVector,  cameraForwardDir,  planeForward,  planeRight);

        // Calculate the recorded point using the actual dimensions along local axes
        Vector3 recordedPoint = planeCenter
            + planeRight * Mathf.Lerp(-worldSize.x * 0.5f, worldSize.x * 0.5f, (navVector.x + 1) / 2f) // Map [-1, 1] to world size
            + planeForward * Mathf.Lerp(-worldSize.z * 0.5f, worldSize.z * 0.5f, (navVector.z + 1) / 2f); // Map [-1, 1] to world size

        // Debug the recorded point
        CreateDebugCube(recordedPoint, "recorded_point", Color.green);

        visualCursor.transform.position = recordedPoint;




    }
private Vector3 AdjustNavVectorForPlaneEnemyFocus(Vector3 navVector, Vector3 cameraForwardDir, Vector3 planeForward, Vector3 planeRight)
{
    // Project camera's forward direction onto the plane's coordinate system
    float dotForward = Vector3.Dot(cameraForwardDir, planeForward);
    float dotRight = Vector3.Dot(cameraForwardDir, planeRight);

    // Compute angle relative to the plane
    float angle = Mathf.Atan2(dotRight, dotForward) * Mathf.Rad2Deg;

    // Normalize angle to [-180, 180] range
    angle = Mathf.Repeat(angle + 180f, 360f) - 180f;

    // Reverse navVector if camera orientation is beyond 180 degrees
    return (Mathf.Abs(angle) > 90f) ? -navVector : navVector;
}
private Vector3 AdjustNavVectorForPlane(Vector3 navVector, Vector3 cameraForwardDir, Vector3 planeForward, Vector3 planeRight)
{
    // Project camera's forward direction onto the plane's coordinate system
    float dotForward = Vector3.Dot(cameraForwardDir, planeForward);
    float dotRight = Vector3.Dot(cameraForwardDir, planeRight);

    // Compute angle relative to the plane
    float angle = Mathf.Atan2(dotRight, dotForward) * Mathf.Rad2Deg;

    // Normalize to [0, 360] range
    angle = Mathf.Repeat(angle, 360f);

    // Determine the quadrant and adjust mapping accordingly
    if (angle >= 315f || angle < 45f)
    {
        // Camera facing similar to plane forward (0°)
        return new Vector3(navVector.x, 0f, navVector.z);
    }
    else if (angle >= 45f && angle < 135f)
    {
        // Camera rotated 90° to the right
        return new Vector3(navVector.z, 0f, -navVector.x);
    }
    else if (angle >= 135f && angle < 225f)
    {
        // Camera facing opposite to plane forward (180°)
        return new Vector3(-navVector.x, 0f, -navVector.z);
    }
    else
    {
        // Camera rotated 90° to the left
        return new Vector3(-navVector.z, 0f, navVector.x);
    }
}

    /*
    private Vector3 AdjustNavVectorForPlane(Vector3 navVector, Vector3 cameraForwardDir, Vector3 cameraRightDir)
    {
        // Determine the dominant axis of the camera's forward direction
        float angle = Mathf.Atan2(cameraForwardDir.x, cameraForwardDir.z) * Mathf.Rad2Deg;

        // Normalize the angle to 0-360 range
        angle = Mathf.Repeat(angle, 360f);

        // Determine the quadrant (each covering ~90 degrees)
        if (angle >= 315 || angle < 45)
        {
            // Aligned with Z+ (default)
            return new Vector3(navVector.x, 0f, navVector.z);
        }
        else if (angle >= 45 && angle < 135)
        {
            // Aligned with X+
            return new Vector3(navVector.z, 0f, -navVector.x);
        }
        else if (angle >= 135 && angle < 225)
        {
            // Aligned with Z-
            return new Vector3(-navVector.x, 0f, -navVector.z);
        }
        else
        {
            // Aligned with X-
            return new Vector3(-navVector.z, 0f, navVector.x);
        }
    }*/

    [SerializeField]
    private GameObject visualCursor;

    public void OnCommandReceived(string commandId, Vector2 mousePosition)
    {
        
        PlayerAgent playerAgent = (primaryAgent as PlayerAgent);
        
        if( playerAgent != null && GeneralInputManager.Command.player_control_mode_up == commandId)
        {
            playerAgent.TogglePlayerNavigationMode();
            UINotificationWaterfall.Instance().Dispatch("basic", "player_control_mode", $"nav_mode: {playerAgent.GetPlayerNavigationMode()}", 10f, true); 

        }
        if( playerAgent != null && GeneralInputManager.Command.player_aim_cycle_up == commandId)
        {
            playerAgent.BlindToggleEnemyAgent();
            Agent target =  playerAgent.GetPrimaryEnemyAgent();
            if (target != null) // IF  HAVE AGENT
            {
                playerAgent.SetPlayerNavigationMode(PlayerNavigationMode.ZTargetingEnemy);
                UINotificationWaterfall.Instance().Dispatch("basic", "player_aim_cycle_up", $"enemy_target: {target.gameObject.name}", 10f, true); 
                //playerAgent.AddCommand( AgentCommand.Type.Navigation,  AgentNavigationType.NavigateAim, target.gameObject);
                //ISurfaceNavigationCommand visualCursorCommand = visualCursor.GetComponent<ISurfaceNavigationCommand>();
                //__targetScreen.HandleNavigationEvent(visualCursorCommand, AgentNavigationType.NavigateAim);
            }
            else // IF NO AGENT
            {
                playerAgent.SetPlayerNavigationMode(PlayerNavigationMode.TopDownTraditional);
                playerAgent.ClearEnemyAgent();
                UINotificationWaterfall.Instance().Dispatch("basic", "player_aim_cycle_up", $"enemy_target: NONE", 10f, true); 
                //playerAgent.AddCommand( AgentCommand.Type.Navigation,  AgentNavigationType.NavigateAim, null);
            }
        }

        if (commandId == GeneralInputManager.Command.nav_plane_up)
        {
            ToggleNavPlane();
        }

        UpdatePressed(commandId);
        if(GetNavigationMode() == PlayerNavigationMode.TopDownTraditional)
        {
            //ProcessTopDownDash(commandId);
            ProcessTopDownNav(commandId);
        }
        else if(GetNavigationMode() == PlayerNavigationMode.ZTargetingEnemy)
        {
            ProcessZTargeting(commandId);
            //Debug.Log("Enemy Focused Nav Down Nav Command");

        }
    }
    private GameObject __currentNavPlane;
    [SerializeField]
    private GameObject __quickNavSurfaceLow;
    [SerializeField]
    private GameObject __quickNavSurfaceMedium;
    [SerializeField]
    private GameObject __quickNavSurfaceHigh;


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
            cube.transform.localScale = Vector3.one * 0.5f; // Scale down for better visibility
            cube.GetComponent<Renderer>().material.color = cin; // Make it red for easier identification
            cube.transform.position = position;
            cube.name = id;
            if (cube.TryGetComponent<BoxCollider>(out BoxCollider collider))
            {
                Destroy(collider); // Removes it completely
                // collider.enabled = false; // Alternative: Just disables it
            }            
            // Add the new cube to the dictionary
            debugCubes[id] = cube;
        }
    }
private void ToggleNavPlane()
{
    // Cycle through the navigation planes
    if (__currentNavPlane == __quickNavSurfaceLow)
    {
        SetActiveNavPlane(__quickNavSurfaceMedium);
    }
    else if (__currentNavPlane == __quickNavSurfaceMedium)
    {
        SetActiveNavPlane(__quickNavSurfaceHigh);
    }
    else
    {
        SetActiveNavPlane(__quickNavSurfaceLow);
    }
}

private void SetActiveNavPlane(GameObject newPlane)
{
    // Hide all planes and show only the selected one
    __quickNavSurfaceLow.SetActive(false);
    __quickNavSurfaceMedium.SetActive(false);
    __quickNavSurfaceHigh.SetActive(false);

    newPlane.SetActive(true);
    __currentNavPlane = newPlane;
}
}
