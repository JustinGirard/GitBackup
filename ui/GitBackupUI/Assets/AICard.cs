using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;


public class AICard : MonoBehaviour
{
    // Reference to the UI Document
    public UIDocument uiDocument;

    // Data class instance
    private CYOAData adventureData;

    // UI Elements
    private Label healthLabel;
    private Label manaLabel;
    private Label areaTitleLabel;
    private Image areaImage;
    private Label descriptionLabel;
    private VisualElement actionPanel;
    private VisualElement navigationChoicesPanel;
    private Button characterButton;
    private Button backButton;
    private int __dataRevision = -37498;
    // Current Location
    private string currentLocation = "UNKNOWN"; // Default location
    private bool initalized = false;
    void Start()
    {
        // Initialize data
        adventureData =GetComponent<CYOAData>();

        // Get the root visual element
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Get references to UI elements
        healthLabel = root.Q<Label>("HealthLabel");
        manaLabel = root.Q<Label>("ManaLabel");
        areaTitleLabel = root.Q<Label>("AreaTitle");
        areaImage = root.Q<Image>("AreaImage");
        descriptionLabel = root.Q<Label>("DescriptionLabel");
        actionPanel = root.Q<VisualElement>("ActionPanel");
        navigationChoicesPanel = root.Q<VisualElement>("NavigationChoices");
        characterButton = root.Q<Button>("CharacterButton");
        backButton = root.Q<Button>("BackButton");

        // Assign button event handlers
        characterButton.clicked += OnCharacterButtonClicked;
        backButton.clicked += OnBackButtonClicked;
        initalized = true;
        // Initial render
        RenderUI();
    }

    void Update()
    {
        if (adventureData == null)
        {
            Debug.LogError($"{this.name}: Missing Required Datasource");
            return;
        }
        int dataRevision = adventureData.GetDataRevision();
        if (__dataRevision == dataRevision)
        {
            return;
        }
        // Check if data is dirty and re-render UI
        if (RenderUI())
            __dataRevision = dataRevision;
    }

    private bool RenderUI()
    {
        if (initalized == false)
            return false;
        // Update health and mana labels
        //Debug.Log(healthLabel);
        //Debug.Log(adventureData);
        Dictionary<string,object> location = adventureData.GetCurrentLocation();
        Dictionary<string,object>  navChoices = adventureData.GetCurrentNavigationChoices();
        if (location == null || navChoices== null)
        {
            //Debug.Log($"Not Ready to load {location}, {navChoices}");
            return false;        
        }
        //healthLabel.text = $"Health: {location["Health"]}";
        //manaLabel.text = $"Mana: {location["Mana"]}";

        // Update area title and description
        areaTitleLabel.text = (string)location["AreaTitle"];
        descriptionLabel.text = (string)location["Description"];

        // Update area image
        string areaImageName = (string)location[ "AreaImage"];
        if (areaImageName.StartsWith("./"))
        {
            areaImageName = areaImageName.Replace("./", "/Users/computercomputer/justinops/art/apps/cyoa/");
        }        
        Debug.Log($"Rendering Image 2 + { areaImageName}");
        //areaImage.sprite = Resources.Load<Sprite>(areaImageName);
        areaImage.sprite = ImageLoader.LoadSpriteFromFile(areaImageName);
        // Clear existing actions and navigation choices
        Debug.Log($"Finished Render Image 2 + { areaImageName}");
        actionPanel.Clear();
        navigationChoicesPanel.Clear();

        // Get actions and navigation choices for the current location
        //string actionsStr = location["Actions"];
        //string navigationChoicesStr = location["NavigationChoices"];

        // Populate action panel


        // Populate navigation choices
        foreach (var navChoiceText in navChoices.Keys)
        {
            var navLabel = new Label(navChoiceText);
            navLabel.name = "NavigationLabel";
            navLabel.AddToClassList("navigation-label");
            navLabel.RegisterCallback<ClickEvent>(evt => OnNavigationSelected(navChoiceText,(string)navChoices[navChoiceText]));
            navigationChoicesPanel.Add(navLabel);
        }
        return true;
    }

    // Event handler for character button
    private void OnCharacterButtonClicked()
    {
        Debug.Log("Character button clicked.");
        // Implement character screen logic here
    }

    // Event handler for back button
    private void OnBackButtonClicked()
    {
        Debug.Log("Back button clicked.");
        // Implement back navigation logic here
    }

    // Event handler for action selection
    private void OnActionSelected(string action)
    {
        Debug.Log($"Action selected: {action}");
        // Implement action logic here
        // For example, update a global or location-specific value
        //adventureData.Set(currentLocation, "large_box", "open");
    }

    // Event handler for navigation choice selection
    private void OnNavigationSelected(string navigationChoice,string location)
    {
        Debug.Log($"Navigation choice selected: {navigationChoice}");
        adventureData.SetCurrentLocation(location);
    }
}
