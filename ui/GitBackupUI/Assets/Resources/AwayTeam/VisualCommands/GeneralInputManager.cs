using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using System;
using System.Linq;

public interface ICommandReceiver
{
    void OnCommandReceived(string commandId, Vector2 mousePosition);
    GameObject GetGameObject();
}

public class GeneralInputManager : MonoBehaviour
{
    private static GeneralInputManager __instance;
    public static GeneralInputManager Instance(){
        return __instance;
    }
    public class Command
    {
        public static readonly List<string> all = new List<string>
        {
            primary_down,
            primary_up,
            primary_move,
            secondary_down,
            secondary_up,
            secondary_move,
            cancel,
            nav_up_up,
            nav_down_up,
            nav_left_up,
            nav_right_up,
            formation_up_up,
            attackpattern_up_up,            
            nav_up_down,
            nav_down_down,
            nav_left_down,
            nav_right_down,
            formation_up_down,
            attackpattern_up_down            
        };

        public const string nav_up_up = "nav_up_up";
        public const string nav_down_up = "nav_down_up";
        public const string nav_left_up = "nav_left_up";
        public const string nav_right_up = "nav_right_up";
        public const string formation_up_up = "formation_up_up";
        public const string attackpattern_up_up = "attackpattern_up_up";

        public const string nav_up_down = "nav_up_down";
        public const string nav_down_down = "nav_down_down";
        public const string nav_left_down = "nav_left_down";
        public const string nav_right_down = "nav_right_down";
        public const string formation_up_down = "formation_up_down";
        public const string attackpattern_up_down = "attackpattern_up_down";


        public const string primary_down = "primary_down";
        public const string primary_up = "primary_up";
        public const string primary_move = "primary_move";
        public const string secondary_down = "secondary_down";
        public const string secondary_up = "secondary_up";
        public const string secondary_move = "secondary_move";
        public const string cancel = "cancel";
    }
    [SerializeField]
    public Vector2 lastMousePosition;
    private List<ICommandReceiver> observers = new List<ICommandReceiver>();
    public Camera viewportCamera; // Camera for raycasting
    public bool __debugMode = false;
    public List<string> transparentGameObjects = new List<string> { "uiElement.SystemPanelSettings", "uidoc.AT_SpaceCombatEncounterScreen", "uidoc.element.card-status" };
    public Camera GetCamera(){

        return viewportCamera;
    }
    
    class CommandResult
    {
        public bool active;
        public Vector2 position;

        public CommandResult(bool isActive, Vector2 pos)
        {
            active = isActive;
            position = pos;
        }
    }

    private Dictionary<string, Func<CommandResult>> commandBindings;

    void Awake()
    {
        __instance = this;
        commandBindings = new Dictionary<string, Func<CommandResult>>
        {
            { Command.primary_down, () => Input.GetMouseButtonDown(0) ? new CommandResult(true, Input.mousePosition) : new CommandResult(false,  Input.mousePosition) },
            { Command.primary_up, () => Input.GetMouseButtonUp(0) ? new CommandResult(true, Input.mousePosition) : new CommandResult(false,  Input.mousePosition) },
            { Command.primary_move, () => new CommandResult(true, Input.mousePosition)},
            { Command.secondary_down, () => Input.GetMouseButtonDown(1) ? new CommandResult(true, Input.mousePosition) : new CommandResult(false,Input.mousePosition) },
            { Command.secondary_up, () => Input.GetMouseButtonUp(1) ? new CommandResult(true, Input.mousePosition) : new CommandResult(false,Input.mousePosition) },
            { Command.secondary_move, () => new CommandResult(true, Input.mousePosition)},
            { Command.cancel, () => Input.GetKeyDown(KeyCode.Escape) ? new CommandResult(true, Input.mousePosition) : new CommandResult(false, Input.mousePosition) }
        };
    }
    void Start()
    {
        lastMousePosition = new Vector2();
    }

    void Update()
    {
        HandleDevices();
    }

    public void RegisterObserver(ICommandReceiver observer)
    {
        if (!observers.Contains(observer))
        {
            observers.Add(observer);
        }
    }

    public void UnregisterObserver(ICommandReceiver observer)
    {
        observers.Remove(observer);
    }



    public (bool, Vector3) PollCommandStatus(string command)
    {
        if (!Command.all.Contains(command))
        {
            Debug.LogError($"Request for illegal command {command}");
            return (false, Vector3.zero);
        }

        if (commandBindings.TryGetValue(command, out Func<CommandResult> func))
        {
            CommandResult result = func.Invoke();
            return (result.active,result.position);
        }
        else
            Debug.LogError($"Request for illegal commandBindings:{command}");

        return (false, Vector3.zero);
    }    
    private void HandleDevices()
    {
        Vector2 mousePosition = Input.mousePosition;
        foreach (var observer in observers)
        {
            //if (__debugMode == true && Vector2.Distance(mousePosition, lastMousePosition) > 0.01f)
            //{
            //    Debug.Log($@"--- Investigating pos to  {observer.GetGameObject().name}: {mousePosition}
            //    - Distance:{ Vector2.Distance(mousePosition, lastMousePosition) > 0.01f}
            //    - PointerActive:{IsPointerOverUIDocument()}
            //    ");
            //}
            if (Input.GetMouseButtonDown(0) && !IsPointerOverUIDocument())
            {
                observer.OnCommandReceived(Command.primary_down,mousePosition);
            }
            else if (Input.GetMouseButtonUp(0) && !IsPointerOverUIDocument())
            {
                observer.OnCommandReceived(Command.primary_up,mousePosition);
            }
            else if (Input.GetMouseButtonDown(1) && !IsPointerOverUIDocument())
            {
                observer.OnCommandReceived(Command.secondary_down,mousePosition);
            }
            else if (Input.GetMouseButtonUp(1) && !IsPointerOverUIDocument())
            {
                observer.OnCommandReceived(Command.secondary_up,mousePosition);
            }
            else if (!IsPointerOverUIDocument() && Vector2.Distance(mousePosition, lastMousePosition) > 0.01f)
            {
                //if (__debugMode == true)
                //    Debug.Log($"--- Dispatching pos to  {observer.GetGameObject().name}: {mousePosition}");

                observer.OnCommandReceived(Command.primary_move, mousePosition);
                observer.OnCommandReceived(Command.secondary_move, mousePosition);
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                observer.OnCommandReceived(Command.cancel, mousePosition); // Pass empty Vector2 or adjust as needed
            }            
            if (Input.GetKeyDown(KeyCode.W)) observer.OnCommandReceived(Command.nav_up_down, mousePosition); 
            if (Input.GetKeyDown(KeyCode.S)) observer.OnCommandReceived(Command.nav_down_down, mousePosition); 
            if (Input.GetKeyDown(KeyCode.A)) observer.OnCommandReceived(Command.nav_left_down, mousePosition); 
            if (Input.GetKeyDown(KeyCode.D)) observer.OnCommandReceived(Command.nav_right_down, mousePosition); 
            if (Input.GetKeyDown(KeyCode.LeftBracket)) observer.OnCommandReceived(Command.formation_up_down, mousePosition); 
            if (Input.GetKeyDown(KeyCode.Semicolon)) observer.OnCommandReceived(Command.attackpattern_up_down, mousePosition); 

            if (Input.GetKeyUp(KeyCode.W)) observer.OnCommandReceived(Command.nav_up_up, mousePosition); 
            if (Input.GetKeyUp(KeyCode.S)) observer.OnCommandReceived(Command.nav_down_up, mousePosition); 
            if (Input.GetKeyUp(KeyCode.A)) observer.OnCommandReceived(Command.nav_left_up, mousePosition); 
            if (Input.GetKeyUp(KeyCode.D)) observer.OnCommandReceived(Command.nav_right_up, mousePosition); 
            if (Input.GetKeyUp(KeyCode.LeftBracket)) observer.OnCommandReceived(Command.formation_up_up, mousePosition); 
            if (Input.GetKeyUp(KeyCode.Semicolon)) observer.OnCommandReceived(Command.attackpattern_up_up, mousePosition); 

        }
        lastMousePosition = mousePosition;
    }

    /// <summary>
    /// Checks if the mouse is over a UI Toolkit element.
    /// </summary>
    /// 
    /*
        Vector2 adjustedMousePos = new Vector2(
            mousePosition.x,
            Screen.height - mousePosition.y // Flip the Y-axis to match the top-left origin of UI Toolkit
        );

        // Convert the adjusted screen position to local UI coordinates
        Vector2 localMousePos = RuntimePanelUtils.ScreenToPanel(uiDoc.rootVisualElement.panel, adjustedMousePos);
            
    */

    private bool IsPointerOverUIDocument()
    {
        List<string> collidedNames = new List<string>();
        Vector2 mousePosition = Input.mousePosition;
        Vector2 adjustedMousePos = new Vector2(
            mousePosition.x,
            Screen.height - mousePosition.y // Flip the Y-axis to match the top-left origin of UI Toolkit
        );        
        var uiDocs = FindObjectsOfType<UIDocument>();

        foreach (var uiDoc in uiDocs)
        {
            if (!uiDoc.gameObject.activeInHierarchy || uiDoc.rootVisualElement.resolvedStyle.display == DisplayStyle.None)
                continue;

            var panel = uiDoc.rootVisualElement.panel;
            if (panel != null)
            {
                //Vector2 localMousePos = uiDoc.rootVisualElement.WorldToLocal(mousePosition);
                Vector2 localMousePos = RuntimePanelUtils.ScreenToPanel(uiDoc.rootVisualElement.panel, adjustedMousePos);
                VisualElement hoveredElement = panel.Pick(localMousePos);

                if (hoveredElement != null && hoveredElement.name.Length > 0)
                {
                    collidedNames.Add("uidoc.element." + hoveredElement.name);
                }
                collidedNames.Add("uidoc." + uiDoc.gameObject.name);
                //Debug.Log($"Colliding Inline: {string.Join(", ", collidedNames)}");
            }
        }

        collidedNames.RemoveAll(name => transparentGameObjects.Contains(name));

        //if (__debugMode && collidedNames.Count > 0)
        //{
        //    Debug.Log("________________________________________");
        //    foreach (string uiName in collidedNames)
        //    {
        //        Debug.Log("Collided with " + uiName);
        //    }
        //}
        if (__debugMode)
        {
            // Store original collided names for debugging
            List<string> ignoredCollisions = collidedNames.Where(name => transparentGameObjects.Contains(name)).ToList();
            List<string> remainingCollisions = collidedNames.Except(ignoredCollisions).ToList();

            // Remove ignored collisions from the original list
            collidedNames.RemoveAll(name => transparentGameObjects.Contains(name));

            // Format and log debug message
            if (ignoredCollisions.Count > 0 || remainingCollisions.Count > 0)
            {
                Debug.Log("________________________________________");
                Debug.Log($"Ignored Collisions: {string.Join(", ", ignoredCollisions)}");
                Debug.Log($"Collided With: {string.Join(", ", remainingCollisions)}");
            }
        }

        return collidedNames.Count > 0;
    }
}