/*
using UnityEngine;
using UnityEngine.UIElements;

public class GameScreenController : MonoBehaviour
{
    public SpaceEncounterManager __encounterManager;
    private UIDocument uiDocument;
    private VisualElement root;
    private Button actionButton;
    private Label subjectLabel;

    void OnEnable()
    {
        // Get the UIDocument and root VisualElement
        uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;

        // Find elements in the UXML
        actionButton = root.Q<Button>("actionButton");
        subjectLabel = root.Q<Label>("subject");

        // Add event listener to the button
        actionButton.clicked += OnActionButtonClicked;

        // Set default text for the screen
        SetScreenState(true); // 'true' means it's the Welcome screen
    }

    void OnDisable()
    {
        // Remove event listener when disabled
        if (actionButton != null)
        {
            actionButton.clicked -= OnActionButtonClicked;
        }
    }

    private void OnActionButtonClicked()
    {
        // Handle the button click (e.g., start or restart the game)
        GameObject navigatorObject = GameObject.Find("AwayTeam");
        NavigationManager navigationManager = navigatorObject.GetComponent<NavigationManager>();

        __encounterManager.Initalize();
        __encounterManager.Begin();
        navigationManager.NavigateTo("AT_SpaceCombatEncounterScreen",true);        
    }

    /// <summary>
    /// Sets the screen state to Welcome or Game Over.
    /// </summary>
    /// <param name="isWelcomeScreen">If true, sets to Welcome screen. Otherwise, sets to Game Over screen.</param>
    public void SetScreenState(bool isWelcomeScreen)
    {
        subjectLabel.text = isWelcomeScreen ? "Welcome" : "Game Over";
        actionButton.text = isWelcomeScreen ? "Start" : "Restart";
    }
}
//////////////
/////////////
////////////
////////////
///////////
/////////////////
////////////
///////////

using UnityEngine;
using UnityEngine.UIElements;

public class GameScreenController : MonoBehaviour
{
    public enum MenuScreenType
    {
        MainMenu,
        PausedScreen,
        GameOverScreen
    }

    [Header("Menu Screen Settings")]
    public MenuScreenType menuScreenType;

    public SpaceEncounterManager __encounterManager;
    private UIDocument uiDocument;
    private VisualElement root;
    private Button actionButton;
    private Label subjectLabel;

    void OnEnable()
    {
        // Get the UIDocument and root VisualElement
        uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;

        // Find elements in the UXML
        actionButton = root.Q<Button>("actionButton");
        subjectLabel = root.Q<Label>("subject");

        // Add event listener to the button
        actionButton.clicked += OnActionButtonClicked;

        // Set initial text based on the menu screen type
        SetScreenState(menuScreenType);
    }

    void OnDisable()
    {
        // Remove event listener when disabled
        if (actionButton != null)
        {
            actionButton.clicked -= OnActionButtonClicked;
        }
    }

    private void OnActionButtonClicked()
    {
        // Handle the button click (e.g., start or restart the game)
        GameObject navigatorObject = GameObject.Find("AwayTeam");
        NavigationManager navigationManager = navigatorObject.GetComponent<NavigationManager>();

        __encounterManager.Initalize();
        __encounterManager.Begin();
        navigationManager.NavigateTo("AT_SpaceCombatEncounterScreen", true);
    }

    /// <summary>
    /// Sets the screen state based on the menu screen type.
    /// </summary>
    /// <param name="type">The menu screen type.</param>
    public void SetScreenState(MenuScreenType type)
    {
        switch (type)
        {
            case MenuScreenType.MainMenu:
                subjectLabel.text = "Placeholder Main Menu";
                actionButton.text = "Play";
                break;

            case MenuScreenType.PausedScreen:
                subjectLabel.text = "Placeholder Paused Screen";
                actionButton.text = "Resume";
                break;

            case MenuScreenType.GameOverScreen:
                subjectLabel.text = "Placeholder Game Over Screen";
                actionButton.text = "Restart";
                break;

            default:
                subjectLabel.text = "Unknown Menu Screen";
                actionButton.text = "Action";
                break;
        }
    }
}

*/
using UnityEngine;
using UnityEngine.UIElements;

public class GameScreenController : MonoBehaviour
{
    public enum MenuScreenType
    {
        MainMenu,
        PausedScreen,
        GameOverScreen
    }

    [Header("Menu Screen Settings")]
    public MenuScreenType menuScreenType;

    public SpaceEncounterManager __encounterManager;
    private UIDocument uiDocument;
    private VisualElement root;
    private VisualElement buttonsContainer;
    private Label subjectLabel;

    void Awake()
    {
        // Get the UIDocument and root VisualElement
        uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;

        // Find the container for buttons
        buttonsContainer = root.Q<VisualElement>("buttons-container");
        subjectLabel = root.Q<Label>("subject");

        // Clear existing buttons
        buttonsContainer.Clear();

        // Dynamically create buttons based on the screen type
        CreateButtonsForScreen(menuScreenType);
    }

    void OnEnable()
    {
        // Set initial text for the subject label
        SetScreenState(menuScreenType);
    }

    private void CreateButtonsForScreen(MenuScreenType type)
    {
        switch (type)
        {
            case MenuScreenType.MainMenu:
                AddButton("Play", () =>
                {
                    __encounterManager.Initalize();
                    __encounterManager.Begin();
                    NavigateTo("AT_SpaceCombatEncounterScreen");
                });
                break;

            case MenuScreenType.PausedScreen:
                AddButton("Resume", () =>
                {
                    NavigateTo("AT_SpaceCombatEncounterScreen");
                    __encounterManager.Run();
                });
                AddButton("Quit to Menu", () =>
                {
                     __encounterManager.End();
                    NavigateTo("AT_NewGame");
                });
                AddButton("Quit Application", () =>
                {
                    Application.Quit();
                });
                break;

            case MenuScreenType.GameOverScreen:
                AddButton("Restart", () =>
                {
                    __encounterManager.Initalize();
                    __encounterManager.Begin();
                    NavigateTo("AT_SpaceCombatEncounterScreen");
                });
                break;

            default:
                Debug.LogWarning("Unsupported menu screen type.");
                break;
        }
    }

    private void AddButton(string text, System.Action onClickAction)
    {
        // Create and configure the button
        Button button = new Button
        {
            text = text
        };

        // Attach the event handler
        button.clicked += onClickAction;

        // Add the button to the container
        buttonsContainer.Add(button);
    }

    private void NavigateTo(string screenName)
    {
        GameObject navigatorObject = GameObject.Find("AwayTeam");
        NavigationManager navigationManager = navigatorObject.GetComponent<NavigationManager>();
        navigationManager.NavigateTo(screenName, true);
    }

    /// <summary>
    /// Sets the screen state text.
    /// </summary>
    /// <param name="type">The menu screen type.</param>
    public void SetScreenState(MenuScreenType type)
    {
        subjectLabel.text = type switch
        {
            MenuScreenType.MainMenu => "Main Menu",
            MenuScreenType.PausedScreen => "Game Paused",
            MenuScreenType.GameOverScreen => "Game Over Screen",
            _ => "Unknown Menu Screen"
        };
    }
}
