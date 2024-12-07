using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

using System;
using OpenCover.Framework.Model;
using Unity.VisualScripting;



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
public class SpaceCombatScreen : SpaceEncounterObserver,IShowHide
{
    [SerializeField]
    public List<DynamicButtonCanvas> dynamicButtonInit;
    private Dictionary<string,DockedButton> dynamicButton;
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
    private Dictionary<string, string> agent1Mapping;
    private Dictionary<string, string> agent2Mapping;

    GameObject navigatorObject;
    NavigationManager navigationManager;
    IntervalRunner intervalRunner;
    UIDocument uiDocument;

    void Awake()
    {
        navigatorObject = GameObject.Find("AwayTeam");
        navigationManager = navigatorObject.GetComponent<NavigationManager>();
        //navigationManager.NavigateTo("AT_NewGame",false);
        uxmlToResourceMapping = encounterManager.GetAccountFieldMapping();
        agent1Mapping = encounterManager.GetAgentGUIFieldMapping("agent_1");
        agent2Mapping = encounterManager.GetAgentGUIFieldMapping("agent_2");
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
        dynamicButton = new Dictionary<string,DockedButton>();

        foreach (DynamicButtonCanvas dynamicButtonEntry in dynamicButtonInit)
        {
           // DockedButton   dockedButton  = dynamicButtonEntry.value.gameObject.GetComponentInChildren<DockedButton>();;
            if (dynamicButtonEntry == null)
                Debug.LogError("Error: dynamicButtonEntry is null.");

            Canvas buttonCanvas = dynamicButtonEntry.value;
            if (buttonCanvas == null)
                Debug.LogError("Error: dynamicButtonEntry.value is null.");

            GameObject dynamicGameObject = buttonCanvas.gameObject;
            if (dynamicGameObject == null)
                Debug.LogError("Error: dynamicButtonEntry.value.gameObject is null.");

            DockedButton dockedButton = dynamicGameObject.GetComponentInChildren<DockedButton>();
            if (dockedButton == null)
                Debug.LogError("Error: No DockedButton found in the descendants of the GameObject.");
            else
                Debug.Log("Successfully found DockedButton.");
            
            // Event Registration
            dockedButton.OnButtonInteracted += HandleDynamicButtonInteraction;
            dynamicButton[dynamicButtonEntry.key] = dockedButton;

            // UI Tracking
            string placeholderName = dynamicButtonEntry.placeholder;
            VisualElement placeholder = uiDocument.rootVisualElement.Q<VisualElement>(placeholderName);
            placeholder.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                UpdateCanvasBounds(buttonCanvas, placeholderName);
                Debug.Log($"Updating '{placeholderName}' -> '{buttonCanvas.name}'.");
            });
            Debug.Log($"Registered GeometryChangedEvent for placeholder '{placeholderName}' and canvas '{buttonCanvas.name}'.");

        }

    }
    private void UpdateCanvasBounds(Canvas buttonCanvas, string placeholderName)
    {
        if (buttonCanvas == null)
        {
            Debug.LogError("Button Canvas is null. Cannot update bounds.");
            return;
        }

        RectTransform canvasTransform = buttonCanvas.GetComponent<RectTransform>();
        VisualElement root = uiDocument.rootVisualElement;

        VisualElement placeholder = root.Q<VisualElement>(placeholderName);
        if (placeholder == null)
        {
            Debug.LogError($"Placeholder '{placeholderName}' is missing in the UI Document.");
            return;
        }

        // Check if the placeholder's layout is valid
        if (placeholder.worldBound.width <= 0 || placeholder.worldBound.height <= 0)
        {
            Debug.LogWarning($"Placeholder '{placeholderName}' has invalid bounds (layout may not be finalized). Skipping.");
            return;
        }

        // Calculate world position of the placeholder
        Vector3 screenPos = placeholder.worldBound.center;
        if (float.IsNaN(screenPos.x) || float.IsNaN(screenPos.y))
        {
            Debug.LogError($"Placeholder '{placeholderName}' has invalid screen position: {screenPos}. Skipping.");
            return;
        }

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Camera.main.nearClipPlane));

        // Update Canvas position to align with the placeholder
        canvasTransform.position = worldPos;

        // Optionally set size if needed
        canvasTransform.sizeDelta = new Vector2(placeholder.worldBound.width, placeholder.worldBound.height);
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
        if (buttonId== SpaceEncounterManager.GUICommands.Attack && eventId == "MouseUp" && within == true)
        {
            //Debug.Log($"HandleDynamicButtonInteraction {buttonId}");
            StartCoroutine(encounterManager.ProcessGUIAction(SpaceEncounterManager.GUICommands.Attack));
        }
        if (buttonId== SpaceEncounterManager.GUICommands.Missile && eventId == "MouseUp" && within == true)
        {
            //Debug.Log($"HandleDynamicButtonInteraction {buttonId}");
            StartCoroutine(encounterManager.ProcessGUIAction(SpaceEncounterManager.GUICommands.Missile));
        }
        if (buttonId== SpaceEncounterManager.GUICommands.Shield && eventId == "MouseUp" && within == true)
        {
            //Debug.Log($"HandleDynamicButtonInteraction {buttonId}");
            StartCoroutine(encounterManager.ProcessGUIAction(SpaceEncounterManager.GUICommands.Shield));
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
            //SetButtonState(DynamicButtonID.Shield,"inactive");   
            navigationManager.NavigateTo("AT_PauseGame");
        //if (effect == SpaceEncounterManager.ObservableEffects.ShowUnpaused)
        //    Debug.Log("GAME RUNNING");

        if (effect == SpaceEncounterManager.ObservableEffects.EncounterOver)
            navigationManager.NavigateTo("AT_GameOver");

        return true;
    }

    //ProcessGUIAction
    //public bool ShowUIEffect()
    //{
    //    StartCoroutine(ApplyPulseAndDeactivate(buttonId));     
    //    return true;
    //}

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
            GameObject buttonObject =  dynamicButton[buttonId].gameObject;
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


    //private System.Collections.IEnumerator DelayAction(Action action, float delay)
   // {
   //     yield return new WaitForSeconds(delay);
   //     action?.Invoke();
   // }    

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

        foreach (DynamicButtonCanvas dynamicButtonEntry in dynamicButtonInit)
        {
            Canvas buttonCanvas = dynamicButtonEntry.value;
            if (buttonCanvas != null)
                buttonCanvas.gameObject.SetActive(true);
        }
    }

    public void Hide()
    {
        var root = uiDocument.rootVisualElement;
        root.style.display = DisplayStyle.None;

        foreach (DynamicButtonCanvas dynamicButtonEntry in dynamicButtonInit)
        {
            Canvas buttonCanvas = dynamicButtonEntry.value;
            if (buttonCanvas != null)
                buttonCanvas.gameObject.SetActive(false);
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

    void Update()
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
            UpdateFields(agentId,resourceData, card);
        }
        /*
        List<Canvas> canvasassess = new List<Canvas>();
        foreach (DynamicButtonCanvas dynamicButtonEntry in dynamicButtonInit)
        {
            if (dynamicButtonEntry == null)
                Debug.LogError("Error: dynamicButtonEntry is null.");
            Canvas canvasss = dynamicButtonEntry.value;
            canvasassess.Add(canvasss);
            UpdateCanvasBounds(canvasss, dynamicButtonEntry.placeholder);            
        }   */
    }

    Dictionary <string,int>__revisionData = new Dictionary <string,int> {{"account",0},{"agent_1",0},{"agent_2",0}};
    private void PopulateAccountFields(string agent, ATResourceData resourceData, VisualElement cardElement )
    {
        if (resourceData.GetDataRevision() == __revisionData[agent])
            return;
        __revisionData[agent] = resourceData.GetDataRevision();

        //uxmlToResourceMapping = EncounterSettings.GetAccountFieldMapping();
        Dictionary<string,string> currentMapping= null; 
        
        if (agent =="account" )
            currentMapping= uxmlToResourceMapping;
        if (agent =="agent_1" )
            currentMapping = agent1Mapping;
        if (agent =="agent_2" )
            currentMapping = agent2Mapping;

        foreach (var mapping in currentMapping)
        {
            // Get the resource name and corresponding UXML IDs
            string uxmlId = mapping.Key;
            string resourceName = mapping.Value;

            // Find the Label in the UXML by its ID
            var label = cardElement.Q<Label>(uxmlId);
            if (label != null)
            {
                // Get the resource amount from ResourceData
                object resourceAmount = resourceData.GetResourceAmount(resourceName);

                // Update the label's text
                label.text = $"{resourceName}: {resourceAmount}";
            }
            else
            {
                Debug.LogWarning($"UXML Label with ID {uxmlId} not found.");
            }

            // Find the associated image element and add a name label as a placeholder
            string imageId = uxmlId.Replace("status-value", "status-icon"); // Assuming a consistent naming pattern
            var imageElement = cardElement.Q<VisualElement>(imageId);
        }

    }

    /// <summary>
    /// Updates the fields dynamically during the encounter.
    /// </summary>
    private void UpdateFields(string agent,ATResourceData resourceData, VisualElement cardElement)
    {
        if (resourceData.GetDataRevision() == __revisionData[agent])
        {
            return;
        }
        __revisionData[agent] = resourceData.GetDataRevision();

        if (cardElement == null)
        {
            Debug.LogError("No Card Element Linked Element");
            return;
        }
        Dictionary<string,string> currentMapping= null; 
        
        if (agent =="account" )
            currentMapping= uxmlToResourceMapping;
        if (agent =="agent_1" )
            currentMapping = agent1Mapping;
        if (agent =="agent_2" )
            currentMapping = agent2Mapping;
            
        foreach (var mapping in currentMapping)
        {
            // Get the resource name and corresponding UXML ID
            string uxmlId = mapping.Key;
            string resourceName = mapping.Value;

            // Update the label for the resource value
            var label = cardElement.Q<Label>(uxmlId);
            if (label != null)
            {
                // Get the resource amount from ResourceData
                object resourceAmount = resourceData.GetResourceAmount(resourceName);

                // Update the label's text
                label.text = $"{resourceName}:{resourceAmount}";
            }

            // Update the placeholder label on the image (if needed)
            string imageId = uxmlId.Replace("status-value", "status-icon");
            var imageElement = cardElement.Q<VisualElement>(imageId);
        }
    }
}
