using UnityEngine;
using UnityEngine.UIElements;

public class GameScreenController : MonoBehaviour,IShowHide
{
    public enum MenuScreenType
    {
        MainMenu,
        PausedScreen,
        GameOverScreen,
        YouWinScreen,
    }

    [Header("Menu Screen Settings")]
    public MenuScreenType menuScreenType;

    public IATGameMode __encounterManager;
    private UIDocument uiDocument;
    private VisualElement root;
    private VisualElement buttonsContainer;
    private Label subjectLabel;
    private int count=0;
    void Awake()
    {
        // Get the UIDocument and root VisualElement
        uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;

        // Find the container for buttons
        buttonsContainer = root.Q<VisualElement>("buttons-container");
        subjectLabel = root.Q<Label>("subject");

        // Clear existing buttons

    }

    void OnEnable()
    {
        // Dynamically create buttons based on the screen type
        //CreateButtonsForScreen(menuScreenType);
        // Set initial text for the subject label
        //SetScreenState(menuScreenType);
    }
    public void Show()
    {
        uiDocument.rootVisualElement.style.display = DisplayStyle.Flex;
        CreateButtonsForScreen(menuScreenType);
        SetScreenState(menuScreenType);
 
    }
    public void Hide()
    {
        uiDocument.rootVisualElement.style.display = DisplayStyle.None;
    }

    private void CreateButtonsForScreen(MenuScreenType type)
    {
        __encounterManager = SpaceCombatScreen.Instance().GetEncounterManager();

        count++;
        buttonsContainer.Clear();
        switch (type)
        {
            case MenuScreenType.MainMenu:
                AddButton("Play", () =>
                {
                    __encounterManager = SpaceCombatScreen.Instance().GetEncounterManager();
                    __encounterManager.Initalize();
                    __encounterManager.Begin();
                    NavigateTo("AT_SpaceCombatEncounterScreen");
                });
                break;
            case MenuScreenType.YouWinScreen:
                int level = __encounterManager.GetLevel();
                level = level + 1;
                AddButton($"{count.ToString()}: Start Level Round {level}", () =>
                {
                    __encounterManager = SpaceCombatScreen.Instance().GetEncounterManager();
                    __encounterManager.Initalize();
                    __encounterManager.SetLevel(level);
                    __encounterManager.Begin();
                    NavigateTo("AT_SpaceCombatEncounterScreen");
                });
                AddButton("Quit to Menu", () =>
                {
                    __encounterManager = SpaceCombatScreen.Instance().GetEncounterManager();
                     __encounterManager.End();
                    NavigateTo("AT_NewGame");
                });
                break;
            case MenuScreenType.PausedScreen:
                AddButton("Resume", () =>
                {
                    __encounterManager = SpaceCombatScreen.Instance().GetEncounterManager();
                    NavigateTo("AT_SpaceCombatEncounterScreen");
                    __encounterManager.Run();
                });
                AddButton("Quit to Menu", () =>
                {
                    __encounterManager = SpaceCombatScreen.Instance().GetEncounterManager();
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
                    __encounterManager.SetLevel(1);                    
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
            MenuScreenType.YouWinScreen => "Round Won",
            _ => "Unknown Menu Screen"
        };
    }
}
