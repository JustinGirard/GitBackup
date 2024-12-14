using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

using System;



[System.Serializable]
public class DynamicButtonCanvas
{
    public string key;
    public string placeholder;
    //public DockedButton value;
    public Canvas value; // Reference to the Canvas containing the button

}

public interface IShowHide
{
    public void Show();
    public void Hide();

}
public class VisualProjectionFrame
{
    public Vector2 Size { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 Basis { get; set; }

    // Constructor taking Vector2 parameters
    public VisualProjectionFrame(Vector2 size, Vector2 position, Vector2 basis)
    {
        Size = size;
        Position = position;
        Basis = basis;
    }

    // Constructor taking individual float values
    public VisualProjectionFrame(float width, float height, float posX, float posY, float basisX, float basisY)
    {
        Size = new Vector2(width, height);
        Position = new Vector2(posX, posY);
        Basis = new Vector2(basisX, basisY);
    }
    public override string ToString()
    {
        return $"Size: {Size}, Position: {Position}, Basis: {Basis}";
    }    
}
public class SpaceCombatScreen : SpaceEncounterObserver,IShowHide
{
    [SerializeField]
    public List<DynamicButtonCanvas> dynamicButtonInit;
    private Dictionary<string,IDynamicControl> dynamicButton;
    public SpaceEncounterManager encounterManager;

    public class DynamicButtonID {
        public static readonly List<string> all = new List<string> { Attack, Missile, Shield };
        public const string Attack = "Attack";
        public const string Missile = "Missile";
        public const string Shield = "Shield";
    }


    // The combat will be between a player (agent 1) and an ai (agent 2).
    // The space combat screen will, on Enable,
    
    // UXML root and mapping of UXML#ID to resource names
    private Dictionary<string, string> uxmlToResourceMapping;
    private Dictionary<string,Dictionary<string, string>> cssIdToResourceId;

    GameObject navigatorObject;
    NavigationManager navigationManager;
    IntervalRunner intervalRunner;
    UIDocument uiDocument;
    public static float? ConvertToNumeric(object inputValue, string resourceName, string valueType)
    {
        if (inputValue == null)
            return null;
        try
        {
            if (inputValue is int || inputValue is float || inputValue is double || inputValue is decimal)
            {
                // Already numeric, return as float
                return Convert.ToSingle(inputValue);
            }

            if (float.TryParse(inputValue.ToString(), out float convertedFloat))
            {
                return convertedFloat;
            }

            Debug.LogError($"Could not convert {valueType} value for {resourceName}: {inputValue} (Type: {inputValue.GetType()})");
            return null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error while processing {valueType} value for {resourceName}: {ex.Message} (Type: {(inputValue == null ? "null" : inputValue.GetType().ToString())})");
            return null;
        }
    }
    void Awake_InitAgentCardDynamicControls()
    {
        cssIdToResourceId = new Dictionary<string, Dictionary<string, string>>();
        cssIdToResourceId["agent_1"] = encounterManager.GetAgentGUIFieldMapping("agent_1"); // GUIField - to - ResourceId
        cssIdToResourceId["agent_2"] = encounterManager.GetAgentGUIFieldMapping("agent_2");

        // Set up Agent fields
        foreach (string agent_id in cssIdToResourceId.Keys)
        {
            foreach (string cssId  in cssIdToResourceId[agent_id].Keys)
            {
                //Debug.Log($"Adding Element {cssId}");
                string resourceId = cssIdToResourceId[agent_id][cssId];
                DynamicButtonCanvas canCan = new DynamicButtonCanvas();
                canCan.key = $"{agent_id}.{resourceId}"; // Resource Key
                if (agent_id == "agent_1")
                    canCan.placeholder = $"agent-1-card-status,{cssId}";
                else
                    canCan.placeholder = $"agent-2-card-status,{cssId}";

                canCan.value = Resources.Load<Canvas>("AwayTeam/Screens/StatusbarCanvas");
                dynamicButtonInit.Add(canCan);
            }
        }

    }

    private void Awake_CreateAllDynamicControls()
    {

        foreach (DynamicButtonCanvas dynamicButtonEntry in dynamicButtonInit)
        {
            if (dynamicButtonEntry == null)
                Debug.LogError("Error: dynamicButtonEntry is null.");

            Canvas buttonCanvas = GameObject.Instantiate<Canvas>( dynamicButtonEntry.value);
            if (buttonCanvas == null)
                Debug.LogError("Error: dynamicButtonEntry.value is null.");

            GameObject dynamicGameObject = buttonCanvas.gameObject;
            if (dynamicGameObject == null)
                Debug.LogError("Error: dynamicButtonEntry.value.gameObject is null.");

            IDynamicControl dockedButton = dynamicGameObject.GetComponentInChildren<IDynamicControl>();
            if (dockedButton == null)
                Debug.LogError("Error: No DockedButton found in the descendants of the GameObject.");
            //else
            //    Debug.Log("Successfully found DockedButton.");
            
            // Event Registration
            dockedButton.RegisterInteractionHandler (HandleDynamicButtonInteraction); 
            dynamicButton[dynamicButtonEntry.key] = dockedButton;

            // UI Tracking
            string placeholderName = dynamicButtonEntry.placeholder;
            //VisualElement placeholder = uiDocument.rootVisualElement.Q<VisualElement>(placeholderName);
            VisualElement root = uiDocument.rootVisualElement;            
            VisualElement placeholder = DynamicControlRenderer.FindPlaceholder(root, placeholderName);                
            placeholder.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                DynamicControlRenderer.UpdateCanvasBounds(buttonCanvas, placeholderName,uiDocument);
            });
        }
    }

    void Awake()
    {
        navigatorObject = GameObject.Find("AwayTeam");
        navigationManager = navigatorObject.GetComponent<NavigationManager>();
        //navigationManager.NavigateTo("AT_NewGame",false);
        uxmlToResourceMapping = encounterManager.GetAccountFieldMapping();
        uiDocument = GetComponent<UIDocument>();
        VisualElement root = uiDocument.rootVisualElement;
        var btnPlay = root.Q<Button>("PlayGame");
        btnPlay.RegisterCallback<ClickEvent>(evt =>  encounterManager.Run());
        var btnPause = root.Q<Button>("PauseGame");
        btnPause.RegisterCallback<ClickEvent>(evt => encounterManager.Pause());
        var btnExit = root.Q<Button>("ExitButton");
        btnExit.RegisterCallback<ClickEvent>(evt => {
            encounterManager.End();
            navigationManager.NavigateTo("AT_NewGame");
        });
        dynamicButton = new Dictionary<string,IDynamicControl>();

        Awake_InitAgentCardDynamicControls();
        Awake_CreateAllDynamicControls();
        return;

    }



    private string DebugCompareFrames(string frameName, VisualProjectionFrame currentFrame, VisualProjectionFrame initialFrame)
    {
        float initialAspect = initialFrame.Size.x / initialFrame.Size.y;
        float currentAspect = currentFrame.Size.x / currentFrame.Size.y;

        string comparison = 
            $"{frameName}:\n" +
            $"- Size: [{initialFrame.Size.x} -> {currentFrame.Size.x}, {initialFrame.Size.y} -> {currentFrame.Size.y}]\n" +
            $"- Position: [{initialFrame.Position.x} -> {currentFrame.Position.x}, {initialFrame.Position.y} -> {currentFrame.Position.y}]\n" +
            $"- Aspect: [{initialAspect:F2} -> {currentAspect:F2}]";

        return comparison;
    }


    private void SetButtonPosition(RectTransform button, VisualElement placeholder)
    {
        if (button == null || placeholder == null)
            return;

        Vector3 screenPosition = placeholder.worldBound.center;
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, Camera.main.nearClipPlane));
        button.position = worldPosition;
    }



    private void HandleDynamicButtonInteraction(string buttonId, string eventId,bool within)
    {
        //Debug.Log($"HandleDynamicButtonInteraction called: {buttonId}.{eventId} ({SpaceEncounterManager.GUICommands.Attack })");
        string agent_id = "agent_1";
        if (buttonId== SpaceEncounterManager.AgentActions.Attack && eventId == "MouseUp" && within == true)
        {
            //Debug.Log($"HandleDynamicButtonInteraction {buttonId}");
            encounterManager.SetTargetAction(agent_id,buttonId);
            //StartCoroutine(encounterManager.ProcessAgentAction(SpaceEncounterManager.AgentActions.Attack));
        }
        if (buttonId== SpaceEncounterManager.AgentActions.Missile && eventId == "MouseUp" && within == true)
        {
            encounterManager.SetTargetAction(agent_id,buttonId);
            //Debug.Log($"HandleDynamicButtonInteraction {buttonId}");
            //StartCoroutine(encounterManager.ProcessAgentAction(SpaceEncounterManager.AgentActions.Missile));
        }
        if (buttonId== SpaceEncounterManager.AgentActions.Shield && eventId == "MouseUp" && within == true)
        {
            //Debug.Log($"HandleDynamicButtonInteraction {buttonId}");
            encounterManager.SetTargetAction(agent_id,buttonId);
            //StartCoroutine(encounterManager.ProcessAgentAction(SpaceEncounterManager.AgentActions.Shield));
        }

    }

    public override bool VisualizeEffect(string effect)
    {
        if (effect == SpaceEncounterManager.ObservableEffects.AttackOn)
            StartCoroutine(eApplyPulseAndActivate(DynamicButtonID.Attack));          
        
        if (effect == SpaceEncounterManager.ObservableEffects.AttackOff)
            SetButtonState(DynamicButtonID.Attack,"inactive");   

        if (effect == SpaceEncounterManager.ObservableEffects.MissileOn)
            StartCoroutine(eApplyPulseAndActivate(DynamicButtonID.Missile));          
        
        if (effect == SpaceEncounterManager.ObservableEffects.MissileOff)
            SetButtonState(DynamicButtonID.Missile,"inactive");   

        if (effect == SpaceEncounterManager.ObservableEffects.ShieldOn)
            StartCoroutine(eApplyPulseAndActivate(DynamicButtonID.Shield));
        
        if (effect == SpaceEncounterManager.ObservableEffects.ShieldOff)
            SetButtonState(DynamicButtonID.Shield,"inactive");   

        if (effect == SpaceEncounterManager.ObservableEffects.ShowPaused)
            navigationManager.NavigateTo("AT_PauseGame");

        if (effect == SpaceEncounterManager.ObservableEffects.EncounterOverLost)
            navigationManager.NavigateTo("AT_GameOver");

        if (effect == SpaceEncounterManager.ObservableEffects.EncounterOverWon)
            navigationManager.NavigateTo("AT_RoundWon");

        return true;
    }


    private System.Collections.IEnumerator eApplyPulseAndActivate(string buttonId)
    {
        bool canProceed = Sema.TryAcquireLock($"{buttonId}-pulse");
        if (canProceed == false)
        {
            yield break;
        }
        SetButtonState(buttonId,"active");

        // TryAcquireLock
        //yield return new WaitForSeconds(1f);
        // Find the button's GameObject or target
        try /// UI Related
        {
            GameObject buttonObject =  dynamicButton[buttonId].GetGameObject();
            yield return EffectHandler.Pulse(target:buttonObject, 
                                    duration:0.25f, 
                                    velocity:6f,
                                    size:0.05f,
                                    finalScale:null); // 0.5s pulse with velocity 10

            // Set the button state to inactive
            yield return new WaitForSeconds(0.5f); // Optional delay after the pulse
            //SetButtonState(buttonId, "inactive");
        }
        finally
        {
            Sema.ReleaseLock($"{buttonId}-pulse");
        }
    }



    public void SetButtonState(string buttonKey, string state)
    {
        dynamicButton[buttonKey].SetState(state);
    }    

    void DoRun(ClickEvent evt)
    {
       
    }

    public void Show()
    {
        var root = uiDocument.rootVisualElement;
        root.style.display = DisplayStyle.Flex;
        // dynamicButton[dynamicButtonEntry.key]
        foreach (var key in dynamicButton.Keys)
        {
            //Debug.Log($"Showing {key}");
            dynamicButton[key].GetGameObject().SetActive(true);
        }

        //UpdateUI(true);
        __doSetMaxProgressOnFirstRun = true;
    }
    private bool __doSetMaxProgressOnFirstRun = false;
    public void Hide()
    {
        var root = uiDocument.rootVisualElement;
        root.style.display = DisplayStyle.None;

        foreach (var key in dynamicButton.Keys)
        {
            //Debug.Log($"Hiding {key}");
            dynamicButton[key].GetGameObject().SetActive(true);
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        if (encounterManager == null)
        {
            Debug.LogError("encounterManager is not assigned to SpaceCombatScreen.");
            return;
        }

        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("No UIDocument component found on SpaceCombatScreen GameObject.");
            return;
        }

        foreach (var agentId in new[] { "account", "agent_1", "agent_2" })
        {
            string cardId = encounterManager.GetAgentGUICardId(agentId);
            VisualElement card = uiDocument.rootVisualElement.Q<VisualElement>(cardId);
            ATResourceData resourceData = encounterManager.GetResourceObject(agentId);
            PopulateAccountFields(agentId,resourceData, card);
        }     
    }
    void UpdateUI(bool setMaxProgress=false)
    {
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("No UIDocument component found on SpaceCombatScreen GameObject.");
            return;
        }        
        foreach (var agentId in new[] { "account", "agent_1", "agent_2" })
        {
            string cardId = encounterManager.GetAgentGUICardId(agentId);
            VisualElement card = uiDocument.rootVisualElement.Q<VisualElement>(cardId);
            ATResourceData resourceData = encounterManager.GetResourceObject(agentId);
            UpdateAgentFields(agentId,resourceData, card,setMaxProgress);
        }
    }
    void Update()
    {
        UpdateUI(__doSetMaxProgressOnFirstRun);
        if (__doSetMaxProgressOnFirstRun == true)
        {
            __doSetMaxProgressOnFirstRun = false;
        }
    }

    Dictionary <string,int>__revisionData = new Dictionary <string,int> {{"account",0},{"agent_1",0},{"agent_2",0}};
    private void PopulateAccountFields(string agent, ATResourceData resourceData, VisualElement cardElement )
    {
        //Debug.Log($"-------running");
        if (cardElement == null)
            return;
        if (resourceData.GetDataRevision() == __revisionData[agent])
            return;
        __revisionData[agent] = resourceData.GetDataRevision();

        //uxmlToResourceMapping = EncounterSettings.GetAccountFieldMapping();
        Dictionary<string,string> currentMapping= null; 
        
        //if (agent =="account" )
        //    currentMapping= uxmlToResourceMapping;
        //else
        //    currentMapping = cssIdToResourceId[agent];
        string agent_resource_prefix = "";
        if (agent =="account" )
        {
            currentMapping= uxmlToResourceMapping;
            agent_resource_prefix = "";
        }
        else
        {
            agent_resource_prefix = $"{agent}.";
            currentMapping = cssIdToResourceId[agent];
        }

        Dictionary<string,string> resourceIdToIconMapping = encounterManager.ResourceIdToIconMapping(agent);
        // $"{resourceIdToIconMapping[resourceName]}" 
        foreach (var mapping in currentMapping)
        {
            // Get the resource name and corresponding UXML IDs
            string uxmlId = mapping.Key;
            string resourceName = mapping.Value;

            // Find the Label in the UXML by its ID
            var label = cardElement.Q<Label>(uxmlId);
            //Debug.Log($"-------Inserting label {agent_resource_prefix}:{uxmlId}");
            if (label != null)
            {
                try
                {
                    // ResourceIdToIconMapping
                    object resourceAmount = resourceData.GetResourceAmount(resourceName);
                    IShowProgress progressBar;
                    try
                    {
                        progressBar = (IShowProgress)dynamicButton[$"{agent_resource_prefix}{resourceName}"];
                        //progressBar.SetProgressMax((int)1337);

                    }
                    catch (Exception ex)
                    {
                        Debug.Log(dynamicButton[$"{agent_resource_prefix}{resourceName}"]);
                        Debug.LogError($"Could not find progress bar for uxmlId: {agent_resource_prefix}.{uxmlId}. Exception: {ex.Message}\nStack Trace: {ex.StackTrace}");

                    }

                    var debugLabel = new Label // label.text = $"{resourceName}: {resourceAmount}";
                    {
                        text = $"{resourceIdToIconMapping[resourceName]}" 
                    };
                    //Debug.Log($"Inserting label {agent_resource_prefix}:{uxmlId}");
                    debugLabel.style.color = Color.white; // Set text color to white
                    debugLabel.name = $"icon-label-{uxmlId}";
                    debugLabel.style.fontSize = 10;       // Set smaller font size (adjust the value as needed)
                    
                    label.parent.Insert(label.parent.IndexOf(label), debugLabel);
                    label.text = $"-";

                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error with uxmlId: {uxmlId}. Exception: {ex.Message}\nStack Trace: {ex.StackTrace}");
                }
            }
            else
            {
                Debug.LogWarning($"UXML Label with ID {uxmlId} not found.");
            }

            // Find the associated image element and add a name label as a placeholder
            // string imageId = uxmlId.Replace("status-value", "status-icon"); // Assuming a consistent naming pattern
            // var imageElement = cardElement.Q<VisualElement>(imageId);
        }

    }

    /// <summary>
    /// Updates the fields dynamically during the encounter.
    /// </summary>
    private void UpdateAgentFields(string agent,ATResourceData resourceData, VisualElement cardElement, bool setMaxProgress= false)
    {
        if(setMaxProgress == true)
            Debug.Log("Setting Max Progress!");
        //if (resourceData.GetDataRevision() == __revisionData[agent] || setMaxProgress==false)
        //{
        //    return;
        //}
        if (resourceData.GetDataRevision() == __revisionData[agent] && setMaxProgress==false)
        {
            return;
        }        
        __revisionData[agent] = resourceData.GetDataRevision();

        if (cardElement == null)
        {
            //Debug.LogError("No Card Element Linked Element");
            if (setMaxProgress == true)
                Debug.LogError($"setting: FAILED TO SET PROGRESS");            
            return;
        }
        Dictionary<string,string> currentMapping= null; 
        string agent_resource_prefix = "";
        if (agent =="account" )
        {
            currentMapping= uxmlToResourceMapping;
            agent_resource_prefix = "";
        }
        else
        {
            agent_resource_prefix = $"{agent}.";
            currentMapping = cssIdToResourceId[agent];
        }
        Dictionary<string,string> resourceIdToIconMapping = encounterManager.ResourceIdToIconMapping(agent);
        
            
        foreach (var mapping in currentMapping)
        {
            // Get the resource name and corresponding UXML ID
            string uxmlId = mapping.Key;
            string resourceName = mapping.Value;

            // Update the label for the resource value
            var label = cardElement.Q<Label>(uxmlId);
            /*
            var debug_label = cardElement.Q<Label>($"icon-label-{uxmlId}");
            */


            if (label != null)
            {
                // Get the resource amount from ResourceData
                float resourceAmount = ConvertToNumeric(resourceData.GetResourceAmount(resourceName), resourceName, "progress") ?? 2f; // Use a default value if null
                float resourceMax = ConvertToNumeric(resourceData.GetResourceMax(resourceName), resourceName, "MAX") ?? 10f; // Use a default value if null

                // Update the label's text
                if (agent_resource_prefix != "")
                {
                    //Debug.Log("P");
                    IShowProgress progressBar = (IShowProgress)dynamicButton[$"{agent_resource_prefix}{resourceName}"];
                    //progressBar = (IShowProgress)dynamicButton[$"{agent_resource_prefix}{resourceName}"];
                    // progressBar.SetProgressMax((int)1337);
                    if (setMaxProgress == true)
                    {
                        Debug.Log($"setting: {agent_resource_prefix}{resourceName} = {resourceAmount.ToString()}");
                        progressBar.SetProgressMax((int)resourceAmount);   
                    }
                    //if(progressBar.GetProgressMax() == (int)1337)
                    //    progressBar.SetProgressMax((int)resourceAmount);
                    if ("agent_1.Hull " == $"{agent_resource_prefix}{resourceName}" )
                        Debug.Log($"Updatting Data: {agent_resource_prefix}{resourceName} = {resourceAmount.ToString()}");                    
                    progressBar.SetProgress((int)resourceAmount);
                    var debug_label = cardElement.Q<Label>($"icon-label-{uxmlId}");
                    if (debug_label != null)
                    {
                        debug_label.text = $"{resourceIdToIconMapping[resourceName]} ({resourceAmount.ToString()}):";
                    }
                }
                label.text = $"";
            }

            // Update the placeholder label on the image (if needed)
            string imageId = uxmlId.Replace("status-value", "status-icon");
            var imageElement = cardElement.Q<VisualElement>(imageId);
        }
    }
}
